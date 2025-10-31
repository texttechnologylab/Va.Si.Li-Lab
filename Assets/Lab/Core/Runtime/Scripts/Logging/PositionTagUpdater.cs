using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VaSiLi.Logging
{
    [RequireComponent(typeof(Collider))]
    public class PositionTagUpdater : MonoBehaviour
    {
        // Start is called before the first frame update
        public static UnityAction<string> positonUpdate = delegate { };

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                positonUpdate.Invoke(name);
            }
        }

    }
}