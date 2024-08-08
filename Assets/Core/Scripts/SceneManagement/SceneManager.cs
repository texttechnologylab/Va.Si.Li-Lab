using UnityEngine;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Spawning;
using System.Threading.Tasks;
using VaSiLi.Networking;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Main class to interact various actions and helper functions for 
    /// Va.Si.Li's scene/room switching capabilities
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        public RoomClient roomClient;
        public static bool initalized;
        public static ApiScene[] scenes;
        public static ApiInfos[] infos;
        
        public static string APIURL = "http://api.vasililab.texttechnologylab.org";
        public string apiURL = "http://api.vasililab.texttechnologylab.org";

        public static ApiScene? CurrentScene { set => SetScene(value); get => _currentScene; }
        private static ApiScene? _currentScene;

        public static UnityAction<ApiScene?> sceneChanged;
        public static UnityAction<ApiScene[]> scenesUpdated;

        public static UnityAction<ApiInfos[]> infosUpdated;
        private static NetworkSpawnManager spawnManager;

        private void Awake()
        {
            APIURL = apiURL;
        }

        /// <summary>
        /// Changes the current scene to the specified one
        /// </summary>
        /// <param name="scene">The scene that should be loaded</param>
        public static void SetScene(ApiScene? scene)
        {
            _currentScene = scene;
            sceneChanged.Invoke(scene);
        }

        /// <summary>
        /// Updates and returns the list of available api scenes from the api endpoint
        /// </summary>
        /// <returns>A List of Scenes</returns>
        public static async Task<ApiScene[]> UpdateScenes()
        {
            var response = await JsonRequest.GetRequest($"{APIURL}/scenesv2?small=true");
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<ApiHeader<ApiScene>>(content);
            scenes = data.result;
            scenesUpdated.Invoke(scenes);
            return scenes;
        }

        /// <summary>
        /// Updates and returns the specified (global)-infos from the api endpoint
        /// </summary>
        /// <returns></returns>
        public static async Task<ApiInfos[]> UpdateInfos()
        {
            var response = await JsonRequest.GetRequest($"{APIURL}/infos");
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<ApiHeader<ApiInfos>>(content);
            infos = data.result;
            infosUpdated.Invoke(infos);
            return infos;
        }

    }
}