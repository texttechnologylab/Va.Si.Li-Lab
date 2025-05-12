#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR && !UNITY_WEBGL
using Ubiq.Voip.Implementations.Dotnet;
#endif
using UnityEngine;

namespace VaSiLi.Misc
{
    public class MicrophoneSpawner : MonoBehaviour
    {
        #if !UNITY_STANDALONE_WIN && !UNITY_EDITOR && !UNITY_WEBGL
        private IVoipSource source;
        // Start is called before the first frame update
        void Start()
        {

            // First, see if an source already exists among siblings
            source = this.GetComponentInChildren<IVoipSource>();

            // If not, check if a hint exists and use it
            if (source == null)
            {
                var hint = this.GetComponent<VoipSourceHint>();
                if (hint && hint.prefab)
                {
                    var go = GameObject.Instantiate(hint.prefab);
                    go.transform.parent = this.transform;
                    source = go.GetComponentInChildren<IVoipSource>();
                }
            }

            // If still nothing, use default
            if (source == null)
            {
                var go = new GameObject("Microphone Dotnet Voip Source");
                go.transform.parent = this.transform;
                source = go.AddComponent<MicrophoneVoipSource>();
            }
        }
        #endif
    }
}
