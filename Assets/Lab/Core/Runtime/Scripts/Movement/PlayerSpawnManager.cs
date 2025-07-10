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

        void Start()
        {
            SceneSwitcher.switchedScenes += OnSceneSwitch;
            RoleManager.roleChanged += OnRoleChange;
        }

        void OnSceneSwitch(ApiScene? scene)
        {
            var go = GameObject.Find("DefaultSpawn");
            TeleportPlayer(go);
        }

        void OnRoleChange(ApiRole? role)
        {
            if (role.HasValue && role.Value.spawnPosition != "")
            {
                var target = GameObject.Find(role?.spawnPosition);
                TeleportPlayer(target);
            }
        }

        void TeleportPlayer(GameObject target)
        {
            Debug.Log($"Teleporting player to {target?.name}");
            if (target != null)
            {
                Pose targetPose = new Pose(target.transform.position, target.transform.rotation);
                LocomotionEvent teleportEvent = new LocomotionEvent(1, targetPose, LocomotionEvent.TranslationType.Absolute, LocomotionEvent.RotationType.Absolute);
                playerLocomotor.HandleLocomotionEvent(teleportEvent);
                Debug.Log(playerLocomotor.isActiveAndEnabled);
                var trans = target.GetComponent<Transform>();
                player.transform.position = trans.position;
                player.transform.rotation = trans.rotation;
                Debug.Log("Trans position: " + trans.position);
                playerLocomotor.WhenLocomotionEventHandled += OnTeleportLocomotionEventHandled;

            }
        }

        void OnTeleportLocomotionEventHandled(LocomotionEvent ev, Pose pose)
        {
            if (ev.Identifier != 1)
                return;
            Debug.Log($"Player teleported to {pose.position}");
            playerTeleported.Invoke();
            playerLocomotor.WhenLocomotionEventHandled -= OnTeleportLocomotionEventHandled;
        }
    }
}
