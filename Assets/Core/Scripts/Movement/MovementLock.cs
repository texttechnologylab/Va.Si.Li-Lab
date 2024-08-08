using UnityEngine;
using VaSiLi.SceneManagement;
using Ubiq.XR;

namespace VaSiLi.Movement
{
    /// <summary>
    /// Class to lock the players movement when their status is WAITING
    /// Also temporarily disables the disabilites 
    /// </summary>
    public class MovementLock : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject player;
        public TeleportRay teleportRayRight;
        public TeleportRay teleportRayLeft;
        private Vector3? currentPosition;
        public Canvas blurCanvas;

        void Start()
        {
            LevelManager.levelStatus += OnLevelStatusChanged;
            RoleManager.roleChanged += OnRoleChange;
        }

        private void OnLevelStatusChanged(int level, LevelManager.Status status)
        {
            if (!RoleManager.CurrentRole.HasValue)
                return;

            if (status == LevelManager.Status.WAITING)
            {
                currentPosition = player.transform.position;
                // FIXME: When a player has teleported before it still trys to teleport them to the position
                teleportRayRight.enabled = false;
                teleportRayLeft.enabled = false;
                if (RoleManager.CurrentRole?.disability == "blur")
                {
                    blurCanvas.gameObject.SetActive(false);
                }
            }
            else if (status == LevelManager.Status.RUNNING)
            {
                // FIXME: This is fine for now but needs to be done differently
                if (RoleManager.CurrentRole?.disability != "no_teleportation")
                {
                    teleportRayRight.enabled = true;
                    teleportRayLeft.enabled = true;
                }
                if (RoleManager.CurrentRole?.disability == "blur")
                {
                    blurCanvas.gameObject.SetActive(true);
                }
                currentPosition = null;
            }
        }

        private void OnRoleChange(ApiRole? role)
        {
            if (!role.HasValue)
            {
                // TODO: Disable role disabilities
                currentPosition = null;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (currentPosition.HasValue)
            {
                if (Vector3.Distance(player.transform.position, currentPosition.Value) > 2)
                    player.transform.position = currentPosition.Value;
            }
        }
    }
}
