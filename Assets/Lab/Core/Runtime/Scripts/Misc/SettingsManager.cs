using UnityEngine;
using VaSiLi.SceneManagement;
using Ubiq.Samples;

namespace VaSiLi.Misc
{
    class SettingsManager : MonoBehaviour
    {
        public PanelSwitcher rootPanelSwitcher;
        public PanelSwitcher rolePanelSwitcher;
        public GameObject rolePanel;
        public GameObject roomPanel;

        public void LeaveRoom()
        {
            SceneManager.CurrentScene = null;
            rootPanelSwitcher.SwitchPanelToDefault();
            rolePanelSwitcher.SwitchPanelToDefault();
        }

        public void ChangeRole()
        {
            rootPanelSwitcher.SwitchPanel(rolePanel);
            rolePanelSwitcher.SwitchPanelToDefault();
            RoleManager.CurrentRole = null;
        }

        public void ChangeRoom()
        {
            rootPanelSwitcher.SwitchPanel(roomPanel);
            rolePanelSwitcher.SwitchPanelToDefault();
        }
    }
}