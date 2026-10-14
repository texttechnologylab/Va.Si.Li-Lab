using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using VaSiLi.Networking;

namespace VaSiLi.Llm
{
    public class AgentToolProvider : MonoBehaviour
    {
        private Dictionary<string, ToolBinding> _availableAgentTools = null;

        private static string _chatUrl => VasiliEndpoints.Current.agent + "/";

        void Start()
        {
            RegisterAgentTools();
            SendToolDescriptions();
        }

        async void SendToolDescriptions()
        {
            string toolDescriptions = GetToolDescriptions(_availableAgentTools);
            Debug.Log("[ToolProvider] " + toolDescriptions);

            HttpResponseMessage req =
                await JsonRequest.PostRequest<string>(_chatUrl + "toolDescriptions", toolDescriptions);
            string content = await req.Content.ReadAsStringAsync();
            Debug.Log("[ToolProvider] " + content);
        }

        void RegisterAgentTools()
        {
            _availableAgentTools = new();

            int total = 0;
            int registered = 0;

            // Instance methods with source object
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);

            foreach (var behaviour in allMonoBehaviours)
            {
                if (behaviour == null)
                    continue;

                Type type = behaviour.GetType();

                MethodInfo[] methods =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    total++;
                    AgentToolAttribute toolAttr = method.GetCustomAttribute<AgentToolAttribute>();
                    if (toolAttr == null) continue;

                    string toolName = toolAttr.Name;

                    _availableAgentTools[toolName] = new ToolBinding(behaviour, method, toolAttr);
                    registered++;

                    // Debug.Log("Registered Instance Tool: " + toolName);
                }
            }

            // Static class functions without source object
            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                MethodInfo[] staticMethods =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in staticMethods)
                {
                    total++;
                    AgentToolAttribute toolAttr = method.GetCustomAttribute<AgentToolAttribute>();
                    if (toolAttr == null) continue;

                    string toolName = toolAttr.Name;

                    _availableAgentTools[toolName] = new ToolBinding(null, method, toolAttr);
                    registered++;

                    // Debug.Log("Registered Static Tool: " + toolName);
                }
            }

            // Debug.Log("Total methods: " + total);
            // Debug.Log("Registered methods: " + registered);
        }

        public string GetToolDescriptions(Dictionary<string, ToolBinding> agentTools)
        {
            List<object> toolDescriptions = new List<object>();

            foreach (var (toolName, binding) in agentTools)
            {
                AgentToolAttribute toolAttr = binding.Attribute;
                MethodInfo method = binding.Method;

                Dictionary<string, string> parameters = new();
                foreach (ParameterInfo param in method.GetParameters())
                {
                    parameters[param.Name] = param.ParameterType.Name;
                }

                toolDescriptions.Add(
                    new
                    {
                        name = toolAttr.Name,
                        description = toolAttr.Description,
                        parameters = parameters
                    }
                );
            }

            return JsonConvert.SerializeObject(toolDescriptions);
        }

        public Dictionary<string, ToolBinding> GetAvailableTools()
        {
            if (_availableAgentTools == null)
            {
                RegisterAgentTools();
            }

            return _availableAgentTools;
        }
    }
}