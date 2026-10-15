using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using VaSiLi.Networking;
using Vector3 = UnityEngine.Vector3;

namespace VaSiLi.Llm
{
    public class AgentManager : MonoBehaviour
    {
        private string GenerateStandardPrompt()
        {
            return
                $"You have exactly two possible response modes: 1. Normal assistant reply: - Return plain text only. 2. Tool call: - Return ONLY a single JSON object. - No markdown. - No code fences. - No explanation before or after. - The JSON must follow this exact schema: {{\"type\":\"tool_call\",\"id\":\"<unique string you invent>\",\"name\":\"<tool name you choose out of the available ones>\",\"arguments\":{{ ... }}}} Rules for tool calls: - \"type\" is mandatory and must ALWAYS be exactly \"tool_call\". - Never change, translate, or invent another value for \"type\". - \"id\" is mandatory and must be a new unique string chosen by you. - \"name\" is mandatory and must be the exact name of the tool you want to call. - \"arguments\" is mandatory and must be a JSON object containing the tool arguments. - If no tool is needed, respond with normal plain text instead of JSON.";
        }

        [SerializeField] private AgentInstance agentPrefab;

        [Tooltip("Agents created when the scene starts. The tool-call instructions are appended to each persona.")]
        [SerializeField] private List<AgentDefinition> agents = new();

        private Dictionary<string, AgentInstance> _agentInstances = new();
        private AgentToolProvider _toolProvider;

        private static string _chatUrl => VasiliEndpoints.Current.agent + "/";

        private async void Start()
        {
            _toolProvider = GetComponent<AgentToolProvider>();

            LoadAgentBots();
        }

        // Creates the agents configured for this scene
        private async void LoadAgentBots()
        {
            foreach (AgentDefinition agent in agents)
            {
                if (!_agentInstances.ContainsKey(agent.id))
                    await CreateAgent(agent.id, agent.persona + " " + GenerateStandardPrompt(), agent.position);
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

    /// <summary>An agent to create in a scene: its ID at the agent service, who it is and where it stands.</summary>
    [Serializable]
    public class AgentDefinition
    {
        public string id;
        [TextArea(2, 6)] public string persona;
        public Vector3 position;
    }

    [Serializable]
    public struct AgentRequest
    {
        public string id;
        public string systemPrompt;
    }
}