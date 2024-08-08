using SIPSorceryMedia.Abstractions;
using System;
using Ubiq.Messaging;
using Ubiq.Voip;
using UnityEngine;
using Oculus.Avatar2;
using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine.Windows;

/**
 * @file OvrAvatarLipSyncContext.cs
 */

namespace VaSiLi.MetaAvatar
{

    public class OvrAvatarLipSyncVoip : OvrAvatarLipSyncBehavior
    {
        private const float bufferSizeRatio = 0.4f;


        [SerializeField]
        private CAPI.ovrAvatar2LipSyncMode _mode = CAPI.ovrAvatar2LipSyncMode.Original;

        [Range(0.0f, 100.0f)]
        [SerializeField]
        private int _smoothing;

        [SerializeField]
        private int _audioSampleRate = 16000;


        private IDotnetVoipSource voipInput;

        protected OvrAvatarVisemeContext _visemeContext;

        /**
         * Controls the rate at which audio is sampled.
         */
        public int AudioSampleRate
        {
            get { return _audioSampleRate; }
            set
            {
                if (value != _audioSampleRate)
                {
                    _audioSampleRate = value;
                    _visemeContext?.SetSampleRate((UInt32)_audioSampleRate, (UInt32)(_audioSampleRate * bufferSizeRatio));
                }
            }
        }

        /**
         * Establishes the method used for lip sync.
         * @see CAPI.ovrAvatar2LipSyncMode
         */
        public CAPI.ovrAvatar2LipSyncMode Mode
        {
            get => _mode;
            set
            {
                if (value != _mode)
                {
                    _mode = value;
                    _visemeContext?.SetMode(_mode);
                }
            }
        }

        public override OvrAvatarLipSyncContextBase LipSyncContext
        {
            get
            {
                CreateVisemeContext();

                return _visemeContext;
            }
        }

        // Thread-safe check of this.enabled
        protected bool _active;

        // Core Unity Functions

        private void Start()
        {
            enabled = true;

            if (voipInput == null)
            {
                var go = GameObject.Find("Microphone Dotnet Voip Source");
                if (go != null)
                {
                    Debug.Log("Input resolved");
                    voipInput = go.GetComponent<IDotnetVoipSource>();
                    if (voipInput is MicrophoneDotnetVoipSource)
                    {
                        Debug.Log("Valid Input");
                        ((MicrophoneDotnetVoipSource)voipInput).OnAudioPcmSample += VoipDelegation;
                    }
                }
            }

        }

        public void VoipDelegation(AudioSamplingRatesEnum samplingRate, float[] sample)
        {   
            //TODO: Could change sampling rate if samplingrate changes ...
            ProcessAudioSamples(sample, 1);
        }


        private void OnEnable()
        {
            _active = true;
            CreateVisemeContext();

        }

        private void OnDisable()
        {
            _active = false;
        }

        private void OnDestroy()
        {

            if (voipInput is MicrophoneDotnetVoipSource)
            {
                Debug.Log("Valid Input");
                ((MicrophoneDotnetVoipSource)voipInput).OnAudioPcmSample -= VoipDelegation;
            }

            _visemeContext?.Dispose();
            _visemeContext = null;
        }

        private void OnValidate()
        {
            SetSmoothing(_smoothing);
        }

        // Public Functions

        public void SetSmoothing(int smoothing)
        {
            _smoothing = Math.Max(Math.Min(smoothing, 100), 0);
            _visemeContext?.SetSmoothing(_smoothing);
        }


        public virtual void ProcessAudioSamples(float[] data, int channels)
        {
            if (!_active || !OvrAvatarManager.initialized) return;

            _visemeContext?.FeedAudio(data, channels);
        }

        #region Private Methods

        private void CreateVisemeContext()
        {
            if (_visemeContext == null && OvrAvatarManager.initialized)
            {
                _visemeContext = new OvrAvatarVisemeContext(new CAPI.ovrAvatar2LipSyncProviderConfig
                {
                    mode = _mode,
                    audioBufferSize = (UInt32)(_audioSampleRate * bufferSizeRatio),
                    audioSampleRate = (UInt32)_audioSampleRate
                });
                SetSmoothing(_smoothing);
            }
        }

        #endregion
    }
}
