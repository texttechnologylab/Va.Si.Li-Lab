using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ubiq.Rooms;
using Ubiq.Samples;
using System.Linq;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Manager class for the SceneMenu
    /// This class updates and creates a list of interactable UI elements of the available scenes
    /// </summary>
    public class SceneMenuManager : MonoBehaviour
    {
        public float roomRefreshInterval = 2.0f;
        public SocialMenu mainMenu;
        List<IRoom> availableRooms = new List<IRoom>();
        public GameObject controlTemplate;
        public Transform controlsRoot;
        private ApiScene[] scenes;
        private List<SceneMenuControl> controls = new List<SceneMenuControl>();
        private float nextRoomRefreshTime = -1;

        void Start()
        {
            mainMenu.roomClient.OnRooms.AddListener(RoomClient_OnRoomsDiscovered);
        }

        void OnEnable()
        {
            SceneManager.scenesUpdated += OnScenesUpdated;
        }

        void OnDisable()
        {
            SceneManager.scenesUpdated -= OnScenesUpdated;
        }

        private void RoomClient_OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
        {
            this.availableRooms = rooms;
            UpdateAvailableRooms();
        }

        private void OnScenesUpdated(ApiScene[] scenes)
        {
            this.scenes = scenes;
            UpdateAvailableRooms();
        }

        private void UpdateAvailableRooms()
        {
            int controlI = 0;
            if (scenes == null || scenes.Length == 0)
                return;
            // Iterate through the list of available scenes; derived from the api
            foreach (ApiScene element in scenes)
            {
                // See if the scene already has an open room
                var test = this.availableRooms.FirstOrDefault((item) => item.Name == element.internalName);

                // If the scenes mapped UnityScene isn't included in the current build ignore
                if (SceneUtility.GetBuildIndexByScenePath(element.internalName) == -1)
                {
                    continue;
                }
                // If we don't have enough controls to represent the amount of scenes instantiate a new one
                if (controls.Count <= controlI)
                {
                    controls.Add(InstantiateControl());
                }
                // If we found an open room for this scene
                // TODO: Add player count
                if (test != null)
                    controls[controlI].Bind(element);
                else
                {
                    controls[controlI].Bind(element);
                }
                controlI++;
            }
            // If we have more controls than scenes destroy the leftovers
            while (controls.Count > controlI)
            {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }
        }

        /// <summary>
        /// Helper function to create the Scenes UI element
        /// </summary>
        /// <returns>The UI elements SceneMenuControl</returns>
        private SceneMenuControl InstantiateControl()
        {
            var go = GameObject.Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<SceneMenuControl>();
        }

        // Update the list of available rooms
        private void Update()
        {
            if (Time.realtimeSinceStartup > nextRoomRefreshTime)
            {
                mainMenu.roomClient.DiscoverRooms();
                if (scenes == null || scenes.Length == 0)
                {
                    SceneManager.UpdateScenes().ContinueWith((scene) => { });
                }
                nextRoomRefreshTime = Time.realtimeSinceStartup + roomRefreshInterval;
            }

        }
    }
}

