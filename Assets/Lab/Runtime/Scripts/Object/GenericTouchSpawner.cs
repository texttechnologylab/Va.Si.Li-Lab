using Ubiq.XR;
using UnityEngine;
using Ubiq.Spawning;
using UnityEngine.XR.Interaction.Toolkit;

namespace VaSiLi.Object
{
    /// <summary>
    /// Generic class to spawn an object when a user interacts with it
    /// </summary>
    ///
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
    public class GenericTouchSpawner : MonoBehaviour//, IUseable
    {
        private float grabTime;
        public GameObject spawnable;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interactable.activated.AddListener(XRGrabInteractable_Activated);
        }
        public void XRGrabInteractable_Activated(ActivateEventArgs eventArgs)
        {
            if (Time.time - grabTime > 2)
            {
                var go = NetworkSpawnManager.Find(this).SpawnWithPeerScope(spawnable);
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable graspable = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
                go.transform.localPosition = eventArgs.interactorObject.transform.localPosition;
                grabTime = Time.time;
            }
        }
        /*public void UnUse(Hand controller)
        {
        }

        public void Use(Hand controller)
        {
            if (Time.time - grabTime > 2)
            {
                var go = NetworkSpawnManager.Find(this).SpawnWithPeerScope(spawnable);
                IGraspable graspable = go.GetComponent<IGraspable>();
                if (graspable != null)
                    graspable.Grasp(controller);
                grabTime = Time.time;
            }
        }*/
    }
}
