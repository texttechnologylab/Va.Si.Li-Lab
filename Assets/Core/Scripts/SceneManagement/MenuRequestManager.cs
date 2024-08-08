using Ubiq.Samples;
using UnityEngine;
using VaSiLi.Movement;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Helper class to help with requesting the menu on certain conditions
    /// </summary>
    public class MenuRequestManager : MonoBehaviour
    {
        public SocialMenu socialMenu;
        void OnEnable()
        {
            PlayerSpawnManager.playerTeleported += OnPlayerTeleported;
        }

        void OnDisable()
        {
            PlayerSpawnManager.playerTeleported -= OnPlayerTeleported;
        }

        void OnPlayerTeleported()
        {
            socialMenu.Request();
        }
    }
}