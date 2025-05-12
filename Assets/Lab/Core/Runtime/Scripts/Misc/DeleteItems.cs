using UnityEngine;
using VaSiLi.Interfaces;
using VaSiLi.Object;
using Ubiq.Spawning;

namespace VaSiLi.Misc
{
    public class DeleteItems : MonoBehaviour
    {
        NetworkSpawnManager manager;
        
        void Start()
        {
            manager = NetworkSpawnManager.Find(this);
        }

        void OnTriggerStay(Collider other)
        {
            DestroyIfOwnable(other.gameObject);
        }

        void DestroyIfOwnable(GameObject other)
        {
            var ownable = other.GetComponent<IOwnable>();
            if (ownable != null)
            {
                if (ownable is MultiGrabbable)
                {
                    var multi = ownable as MultiGrabbable;
                    if (ownable.Owner == false && multi.isOwned == false)
                    {
                        manager.Despawn(multi.targetTransform.parent.gameObject);
                    }
                }

            }
        }    


    }
}