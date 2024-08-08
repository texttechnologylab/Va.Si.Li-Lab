using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Helper class used for the element that displays the information for a scene/room
    /// as well as the join button
    /// </summary>
    public class SceneMenuControl : MonoBehaviour
    {
        public Text Name;
        public Text SceneName;
        public RawImage ScenePreview;

        [System.Serializable]
        public class BindEvent : UnityEvent<ApiScene> { };
        public BindEvent OnBind;

        private string existing;

        public void Bind(ApiScene scene)
        {
            Name.text = scene.shortName;
            SceneName.text = scene.internalName;

            OnBind.Invoke(scene);
        }

    }
}