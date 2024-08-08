using UnityEngine;
using Ubiq.XR;
using VaSiLi.SceneManagement;


namespace VaSiLi.Misc
{
    public class RoleDisability : MonoBehaviour
    {
        public GameObject blurCanvas;
        public HandController leftHand;
        public HandController rightHand;
        public AudioLowPassFilter lowPassFilter;
        public AudioHighPassFilter highPassFilter;
        public AudioReverbFilter reverbFilter;
        public AudioDistortionFilter distortionFilter;


        // Start is called before the first frame update
        void OnEnable()
        {
            RoleManager.roleChanged += CheckDisability;
        }

        void Start()
        {
            CheckDisability(null);
        }

        protected void CheckDisability(ApiRole? role)
        {
            if (SceneManager.CurrentScene?.internalName == "SchoolHetero")
            {
                if (RoleManager.CurrentRole?.disability == "blur")
                {
                    blurCanvas.SetActive(true);
                }
                else if (RoleManager.CurrentRole?.disability == "voice_changer")
                {
                    leftHand.GripPress.RemoveAllListeners();
                    rightHand.GripPress.RemoveAllListeners();
                    return;
                }
            }
            if (SceneManager.CurrentScene?.internalName == "ICIDS")
            {
                // Reset the previous disabilities
                lowPassFilter.enabled = false;
                highPassFilter.enabled = false;
                distortionFilter.enabled = false;
                reverbFilter.enabled = false;
                blurCanvas.SetActive(false);

                if (RoleManager.CurrentRole?.disability == "blur")
                {
                    blurCanvas.SetActive(true);
                }
                else if (RoleManager.CurrentRole?.disability == "controlling")
                {
                    leftHand.GripPress.RemoveAllListeners();
                    rightHand.GripPress.RemoveAllListeners();
                    return;
                }
                else if (RoleManager.CurrentRole?.disability == "hearing")
                {
                    lowPassFilter.enabled = true;
                    highPassFilter.enabled = true;
                    distortionFilter.enabled = true;
                    reverbFilter.enabled = true;
                }
            }

        }
    }
}
