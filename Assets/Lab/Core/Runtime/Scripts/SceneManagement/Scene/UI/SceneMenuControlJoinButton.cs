using UnityEngine;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Helper class for the button that should make a user join a specified room/scene
    /// </summary>
    public class SceneMenuControlJoinButton : MonoBehaviour
    {
        public SceneMenuControl sceneMenuControl;
        private ApiScene scene;
        private void OnEnable()
        {
            sceneMenuControl.OnBind.AddListener(BrowseRoomControl_OnBind);
        }

        private void OnDisable()
        {
            if (sceneMenuControl)
                sceneMenuControl.OnBind.RemoveListener(BrowseRoomControl_OnBind);
        }

        private void BrowseRoomControl_OnBind(ApiScene scene)
        {
            this.scene = scene;
        }

        /*void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            
            mainMenu.Request();
        }*/

        // Expected to be called by a UI element
        public void Join()
        {
            Debug.Log("Join Scene: " + scene.name);
            SceneManager.CurrentScene = scene;
        }

    }
}