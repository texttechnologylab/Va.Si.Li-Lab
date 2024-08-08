using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine;

namespace VaSiLi.Misc
{
    public class MicrophoneSpawner : MonoBehaviour
    {

        private IDotnetVoipSource source;
        // Start is called before the first frame update
        void Start()
        {

            // First, see if an source already exists among siblings
            source = this.GetComponentInChildren<IDotnetVoipSource>();

            // If not, check if a hint exists and use it
            if (source == null)
            {
                var hint = this.GetComponent<DotnetVoipSourceHint>();
                if (hint && hint.prefab)
                {
                    var go = GameObject.Instantiate(hint.prefab);
                    go.transform.parent = this.transform;
                    source = go.GetComponentInChildren<IDotnetVoipSource>();
                }
            }

            // If still nothing, use default
            if (source == null)
            {
                var go = new GameObject("Microphone Dotnet Voip Source");
                go.transform.parent = this.transform;
                source = go.AddComponent<MicrophoneDotnetVoipSource>();
            }
        }
    }
}
