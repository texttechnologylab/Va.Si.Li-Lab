using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using VaSiLi.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Threading;
using Ubiq.Messaging;
using Ubiq.Voip.Implementations.Dotnet;
using SIPSorceryMedia.Abstractions;

namespace VaSiLi.Misc
{
    [RequireComponent(typeof(AudioSource))]
    public class ChatGpt : MonoBehaviour
    {
        // Start is called before the first frame update
        bool recording;
        AudioSource audioSource;
        IDotnetVoipSource input;
        private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
        public Toggle playbackSelf;
        public TMP_Text selfText;
        public GameObject loadingIndicator;
        public Toggle playbackResult;
        public TMP_Text responseText;
        public string base_url = "http://gpu.audio.vasililab.texttechnologylab.org";
        private static string chatBot = "robert";
        private List<float> audioData = new List<float>();
        private NetworkContext context;
        private CancellationTokenSource currentTaskToken;

        [Serializable]
        private struct AudioData
        {
            public string base64;

            public AudioData(string base64)
            {
                this.base64 = base64;
            }
        }

        [Serializable]
        private struct AudioMessage
        {
            public AudioData audioData;

            public AudioMessage(string base64)
            {
                this.audioData = new AudioData(base64);
            }
        }

        private struct AudioReturn
        {
            public string success;
            public AudioData audio;
            public string text_in;
            public string text_out;
            public string lang;
            public string base64Image;
        }

        void Start()
        {
            context = NetworkScene.Register(this);
            audioSource = GetComponent<AudioSource>();
            StartCoroutine(PlayAudioClips());
            GetInput();

        }

        private void GetInput()
        {
            var go = GameObject.Find("Microphone Dotnet Voip Source");
            if (go != null)
            {
                Debug.Log("Input resolved");
                input = go.GetComponent<IDotnetVoipSource>();
                if (input is MicrophoneDotnetVoipSource)
                {
                    Debug.Log("Valid Input");
                    ((MicrophoneDotnetVoipSource)input).OnAudioPcmSample += OnAudioEncoded;
                }
            }
        }

        public void RecordAudio()
        {
            if (loadingIndicator.activeSelf)
                return;
            recording = true;
            if (input == null)
                GetInput();
            Debug.Log("Recording Audio");

        }

        public void SetChatbot(string newBot)
        {
            Debug.Log(newBot);
            chatBot = newBot;
        }

        void OnAudioEncoded(AudioSamplingRatesEnum durationRtpUnits, float[] bytes)
        {
            if (recording)
                audioData.AddRange(bytes);
        }


        public void EndAudio()
        {
            if (input == null)
                return;
            recording = false;
            selfText?.SetText("");
            responseText?.SetText("");

            currentTaskToken = new CancellationTokenSource();
            _ = SendAudio(audioData.ToArray(), currentTaskToken.Token);
            //Play recording
            if (playbackSelf != null && playbackSelf.isOn)
            {
                var clip = AudioClip.Create("self", audioData.Count, 1, 16000, false);
                clip.SetData(audioData.ToArray(), 0);
                audioQueue.Enqueue(clip);
            }
            audioData.Clear();

        }

        private async Task SendAudio(float[] pcm, CancellationToken token)
        {
            Debug.Log("Sending Audio");
            var base64 = Convert.ToBase64String(FloatPcmToWav(pcm));
            var floaties = ByteToFloatPcm(FloatPcmToWav(pcm));
            AudioMessage message = new AudioMessage(base64);
            loadingIndicator.SetActive(true);
            Debug.Log($"{base_url}/{chatBot}");
            var req = await JsonRequest.PostRequest<AudioMessage>($"{base_url}/{chatBot}", message, token);
            if (req.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                Debug.Log(req.StatusCode);
                loadingIndicator.SetActive(false);
                return;
            }
            var content = await req.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<AudioReturn>(content);
            if (playbackResult.isOn && !token.IsCancellationRequested)
            {
                ProcessData(data);
                context.SendJson(data);
            }
            loadingIndicator.SetActive(false);
            return;
        }
        private bool once = false;
        private void ProcessData(AudioReturn data)
        {
            selfText?.SetText(data.text_in);
            if (once == false) {
                ChatLogPanel.setText("Chatbot", data.text_out);
                once = true;
            }
            responseText?.SetText(data.text_out);
            Debug.Log("Playing Audio");

            byte[] response = Convert.FromBase64String(data.audio.base64);
            float[] floatPcms = ByteToFloatPcm(response);
            var clip = AudioClip.Create("response", floatPcms.Length, 1, 16000, false);
            clip.SetData(floatPcms, 0);
            audioQueue.Enqueue(clip);
        }

        public void StopRequest()
        {
            if (currentTaskToken != null)
            {
                currentTaskToken.Cancel();
            }
            while (audioQueue.Count > 0)
            {
                audioQueue.Dequeue();
            }
            audioSource.Stop();
            loadingIndicator.SetActive(false);
        }

        private IEnumerator PlayAudioClips()
        {
            while (true)
            {
                if (!audioSource.isPlaying && audioQueue.Count > 0)
                {
                    audioSource.clip = audioQueue.Dequeue();
                    audioSource.Play();
                }
                yield return new WaitForSeconds(1);
            }
        }

        public float[] ByteToFloatPcm(byte[] bytes)
        {
            float[] res = new float[bytes.Length / 2]; // will drop last byte if odd number
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (float)BitConverter.ToInt16(bytes, i * 2) / short.MaxValue;
            }
            return res;
        }

        private Byte[] FloatPcmToWav(float[] samples)
        {
            Int16[] intData = new Int16[samples.Length];

            Byte[] bytesData = new Byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = short.MaxValue; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            return bytesData;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<AudioReturn>();
            ProcessData(msg);
        }
    }
}
