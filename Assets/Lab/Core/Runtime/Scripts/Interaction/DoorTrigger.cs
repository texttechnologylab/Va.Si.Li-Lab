using UnityEngine;
using UnityEngine.Serialization;

namespace VaSiLi.Interaction
{
    public class DoorTrigger : MonoBehaviour
    {
        [FormerlySerializedAs("Door")]
        public Door door;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<CapsuleCollider>(out _))
            {
                if (!door.isOpen)
                {
                    door.Open(other.transform.position);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<CapsuleCollider>(out _))
            {
                if (door.isOpen)
                {
                    door.Close();
                }
            }
        }
    }
}
