using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab.Core.Runtime.Scripts.LLM {
    public class AgentActivatorUI : MonoBehaviour {
        public Button activate;
        public TextMeshProUGUI buttonText;
        public Sprite checkmark, normal;
        public AgentInstance agent;

        private void Awake() {
            activate.onClick.AddListener(ChangeAgentState);
        }

        private void ChangeAgentState() {
            agent.active = !agent.active;
            
            buttonText.text = agent.active ? "" : "Activate";
            
            activate.image.sprite = agent.active ? checkmark : normal;
        }
    }
}
