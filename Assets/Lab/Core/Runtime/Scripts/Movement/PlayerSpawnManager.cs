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
            if (target != null)
            {
                var trans = target.GetComponent<Transform>();
                player.transform.position = trans.position;
                playerTeleported.Invoke();
            }
        }
    }
}
