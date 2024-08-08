using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip.Implementations.Dotnet;
using SIPSorceryMedia.Abstractions;
using System;
using VaSiLi.Networking;
using TMPro;

namespace VaSiLi.Misc
{
    public class MicrophoneListener : MonoBehaviour
    {
        private IDotnetVoipSource input;
        private List<float> audioData = new List<float>();
        public float minVolume = 0.11f;
        public float maxVolume = 0.02f;
        private float[] volumeFrames;
        public int stoppedSpeakingDelay = 75;
        private bool isSpeaking = false;
        private int stoppedSpeaking = 0;
        public string baseUrl = "http://gpu.audio.vasililab.texttechnologylab.org";

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
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (input == null)
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
        }

        void OnAudioEncoded(AudioSamplingRatesEnum durationRtpUnits, float[] bytes)
        {
            bool reachesMinVolume = false;
            foreach (float sample in bytes)
            {
                if (sample > minVolume)
                {
                    reachesMinVolume = true;
                    break;
                }
            }

            if (isSpeaking)
            {
                audioData.AddRange(bytes);
            }

            // If we reached the min volume
            if (reachesMinVolume)
            {
                // add the data 
                
                stoppedSpeaking = 0;
                // If we haven't started speaking
                if (!isSpeaking)
                {
                    isSpeaking = true;
                }
            }
            // If we are speaking and the sample is quiete
            else if (isSpeaking && !reachesMinVolume)
            {
                stoppedSpeaking++;
                if (stoppedSpeaking > stoppedSpeakingDelay)
                {
                    isSpeaking = false; 
                    SendSpeechData(audioData.ToArray());
                    audioData.Clear();
                }
            }

        }

        async void SendSpeechData(float[] pcmData)
        {
            var base64 = Convert.ToBase64String(FloatPcmToWav(pcmData));
            AudioMessage message = new AudioMessage(base64);
            // sendAudio
            var req = await JsonRequest.PostRequest<AudioMessage>($"{baseUrl}/whisper", message);
            var content = await req.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<AudioReturn>(content);
            if (data.text_in != null && !data.text_in.Equals(""))
            {
                ChatLogPanel.setText(SceneManagement.RoleManager.CurrentRole?.name ?? "Player", data.text_in);
            }
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

        public float[] ByteToFloatPcm(byte[] bytes)
        {
            float[] res = new float[bytes.Length / 2]; // will drop last byte if odd number
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (float)BitConverter.ToInt16(bytes, i * 2) / short.MaxValue;
            }
            return res;
        }
    }
}
