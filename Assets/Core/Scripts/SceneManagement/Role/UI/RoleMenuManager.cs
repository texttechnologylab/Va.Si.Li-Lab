using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Samples;
using System.Linq;
using VaSiLi.Logging;

namespace VaSiLi.SceneManagement
{
    public class RoleMenuManager : MonoBehaviour
    {
        public SocialMenu mainMenu;
        public PanelSwitcher panelSwitcher;
        public GameObject roleTemplate;
        public GameObject rolePickedTemplate;
        public Transform controlsRoot;
        public GameObject roleListPanel;
        public GameObject startPanel;
        public LogToScreen screenLogger;
        private List<RoleMenuControl> controls = new List<RoleMenuControl>();
        private ApiRole hiddenRole = new ApiRole()
        {
            spawnPosition = null,
            name = "hidden",
            mode = Mode.Spectator,
            admin = false,
            maxCount = -1,
            disability = null
        };

        void Start()
        {
            mainMenu.roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            mainMenu.roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
            RoleManager.rolesUpdated += UpdateRoles;
            RoleManager.rolesUpdated += UpdateHiddenAvatarWithSpectatorLevels;
            RoleManager.roleChanged += OnRoleChanged;
            RoleManager.roleTrackerUpdated += TrackerUpdated;
        }

        void OnEnable()
        {
            UpdateAvailableRoles();
        }

        void TrackerUpdated(ApiRole? role)
        {
            UpdateAvailableRoles();
        }

        void OnJoinedRoom(IRoom room)
        {
            UpdateAvailableRoles();
        }

        void OnPeerRemoved(IPeer peer)
        {
            UpdateAvailableRoles();
        }

        void OnRoleChanged(ApiRole? role)
        {
            if (!role.HasValue)
            {
                panelSwitcher.SwitchPanelToDefault();
            }
            UpdateAvailableRoles();
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.H) && Input.GetKey(KeyCode.R))
            {
                if (RoleManager.CurrentRole == null) {
                    RolePicker.PickRole(mainMenu.roomClient, hiddenRole);
                    GetComponent<PanelSwitcher>().SwitchPanel(startPanel);
                }
                screenLogger._enabled = true;
            }
        }

        void UpdateRoles(ApiRole[] roles)
        {
            int controlI = 0;

            if (roles == null || roles.Length == 0)
                return;

            ApiRole?[] pickedRoles = RoleManager.GetPickedRoles();

            foreach (ApiRole role in roles)
            {

                if (controls.Count <= controlI)
                {
                    if (IsAtLimit(role, pickedRoles))
                        controls.Add(InstantiatePickedControl());
                    else
                        controls.Add(InstantiateControl());
                }
                else if (IsAtLimit(role, pickedRoles))
                {
                    // Destroy the previous object and replace it with a picked control
                    Destroy(controls[controlI].gameObject);
                    controls[controlI] = InstantiatePickedControl();
                }
                else if (controls[controlI].gameObject.name == rolePickedTemplate.name + "(Clone)")
                {
                    Destroy(controls[controlI].gameObject);
                    controls[controlI] = InstantiateControl();
                }
                controls[controlI].Bind(mainMenu.roomClient, role);
                controlI++;
            }

            while (controls.Count > controlI)
            {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }
        }

        private void UpdateHiddenAvatarWithSpectatorLevels(ApiRole[] roles)
        {
            foreach (ApiRole r in roles)
            {
                if (r.mode == Mode.Spectator)
                {
                    hiddenRole.level = r.level;
                }
            }

            RoleManager.rolesUpdated -= UpdateHiddenAvatarWithSpectatorLevels;
        }

        private bool IsAtLimit(ApiRole role, ApiRole?[] roles)
        {
            return roles.Count((item) => item.HasValue && item.Value.name == role.name) >= role.maxCount;
        }

        private async void UpdateAvailableRoles()
        {
            if (SceneManager.CurrentScene != null)
                await RoleManager.FetchRoles((ApiScene)SceneManager.CurrentScene);
        }

        private RoleMenuControl InstantiateControl()
        {
            var go = GameObject.Instantiate(roleTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<RoleMenuControl>();
        }

        private RoleMenuControl InstantiatePickedControl()
        {
            var go = GameObject.Instantiate(rolePickedTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<RoleMenuControl>();
        }

    }
}
