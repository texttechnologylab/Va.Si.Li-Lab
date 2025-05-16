using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ubiq.Rooms;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.Events;
using VaSiLi.Misc;
using VaSiLi.Networking;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Class to manage the current role a user has
    /// Contains various UnityActions to keep track of updates in regards to the role state as well
    /// as functions to update the currentl role a user has selected
    /// </summary>
    public class RoleManager : MonoBehaviour
    {
        public GameObject roleTrackerPrefab;
        public RoomClient roomClient;
        public NetworkSpawnManager _spawnManager;
        private static NetworkSpawnManager spawnManager;
        public static ApiRole[] roles;
        public static ApiRole? CurrentRole { get => _currentRole; set => SetCurrentRole(value); }
        private static ApiRole? _currentRole;
        private static Dictionary<string, ApiRole> AllRoles;
        public static UnityAction<ApiRole?> roleChanged = delegate { };
        private static RoleTracker roleTracker;
        public static UnityAction<Dictionary<string, ApiRole>> rolesUpdated = delegate { };
        public static UnityAction<ApiRole?> roleTrackerUpdated = delegate { };
        void Start()
        {
            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            SceneManager.sceneChanged += OnSceneChanged;
            spawnManager = _spawnManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="room"></param>
        private void OnJoinedRoom(IRoom room)
        {
            // If we joined an EmptyRoom or we already have a role tracker
            if (room.UUID == "" || roleTracker != null)
                return;
            // Spawn a role tracker to keep track of the current role selected for other users
            roleTracker = NetworkSpawnManager.Find(this).SpawnWithPeerScope(roleTrackerPrefab).GetComponent<RoleTracker>();
            Debug.Log("Spawned Tracker");
        }

        // If we change the scene always reset the role
        private void OnSceneChanged(ApiScene? scene)
        {
            CurrentRole = null;
        }

        // Sets the current role
        private static void SetCurrentRole(ApiRole? role)
        {
            _currentRole = role;
            if (roleTracker != null)
                roleTracker.Role = role;
            roleChanged.Invoke(role);
        }

        /// <summary>
        /// Retrieves a list of available roles derived from the role trackers that exist in the
        /// scope of the peer spawned objects
        /// </summary>
        /// <returns>An array of nullable ApiRoles</returns>
        public static ApiRole?[] GetPickedRoles()
        {
            //TODO: RoleManager should probably include a 'Find'-like method
            RoleTracker[] roleTrackers = spawnManager.GetComponentsInChildren<RoleTracker>();
            return spawnManager.GetComponentsInChildren<RoleTracker>().
                Select(tracker =>
                {
                    if (tracker != null)
                        return tracker.Role;
                    else
                        return null;
                }).ToArray();
        }

        /// <summary>
        /// Fetches the roles available for a scene
        /// </summary>
        /// <param name="scene">The scene we should fetch the roles for</param>
        /// <returns>An array of available ApiRoles</returns>
        public static async Task<ApiRole[]> FetchRoles(ApiScene scene)
        {
            var response = await JsonRequest.GetRequest($"{SceneManager.APIURL}/roles?id={scene._id}");
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<ApiHeaderNonArray<Dictionary<string, ApiRole>>>(content);
            AllRoles = data.result;
            Debug.Log($"{SceneManager.APIURL}/roles?id={scene._id}");
            Debug.Log(AllRoles.Keys);
            rolesUpdated.Invoke(AllRoles);
            return roles;
        }

        public static ApiRoleLocale GetRoleLocale()
        {

            switch (LanguageManager.SelectedLanguage)
            {
                case LanguageManager.Language.EN:
                    if (CurrentRole.HasValue && CurrentRole.Value.locales.ContainsKey("en"))
                    {
                        return CurrentRole.Value.locales["en"];
                    }
                    break;
                case LanguageManager.Language.DE:
                    if (CurrentRole.HasValue && CurrentRole.Value.locales.ContainsKey("de"))
                    {
                        return CurrentRole.Value.locales["de"];
                    }
                    break;
                default:
                    if (CurrentRole.HasValue && CurrentRole.Value.locales.ContainsKey("en"))
                    {
                        return CurrentRole.Value.locales["en"];
                    }
                    break;

            }
            return new ApiRoleLocale() {name = "Locale not found"};
        }
    }
}