/*
 * Eye Gaze Pointer Pose for Meta Quest Eye Tracking
 * 
 * This script provides a ray origin based on the user's eye gaze direction,
 * allowing interaction with UI elements and other RayInteractables using eye tracking.
 * 
 * Usage:
 * 1. Add this component to a GameObject
 * 2. Assign a reference to the OVRCameraRig (or leave empty to auto-find)
 * 3. Connect this GameObject's transform to a RayInteractor's Ray Origin
 * 4. The RayInteractor will now use eye gaze for interactions
 */

using UnityEngine;
using Oculus.Interaction;

namespace VaSiLi.Interaction
{
    /// <summary>
    /// Provides a ray origin and direction based on the user's eye gaze from Meta Quest eye tracking.
    /// This can be used as the ray origin for a <see cref="RayInteractor"/> to enable eye gaze interactions.
    /// </summary>
    /// <remarks>
    /// This type implements <see cref="IActiveState"/> to indicate whether eye tracking is currently
    /// providing valid data. The ray will be cast from the combined eye gaze position in the specified
    /// tracking mode direction.
    /// </remarks>
    public class EyeGazePointerPose : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// Which eye to track. Combined uses an average of both eyes for more stable tracking.
        /// </summary>
        public enum GazeEye
        {
            Left,
            Right,
            Combined
        }

        /// <summary>
        /// The tracking space mode for eye gaze positioning.
        /// </summary>
        public enum TrackingMode
        {
            /// <summary>Track eye relative to head space (local to camera)</summary>
            HeadSpace,
            /// <summary>Track eye in world space</summary>
            WorldSpace,
            /// <summary>Track eye relative to OVRCameraRig tracking space</summary>
            TrackingSpace
        }

        [Header("Eye Tracking Settings")]
        [Tooltip("Which eye to track. Combined is recommended for stability.")]
        [SerializeField]
        private GazeEye _gazeEye = GazeEye.Combined;

        [Tooltip("Minimum confidence threshold for eye tracking to be considered valid.")]
        [SerializeField, Range(0f, 1f)]
        private float _confidenceThreshold = 0.5f;

        [Tooltip("How to interpret the eye tracking pose data.")]
        [SerializeField]
        private TrackingMode _trackingMode = TrackingMode.WorldSpace;

        [Header("Ray Settings")]
        [Tooltip("Optional offset applied to the ray origin in local space.")]
        [SerializeField]
        private Vector3 _offset = Vector3.zero;

        [Header("References")]
        [Tooltip("Reference to the main camera. If null, will use Camera.main.")]
        [SerializeField]
        private Camera _camera;

        [Header("Smoothing")]
        [Tooltip("Enable smoothing to reduce jitter in eye tracking.")]
        [SerializeField]
        private bool _enableSmoothing = true;

        [Tooltip("Smoothing factor for eye gaze. Higher values = more smoothing but more lag.")]
        [SerializeField, Range(0f, 0.95f)]
        private float _smoothingFactor = 0.7f;

        [Header("Debug")]
        [Tooltip("Draw debug ray in Scene view.")]
        [SerializeField]
        private bool _debugDrawRay = false;

        [Tooltip("Length of debug ray.")]
        [SerializeField]
        private float _debugRayLength = 5f;

        private OVRPlugin.EyeGazesState _currentEyeGazesState;
        private bool _started = false;
        private Quaternion _smoothedRotation;
        private Vector3 _smoothedPosition;
        private bool _hasInitialPose = false;

        /// <summary>
        /// Whether eye tracking is currently active and providing valid data.
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// The current confidence value of the eye tracking (0-1).
        /// </summary>
        public float Confidence { get; private set; }

        /// <summary>
        /// Whether eye tracking permission has been granted and tracking is enabled.
        /// </summary>
        public bool EyeTrackingEnabled => OVRPlugin.eyeTrackingEnabled;

        /// <summary>
        /// The current world-space position of the eye gaze origin.
        /// </summary>
        public Vector3 GazeOrigin => transform.position;

        /// <summary>
        /// The current world-space direction of the eye gaze.
        /// </summary>
        public Vector3 GazeDirection => transform.forward;

        /// <summary>
        /// Gets or sets the current calibration offset.
        /// </summary>
        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        protected virtual void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        protected virtual void Start()
        {
            _started = true;
            
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning($"[{nameof(EyeGazePointerPose)}] No camera assigned and Camera.main is null. Eye gaze tracking may not work correctly.");
                }
            }

            // Try to start eye tracking
            if (!StartEyeTracking())
            {
                Debug.LogWarning($"[{nameof(EyeGazePointerPose)}] Failed to start eye tracking. Make sure eye tracking permission is granted.");
            }
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                StartEyeTracking();
            }
        }

        protected virtual void OnDisable()
        {
            Active = false;
        }

        private bool StartEyeTracking()
        {
            if (!OVRPlugin.StartEyeTracking())
            {
                Debug.LogWarning($"[{nameof(EyeGazePointerPose)}] OVRPlugin.StartEyeTracking() returned false.");
                return false;
            }
            return true;
        }

        protected virtual void Update()
        {
            UpdateEyeGaze();
        }

        private void UpdateEyeGaze()
        {
            // Get the current eye gaze state
            if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref _currentEyeGazesState))
            {
                Active = false;
                return;
            }

            // Get the appropriate eye gaze based on settings
            OVRPlugin.EyeGazeState eyeGaze;
            bool isValid = GetEyeGaze(out eyeGaze);

            if (!isValid)
            {
                Active = false;
                return;
            }

            Confidence = eyeGaze.Confidence;
            if (Confidence < _confidenceThreshold)
            {
                Active = false;
                return;
            }

            // Convert the pose based on tracking mode
            var pose = eyeGaze.Pose.ToOVRPose();
            switch (_trackingMode)
            {
                case TrackingMode.HeadSpace:
                    pose = pose.ToHeadSpacePose();
                    break;
                case TrackingMode.WorldSpace:
                    pose = pose.ToWorldSpacePose(_camera);
                    break;
                case TrackingMode.TrackingSpace:
                    // Raw tracking space - no conversion needed
                    break;
            }

            // Apply smoothing if enabled
            Vector3 targetPosition = pose.position;
            Quaternion targetRotation = pose.orientation;

            if (_enableSmoothing && _hasInitialPose)
            {
                _smoothedPosition = Vector3.Lerp(targetPosition, _smoothedPosition, _smoothingFactor);
                _smoothedRotation = Quaternion.Slerp(targetRotation, _smoothedRotation, _smoothingFactor);
            }
            else
            {
                _smoothedPosition = targetPosition;
                _smoothedRotation = targetRotation;
                _hasInitialPose = true;
            }

            // Apply offset in local space
            Vector3 offsetPosition = _smoothedPosition + (_smoothedRotation * _offset);

            // Update transform
            transform.position = offsetPosition;
            transform.rotation = _smoothedRotation;

            Active = true;
        }

        private bool GetEyeGaze(out OVRPlugin.EyeGazeState eyeGaze)
        {
            switch (_gazeEye)
            {
                case GazeEye.Left:
                    eyeGaze = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Left];
                    return eyeGaze.IsValid;

                case GazeEye.Right:
                    eyeGaze = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Right];
                    return eyeGaze.IsValid;

                case GazeEye.Combined:
                default:
                    // For combined, use the eye with higher confidence, or fallback to whichever is valid
                    var leftEye = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Left];
                    var rightEye = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Right];

                    if (leftEye.IsValid && rightEye.IsValid)
                    {
                        // Use the eye with higher confidence for more stable tracking
                        eyeGaze = leftEye.Confidence >= rightEye.Confidence ? leftEye : rightEye;
                        return true;
                    }
                    else if (leftEye.IsValid)
                    {
                        eyeGaze = leftEye;
                        return true;
                    }
                    else if (rightEye.IsValid)
                    {
                        eyeGaze = rightEye;
                        return true;
                    }
                    else
                    {
                        eyeGaze = default;
                        return false;
                    }
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (_debugDrawRay && Active)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, transform.forward * _debugRayLength);
                Gizmos.DrawWireSphere(transform.position, 0.01f);
            }
        }

        #region Inject

        /// <summary>
        /// Sets the camera reference for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectCamera(Camera camera)
        {
            _camera = camera;
        }

        /// <summary>
        /// Sets which eye to track for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectGazeEye(GazeEye gazeEye)
        {
            _gazeEye = gazeEye;
        }

        /// <summary>
        /// Sets the confidence threshold for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectConfidenceThreshold(float threshold)
        {
            _confidenceThreshold = Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// Sets the tracking mode for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectTrackingMode(TrackingMode mode)
        {
            _trackingMode = mode;
        }

        /// <summary>
        /// Sets the offset for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }

        /// <summary>
        /// Sets the smoothing settings for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectSmoothing(bool enable, float factor)
        {
            _enableSmoothing = enable;
            _smoothingFactor = Mathf.Clamp(factor, 0f, 0.95f);
        }

        /// <summary>
        /// Injects all settings for a dynamically instantiated EyeGazePointerPose.
        /// </summary>
        public void InjectAllEyeGazePointerPose(
            Camera camera,
            GazeEye gazeEye,
            float confidenceThreshold,
            TrackingMode trackingMode,
            Vector3 offset)
        {
            InjectCamera(camera);
            InjectGazeEye(gazeEye);
            InjectConfidenceThreshold(confidenceThreshold);
            InjectTrackingMode(trackingMode);
            InjectOffset(offset);
        }

        #endregion
    }
}
