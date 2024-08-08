using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace VaSiLi.SceneManagement
{
    public class RoleMenuControl : MonoBehaviour
    {
        public Text Name;
        public Text SceneName;
        public RawImage ScenePreview;

        [System.Serializable]
        public class BindEvent : UnityEvent<RoomClient, ApiRole> { };
        public BindEvent OnBind;

        private string existing;

        public void Bind(RoomClient client, ApiRole role)
        {
            Name.text = role.name;
            SceneName.text = "";

            OnBind.Invoke(client, role);
        }

    }
}