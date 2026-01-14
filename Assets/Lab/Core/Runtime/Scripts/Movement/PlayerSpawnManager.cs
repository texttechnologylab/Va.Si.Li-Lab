using System;
using System.Collections;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Events;
using VaSiLi.SceneManagement;

namespace VaSiLi.Movement
{
    /// <summary>
    /// Class that changes the position of the local player once a role or scene has changed
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        public GameObject player;
        public PlayerLocomotor playerLocomotor;
        public static UnityAction playerTeleported = delegate { };
        public bool lockPlayer = false;
        private Coroutine teleportCoroutine;
        void Start()
        {
            SceneSwitcher.switchedScenes += OnSceneSwitch;
            RoleManager.roleChanged += OnRoleChange;
        }

        void OnSceneSwitch(ApiScene? scene)
        {
            var go = GameObject.Find("DefaultSpawn");
            TeleportPlayer(go, lockPlayer);
        }

        void OnRoleChange(ApiRole? role)
        {
            if (role.HasValue && role.Value.spawnPosition != "")
            {
                var target = GameObject.Find(role?.spawnPosition);
                TeleportPlayer(target, lockPlayer);
            }
        }
        void TeleportPlayer(GameObject target)
        {

            Debug.Log($"Teleporting player to {target?.name}");
            if (target != null)
            {
                // Since we teleport to eyelevel but our teleport targets are on the ground add the players height
                Vector3 targetPosition = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);
                Debug.Log(playerLocomotor.isActiveAndEnabled);
                var trans = target.GetComponent<Transform>();
                player.GetComponent<OVRPlayerController>().enabled = false;
                player.transform.position = targetPosition;
                player.transform.rotation = trans.rotation;
                player.GetComponent<OVRPlayerController>().enabled = true;
                playerTeleported.Invoke();
            }
        }
        void TeleportPlayer(GameObject target, bool shouldLockPlayer = false)
        {
            if (!shouldLockPlayer)
            {
                TeleportPlayer(target);
            }
            else
            {
                if (teleportCoroutine != null)
                {
                    StopCoroutine(teleportCoroutine);
                }
                teleportCoroutine = StartCoroutine(ContinuesTeleport(target));
            }
        }

        IEnumerator ContinuesTeleport(GameObject target)
        {
            var playerTransform = player.transform;
            while (true) {

                if (Vector3.Distance(playerTransform.position, target.transform.position) > 0.5f)
                    TeleportPlayer(target);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
