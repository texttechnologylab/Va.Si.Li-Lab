

using Ubiq.Avatars;
using Ubiq.Rooms;
using Ubiq.Samples;
using Ubiq.Samples.Social;
using Ubiq.Voip;
using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Helper class to manage user state given a role was picked
    /// This also manages the UI elements
    /// </summary>
    public class RolePicker : MonoBehaviour
    {
        private static NameManager _manager;
        public NameManager manager;
        private static AvatarManager _avatarManager;
        public AvatarManager avatarManager;
        private static GameObject _spectator;
        public GameObject spectator;
        private static GameObject _defaultPlayer;
        public GameObject defaultPlayer;
        private static VoipPeerConnectionManager _voipManager;
        public VoipPeerConnectionManager voipManager;
        private static StartPanel _startPanel;
        public StartPanel startPanel;
        private static GameObject _roleListPanel;
        public GameObject roleListPanel;
        private static GameObject _player;
        public GameObject player;
        private static SocialMenu _menu;
        public SocialMenu menu;

        void Start()
        {
            _manager = manager;
            _avatarManager = avatarManager;
            _spectator = spectator;
            _defaultPlayer = defaultPlayer;
            _voipManager = voipManager;
            _startPanel = startPanel;
            _roleListPanel = roleListPanel;
            _player = player;
            _menu = menu;
        }

        /// <summary>
        /// Picks a role for a user
        /// </summary>
        /// <param name="roomClient">The current room client</param>
        /// <param name="role">The ApiRole that was picked</param>
        public static void PickRole(RoomClient roomClient, ApiRole role)
        {
            // If the room client doesn't exist
            if (!roomClient)
            {
                return;
            }
            // Sets the name of the user to the name of the role
            _manager.SetName(role.name);
            
            // FIXME: WebGL support
            if (role.mode != Mode.Player)
            {
                // Change the avatar to the spectator avatar (invisible)
                if (_avatarManager.avatarPrefab != _spectator)
                {
                    // Update prefab
                    _avatarManager.avatarPrefab = _spectator;
                    _avatarManager.UpdateLocalAvatar();
                }
                // Find the microphone source
                var microphoneGo = GameObject.Find("Microphone Dotnet Voip Source");
                if (microphoneGo != null) {
                    var microphoneSource = microphoneGo.GetComponent<MicrophoneDotnetVoipSource>();
                    if (microphoneSource != null)
                    {
                        // FIXME: We don't need the null source anymore technically
                        microphoneGo.AddComponent<NullDotnetVoipSource>();
                        microphoneSource.CloseAudio();
                    }
                }
            }
            // If we are a player
            else
            {
                var microphoneGo = GameObject.Find("Microphone Dotnet Voip Source");
                if (microphoneGo != null) {
                    var microphoneSource = microphoneGo.GetComponent<NullDotnetVoipSource>();
                    if (microphoneSource != null)
                    {
                        var microphoneOriginal = microphoneGo.GetComponent<MicrophoneDotnetVoipSource>();
                        //microphoneGo.AddComponent<MicrophoneDotnetVoipSource>();
                        Destroy(microphoneSource);
                        microphoneOriginal.StartAudio();
                    }
                }
                // Change the avatar of a user to default player model
                if (_avatarManager.avatarPrefab == _spectator)
                {
                    _avatarManager.avatarPrefab = _defaultPlayer;
                    _avatarManager.UpdateLocalAvatar();
                }
            }

            // Find appropriate spawn and teleport player
            var go = GameObject.Find(role.spawnPosition);
            if (go == null)
                go = GameObject.Find("DefaultSpawn");
                
            if (go != null)
            {
                var trans = go.GetComponent<Transform>();
                _player.transform.position = trans.position;
                _menu.Request();
            }
            // Set the current role in the RoleManager
            RoleManager.CurrentRole = role;
            // Init the start panel
            _startPanel.InitMenu();
        }
    }

}