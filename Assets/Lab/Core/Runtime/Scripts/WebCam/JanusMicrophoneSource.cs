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

        // --- capture-cadence triage (audio thread writes, main thread reads/logs) ---
        long _cbCount;            // OnAudioFilterRead invocations since last report
        long _sampleCount;        // PCM frames seen since last report
        double _lastCbDspTime;    // AudioSettings.dspTime of previous callback
        double _maxGapSeconds;    // largest inter-callback gap since last report
        int _lastInputChannels;   // channels Unity delivered to the filter
        int _lastSentChannels;    // channels we actually forwarded to WebRTC (1 after downmix)
        int _lastBufferLen;
        float[] _monoBuffer;      // reused downmix scratch (audio thread only)

        void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.volume = 1f;      // must be >0 so Unity runs the DSP chain and calls OnAudioFilterRead
            AudioSource.spatialBlend = 0f;
            AudioSource.playOnAwake = false;
            _outputSampleRate = AudioSettings.outputSampleRate;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;

            // One-time: surface the three rates that must agree for clean Opus encoding.
            // If outputSampleRate != 48000, the PCM we tag for WebRTC is mis-rated -> stutter.
            var cfg = AudioSettings.GetConfiguration();
            Debug.Log($"[JanusMic] Audio config: outputSampleRate={AudioSettings.outputSampleRate} " +
                      $"dspBufferSize={cfg.dspBufferSize} speakerMode={cfg.speakerMode} " +
                      $"(reporting {_outputSampleRate} Hz to WebRTC)");
            if (_outputSampleRate != 48000)
                Debug.LogWarning($"[JanusMic] outputSampleRate is {_outputSampleRate} Hz, not 48000 — " +
                                 "Opus expects 48 kHz; mismatch causes send-side stutter. Set Project Audio sample rate to 48000.");
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
            // --- cadence triage: measure callback interval/jitter (audio thread) ---
            double now = AudioSettings.dspTime;
            if (_lastCbDspTime > 0)
            {
                double gap = now - _lastCbDspTime;
                if (gap > _maxGapSeconds) _maxGapSeconds = gap;
            }
            _lastCbDspTime = now;
            _cbCount++;
            _sampleCount += channels > 0 ? data.Length / channels : data.Length;
            _lastInputChannels = channels;
            _lastBufferLen = data.Length;

            if (gain != 1f)
            {
                float g = gain;
                for (int i = 0; i < data.Length; i++)
                {
                    float s = data[i] * g;
                    data[i] = s > 1f ? 1f : s < -1f ? -1f : s;
                }
            }

            // Downmix to mono before handing PCM to WebRTC. Unity delivers this buffer at the
            // output channel count (stereo on Quest), but a mic is mono — sending stereo just
            // doubles the data and risks channel-framing artifacts in the Opus path. We average
            // the channels into a reusable mono buffer and report ch=1.
            if (channels > 1)
            {
                int frames = data.Length / channels;
                if (_monoBuffer == null || _monoBuffer.Length < frames)
                    _monoBuffer = new float[frames];
                for (int f = 0; f < frames; f++)
                {
                    float sum = 0f;
                    int baseIdx = f * channels;
                    for (int c = 0; c < channels; c++) sum += data[baseIdx + c];
                    _monoBuffer[f] = sum / channels;
                }
                OnAudioRead?.Invoke(_monoBuffer, 1, _outputSampleRate);
                _lastSentChannels = 1;
            }
            else
            {
                OnAudioRead?.Invoke(data, channels, _outputSampleRate);
                _lastSentChannels = channels;
            }

            Array.Clear(data, 0, data.Length); // silence local speaker — WebRTC already captured the signal
        }

        float _nextReportTime;

        void Update()
        {
            // Report capture cadence ~every 2 s from the main thread. A steady stream at the
            // output rate should show samples/s ≈ outputSampleRate and a small max gap
            // (≈ dspBufferSize / rate). A large maxGap or samples/s far from the rate means the
            // audio thread is starving/irregular — the cause of send-side stutter.
            if (Time.unscaledTime < _nextReportTime) return;
            const float Interval = 2f;
            _nextReportTime = Time.unscaledTime + Interval;

            long cb = System.Threading.Interlocked.Exchange(ref _cbCount, 0);
            long samples = System.Threading.Interlocked.Exchange(ref _sampleCount, 0);
            double maxGapMs = _maxGapSeconds * 1000.0;
            _maxGapSeconds = 0;
            if (cb == 0) return; // not capturing yet

            double samplesPerSec = samples / Interval;
            double drift = _outputSampleRate > 0 ? samplesPerSec / _outputSampleRate : 0;
            Debug.Log($"[JanusMic] capture: {cb} cb/{Interval:0}s, {samplesPerSec:0} samp/s " +
                      $"(rate={_outputSampleRate}, ratio={drift:0.00}), ch={_lastInputChannels}->{_lastSentChannels}, " +
                      $"buf={_lastBufferLen}, maxGap={maxGapMs:0.0}ms");
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
