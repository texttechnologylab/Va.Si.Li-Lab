using UnityEngine;

namespace VaSiLi.Tutorial
{
    public class TutorialPanelLimiter : MonoBehaviour
    {
        private static bool initiated;

        void Awake()
        {
            if (initiated)
            {
                gameObject.SetActive(false);
                Destroy(this);
            }
            initiated = true;
        }
    }
}