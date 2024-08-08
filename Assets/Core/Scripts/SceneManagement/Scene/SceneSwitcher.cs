using Ubiq.Rooms;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Handles the actual switching of Scenes in Unity when the SceneManager's scene changes
    /// </summary>
    public class SceneSwitcher : MonoBehaviour
    {
        public string defaultSceneName = "Start";
        public ApiScene? currentScene;
        public RoomClient client;
        public NetworkSpawnManager spawnManager;
        public static UnityAction<ApiScene?> switchedScenes = delegate {};
        private List<IRoom> rooms;
        private Task discoveringRooms;
        private bool discovering;
        private bool switchingRooms;

        void OnEnable()
        {
            SceneManager.sceneChanged += OnApiSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            client.OnRooms.AddListener(OnRoomsDiscovered);
        }

        void OnDisable()
        {
            SceneManager.sceneChanged -= OnApiSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            client.OnRooms.RemoveListener(OnRoomsDiscovered);
        }

        /// <summary>
        /// After we have loaded a scene we should switch to the appropriate room
        /// </summary>
        /// <param name="newScene">The new Scene that was loaded</param>
        /// <param name="mode">The mode the Scene was loaded in (usually Single)</param>
        /// <returns></returns>
        async void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            Debug.Log("Scene changed");
            if (discoveringRooms == null)
                return;
            // Make sure we despawn all our peer objects
            spawnManager.ForceDespawnAll();
            // If we've called to update the list of currently active rooms: await
            await discoveringRooms;
            // If the current scene is null it means we're unloading everything / leaving the room
            if (currentScene == null)
            {
                client.Join("LEFT_ROOM", false);
                switchedScenes.Invoke(currentScene);
                return;
            }
            // See if a room exists with the given scene name
            var existingRoom = rooms.FirstOrDefault(room => room.Name == newScene.name);

            // If we've found a matching room
            if (existingRoom != null)
                client.Join(existingRoom.JoinCode);
            else
            {
                client.Join(currentScene?.internalName, true);
            }
            // Call that we've successfully loaded the scene / are now joinign the room
            switchedScenes.Invoke(currentScene);
        }

        void OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
        {
            this.rooms = rooms;
            discovering = false;
        }

        public void OnApiSceneChanged(ApiScene? scene)
        {
            discovering = true;
            client.DiscoverRooms();
            // Task to make sure the most room list is the most recent
            discoveringRooms = WaitForDiscoverToComplete();
            // "Leave" the current scene
            if (!scene.HasValue)
            {
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(defaultSceneName, LoadSceneMode.Single);
            }
            else if(currentScene == null && scene.HasValue)
            {
                Ubiq.Samples.RoomSceneManager.ChangeScene(this, scene?.internalName);
            }
            // Prepare switching
            else if (scene.HasValue)
            {
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene?.internalName, LoadSceneMode.Single);
            }
           
            currentScene = scene;

        }

        public async Task WaitForDiscoverToComplete()
        {
            while (discovering) { 
                await Task.Delay(10); 
            } 
            return;
        }
    }
}