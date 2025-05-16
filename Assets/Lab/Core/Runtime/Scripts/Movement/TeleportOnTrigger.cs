using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaSiLi.Movement
{
    /// <summary>
    /// Class that teleports a player to the target position once they collide with it
    /// </summary>
    public class TeleportOnTrigger : MonoBehaviour
    {
        public Transform teleportTargert;
        public GameObject collides;

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                other.transform.position = teleportTargert.position;
                other.transform.rotation = teleportTargert.rotation;
            }
        }
    }
}
