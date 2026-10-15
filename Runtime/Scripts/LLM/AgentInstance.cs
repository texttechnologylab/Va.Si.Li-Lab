using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using VaSiLi.Networking;
using Application = UnityEngine.Application;

namespace VaSiLi.Llm
{
    [RequireComponent(typeof(AudioSource))]
    public class AgentInstance : MonoBehaviour
    {
        /** Supposed to be true once a single agent has text to speech going on. */
        public static readonly SemaphoreSlim SpeechLock = new(1, 1);

        /** identifier for referencing the same agent in the backend */
        public string id;

        /** allowed list of tools as a subset of available tools from the ToolProvider */
        public Dictionary<string, ToolBinding> allowedTools = new();

        /** indicates that the agent is active and is being interacted with */
        public bool active;

        /** stores complex objects returned by tool calls for use as parameters for future tool calls  */
        private Dictionary<string, object> objectStore = new();

        private bool _startedSpeaking, _isPaused;
        private List<AudioObject> _audioObjects = new();
        private AudioSource _audioSource;
        private Coroutine _currentSpeaking;
        private string _tempPath;

        private static string _chatUrl => VasiliEndpoints.Current.agent + "/";

        private ChatAPI _chatAPI;
        
        private ToolLogUI _toolLogUI;

        public void SetId(string id)
        {
            this.id = id;
        }

        public string GetId()
        {
            return id;
        }

        private async void Start()
        {
            _chatAPI = GetComponent<ChatAPI>();

            _tempPath = Application.persistentDataPath + "tmpWAVBase64.wav";
            _audioSource = GetComponent<AudioSource>();

            _toolLogUI = GetComponentInChildren<ToolLogUI>();

            // Test Code
            /*AudioClip clip = await _chatAPI.TextToSpeech("Mein Name ist Günther und ich bin eine Beispiel-Stimme.");
            await SpeakBase64(clip);
            AudioClip clip2 = await _chatAPI.TextToSpeech("Hier ist irgendein anderer Text. Halli hallo!");
            await SpeakBase64(clip2);*/
        }

        public void SetAllowedTools(Dictionary<string, ToolBinding> tools)
        {
            this.allowedTools = tools;
        }

        public async Task<string> AddTool(string toolName, ToolBinding tool)
        {
            try
            {
                allowedTools.Add(toolName, tool);

                var req = await JsonRequest.PostRequest<ToolAddRemoveRequest>(
                    _chatUrl + "addTool",
                    new ToolAddRemoveRequest { id = id, tool_name = toolName }
                );

                string content = await req.Content.ReadAsStringAsync();
                Debug.Log($"Status: {req.StatusCode}");
                Debug.Log($"Response: {content}");

                return content;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Request failed: {ex}");
                throw;
            }
        }

        public async Task<string> RemoveTool(string toolName)
        {
            allowedTools.Remove(toolName);

            HttpResponseMessage req = await JsonRequest.PostRequest<ToolAddRemoveRequest>(_chatUrl + "removeTool",
                new ToolAddRemoveRequest { id = id, tool_name = toolName });
            string content = await req.Content.ReadAsStringAsync();

            return content;
        }

        /**
         * Encapsulates the entire STT -> LLM -> TTS pipeline. Call this after microphone audio has been recorded.
         * <param name="clip">AudioClip to be used as prompt input.</param>
         */
        public async void ProcessUserPrompt(AudioClip clip)
        {
            string transcription = await _chatAPI.Transcribe(clip);

            AgentReturn agentResponse = await _chatAPI.Converse(transcription, id);

            int depth = 0; // limit consecutive tool calls

            while (agentResponse.is_tool && depth < 10)
            {
                string result = HandleIncomingAgentMessage(agentResponse.reply);
                Debug.Log(result);
                ToolCallResult res = JsonConvert.DeserializeObject<ToolCallResult>(result);
                string further = res.Result?.ToString();
                agentResponse = await _chatAPI.Converse(further, id);
                depth++;
            }

            AudioClip ttsClip = await _chatAPI.TextToSpeech(agentResponse.reply);

            await SpeechLock.WaitAsync();
            try
            {
                await SpeakBase64(ttsClip);
            }
            finally
            {
                // Debug.Log("Chatbot mit ID " + id + " hat fertig geredet.");
                SpeechLock.Release();
            }
        }

        public ToolCallResult HandleToolCall(string callId, string toolName, JSONNode toolArguments)
        {
            HashSet<Type> simpleTypes = new HashSet<Type>()
                { typeof(string), typeof(int), typeof(float), typeof(double), typeof(bool), typeof(Vector3) };

            ToolCallResult result = new ToolCallResult(callId, false, null, string.Empty);

            // Get called method
            if (!allowedTools.TryGetValue(toolName, out var toolMethod))
            {
                result.error = "Agent Tool not found: " + toolName;
                return result;
            }

            MethodInfo method = toolMethod.Method;
            ParameterInfo[] toolParameters = method.GetParameters();

            if (toolParameters.Length > 0 && toolArguments == null)
            {
                result.error = $"Tool '{toolName}' expects {toolParameters.Length} arguments but got null";
                return result;
            }

            // Parse method parameters
            object[] parsedToolArguments = new object[toolParameters.Length];

            for (int i = 0; i < toolParameters.Length; ++i)
            {
                ParameterInfo p = toolParameters[i];
                JSONNode jsonValue = toolArguments?[p.Name];

                // Apply default values or report missing parameters
                if (jsonValue == null)
                {
                    if (p.HasDefaultValue)
                    {
                        parsedToolArguments[i] = p.DefaultValue;
                        continue;
                    }

                    result.error = $"Missing argument '{p.Name}' for tool '{toolName}'";
                    return result;
                }

                // Check types and parse value
                if (simpleTypes.Contains(p.ParameterType))
                {
                    parsedToolArguments[i] = ParseJsonArgument(jsonValue, p.ParameterType);
                }
                else if (objectStore.TryGetValue(jsonValue.Value, out var complexObject))
                {
                    parsedToolArguments[i] = complexObject;
                }
                else
                {
                    result.error = $"Missing value for parameter '{p.Name}' for tool '{toolName}'";
                    return result;
                }
            }

            // Execute Method
            try
            {
                object rawReturn = method.Invoke(toolMethod.Source, parsedToolArguments);
                objectStore[toolName] = rawReturn;
                if (rawReturn == null)
                {
                    result.Result = "";
                }
                else if (!simpleTypes.Contains(rawReturn.GetType()))
                {
                    result.Result = toolName;
                }
                else
                {
                    result.Result = rawReturn;
                }

                _toolLogUI.AddMessage(toolName + "(" + ToReadableString(toolArguments) + ")");
                
                result.ok = true;
                return result;
            }
            catch (TargetInvocationException e)
            {
                result.error = e.InnerException?.Message ?? e.Message;
                return result;
            }
            catch (Exception e)
            {
                result.error = e.Message;
                return result;
            }
        }

        /**
         * Speaks a given audio using the required AudioSource component on the game object this script is attached to.
         * <param name="clip">The AudioClip to be played.</param>
         */
        public async Task SpeakBase64(AudioClip clip)
        {
            if (_audioSource is null)
            {
                Debug.LogError("AudioSource is null");
                return;
            }

            var listener = FindFirstObjectByType<AudioListener>();
            // Debug.Log($"Listener: {(listener ? listener.name : "NONE")}");

            AudioListener.pause = false;
            AudioListener.volume = 1f;

            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.volume = 1f;
            _audioSource.pitch = 1f;
            _audioSource.loop = false;

            _audioSource.spatialBlend = 1f;
            _audioSource.dopplerLevel = 0f;
            _audioSource.minDistance = 1f;
            _audioSource.maxDistance = 500f;

            _audioSource.mute = false;
            _audioSource.bypassEffects = true;
            _audioSource.bypassListenerEffects = true;
            _audioSource.bypassReverbZones = true;

            _audioSource.Play();

            // Debug.Log($"Play() called. isPlaying={_audioSource.isPlaying}, time={_audioSource.time}, " + $"srcVol={_audioSource.volume}, listenerVol={AudioListener.volume}, paused={AudioListener.pause}");

            while (_audioSource.isPlaying)
                await Task.Yield();

            File.Delete(_chatAPI.deleteAudioFiles[0]);
            _chatAPI.deleteAudioFiles.RemoveAt(0);
        }

        /**
         * Clears the TTS system and aborts any speaking process.
         */
        private void StopSpeak()
        {
            if (_currentSpeaking != null)
            {
                StopCoroutine(_currentSpeaking);
            }

            _audioObjects.Clear();
            _audioSource.Stop();

            if (File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        /**
         * Coroutine for speaking a given text. This is called inside the SpeakBase64 method.
         */
        private IEnumerator SpeakBase64Enumerator()
        {
            int i = 0;

            while (_audioObjects.Count > i)
            {
                AudioClip clipToPlay = null;

                if (_audioObjects[i].AudioClip is null)
                {
                    if (_audioObjects[i].B64Audio == null)
                    {
                        i++;

                        if (!(_audioObjects.Count > i))
                            _startedSpeaking = true;
                        continue;
                    }

                    byte[] audioBytes = Convert.FromBase64String(_audioObjects[i].B64Audio);

                    File.WriteAllBytes(_tempPath, audioBytes);
                    UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(_tempPath, AudioType.WAV);
                    yield return request.SendWebRequest();

                    if (request.result.Equals(UnityWebRequest.Result.ConnectionError))
                        Debug.LogError(request.error);
                    else
                        clipToPlay = DownloadHandlerAudioClip.GetContent(request);
                }
                else
                    clipToPlay = _audioObjects[i].AudioClip;

                if (clipToPlay is not null)
                {
                    _audioSource.clip = clipToPlay;
                    _audioSource.Play();
                }

                _startedSpeaking = true;

                while (_audioSource.isPlaying || _isPaused)
                    yield return null;

                i++;

                if (File.Exists(_tempPath))
                    File.Delete(_tempPath);
            }
        }

        /**
         * <summary>
         * Handles tool calls from an agent.
         * </summary>
         * <param name="json">
         * <para>The JSON string the agent sends to request a tool call. This must be of the following format:</para>
         * {<br/>
         *  "type": "tool_call",<br/>
         *  "id": **Whatever the LLM considers sensible**,<br/>
         *  "name": **The name of the tool to be called**,<br/>
         *  "arguments": {"arg1": value1, "arg2": value2, ...}<br/>
         * }<br/>
         * </param>
         */
        public string HandleIncomingAgentMessage(string json)
        {
            JSONNode msg;
            try
            {
                msg = JSON.Parse(json);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new ToolCallResult(
                    id: "",
                    ok: false,
                    result: null,
                    error: "Invalid JSON: " + e.Message
                ));
            }

            var type = msg?["type"]?.Value;
            if (type != "tool_call")
            {
                return JsonConvert.SerializeObject(new ToolCallResult(
                    id: msg?["id"]?.Value ?? "",
                    ok: false,
                    result: null,
                    error: $"Unsupported message type '{type}'"
                ));
            }

            string callId = msg["id"]?.Value;
            string toolName = msg["name"]?.Value;

            if (string.IsNullOrEmpty(callId))
            {
                return JsonConvert.SerializeObject(new ToolCallResult(
                    id: "",
                    ok: false,
                    result: null,
                    error: "Missing 'id' in tool_call"
                ));
            }

            if (string.IsNullOrEmpty(toolName))
            {
                return JsonConvert.SerializeObject(new ToolCallResult(
                    id: callId,
                    ok: false,
                    result: null,
                    error: "Missing 'name' in tool_call"
                ));
            }

            JSONNode argsNode = msg["arguments"];
            if (argsNode is null || !argsNode.IsObject)
                argsNode = new JSONObject();

            ToolCallResult result = HandleToolCall(callId, toolName, argsNode);

            return JsonConvert.SerializeObject(result);
        }

        object ParseJsonArgument(JSONNode argumentValue, Type targetType)
        {
            if (targetType == typeof(string))
                return argumentValue.Value;

            if (targetType == typeof(int))
                return argumentValue.AsInt;

            if (targetType == typeof(float))
                return argumentValue.AsFloat;

            if (targetType == typeof(double))
                return argumentValue.AsDouble;

            if (targetType == typeof(bool))
                return argumentValue.AsBool;

            if (targetType == typeof(Vector3))
                return StringToVector3(argumentValue.Value);

            return argumentValue.Value;
        }

        static Vector3 StringToVector3(string s)
        {
            var p = s.Trim('(', ')').Split(',');
            return new Vector3(
                float.Parse(p[0]),
                float.Parse(p[1]),
                float.Parse(p[2])
            );
        }
        
        string ToReadableString(JSONNode node)
        {
            if (!node.IsObject)
                return node.ToString();

            StringBuilder sb = new StringBuilder();

            foreach (var kvp in node.AsObject)
            {
                sb.Append($"{kvp.Key}: {kvp.Value}, ");
            }

            if (sb.Length >= 2)
                sb.Length -= 2;

            return sb.ToString();
        }

        public struct AudioObject
        {
            public string B64Audio;
            public AudioClip AudioClip;
        }
    }

    [Serializable]
    public struct ToolAddRemoveRequest
    {
        public string id;
        public string tool_name;
    }
}