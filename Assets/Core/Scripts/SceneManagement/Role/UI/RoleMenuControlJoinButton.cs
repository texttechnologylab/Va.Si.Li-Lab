using Ubiq.Rooms;
using UnityEngine;
using Ubiq.Samples.Social;
using Ubiq.Samples;
using Ubiq.Avatars;
using Ubiq.Voip;

namespace VaSiLi.SceneManagement
{
    public class RoleMenuControlJoinButton : MonoBehaviour
    {
        private RoomClient roomClient;
        public RoleMenuControl roleMenuControl;
        private ApiRole role;

        private void OnEnable()
        {
            roleMenuControl.OnBind.AddListener(RoleControl_OnBind);
        }

        private void OnDisable()
        {
            if (roleMenuControl)
            {
                roleMenuControl.OnBind.RemoveListener(RoleControl_OnBind);
            }
        }

        public void RoleControl_OnBind(RoomClient roomClient, ApiRole role)
        {
            this.roomClient = roomClient;
            this.role = role;
        }

        // Expected to be called by a UI element
        public void Join()
        {
            RolePicker.PickRole(roomClient, this.role);
        }
    }
}