// Code largely inspired by VRParlExplorer.
// https://github.com/texttechnologylab/VR-Parliamentary-Debates-Explorer/tree/main
// https://dl.acm.org/doi/10.1145/3720553.3746672

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VaSiLi.Networking;

namespace VaSiLi.Llm
{
    /**
     * This component allows audio communication with an LLM in any scene. Place an empty game object and attach this
     * script to it.
     */
    public class ChatAPI : MonoBehaviour, IAgentContextInfo
    {
        /** Server address for STT and TTS. */
        private static string whisperxUrl => VasiliEndpoints.Current.speechSynthesis;

        /** Server address for LLM chatting. */
        private static string agentUrl => VasiliEndpoints.Current.agent;

        public List<string> deleteAudioFiles = new();

        private async void Awake()
        {
            UnityWebRequest loadWhisper = new UnityWebRequest(whisperxUrl + "/load", "POST");
            await loadWhisper.SendWebRequest();

            if (loadWhisper.result != UnityWebRequest.Result.Success)
                Debug.LogError("[ChatAPI] WhisperX setup failed");
            else
                Debug.Log("[ChatAPI] WhisperX setup succeeded");
        }

        public async Task<string> Transcribe(AudioClip clip)
        {
            byte[] data = SavWav.GetWav(clip, out uint length);

            var base64 = Convert.ToBase64String(data);

            AudioMessage msg = new AudioMessage(base64);

            HttpResponseMessage req = await JsonRequest.PostRequest<AudioMessage>(whisperxUrl + "/whisperx", msg);
            string content = await req.Content.ReadAsStringAsync();

            AudioReturn audioData = JsonUtility.FromJson<AudioReturn>(content);

            string promptTranscription = audioData.transcription;

            Debug.Log("[ChatAPI] Transcription: " + promptTranscription);

            return promptTranscription;
        }

        /**
         * General asynchronous method for conversations with the Llm.
         * <param name="message">The user prompt.</param>
         * <param name="agentId">The id of the respective agent.</param>
         * <returns>The model's response as a string.</returns>
         */
        public async Task<AgentReturn> Converse(string message, string agentId)
        {
            AgentMessage msg = new AgentMessage { id = agentId, message = message };

            string json = "{\"id\":\"" + agentId + "\",\"message\":\"" + message + "\"}";

            using (UnityWebRequest req = new UnityWebRequest(agentUrl + "/chat", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();

                req.SetRequestHeader("Content-Type", "application/json");

                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ChatAPI] Fehler: {req.error} | Code: {req.responseCode}");
                    Debug.LogError("[ChatAPI] Server response: " + req.downloadHandler.text);
                }
                else
                {
                    Debug.Log("[ChatAPI] Server response: " + req.downloadHandler.text);
                }

                AgentReturn agentData = JsonUtility.FromJson<AgentReturn>(req.downloadHandler.text);
                return agentData;
            }
        }

        /**
         * Requests an audio file from Bark, containing a given text as spoken words.
         * <param name="text">The text to be spoken.</param>
         * <returns>An AudioClip of the text.</returns>
         */
        public async Task<AudioClip> TextToSpeech(string text)
        {
            HttpResponseMessage req =
                await JsonRequest.PostRequest(whisperxUrl + "/tts", new BarkMessage { prompt = text });

            Debug.Log($"[ChatAPI] Bark response: {(int)req.StatusCode} {req.ReasonPhrase}");
            string content = await req.Content.ReadAsStringAsync();

            Debug.Log("[ChatAPI] Bark content: " + content);

            BarkReturn barkData = JsonUtility.FromJson<BarkReturn>(content);

            if (!barkData.status.Equals("success"))
            {
                Debug.LogError("[ChatAPI] Bark Error");
                return null;
            }

            byte[] audioBytes = Convert.FromBase64String(barkData.base64Audio);

            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".wav");
            deleteAudioFiles.Add(path);
            await File.WriteAllBytesAsync(path, audioBytes);

            using UnityWebRequest www =
                UnityWebRequestMultimedia.GetAudioClip(
                    "file://" + path,
                    AudioType.WAV
                );

            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ChatAPI] Audio load failed: {www.error} (code {www.responseCode}) url={path}");
                return null;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            Debug.Log(
                $"[ChatAPI] Loaded clip: length={clip.length}, samples={clip.samples}, channels={clip.channels}, " +
                $"freq={clip.frequency}");

            float[] data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);

            float peak = 0f;
            double sumSq = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > peak) peak = a;
                sumSq += data[i] * data[i];
            }

            float rms = Mathf.Sqrt((float)(sumSq / data.Length));

            // Debug.Log($"Audio stats: peak={peak:F6}, rms={rms:F6}");

            return clip;
        }

        private async Task WaitUntilFileIsReady(string path, int attempts = 80, int delayMs = 25)
        {
            long lastSize = -1;

            for (int i = 0; i < attempts; i++)
            {
                if (File.Exists(path))
                {
                    long size = new FileInfo(path).Length;
                    if (size >= 44 && size == lastSize)
                        return;

                    lastSize = size;
                }

                await Task.Delay(delayMs);
            }

            Debug.LogWarning($"File may not be ready: {path}");
        }

        public string GetContextInfo()
        {
            return "{\\\"description\\\":\\\"Hallo, ich bin eine API.\\\"}";
        }

        [Serializable]
        private struct BarkMessage
        {
            public string prompt;
        }

        [Serializable]
        private struct BarkReturn
        {
            public string status;
            public string error;
            public string base64Audio;
        }

        [Serializable]
        private struct AudioMessage
        {
            public string audioBase64;

            public AudioMessage(string audioBase64)
            {
                this.audioBase64 = audioBase64;
            }
        }

        private struct AudioReturn
        {
            public string error;
            public string status;
            public string transcription;
        }
    }

    [Serializable]
    public struct AgentMessage
    {
        public string id;
        public string message;
    }

    [Serializable]
    public struct AgentReturn
    {
        public string reply;
        public bool is_tool;
        public string agent_status;
    }
}