using UnityEngine;

namespace VaSiLi.Misc
{
    public class VideoSettings : MonoBehaviour
    {
        public int targetFrameRate;
        // Start is called before the first frame update
        void Start()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
