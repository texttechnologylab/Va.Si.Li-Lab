using UnityEngine;
using System.Linq;
namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Displays a simple text derived from the APInfos to display after a user selects a role
    /// </summary>
    public class StartPanel : MonoBehaviour
    {
        public RoleDescriptionControl roleDescriptionControl;
        public TMPro.TMP_Text startText;

        public async void InitMenu()
        {
            // TODO: Sometimes if the api hasn't been used in a while it returns null on the first call
            if (SceneManager.infos == null)
            {
                await SceneManager.UpdateInfos();
            }
            var info = SceneManager.infos?.FirstOrDefault(item => item.mode == "start");
            startText.text = info?.description;
        }

        public void ShowRoleMenu()
        {
            roleDescriptionControl.InitMenu();
        }
    }
}
