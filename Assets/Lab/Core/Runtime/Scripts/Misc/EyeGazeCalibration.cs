/*
 * Eye Gaze Calibration for Meta Quest Eye Tracking
 * 
 * This script calibrates eye gaze tracking by having the user look at 
 * specific target points on a canvas and calculating the average offset
 * needed to correct the eye gaze ray direction.
 * 
 * Usage:
 * 1. Add this component to a GameObject
 * 2. Assign the EyeGazePointerPose, Canvas, and RayInteractor references
 * 3. Create target points on the canvas (or let the script generate them)
 * 4. Call StartCalibration() to begin the calibration process
 * 5. The user looks at each target and confirms with a button press or dwell time
 * 6. After all points are captured, the offset is calculated and applied
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using VaSiLi.Helper;

namespace VaSiLi.Interaction
{
    /// <summary>
    /// Calibrates the EyeGazePointerPose by measuring the offset between where the user
    /// is looking and where the eye tracking ray actually hits on a canvas.
    /// </summary>
    public class EyeGazeCalibration : MonoBehaviour
    {
        [Header("Required References")]
        [Tooltip("The EyeGazePointerPose component to calibrate.")]
        [SerializeField]
        private EyeGazePointerPose _eyeGazePointerPose;

        [Tooltip("The RayInteractor used with the eye gaze.")]
        [SerializeField]
        private RayInteractor _rayInteractor;

        [Tooltip("The canvas to use for calibration targets.")]
        [SerializeField]
        private Canvas _calibrationCanvas;

        [Header("Calibration Settings")]
        [Tooltip("Number of calibration points to use.")]
        [SerializeField, Range(3, 9)]
        private int _calibrationPointCount = 5;

        [Tooltip("Time in seconds the user must look at a target to confirm it.")]
        [SerializeField, Range(0.5f, 5f)]
        private float _dwellTime = 1.5f;

        [Tooltip("Radius around target (in canvas units) within which gaze is considered 'on target'.")]
        [SerializeField]
        private float _targetRadius = 50f;

        [Tooltip("If true, automatically generate calibration points on the canvas.")]
        [SerializeField]
        private bool _autoGeneratePoints = true;

        [Tooltip("Margin from canvas edges for auto-generated points (0-0.5).")]
        [SerializeField, Range(0.1f, 0.4f)]
        private float _edgeMargin = 0.15f;

        [Header("Visual Settings")]
        [Tooltip("Prefab for calibration target points. If null, will create simple circles.")]
        [SerializeField]
        private GameObject _targetPrefab;

        [Tooltip("Size of the calibration target points.")]
        [SerializeField]
        private float _targetSize = 40f;

        [Tooltip("Color of inactive targets.")]
        [SerializeField]
        private Color _inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Tooltip("Color of the current active target.")]
        [SerializeField]
        private Color _activeColor = Color.yellow;

        [Tooltip("Color when gaze is on target.")]
        [SerializeField]
        private Color _gazeOnTargetColor = Color.green;

        [Tooltip("Color of completed targets.")]
        [SerializeField]
        private Color _completedColor = new Color(0f, 1f, 0f, 0.3f);

        [Header("UI Elements")]
        [Tooltip("Optional text element to show instructions.")]
        [SerializeField]
        private Text _instructionText;

        [Tooltip("Optional progress indicator image (filled type).")]
        [SerializeField]
        private Image _progressIndicator;

        [Header("Input")]
        [Tooltip("If true, use controller button to confirm instead of dwell time.")]
        [SerializeField]
        private bool _useButtonConfirm = false;

        [Tooltip("Controller button to use for confirmation. One/Three also map to the right/left index pinch with hand tracking.")]
        [SerializeField]
        private OVRInput.Button _confirmButton = OVRInput.Button.One;

        [Header("Offset Calculation")]
        [Tooltip("If true, use the actual distance to the canvas to calculate the offset. If false, use a fixed typical distance.")]
        [SerializeField]
        private bool _useActualDistance = true;

        [Tooltip("Fixed distance to use for offset calculation when not using actual distance (in meters).")]
        [SerializeField]
        private float _typicalInteractionDistance = 1f;

        // Runtime state
        private List<RectTransform> _targetPoints = new List<RectTransform>();
        private List<Image> _targetImages = new List<Image>();
        private List<Vector2> _targetCanvasPositions = new List<Vector2>();
        private List<Vector3> _measuredGazeHitPoints = new List<Vector3>();
        private List<Vector3> _expectedTargetWorldPositions = new List<Vector3>();

        private int _currentTargetIndex = -1;
        private float _currentDwellProgress = 0f;
        private bool _isCalibrating = false;
        private bool _gazeOnCurrentTarget = false;

        private GameObject _calibrationUI;
        private RectTransform _canvasRect;

        /// <summary>
        /// Event fired when calibration starts.
        /// </summary>
        public event Action OnCalibrationStarted;

        /// <summary>
        /// Event fired when a calibration point is captured. Parameter is the point index.
        /// </summary>
        public event Action<int> OnPointCaptured;

        /// <summary>
        /// Event fired when calibration completes successfully. Parameter is the calculated offset.
        /// </summary>
        public event Action<Vector3> OnCalibrationCompleted;

        /// <summary>
        /// Event fired when calibration is cancelled.
        /// </summary>
        public event Action OnCalibrationCancelled;

        /// <summary>
        /// Whether calibration is currently in progress.
        /// </summary>
        public bool IsCalibrating => _isCalibrating;

        /// <summary>
        /// Current progress through calibration points (0 to 1).
        /// </summary>
        public float CalibrationProgress => _currentTargetIndex >= 0 
            ? (float)_currentTargetIndex / _calibrationPointCount 
            : 0f;

        protected virtual void Awake()
        {
            if (_calibrationCanvas != null)
            {
                _canvasRect = _calibrationCanvas.GetComponent<RectTransform>();
            }
        }

        protected virtual void Start()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (_eyeGazePointerPose == null)
            {
                Debug.LogError($"[{nameof(EyeGazeCalibration)}] EyeGazePointerPose reference is required.");
            }
            if (_rayInteractor == null)
            {
                Debug.LogError($"[{nameof(EyeGazeCalibration)}] RayInteractor reference is required.");
            }
            if (_calibrationCanvas == null)
            {
                Debug.LogError($"[{nameof(EyeGazeCalibration)}] Calibration Canvas reference is required.");
            }
        }

        protected virtual void Update()
        {
            if (!_isCalibrating) return;

            UpdateCalibration();
        }

        /// <summary>
        /// Starts the calibration process.
        /// </summary>
        public void StartCalibration()
        {
            if (_isCalibrating)
            {
                Debug.LogWarning($"[{nameof(EyeGazeCalibration)}] Calibration already in progress.");
                return;
            }

            if (!ValidateCanStart())
            {
                return;
            }

            _isCalibrating = true;
            _measuredGazeHitPoints.Clear();
            _expectedTargetWorldPositions.Clear();

            // Create calibration UI
            SetupCalibrationUI();

            // Generate or setup target points
            if (_autoGeneratePoints)
            {
                GenerateCalibrationPoints();
            }

            // Start with first target
            _currentTargetIndex = 0;
            _currentDwellProgress = 0f;
            ActivateTarget(_currentTargetIndex);

            UpdateInstructions("Look at the highlighted target");
            OnCalibrationStarted?.Invoke();
        }

        /// <summary>
        /// Cancels the current calibration process.
        /// </summary>
        public void CancelCalibration()
        {
            if (!_isCalibrating) return;

            _isCalibrating = false;
            _currentTargetIndex = -1;
            CleanupCalibrationUI();
            OnCalibrationCancelled?.Invoke();
        }

        /// <summary>
        /// Resets the calibration offset to zero.
        /// </summary>
        public void ResetCalibration()
        {
            if (_eyeGazePointerPose != null)
            {
                _eyeGazePointerPose.InjectOffset(Vector3.zero);
            }
        }

        private bool ValidateCanStart()
        {
            if (_eyeGazePointerPose == null || _rayInteractor == null || _calibrationCanvas == null)
            {
                Debug.LogError($"[{nameof(EyeGazeCalibration)}] Missing required references. Cannot start calibration.");
                return false;
            }

            if (!_eyeGazePointerPose.EyeTrackingEnabled)
            {
                Debug.LogWarning($"[{nameof(EyeGazeCalibration)}] Eye tracking is not enabled. Calibration may not work.");
            }

            return true;
        }

        private void SetupCalibrationUI()
        {
            // Create container for calibration UI elements
            _calibrationUI = new GameObject("CalibrationUI");
            _calibrationUI.transform.SetParent(_calibrationCanvas.transform, false);

            var rectTransform = _calibrationUI.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void GenerateCalibrationPoints()
        {
            _targetPoints.Clear();
            _targetImages.Clear();
            _targetCanvasPositions.Clear();

            // Calculate positions based on point count
            List<Vector2> normalizedPositions = GetCalibrationPointPositions(_calibrationPointCount);

            foreach (var normPos in normalizedPositions)
            {
                // Convert normalized position to canvas position
                Vector2 canvasPos = new Vector2(
                    Mathf.Lerp(_edgeMargin, 1f - _edgeMargin, normPos.x) * _canvasRect.rect.width - _canvasRect.rect.width / 2f,
                    Mathf.Lerp(_edgeMargin, 1f - _edgeMargin, normPos.y) * _canvasRect.rect.height - _canvasRect.rect.height / 2f
                );

                CreateTargetPoint(canvasPos);
            }
        }

        private List<Vector2> GetCalibrationPointPositions(int count)
        {
            var positions = new List<Vector2>();

            // Always include center
            positions.Add(new Vector2(0.5f, 0.5f));

            if (count >= 5)
            {
                // Add corners
                positions.Add(new Vector2(0f, 0f));     // Bottom-left
                positions.Add(new Vector2(1f, 0f));     // Bottom-right
                positions.Add(new Vector2(0f, 1f));     // Top-left
                positions.Add(new Vector2(1f, 1f));     // Top-right
            }

            if (count >= 9)
            {
                // Add edge midpoints
                positions.Add(new Vector2(0.5f, 0f));   // Bottom-center
                positions.Add(new Vector2(0.5f, 1f));   // Top-center
                positions.Add(new Vector2(0f, 0.5f));   // Left-center
                positions.Add(new Vector2(1f, 0.5f));   // Right-center
            }

            // Trim to requested count
            while (positions.Count > count)
            {
                positions.RemoveAt(positions.Count - 1);
            }

            return positions;
        }

        private void CreateTargetPoint(Vector2 canvasPosition)
        {
            GameObject targetObj;

            if (_targetPrefab != null)
            {
                targetObj = Instantiate(_targetPrefab, _calibrationUI.transform);
            }
            else
            {
                // Create a simple circle target
                targetObj = new GameObject($"CalibrationTarget_{_targetPoints.Count}");
                targetObj.transform.SetParent(_calibrationUI.transform, false);

                var image = targetObj.AddComponent<Image>();
                image.color = _inactiveColor;
                
                // Create a circular sprite or use default
                image.sprite = CreateCircleSprite();
                
                _targetImages.Add(image);
            }

            var rect = targetObj.GetComponent<RectTransform>();
            if (rect == null) rect = targetObj.AddComponent<RectTransform>();

            rect.anchoredPosition = canvasPosition;
            rect.sizeDelta = new Vector2(_targetSize, _targetSize);

            _targetPoints.Add(rect);
            _targetCanvasPositions.Add(canvasPosition);

            // If no image was added yet (from prefab), try to get it
            if (_targetImages.Count < _targetPoints.Count)
            {
                var img = targetObj.GetComponent<Image>();
                if (img != null)
                {
                    _targetImages.Add(img);
                }
            }
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        // Inner filled circle
                        if (dist <= radius * 0.3f)
                        {
                            pixels[y * size + x] = Color.white;
                        }
                        // Ring
                        else if (dist >= radius - 3)
                        {
                            pixels[y * size + x] = Color.white;
                        }
                        else
                        {
                            pixels[y * size + x] = new Color(1, 1, 1, 0.3f);
                        }
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void ActivateTarget(int index)
        {
            // Update all target visuals
            for (int i = 0; i < _targetImages.Count; i++)
            {
                if (i < index)
                {
                    _targetImages[i].color = _completedColor;
                }
                else if (i == index)
                {
                    _targetImages[i].color = _activeColor;
                }
                else
                {
                    _targetImages[i].color = _inactiveColor;
                }
            }

            _currentDwellProgress = 0f;
            _gazeOnCurrentTarget = false;

            if (_progressIndicator != null)
            {
                _progressIndicator.fillAmount = 0f;
            }
        }

        private void UpdateCalibration()
        {
            if (_currentTargetIndex < 0 || _currentTargetIndex >= _targetPoints.Count)
            {
                return;
            }

            // Check if gaze is on current target
            bool gazeOnTarget = IsGazeOnTarget(_currentTargetIndex, out Vector3 gazeHitPoint);
            _gazeOnCurrentTarget = gazeOnTarget;

            // Update target color based on gaze
            if (_currentTargetIndex < _targetImages.Count)
            {
                _targetImages[_currentTargetIndex].color = gazeOnTarget ? _gazeOnTargetColor : _activeColor;
            }

            // Handle confirmation
            if (_useButtonConfirm)
            {
                if (OVRInput.GetDown(_confirmButton))
                {
                    CaptureCurrentPoint(gazeHitPoint);
                }
            }
            else
            {
                // Dwell-based confirmation
                if (gazeOnTarget)
                {
                    _currentDwellProgress += Time.deltaTime / _dwellTime;

                    if (_progressIndicator != null)
                    {
                        _progressIndicator.fillAmount = _currentDwellProgress;
                    }

                    if (_currentDwellProgress >= 1f)
                    {
                        CaptureCurrentPoint(gazeHitPoint);
                    }
                }
                else
                {
                    // Reset dwell progress if gaze leaves target
                    _currentDwellProgress = Mathf.Max(0f, _currentDwellProgress - Time.deltaTime / _dwellTime * 2f);

                    if (_progressIndicator != null)
                    {
                        _progressIndicator.fillAmount = _currentDwellProgress;
                    }
                }
            }

            // Allow cancellation
            // B on controllers, left-hand menu gesture with hand tracking, Escape on desktop
            if (LabInput.BackDown())
            {
                CancelCalibration();
            }
        }

        private bool IsGazeOnTarget(int targetIndex, out Vector3 gazeHitPoint)
        {
            gazeHitPoint = Vector3.zero;

            if (!_eyeGazePointerPose.Active)
            {
                return false;
            }

            // Get where the ray is currently hitting
            var collisionInfo = _rayInteractor.CollisionInfo;
            if (!collisionInfo.HasValue)
            {
                return false;
            }

            gazeHitPoint = collisionInfo.Value.Point;

            // Get target world position
            Vector3 targetWorldPos = _targetPoints[targetIndex].position;

            // Calculate distance in screen/canvas space
            // Project both points to canvas plane
            Vector2 gazeCanvasPos;
            Vector2 targetCanvasPos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, 
                RectTransformUtility.WorldToScreenPoint(_calibrationCanvas.worldCamera, gazeHitPoint),
                _calibrationCanvas.worldCamera,
                out gazeCanvasPos);

            targetCanvasPos = _targetCanvasPositions[targetIndex];

            float distance = Vector2.Distance(gazeCanvasPos, targetCanvasPos);

            return distance <= _targetRadius;
        }

        private void CaptureCurrentPoint(Vector3 gazeHitPoint)
        {
            // Store the measured gaze hit point
            _measuredGazeHitPoints.Add(gazeHitPoint);

            // Store the expected target world position
            Vector3 targetWorldPos = _targetPoints[_currentTargetIndex].position;
            _expectedTargetWorldPositions.Add(targetWorldPos);

            OnPointCaptured?.Invoke(_currentTargetIndex);

            // Move to next target
            _currentTargetIndex++;

            if (_currentTargetIndex >= _targetPoints.Count)
            {
                // Calibration complete
                CompleteCalibration();
            }
            else
            {
                ActivateTarget(_currentTargetIndex);
                UpdateInstructions($"Point {_currentTargetIndex + 1} of {_targetPoints.Count} - Look at the highlighted target");
            }
        }

        private void CompleteCalibration()
        {
            _isCalibrating = false;

            // Calculate the average offset needed
            Vector3 totalOffset = Vector3.zero;

            for (int i = 0; i < _measuredGazeHitPoints.Count; i++)
            {
                // Calculate the offset from measured hit to expected target
                // We need to convert this to a rotation offset that can be applied to the gaze direction
                Vector3 gazeOrigin = _eyeGazePointerPose.GazeOrigin;
                
                Vector3 toMeasured = (_measuredGazeHitPoints[i] - gazeOrigin).normalized;
                Vector3 toExpected = (_expectedTargetWorldPositions[i] - gazeOrigin).normalized;

                // Calculate the angular difference
                Vector3 offsetDirection = toExpected - toMeasured;
                totalOffset += offsetDirection;
            }

            // Average the offset
            Vector3 averageOffset = totalOffset / _measuredGazeHitPoints.Count;

            // Convert to local offset for the EyeGazePointerPose
            // The offset is applied in local space relative to the gaze direction
            // We need to express this as a small angular adjustment
            Vector3 localOffset = CalculateLocalOffset(averageOffset);

            // Apply the calibrated offset
            _eyeGazePointerPose.InjectOffset(localOffset);

            UpdateInstructions("Calibration complete!");
            CleanupCalibrationUI();

            OnCalibrationCompleted?.Invoke(localOffset);

            Debug.Log($"[{nameof(EyeGazeCalibration)}] Calibration completed. Applied offset: {localOffset}");
        }

        private Vector3 CalculateLocalOffset(Vector3 worldDirectionOffset)
        {
            // The EyeGazePointerPose offset is applied in local space as a position offset
            // We need to convert the angular offset to a position offset that will
            // result in the correct direction change at the interaction distance
            
            float distance;
            
            if (_useActualDistance)
            {
                // Calculate the average actual distance from the captured calibration points
                // This inherently accounts for canvas angle since we use the real hit points
                Vector3 gazeOrigin = _eyeGazePointerPose.GazeOrigin;
                float totalDistance = 0f;
                int validPoints = 0;
                
                for (int i = 0; i < _measuredGazeHitPoints.Count; i++)
                {
                    // Use both measured and expected points to get a representative distance
                    float measuredDist = Vector3.Distance(gazeOrigin, _measuredGazeHitPoints[i]);
                    float expectedDist = Vector3.Distance(gazeOrigin, _expectedTargetWorldPositions[i]);
                    
                    if (measuredDist > 0.01f && expectedDist > 0.01f)
                    {
                        totalDistance += (measuredDist + expectedDist) / 2f;
                        validPoints++;
                    }
                }
                
                if (validPoints > 0)
                {
                    distance = totalDistance / validPoints;
                    Debug.Log($"[{nameof(EyeGazeCalibration)}] Using average distance from {validPoints} calibration points: {distance:F3}m");
                }
                else
                {
                    // Fallback: calculate perpendicular distance to canvas plane
                    distance = CalculatePerpendicularDistanceToCanvas(gazeOrigin);
                    
                    if (distance <= 0.01f)
                    {
                        distance = _typicalInteractionDistance;
                        Debug.LogWarning($"[{nameof(EyeGazeCalibration)}] Could not determine actual distance, using typical distance: {distance}m");
                    }
                    else
                    {
                        Debug.Log($"[{nameof(EyeGazeCalibration)}] Using perpendicular distance to canvas: {distance:F3}m");
                    }
                }
            }
            else
            {
                distance = _typicalInteractionDistance;
            }
            
            // The position offset that would shift the end point by the measured amount
            // at the calculated distance
            Vector3 localOffset = worldDirectionOffset * distance;

            // Transform to local space of the gaze pose
            Quaternion inverseGazeRotation = Quaternion.Inverse(_eyeGazePointerPose.transform.rotation);
            localOffset = inverseGazeRotation * localOffset;

            return localOffset;
        }

        /// <summary>
        /// Calculates the perpendicular distance from a point to the canvas plane.
        /// This accounts for the canvas being at an angle.
        /// </summary>
        private float CalculatePerpendicularDistanceToCanvas(Vector3 point)
        {
            if (_calibrationCanvas == null) return 0f;
            
            // Get the canvas plane normal (the canvas forward direction)
            Vector3 canvasNormal = _calibrationCanvas.transform.forward;
            Vector3 canvasPosition = _calibrationCanvas.transform.position;
            
            // Calculate the signed distance from point to the plane
            // Plane equation: dot(normal, point - planePoint) = 0
            // Distance = dot(normal, point - canvasPosition)
            float signedDistance = Vector3.Dot(canvasNormal, point - canvasPosition);
            
            return Mathf.Abs(signedDistance);
        }

        private void UpdateInstructions(string text)
        {
            if (_instructionText != null)
            {
                _instructionText.text = text;
            }
        }

        private void CleanupCalibrationUI()
        {
            if (_calibrationUI != null)
            {
                Destroy(_calibrationUI);
                _calibrationUI = null;
            }

            _targetPoints.Clear();
            _targetImages.Clear();
            _targetCanvasPositions.Clear();
        }

        /// <summary>
        /// Manually adds a calibration target point at the specified canvas position.
        /// Only works when autoGeneratePoints is disabled.
        /// </summary>
        public void AddTargetPoint(Vector2 canvasPosition)
        {
            if (_autoGeneratePoints)
            {
                Debug.LogWarning($"[{nameof(EyeGazeCalibration)}] Cannot manually add points when autoGeneratePoints is enabled.");
                return;
            }

            CreateTargetPoint(canvasPosition);
        }

        /// <summary>
        /// Clears all manually added target points.
        /// </summary>
        public void ClearTargetPoints()
        {
            foreach (var target in _targetPoints)
            {
                if (target != null)
                {
                    Destroy(target.gameObject);
                }
            }

            _targetPoints.Clear();
            _targetImages.Clear();
            _targetCanvasPositions.Clear();
        }

        /// <summary>
        /// Saves the current calibration offset to PlayerPrefs.
        /// </summary>
        public void SaveCalibration()
        {
            if (_eyeGazePointerPose == null) return;

            Vector3 offset = _eyeGazePointerPose.Offset;
            PlayerPrefs.SetFloat("EyeGazeCalibration_OffsetX", offset.x);
            PlayerPrefs.SetFloat("EyeGazeCalibration_OffsetY", offset.y);
            PlayerPrefs.SetFloat("EyeGazeCalibration_OffsetZ", offset.z);
            PlayerPrefs.Save();

            Debug.Log($"[{nameof(EyeGazeCalibration)}] Calibration saved: {offset}");
        }

        public void TryLoadCalibration()
        {
            if (!LoadCalibration())
            {
                Debug.LogWarning($"[{nameof(EyeGazeCalibration)}] No saved calibration data found.");
            }
        }

        /// <summary>
        /// Loads a previously saved calibration offset from PlayerPrefs.
        /// </summary>
        /// <returns>True if calibration data was found and loaded.</returns>
        public bool LoadCalibration()
        {
            if (_eyeGazePointerPose == null) return false;

            if (!PlayerPrefs.HasKey("EyeGazeCalibration_OffsetX"))
            {
                Debug.Log($"[{nameof(EyeGazeCalibration)}] No saved calibration found.");
                return false;
            }

            Vector3 offset = new Vector3(
                PlayerPrefs.GetFloat("EyeGazeCalibration_OffsetX", 0f),
                PlayerPrefs.GetFloat("EyeGazeCalibration_OffsetY", 0f),
                PlayerPrefs.GetFloat("EyeGazeCalibration_OffsetZ", 0f)
            );

            _eyeGazePointerPose.InjectOffset(offset);
            Debug.Log($"[{nameof(EyeGazeCalibration)}] Calibration loaded: {offset}");
            return true;
        }

        /// <summary>
        /// Deletes any saved calibration data.
        /// </summary>
        public void DeleteSavedCalibration()
        {
            PlayerPrefs.DeleteKey("EyeGazeCalibration_OffsetX");
            PlayerPrefs.DeleteKey("EyeGazeCalibration_OffsetY");
            PlayerPrefs.DeleteKey("EyeGazeCalibration_OffsetZ");
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(EyeGazeCalibration)}] Saved calibration deleted.");
        }

        /// <summary>
        /// Gets the current calibration offset.
        /// </summary>
        public Vector3 GetCurrentOffset()
        {
            return _eyeGazePointerPose != null ? _eyeGazePointerPose.Offset : Vector3.zero;
        }

        #region Inject

        public void InjectEyeGazePointerPose(EyeGazePointerPose eyeGazePointerPose)
        {
            _eyeGazePointerPose = eyeGazePointerPose;
        }

        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        public void InjectCalibrationCanvas(Canvas canvas)
        {
            _calibrationCanvas = canvas;
            _canvasRect = canvas?.GetComponent<RectTransform>();
        }

        public void InjectAllEyeGazeCalibration(
            EyeGazePointerPose eyeGazePointerPose,
            RayInteractor rayInteractor,
            Canvas calibrationCanvas)
        {
            InjectEyeGazePointerPose(eyeGazePointerPose);
            InjectRayInteractor(rayInteractor);
            InjectCalibrationCanvas(calibrationCanvas);
        }

        #endregion
    }
}
