using Ubiq.XR;
using UnityEngine;
using Ubiq.Spawning;

namespace VaSiLi.Object
{
    /// <summary>
    /// Generic class to spawn an object when a user interacts with it
    /// </summary>
    public class GenericTouchSpawner : MonoBehaviour, IUseable
    {
        private float grabTime;
        public GameObject spawnable;
        private Hand follow;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void UnUse(Hand controller)
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
        }
    }
}
