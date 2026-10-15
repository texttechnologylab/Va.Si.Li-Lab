using UnityEngine;
using UnityEngine.InputSystem;
using VaSiLi.Helper;
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
            // Push-to-talk: A button, right index pinch with hand tracking, or V on desktop
            // Only record when someone is listening and a microphone exists
            if (!_currentlyTalking && LabInput.PrimaryDown(Key.V)
                && Microphone.devices.Length > 0 && manager.GetActiveAgents().Count > 0) {
                _currentlyTalking = true;
                Debug.Log("Now recording.");
                _clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
            }

            if (_currentlyTalking && LabInput.PrimaryUp(Key.V)) {
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
