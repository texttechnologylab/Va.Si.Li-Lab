using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace VaSiLi.Networking
{
    public class JsonRequest
    {

        public static async Task<HttpResponseMessage> PostRequest<T>(string uri, T message)
        {
            using (var client = new HttpClient())
            {
                //return await client.PostAsync(uri, new StringContent(JsonUtility.ToJson(message), Encoding.UTF8, "application/json"));
                return await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"));

            }
        }

        public static async Task<HttpResponseMessage> PostRequest<T>(string uri, T message, CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                try {
                    //return await client.PostAsync(uri, new StringContent(JsonUtility.ToJson(message), Encoding.UTF8, "application/json"), token);
                    return await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"), token);

                }
                catch (TaskCanceledException)
                {
                    return new HttpResponseMessage(HttpStatusCode.ResetContent);
                }
            }
        }

        public static async Task<HttpResponseMessage> PutRequest<T>(string uri, T message)
        {
            using (var client = new HttpClient())
            {
                //return await client.PutAsync(uri, new StringContent(JsonUtility.ToJson(message), Encoding.UTF8, "application/json"));
                return await client.PutAsync(uri, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"));

            }
        }

        public static async Task<HttpResponseMessage> GetRequest(string uri)
        {
            using (var client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }

}