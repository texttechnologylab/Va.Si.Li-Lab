using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VaSiLi.MetaAvatar;

namespace Lab.Core.Runtime.Scripts.LLM {
    public class ToolExamples : MonoBehaviour
    {
        public List<GameObject> spawnablePrefabs = new();
        
        [AgentTool(
            "spawn_prefab",
            "Call when the user wants to spawn an object."
        )]
        public string SpawnPrefab(string name, float x, float y, float z) {
            Debug.LogWarning("Checking for object called " + name);
            Vector3 spawnPos = new Vector3(x, y, z);
            string fixedName = name.Trim().ToLower();
            GameObject prefab = null;
            foreach (GameObject g in spawnablePrefabs) {
                if (g.name.Trim().ToLower().Contains(fixedName))
                    prefab = g;
            }

            if (prefab is null)
                return "Could not find prefab " + name;

            Instantiate(prefab, spawnPos, Quaternion.identity);

            return "Successfully instantiated " + name;
        }
        
        [AgentTool(
            "do_greetings",
            "Call when the user wants to be greeted."
        )]
        public static string DoGreetings(string message, string credits)
        {
            Debug.Log(message + " by " + credits);
            return message + " by " + credits;
        }

        [AgentTool(
            "do_greetings_instance",
            "Call when the user wants to be greeted by an object instance."
        )]
        public string DoGreetingsInstance(string message, string credits)
        {
            Debug.Log(message + " by " + credits);
            return message + " by " + credits;
        }

        [AgentTool(
            "get_scene_info",
            "Call when the user asks for information about the entire scene."
        )]
        public string GetSceneInfo() {
            StringBuilder json = new("Folgende Informationen sind zur Szene bekannt. Formuliere daraus einen kohärenten Text: {");
            bool first = true;
            List<MonoBehaviour> infos =
                FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
            foreach (MonoBehaviour beh in infos) {
                if (beh is not IAgentContextInfo info)
                    continue;
                
                if (!first)
                    json.Append(",");
                first = false;
                
                json.Append("{\\\"name\\\":\\\"").Append(beh.gameObject.name).Append("\\\",\\\"info\\\":");
                json.Append(info.GetContextInfo());
                json.Append("}");
            }
            json.Append("}");
            
            Debug.LogWarning(json.ToString());
            
            return json.ToString();
        }
        
        [AgentTool(
            "get_player_pointing_at",
            "Call when the user requires information about what object he is pointing at."
        )]
        public GameObject GetPlayerPointingAt()
        {
            var player = FindFirstObjectByType<TrackedMetaAvatar>();
            Pose leftHand = player.GetLeftHandPose();
            Ray ray = new Ray(leftHand.position, leftHand.forward);
            RaycastHit hit;
            GameObject pointingAt = null;
            if (Physics.Raycast(ray, out hit))
            {
                pointingAt = hit.collider.gameObject;
                Debug.LogError(pointingAt.name);
            }

            return pointingAt;
        }
        
        [AgentTool(
            "get_player_looking_at",
            "Call when the user requires information about what object he is looking at."
        )]
        public GameObject GetPlayerLookingAt()
        {
            var player = FindFirstObjectByType<TrackedMetaAvatar>();
            Pose head;
            player.TryGetLocalHeadPose(out head);
            Ray ray = new Ray(head.position, head.forward);
            RaycastHit hit;
            GameObject lookingAt = null;
            if (Physics.Raycast(ray, out hit))
            {
                lookingAt = hit.collider.gameObject;
                Debug.LogError(lookingAt.name);
            }

            return lookingAt;
        }

        [AgentTool(
            "change_color",
            "Call when the player wants to change a color."
        )]
        public void ChangeColor(GameObject obj, Color color) {
            Renderer r =  obj.GetComponent<Renderer>();
            r.material.SetColor("_Color", color);
        }
        
        [AgentTool(
            "change_texture",
            "Call when the player wants to change a texture."
        )]
        public void ChangeTexture(GameObject obj, string path) {
            Texture2D tex = Resources.Load<Texture2D>(path);
            Renderer r =  obj.GetComponent<Renderer>();
            r.material.SetTexture("_MainTexture", tex);
        }

        private void Awake() {
            Debug.LogWarning(GetSceneInfo());
        }
    }
}