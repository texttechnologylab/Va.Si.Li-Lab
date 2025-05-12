using UnityEngine;
using VaSiLi.Interfaces;
using VaSiLi.Object;
using Ubiq.Spawning;
using UnityEngine.Events;

namespace VaSiLi.Tutorial
{
    public class ObjectCollision : MonoBehaviour
    {
        NetworkSpawnManager manager;
        public static UnityAction<GameObject> collided = delegate {};
        void Start()
        {
            manager = NetworkSpawnManager.Find(this);
        }

        void OnTriggerStay(Collider other)
        {
            var ownable = other.GetComponent<IOwnable>();
            if (ownable != null)
            {
                if (ownable is MultiGrabbable)
                {
                    var multi = ownable as MultiGrabbable;
                    if (ownable.Owner == false && multi.isOwned == false)
                    {
                        collided(other.gameObject);
                    }
                }
            }
        }
    }
}