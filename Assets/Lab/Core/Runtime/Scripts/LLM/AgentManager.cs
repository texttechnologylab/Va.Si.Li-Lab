using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using VaSiLi.Networking;
using UnityEngine.SceneManagement;
using Vector3 = UnityEngine.Vector3;

namespace Lab.Core.Runtime.Scripts.LLM
{
    public class AgentManager : MonoBehaviour
    {
        private string GenerateStandardPrompt()
        {
            return
                $"You have exactly two possible response modes: 1. Normal assistant reply: - Return plain text only. 2. Tool call: - Return ONLY a single JSON object. - No markdown. - No code fences. - No explanation before or after. - The JSON must follow this exact schema: {{\"type\":\"tool_call\",\"id\":\"<unique string you invent>\",\"name\":\"<tool name you choose out of the available ones>\",\"arguments\":{{ ... }}}} Rules for tool calls: - \"type\" is mandatory and must ALWAYS be exactly \"tool_call\". - Never change, translate, or invent another value for \"type\". - \"id\" is mandatory and must be a new unique string chosen by you. - \"name\" is mandatory and must be the exact name of the tool you want to call. - \"arguments\" is mandatory and must be a JSON object containing the tool arguments. - If no tool is needed, respond with normal plain text instead of JSON.";
        }

        [SerializeField] private AgentInstance agentPrefab;

        private Dictionary<string, AgentInstance> _agentInstances = new();
        private AgentToolProvider _toolProvider;

        private string _chatUrl = "http://vragent.lehre.texttechnologylab.org/";

        private async void Start()
        {
            _toolProvider = GetComponent<AgentToolProvider>();

            LoadAgentBots();
        }

        private async void LoadAgentBots()
        {
            switch (SceneManager.GetActiveScene().name)
            {
                case "MetaStart":
                    if (!_agentInstances.ContainsKey("agent_jauch"))
                        await CreateAgent("agent_jauch",
                            "You are the German TV host Günther Jauch. " + GenerateStandardPrompt(),
                            new Vector3(3, 0, 0));
                    if (!_agentInstances.ContainsKey("agent_barbara"))
                        await CreateAgent("agent_barbara",
                            "You are the German TV host Barbara Schöneberger. " + GenerateStandardPrompt(),
                            new Vector3(-3, 0, 0));
                    break;
                case "EventSystemShowcase_2025":
                    if (!_agentInstances.ContainsKey("agent_helper_bot"))
                        await CreateAgent("agent_helper_bot",
                            "You are a helpful VR assistant. " + GenerateStandardPrompt(), new Vector3(-2, 0, 0));
                    break;
            }
        }

        public async Task<AgentInstance> CreateAgent(string id, string systemPrompt, Vector3 position)
        {
            if (_agentInstances.ContainsKey(id))
                return _agentInstances[id];

            AgentInstance newAgent = Instantiate(agentPrefab, position, Quaternion.identity);
            newAgent.SetId(id);

            HttpResponseMessage req = await JsonRequest.PostRequest<AgentRequest>(_chatUrl + "createAgent",
                new AgentRequest { id = id, systemPrompt = systemPrompt });
            string content = await req.Content.ReadAsStringAsync();

            Debug.Log(content); // TODO: just returns null?

            newAgent.SetAllowedTools(new Dictionary<string, ToolBinding>(_toolProvider.GetAvailableTools()));
            _agentInstances.Add(id, newAgent);
            ToolWhitelistUI[] tools = newAgent.GetComponentsInChildren<ToolWhitelistUI>();
            foreach (ToolWhitelistUI tool in tools)
            {
                tool.enabled = true;
                tool.GenerateActiveButtons();
                tool.GenerateSelectorButtons();
            }

            return newAgent;
        }

        public void RemoveAgent(string agentId)
        {
            _agentInstances.Remove(agentId);
        }

        public List<AgentInstance> GetActiveAgents()
        {
            List<AgentInstance> activeAgents = new();

            foreach (string key in _agentInstances.Keys)
            {
                AgentInstance agent = _agentInstances[key];
                if (agent.active)
                    activeAgents.Add(agent);
            }

            return activeAgents;
        }
    }

    [Serializable]
    public struct AgentRequest
    {
        public string id;
        public string systemPrompt;
    }
}