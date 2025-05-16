using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Ubiq.Messaging;
#if !UNITY_WEBGL
using Ubiq.Voip.Implementations.Dotnet;
#endif
using SIPSorceryMedia.Abstractions;
using System.Threading.Tasks;

namespace VaSiLi.Modules.Chatbot
{
    [RequireComponent(typeof(AudioSource))]
    public class ChatBotManager : Module
    {
#if !UNITY_WEBGL
        public bool playbackSelf;
        public bool playbackResult;
        //TODO: public GameObject loadingIndicator;
        public UnityAction<Util.AudioReturn?> audioReturned = delegate {};
        public static string baseUrl = "http://localhost:5000";
        public string _baseUrl = "http://gpu.audio.vasililab.texttechnologylab.org";
        private static string chatBot = "longform";
        private bool recording;
        private AudioSource audioSource;
        private IVoipSource input;
        private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
        private List<float> audioData = new List<float>();
        private NetworkContext context;
        private CancellationTokenSource currentTaskToken;


        void Start()
        {
            baseUrl = _baseUrl;
            context = NetworkScene.Register(this);
            audioSource = GetComponent<AudioSource>();
            StartCoroutine(PlayAudioClips());
            TryAttachAudioEncode();
            Debug.Log(baseUrl);

        }

        private void TryAttachAudioEncode()
        {
            input = Util.GetInput();
            if (input is MicrophoneVoipSource source)
            {
                source.OnAudioPcmSample += OnAudioEncoded;
            }
        }

        public void RecordAudio()
        {
            recording = true;
            if (input == null)
                TryAttachAudioEncode();
            Debug.Log("Recording Audio");
        }
        public static void SetChatbot(string newBot)
        {
            Debug.Log(newBot);
            chatBot = newBot;
        }

        public static string GetChatbot()
        {
            return chatBot;
        }
        void OnAudioEncoded(AudioSamplingRatesEnum durationRtpUnits, float[] bytes)
        {
            if (recording)
                audioData.AddRange(bytes);
        }

        public void SetPlaybackResult(bool boolean)
        {
            playbackResult = boolean;
        }

        public void SetPlaybackSelf(bool boolean)
        {
            playbackSelf = boolean;
        }

        public async Task EndAudio(CancellationTokenSource source = null)
        {
            if (input == null)
                return;
            recording = false;

            currentTaskToken = source;
            if (source == null)
                currentTaskToken = new CancellationTokenSource();

            TryPlaybackSelf();

            var data = await Util.SendAudio(audioData.ToArray(), chatBot, baseUrl + "/query", currentTaskToken.Token);
            if (data.HasValue)
            {
                TryPlayBackResult(data.Value);
                // Set text and other things
                ChatLogPanel.setText("Chatbot", data.Value.text_out);
                // Send data to other users
                context.SendJson(data.Value);
            }

            audioData.Clear();
            audioReturned.Invoke(data);
        }

        private void TryPlayBackResult(Util.AudioReturn data)
        {
            if (playbackResult && !currentTaskToken.Token.IsCancellationRequested)
            {
                byte[] response = Convert.FromBase64String(data.audio.base64);
                float[] floatPcms = Util.ByteToFloatPcm(response);
                var clip = AudioClip.Create("response", floatPcms.Length, 1, 16000, false);
                clip.SetData(floatPcms, 0);
                audioQueue.Enqueue(clip);
                Debug.Log("Enqueing Chatbot Audio response");
            }
        }

        private void TryPlaybackSelf()
        {
            if (playbackSelf)
            {
                var clip = AudioClip.Create("self", audioData.Count, 1, 16000, false);
                clip.SetData(audioData.ToArray(), 0);
                audioQueue.Enqueue(clip);
            }
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
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Util.AudioReturn>();
            audioReturned.Invoke(msg);
            ChatLogPanel.setText("Chatbot", msg.text_out);
        }
#endif
        public static ChatBotManager GetInstance()
        {
            return GameObject.Find("Chatbot Manager").GetComponent<ChatBotManager>();
        }

    }
}
