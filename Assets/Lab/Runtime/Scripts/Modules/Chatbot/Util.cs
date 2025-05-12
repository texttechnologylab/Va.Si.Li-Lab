using System;
using System.Threading;
using System.Threading.Tasks;
#if !UNITY_WEBGL
using Ubiq.Voip.Implementations.Dotnet;
#endif
using UnityEngine;
using VaSiLi.Networking;

namespace VaSiLi.Modules.Chatbot
{
    public class Util
    {
        [Serializable]
        public struct AudioData
        {
            public string base64;

            public AudioData(string base64)
            {
                this.base64 = base64;
            }
        }

        [Serializable]
        public struct AudioMessage
        {
            public AudioData audioData;
            public string chatBotName;

            public AudioMessage(string base64, string chatBotName)
            {
                this.audioData = new AudioData(base64);
                this.chatBotName = chatBotName;
            }
        }

        public struct AudioReturn
        {
            public string success;
            public AudioData audio;
            public string text_in;
            public string text_out;
            public string lang;
        }

        public struct ChatBotList
        {
            public string success;
            public string[] result;
        }

        public static float[] ByteToFloatPcm(byte[] bytes)
        {
            float[] res = new float[bytes.Length / 2]; // will drop last byte if odd number
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (float)BitConverter.ToInt16(bytes, i * 2) / short.MaxValue;
            }
            return res;
        }

        public static Byte[] FloatPcmToWav(float[] samples)
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

        public static async Task<AudioReturn?> SendAudio(float[] pcm, string chatBot, string url, CancellationToken token)
        {
            Debug.Log("Sending Audio");
            var base64 = Convert.ToBase64String(FloatPcmToWav(pcm));
            AudioMessage message = new AudioMessage(base64, chatBot);
            var req = await JsonRequest.PostRequest<AudioMessage>(url, message, token);
            if (req.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                Debug.Log(req.StatusCode);
                return null;
            }
            var content = await req.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<AudioReturn>(content);
            return data;
        }
        #if !UNITY_WEBGL
        public static IVoipSource GetInput()
        {
            
            var go = GameObject.Find("Microphone Dotnet Voip Source");
            if (go != null)
            {
                var input = go.GetComponent<MicrophoneVoipSource>();
                return input;
            }
            
            return null;
        }
        #endif
        public static async Task<ChatBotList?> GetChatBots(string url)
        {
            return await JsonRequest.GetRequest<ChatBotList>(url);
        }
    }


}