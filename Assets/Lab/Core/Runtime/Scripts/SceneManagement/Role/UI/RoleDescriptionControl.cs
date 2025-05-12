using UnityEngine;
using UnityEngine.UI;
using Ubiq.Samples;

namespace VaSiLi.SceneManagement
{
    public class RoleDescriptionControl : MonoBehaviour
    {
        private int descriptionPage = 0;
        public TMPro.TMP_Text roleDescription;
        public Button nextDescriptionButton;
        public Button previousDescriptionButton;
        public Button goToLevelsButton;
        public PanelSwitcher panelSwitcher;
        public GameObject levelPanel;

        public void InitMenu(){
            descriptionPage = 0;
            ShowDescription();
            
            UpdateButtons();
        }

        public void NextDescription(){
            descriptionPage++;
            ShowDescription();
            UpdateButtons(); 
        }

        public void PreviousDescription(){
            descriptionPage--;
            ShowDescription();
            UpdateButtons();
        }

        public void ShowLevelsMenu(){
            panelSwitcher.SwitchPanel(levelPanel);
        }

        private void ShowDescription() {
            if (RoleManager.GetRoleLocale().description?.Length > descriptionPage)
                roleDescription.text = RoleManager.GetRoleLocale().description[descriptionPage];
            else
                roleDescription.text = $"Page {descriptionPage} text";
        }

        private void UpdateButtons(){
            nextDescriptionButton.gameObject.SetActive(RoleManager.GetRoleLocale().description?.Length-1 > descriptionPage);
            previousDescriptionButton.gameObject.SetActive(descriptionPage > 0);
            goToLevelsButton.gameObject.SetActive(RoleManager.GetRoleLocale().description == null || RoleManager.GetRoleLocale().description.Length-1 == descriptionPage || RoleManager.CurrentRole?.mode != Mode.Player);
        }
    }
}