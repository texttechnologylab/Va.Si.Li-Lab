using System.Collections;
using UnityEngine;

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Owns an AudioSource that captures the microphone at 48 kHz so that
    /// Unity WebRTC's AudioStreamTrack(AudioSource) constructor can hook
    /// OnAudioFilterRead at the native audio-thread cadence.
    ///
    /// This avoids the pops and clicks caused by feeding resampled 16 kHz PCM
    /// from MicrophoneVoipSource into SetData() on the main thread (irregular
    /// timing, wrong sample rate for WebRTC's internal 48 kHz pipeline).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class JanusMicrophoneSource : MonoBehaviour
    {
        [Tooltip("Leave empty to use the default microphone device.")]
        public string deviceName = "";

        public AudioSource AudioSource { get; private set; }

        // WebRTC's internal pipeline runs at 48 kHz.
        const int SampleRate = 48000;

        // ---------- Debug state (written on audio thread, read on main thread) ----------
        // Use volatile so main-thread reads aren't optimised away.
        volatile int _filterCallCount;
        volatile float _peakAmplitude;   // max |sample| seen across all filter calls since last log
        volatile bool _hasNonZero;       // true once any non-silent sample has arrived

        void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.volume = 0f;   // silence local playback without muting the DSP graph
            AudioSource.spatialBlend = 0f;
            AudioSource.playOnAwake = false;
        }

        void OnEnable()
        {
            var device = string.IsNullOrEmpty(deviceName) ? null : deviceName;
            Debug.Log($"[JanusMic] Starting microphone '{device ?? "default"}' at {SampleRate} Hz");
            AudioSource.clip = Microphone.Start(device, loop: true, lengthSec: 1, frequency: SampleRate);
            StartCoroutine(WaitAndPlay(device));
            StartCoroutine(DebugLogger());
        }

        IEnumerator WaitAndPlay(string device)
        {
            // Wait until the microphone has actually started before playing,
            // otherwise the AudioSource plays silence and the filter gets no data.
            int waited = 0;
            while (Microphone.GetPosition(device) <= 0)
            {
                yield return null;
                waited++;
            }
            Debug.Log($"[JanusMic] Microphone ready after {waited} frames, position={Microphone.GetPosition(device)}. Playing AudioSource.");
            AudioSource.Play();
        }

        IEnumerator DebugLogger()
        {
            var wait5 = new WaitForSeconds(5f);
            yield return wait5; // first report after 5 s to let things stabilise

            while (isActiveAndEnabled)
            {
                int calls = _filterCallCount;
                float peak = _peakAmplitude;
                bool nonZero = _hasNonZero;
                // Reset for next window
                _filterCallCount = 0;
                _peakAmplitude = 0f;

                Debug.Log($"[JanusMic] 5 s window — OnAudioFilterRead calls: {calls}, " +
                          $"peak amplitude: {peak:F4}, " +
                          $"mic playing: {AudioSource.isPlaying}, " +
                          $"clip: {(AudioSource.clip != null ? $"{AudioSource.clip.frequency} Hz" : "null")}, " +
                          $"ever non-silent: {nonZero}, " +
                          $"mic pos: {Microphone.GetPosition(string.IsNullOrEmpty(deviceName) ? null : deviceName)}");

                yield return wait5;
            }
        }

        // Called by Unity's audio engine on the audio thread every time the AudioSource
        // needs a buffer — this is the same callback WebRTC's AudioCustomFilter uses.
        // NOTE: volume=0 already silences the speaker output; we still inspect the data
        // before it reaches the speaker so we can measure real mic amplitude.
        void OnAudioFilterRead(float[] data, int channels)
        {
            _filterCallCount++;
            for (int i = 0; i < data.Length; i++)
            {
                float abs = data[i] < 0 ? -data[i] : data[i];
                if (abs > _peakAmplitude) _peakAmplitude = abs;
                if (abs > 0.0001f) _hasNonZero = true;
            }
            // Don't zero out — WebRTC's AudioCustomFilter reads this same buffer.
            // volume=0 prevents the AudioSource from emitting to the local speaker.
        }

        void OnDisable()
        {
            AudioSource.Stop();
            var device = string.IsNullOrEmpty(deviceName) ? null : deviceName;
            Microphone.End(device);
            Debug.Log($"[JanusMic] Microphone stopped.");
            if (AudioSource.clip != null)
            {
                Destroy(AudioSource.clip);
                AudioSource.clip = null;
            }
        }
    }
}
