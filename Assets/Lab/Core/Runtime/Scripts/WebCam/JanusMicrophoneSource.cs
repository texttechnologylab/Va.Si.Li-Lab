using System;
using System.Collections;
using UnityEngine;

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Owns the AudioSource that feeds microphone audio into a WebRTC AudioStreamTrack.
    ///
    /// OnAudioFilterRead applies gain, fires OnAudioRead (audio thread) so WebCamClient can
    /// forward PCM to the track via SetData, then clears the buffer to prevent local self-hearing.
    /// volume=1f is required so Unity does not skip the DSP chain as an optimisation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class JanusMicrophoneSource : MonoBehaviour
    {
        [Tooltip("Leave empty to use the default microphone device.")]
        public string deviceName = "";

        [Tooltip("Linear gain applied before the signal reaches WebRTC. 1 = unity gain.")]
        [Range(1f, 8f)]
        public float gain = 1f;

        /// <summary>Fired on the audio thread with (data, channels, sampleRate) after gain is applied.
        /// Subscribe to forward PCM to an AudioStreamTrack via SetData. The buffer is cleared
        /// immediately after so local speakers hear silence.</summary>
        public event Action<float[], int, int> OnAudioRead;

        public AudioSource AudioSource { get; private set; }

        const int DesiredSampleRate = 48000;

        string _device;
        int _outputSampleRate;

        void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.volume = 1f;      // must be >0 so Unity runs the DSP chain and calls OnAudioFilterRead
            AudioSource.spatialBlend = 0f;
            AudioSource.playOnAwake = false;
            _outputSampleRate = AudioSettings.outputSampleRate;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;
        }

        void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigChanged;
        }

        void OnAudioConfigChanged(bool _) => _outputSampleRate = AudioSettings.outputSampleRate;

        void OnEnable()
        {
            _device = ResolveDevice();
            int freq = ResolveFrequency(_device);
            Debug.Log($"[JanusMic] Starting '{_device ?? "default"}' at {freq} Hz");
            try
            {
                AudioSource.clip = Microphone.Start(_device, loop: true, lengthSec: 1, frequency: freq);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JanusMic] Microphone.Start threw: {ex.Message}. Retrying with default.");
                _device = null;
                freq = ResolveFrequency(null);
                AudioSource.clip = Microphone.Start(null, loop: true, lengthSec: 1, frequency: freq);
            }

            // Microphone.Start can fail silently on Quest (FMOD error 80 / OVR Oboe conflict):
            // it logs a native error but does not throw, returning a null or invalid clip.
            if (AudioSource.clip == null || !Microphone.IsRecording(_device))
            {
                Debug.LogWarning($"[JanusMic] Microphone.Start returned no clip (device may be claimed by OVR Oboe). Retrying with null device.");
                Microphone.End(_device);
                _device = null;
                freq = ResolveFrequency(null);
                AudioSource.clip = Microphone.Start(null, loop: true, lengthSec: 1, frequency: freq);
            }

            StartCoroutine(WaitAndPlay());
        }

        IEnumerator WaitAndPlay()
        {
            const int TimeoutFrames = 300; // ~5 s at 60 fps
            int waited = 0;
            while (Microphone.GetPosition(_device) <= 0)
            {
                if (++waited >= TimeoutFrames)
                {
                    Debug.LogError("[JanusMic] Microphone did not start within timeout — no audio will be sent.");
                    yield break;
                }
                yield return null;
            }
            Debug.Log($"[JanusMic] Microphone ready after {waited} frames. Playing.");
            AudioSource.Play();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (gain != 1f)
            {
                float g = gain;
                for (int i = 0; i < data.Length; i++)
                {
                    float s = data[i] * g;
                    data[i] = s > 1f ? 1f : s < -1f ? -1f : s;
                }
            }
            OnAudioRead?.Invoke(data, channels, _outputSampleRate);
            Array.Clear(data, 0, data.Length); // silence local speaker — WebRTC already captured the signal
        }

        void OnDisable()
        {
            AudioSource.Stop();
            Microphone.End(_device);
            if (AudioSource.clip != null)
            {
                Destroy(AudioSource.clip);
                AudioSource.clip = null;
            }
            Debug.Log("[JanusMic] Microphone stopped.");
        }

        string ResolveDevice()
        {
            if (string.IsNullOrEmpty(deviceName)) return null;
            foreach (var d in Microphone.devices)
                if (d == deviceName) return deviceName;
            Debug.LogWarning($"[JanusMic] Device '{deviceName}' not found; falling back to default.");
            return null;
        }

        static int ResolveFrequency(string device)
        {
            Microphone.GetDeviceCaps(device, out int min, out int max);
            if (min == 0 && max == 0) return DesiredSampleRate;
            return Mathf.Clamp(DesiredSampleRate, min, max);
        }
    }
}
