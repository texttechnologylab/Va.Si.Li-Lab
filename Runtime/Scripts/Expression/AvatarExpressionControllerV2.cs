#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Unified controller for applying facial expressions to Meta Avatars while preserving lip sync.
/// This version uses a combined face pose provider that manually merges lip sync visemes 
/// with custom expressions, solving the issue where setting a face pose provider 
/// overrides lip sync.
/// </summary>
public class AvatarExpressionControllerV2 : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("References")]
    [SerializeField, Tooltip("The lip sync behavior to read visemes from")]
    private OvrAvatarLipSyncBehavior? _lipSyncBehavior;
    
    [Header("Expression Settings")]
    [SerializeField, Tooltip("Time in seconds to transition between expressions")]
    private float _transitionDuration = 0.3f;
    
    [SerializeField, Tooltip("Global multiplier for all expression weights")]
    [Range(0f, 2f)]
    private float _expressionIntensity = 1.0f;
    
    [Header("Mouth Blend Settings")]
    [SerializeField, Tooltip("How to blend mouth expressions between custom and lip sync")]
    private MouthBlendMode _mouthBlendMode = MouthBlendMode.Blend;
    
    [SerializeField, Tooltip("Blend weight: 0 = 100% custom, 1 = 100% lip sync. Only used when BlendMode is Blend.")]
    [Range(0f, 1f)]
    private float _lipSyncBlendWeight = 0.7f;
    
    [Header("Debug")]
    [SerializeField]
    private bool _debugMode = false;
    
    #endregion

    #region Private Fields
    
    private OvrAvatarEntity? _avatarEntity;
    private CombinedFacePoseProvider? _facePoseProvider;
    private CombinedFacePoseBehavior? _facePoseBehavior;
    private Dictionary<string, FaceExpressionPreset> _presets = new();
    private Coroutine? _transitionCoroutine;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Gets whether the controller is properly initialized and ready to use.
    /// </summary>
    public bool IsInitialized => _facePoseProvider != null && _avatarEntity != null;
    
    /// <summary>
    /// Gets or sets the transition duration for expression changes.
    /// </summary>
    public float TransitionDuration
    {
        get => _transitionDuration;
        set => _transitionDuration = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// Gets or sets the global expression intensity multiplier.
    /// </summary>
    public float ExpressionIntensity
    {
        get => _expressionIntensity;
        set => _expressionIntensity = Mathf.Clamp(value, 0f, 2f);
    }
    
    /// <summary>
    /// Gets or sets how mouth expressions are blended between custom expressions and lip sync.
    /// </summary>
    public MouthBlendMode MouthBlendMode
    {
        get => _mouthBlendMode;
        set
        {
            _mouthBlendMode = value;
            if (_facePoseProvider != null)
            {
                _facePoseProvider.BlendMode = value;
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the blend weight between custom expressions and lip sync for mouth.
    /// 0 = 100% custom expressions, 1 = 100% lip sync.
    /// Default is 0.7 (70% lip sync, 30% custom).
    /// Only applies when MouthBlendMode is set to Blend.
    /// </summary>
    public float LipSyncBlendWeight
    {
        get => _lipSyncBlendWeight;
        set
        {
            _lipSyncBlendWeight = Mathf.Clamp01(value);
            if (_facePoseProvider != null)
            {
                _facePoseProvider.LipSyncBlendWeight = _lipSyncBlendWeight;
            }
        }
    }
    
    /// <summary>
    /// Gets the current expression weights being applied (non-mouth only).
    /// </summary>
    public float[] CurrentWeights => _facePoseProvider?.CurrentWeights ?? Array.Empty<float>();
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        LoadPresetsFromResources();
        
        // Try to find lip sync behavior if not assigned
        if (_lipSyncBehavior == null)
        {
            _lipSyncBehavior = GetComponent<OvrAvatarLipSyncBehavior>();
            if (_lipSyncBehavior == null)
            {
                _lipSyncBehavior = GetComponentInChildren<OvrAvatarLipSyncBehavior>();
            }
        }
    }
    
    private void OnDestroy()
    {
        Cleanup();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initializes the expression controller for a specific avatar entity.
    /// Call this after the avatar has been loaded.
    /// This version does NOT call SetLipSync on the entity - it handles lip sync internally.
    /// </summary>
    /// <param name="avatarEntity">The avatar entity to control expressions for.</param>
    /// <param name="lipSyncBehavior">Optional lip sync behavior. If null, will try to find one.</param>
    public void Initialize(OvrAvatarEntity avatarEntity, OvrAvatarLipSyncBehavior? lipSyncBehavior = null)
    {
        if (avatarEntity == null)
        {
            Debug.LogError("[AvatarExpressionControllerV2] Cannot initialize with null avatar entity.");
            return;
        }
        
        Cleanup();
        
        _avatarEntity = avatarEntity;
        
        if (lipSyncBehavior != null)
        {
            _lipSyncBehavior = lipSyncBehavior;
        }
        
        // Create the combined face pose provider
        _facePoseProvider = new CombinedFacePoseProvider();
        _facePoseProvider.SetLipSyncSource(_lipSyncBehavior, _avatarEntity);
        
        // Apply blend settings from serialized fields
        _facePoseProvider.BlendMode = _mouthBlendMode;
        _facePoseProvider.LipSyncBlendWeight = _lipSyncBlendWeight;
        
        // Create the behavior wrapper
        _facePoseBehavior = gameObject.AddComponent<CombinedFacePoseBehavior>();
        _facePoseBehavior.Initialize(_facePoseProvider);
        
        // Set our combined face pose provider on the avatar entity
        // IMPORTANT: Do NOT call SetLipSync separately - our provider handles both!
        _avatarEntity.SetFacePoseProvider(_facePoseBehavior);
        
        if (_debugMode)
        {
            Debug.Log($"[AvatarExpressionControllerV2] Initialized for avatar: {avatarEntity.name}");
            Debug.Log($"[AvatarExpressionControllerV2] Lip sync source: {(_lipSyncBehavior != null ? "Found" : "None")}");
        }
    }
    
    /// <summary>
    /// Cleans up resources when the controller is destroyed or reinitialized.
    /// </summary>
    private void Cleanup()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
        
        if (_facePoseBehavior != null)
        {
            Destroy(_facePoseBehavior);
            _facePoseBehavior = null;
        }
        
        _facePoseProvider?.Dispose();
        _facePoseProvider = null;
        _avatarEntity = null;
    }
    
    #endregion

    #region Preset Management
    
    /// <summary>
    /// Loads face expression presets from Resources/FaceStates folder.
    /// </summary>
    private void LoadPresetsFromResources()
    {
        TextAsset[] presetAssets = Resources.LoadAll<TextAsset>("FaceStates");
        
        foreach (var asset in presetAssets)
        {
            try
            {
                string presetName = ExtractPresetName(asset.name);
                var preset = FaceExpressionPreset.FromJson(asset.text);
                
                if (preset != null)
                {
                    _presets[presetName] = preset;
                    
                    if (_debugMode)
                    {
                        Debug.Log($"[AvatarExpressionControllerV2] Loaded preset: {presetName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarExpressionControllerV2] Failed to load preset {asset.name}: {e.Message}");
            }
        }
        
        Debug.Log($"[AvatarExpressionControllerV2] Loaded {_presets.Count} expression presets.");
    }
    
    private string ExtractPresetName(string fileName)
    {
        int underscoreIndex = fileName.IndexOf('_');
        return underscoreIndex > 0 ? fileName.Substring(0, underscoreIndex) : fileName;
    }
    
    /// <summary>
    /// Registers a custom expression preset at runtime.
    /// </summary>
    public void RegisterPreset(string name, FaceExpressionPreset preset)
    {
        if (string.IsNullOrEmpty(name) || preset == null) return;
        _presets[name] = preset;
    }
    
    /// <summary>
    /// Gets all available preset names.
    /// </summary>
    public IEnumerable<string> GetPresetNames() => _presets.Keys;
    
    /// <summary>
    /// Checks if a preset with the given name exists.
    /// </summary>
    public bool HasPreset(string name) => _presets.ContainsKey(name);
    
    #endregion

    #region Expression Application
    
    /// <summary>
    /// Applies a named expression preset to the avatar.
    /// Only non-mouth expressions will be applied; mouth is controlled by lip sync.
    /// </summary>
    public void ApplyExpression(string presetName, float transitionTime = -1f)
    {
        if (!_presets.TryGetValue(presetName, out var preset))
        {
            Debug.LogWarning($"[AvatarExpressionControllerV2] Expression preset '{presetName}' not found.");
            return;
        }
        
        ApplyExpression(preset, transitionTime);
    }
    
    /// <summary>
    /// Applies a face expression preset to the avatar.
    /// Only non-mouth expressions will be applied; mouth is controlled by lip sync.
    /// </summary>
    public void ApplyExpression(FaceExpressionPreset preset, float transitionTime = -1f)
    {
        if (!IsInitialized || preset == null) return;
        
        float duration = transitionTime >= 0f ? transitionTime : _transitionDuration;
        float[] targetWeights = ApplyIntensityToWeights(preset.ExpressionWeights);
        
        if (duration > 0f)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(TransitionToWeights(targetWeights, duration));
        }
        else
        {
            _facePoseProvider!.SetWeights(targetWeights);
        }
        
        if (_debugMode)
        {
            Debug.Log($"[AvatarExpressionControllerV2] Applying expression, duration: {duration}s");
        }
    }
    
    /// <summary>
    /// Applies raw expression weights to the avatar (non-mouth only).
    /// </summary>
    public void ApplyWeights(float[] weights, float transitionTime = -1f)
    {
        if (!IsInitialized || weights == null) return;
        
        var preset = new FaceExpressionPreset(weights);
        ApplyExpression(preset, transitionTime);
    }
    
    /// <summary>
    /// Sets a single expression weight (ignored for mouth expressions).
    /// </summary>
    public void SetExpressionWeight(CAPI.ovrAvatar2FaceExpression expression, float weight)
    {
        if (!IsInitialized) return;
        _facePoseProvider!.SetWeight(expression, weight * _expressionIntensity);
    }
    
    /// <summary>
    /// Resets all non-mouth expressions to neutral (all weights to 0).
    /// Mouth will continue to be controlled by lip sync.
    /// </summary>
    public void ResetToNeutral(float transitionTime = -1f)
    {
        if (!IsInitialized) return;
        
        float[] neutralWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        ApplyWeights(neutralWeights, transitionTime);
    }
    
    private float[] ApplyIntensityToWeights(float[] weights)
    {
        float[] result = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            result[i] = weights[i] * _expressionIntensity;
        }
        return result;
    }
    
    #endregion

    #region Transitions
    
    private IEnumerator TransitionToWeights(float[] targetWeights, float duration)
    {
        float[] startWeights = (float[])_facePoseProvider!.CurrentWeights.Clone();
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            
            float[] interpolatedWeights = new float[targetWeights.Length];
            for (int i = 0; i < targetWeights.Length; i++)
            {
                interpolatedWeights[i] = Mathf.Lerp(
                    i < startWeights.Length ? startWeights[i] : 0f,
                    targetWeights[i],
                    t
                );
            }
            
            _facePoseProvider.SetWeights(interpolatedWeights);
            yield return null;
        }
        
        _facePoseProvider.SetWeights(targetWeights);
        _transitionCoroutine = null;
    }
    
    #endregion

    #region Blending
    
    /// <summary>
    /// Blends between two expression presets.
    /// </summary>
    public void BlendExpressions(string presetA, string presetB, float blendFactor)
    {
        if (!_presets.TryGetValue(presetA, out var a) || !_presets.TryGetValue(presetB, out var b))
        {
            Debug.LogWarning("[AvatarExpressionControllerV2] One or both presets not found for blending.");
            return;
        }
        
        BlendExpressions(a, b, blendFactor);
    }
    
    /// <summary>
    /// Blends between two expression presets.
    /// </summary>
    public void BlendExpressions(FaceExpressionPreset presetA, FaceExpressionPreset presetB, float blendFactor)
    {
        if (!IsInitialized || presetA == null || presetB == null) return;
        
        blendFactor = Mathf.Clamp01(blendFactor);
        float[] blendedWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        
        for (int i = 0; i < blendedWeights.Length; i++)
        {
            float weightA = i < presetA.ExpressionWeights.Length ? presetA.ExpressionWeights[i] : 0f;
            float weightB = i < presetB.ExpressionWeights.Length ? presetB.ExpressionWeights[i] : 0f;
            blendedWeights[i] = Mathf.Lerp(weightA, weightB, blendFactor) * _expressionIntensity;
        }
        
        _facePoseProvider!.SetWeights(blendedWeights);
    }
    
    #endregion

    #region Testing
    
    /// <summary>
    /// Cycles through all loaded presets for testing purposes.
    /// </summary>
    public void TestAllPresets(float displayDuration = 3f)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AvatarExpressionControllerV2] Cannot test presets - not initialized.");
            return;
        }
        
        StartCoroutine(TestPresetsCoroutine(displayDuration));
    }
    
    private IEnumerator TestPresetsCoroutine(float displayDuration)
    {
        foreach (var kvp in _presets)
        {
            Debug.Log($"[AvatarExpressionControllerV2] Testing preset: {kvp.Key}");
            ApplyExpression(kvp.Value);
            yield return new WaitForSeconds(displayDuration);
        }
        
        ResetToNeutral();
        Debug.Log("[AvatarExpressionControllerV2] Preset testing complete.");
    }
    
    #endregion
}
