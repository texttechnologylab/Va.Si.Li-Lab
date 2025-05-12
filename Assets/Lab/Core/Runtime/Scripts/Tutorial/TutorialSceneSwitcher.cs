using UnityEngine;
using UnityEngine.SceneManagement;

namespace VaSiLi.Tutorial
{
    public class TutorialSceneSwitcher : MonoBehaviour
    {

        void Start()
        {

        }
        public static void SwitchScene()
        {
            SceneManager.LoadScene("Tutorial", LoadSceneMode.Single);
        }
    }
}