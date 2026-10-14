using UnityEngine;
using UnityEngine.InputSystem;
using VaSiLi.MetaAvatar;

namespace VaSiLi.Llm {
    public class PlayerMicListener : MonoBehaviour {
        public AgentManager manager;
    
        private bool _currentlyTalking;
        private AudioClip _clip;
        
        private void Awake() {
            if (manager is null)
                manager = FindFirstObjectByType<AgentManager>();
        }
        
        private void Update() {
            bool vDown = Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
            bool vUp   = Keyboard.current != null && Keyboard.current.vKey.wasReleasedThisFrame;
            
            if (!_currentlyTalking && (OVRInput.GetDown(OVRInput.Button.One) || vDown)) {
                _currentlyTalking = true;
                Debug.Log("Now recording.");
                _clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
            }

            if (_currentlyTalking && (OVRInput.GetUp(OVRInput.Button.One) || vUp)) {
                Microphone.End(Microphone.devices[0]);
                Debug.Log("Stop.");

                /*Pose head;
                GetComponent<TrackedMetaAvatar>().TryGetLocalHeadPose(out head);
                Vector3 position = head.position;*/
                
                foreach(AgentInstance agent in manager.GetActiveAgents())
                    agent.ProcessUserPrompt(_clip);
                
                _currentlyTalking = false;
            }
        }
    }
}
