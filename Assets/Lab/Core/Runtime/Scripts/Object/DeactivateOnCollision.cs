using UnityEngine;

namespace VaSiLi.Object
{
    /// <summary>
    /// Class that disables rendering an object when the player collides with it
    /// </summary>
    public class DeactivateOnCollision : MonoBehaviour
    {

        void Start()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                GetComponent<MeshRenderer>().enabled = false;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }
}
