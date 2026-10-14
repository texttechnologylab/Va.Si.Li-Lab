#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Controller for applying facial expressions to Meta Avatars.
/// Works with the current Meta Avatar SDK by implementing a custom face pose provider.
/// Lip sync remains functional when LipSyncCompatible mode is enabled.
/// </summary>
public class AvatarExpressionController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Expression Settings")]
    [SerializeField, Tooltip("Time in seconds to transition between expressions")]
    private float _transitionDuration = 0.3f;
    
    [SerializeField, Tooltip("Global multiplier for all expression weights")]
    [Range(0f, 2f)]
    private float _expressionIntensity = 1.0f;
    
    [SerializeField, Tooltip("When enabled, mouth/lip/jaw expressions are excluded to allow lip sync to work")]
    private bool _lipSyncCompatible = true;
    
    [SerializeField, Tooltip("Additional expressions to exclude from custom control")]
    private List<CAPI.ovrAvatar2FaceExpression> _additionalExcludedExpressions = new();
    
    [Header("Debug")]
    [SerializeField]
    private bool _debugMode = false;
    
    #endregion

    #region Private Fields
    
    private OvrAvatarEntity? _avatarEntity;
    private CustomFacePoseProvider? _facePoseProvider;
    private CustomFacePoseBehavior? _facePoseBehavior;
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
    /// Gets or sets whether lip sync compatible mode is enabled.
    /// When true, mouth/lip/jaw expressions are excluded from custom control.
    /// </summary>
    public bool LipSyncCompatible
    {
        get => _lipSyncCompatible;
        set
        {
            _lipSyncCompatible = value;
            UpdateExcludedExpressions();
        }
    }
    
    /// <summary>
    /// Gets the current expression weights being applied.
    /// </summary>
    public float[] CurrentWeights => _facePoseProvider?.CurrentWeights ?? Array.Empty<float>();
    
    #endregion

    #region Static Helpers
    
    /// <summary>
    /// Returns all mouth, lip, and jaw related expressions that should be excluded for lip sync compatibility.
    /// </summary>
    public static HashSet<CAPI.ovrAvatar2FaceExpression> GetLipSyncExpressions()
    {
        return new HashSet<CAPI.ovrAvatar2FaceExpression>
        {
            // Jaw expressions
            CAPI.ovrAvatar2FaceExpression.JawDrop,
            CAPI.ovrAvatar2FaceExpression.JawSidewaysLeft,
            CAPI.ovrAvatar2FaceExpression.JawSidewaysRight,
            CAPI.ovrAvatar2FaceExpression.JawThrust,
            
            // Lip corner expressions
            CAPI.ovrAvatar2FaceExpression.LipCornerDepressorL,
            CAPI.ovrAvatar2FaceExpression.LipCornerDepressorR,
            CAPI.ovrAvatar2FaceExpression.LipCornerPullerL,
            CAPI.ovrAvatar2FaceExpression.LipCornerPullerR,
            
            // Lip funneler expressions
            CAPI.ovrAvatar2FaceExpression.LipFunnelerLB,
            CAPI.ovrAvatar2FaceExpression.LipFunnelerLT,
            CAPI.ovrAvatar2FaceExpression.LipFunnelerRB,
            CAPI.ovrAvatar2FaceExpression.LipFunnelerRT,
            
            // Lip pressor/pucker expressions
            CAPI.ovrAvatar2FaceExpression.LipPressorL,
            CAPI.ovrAvatar2FaceExpression.LipPressorR,
            CAPI.ovrAvatar2FaceExpression.LipPuckerL,
            CAPI.ovrAvatar2FaceExpression.LipPuckerR,
            
            // Lip stretcher expressions
            CAPI.ovrAvatar2FaceExpression.LipStretcherL,
            CAPI.ovrAvatar2FaceExpression.LipStretcherR,
            
            // Lip suck expressions
            CAPI.ovrAvatar2FaceExpression.LipSuckLB,
            CAPI.ovrAvatar2FaceExpression.LipSuckLT,
            CAPI.ovrAvatar2FaceExpression.LipSuckRB,
            CAPI.ovrAvatar2FaceExpression.LipSuckRT,
            
            // Lip tightener expressions
            CAPI.ovrAvatar2FaceExpression.LipTightenerL,
            CAPI.ovrAvatar2FaceExpression.LipTightenerR,
            
            // Lips toward expressions
            CAPI.ovrAvatar2FaceExpression.LipsTowardLB,
            CAPI.ovrAvatar2FaceExpression.LipsTowardLT,
            CAPI.ovrAvatar2FaceExpression.LipsTowardRB,
            CAPI.ovrAvatar2FaceExpression.LipsTowardRT,
            
            // Lower lip expressions
            CAPI.ovrAvatar2FaceExpression.LowerLipDepressorL,
            CAPI.ovrAvatar2FaceExpression.LowerLipDepressorR,
            
            // Mouth expressions
            CAPI.ovrAvatar2FaceExpression.MouthLeft,
            CAPI.ovrAvatar2FaceExpression.MouthRight,
            
            // Upper lip expressions
            CAPI.ovrAvatar2FaceExpression.UpperLipRaiserL,
            CAPI.ovrAvatar2FaceExpression.UpperLipRaiserR,
            
            // Chin expressions (also affect mouth area)
            CAPI.ovrAvatar2FaceExpression.ChinRaiserL,
            CAPI.ovrAvatar2FaceExpression.ChinRaiserR,
        };
    }
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        LoadPresetsFromResources();
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
    /// </summary>
    /// <param name="avatarEntity">The avatar entity to control expressions for.</param>
    public void Initialize(OvrAvatarEntity avatarEntity)
    {
        if (avatarEntity == null)
        {
            Debug.LogError("[AvatarExpressionController] Cannot initialize with null avatar entity.");
            return;
        }
        
        Cleanup();
        
        _avatarEntity = avatarEntity;
        
        // Build the list of excluded expressions
        var excludedExpressions = BuildExcludedExpressionsList();
        
        // Create the face pose provider and behavior
        _facePoseProvider = new CustomFacePoseProvider(excludedExpressions);
        _facePoseBehavior = gameObject.AddComponent<CustomFacePoseBehavior>();
        _facePoseBehavior.Initialize(_facePoseProvider);
        
        // Set our face pose provider on the avatar entity
        _avatarEntity.SetFacePoseProvider(_facePoseBehavior);
        
        if (_debugMode)
        {
            Debug.Log($"[AvatarExpressionController] Initialized for avatar: {avatarEntity.name}");
            Debug.Log($"[AvatarExpressionController] LipSync compatible: {_lipSyncCompatible}, Excluded expressions: {excludedExpressions.Count}");
        }
    }
    
    /// <summary>
    /// Builds the complete list of excluded expressions based on settings.
    /// </summary>
    private HashSet<CAPI.ovrAvatar2FaceExpression> BuildExcludedExpressionsList()
    {
        var excluded = new HashSet<CAPI.ovrAvatar2FaceExpression>();
        
        // Add lip sync expressions if compatible mode is enabled
        if (_lipSyncCompatible)
        {
            foreach (var expr in GetLipSyncExpressions())
            {
                excluded.Add(expr);
            }
        }
        
        // Add any additional user-specified exclusions
        foreach (var expr in _additionalExcludedExpressions)
        {
            excluded.Add(expr);
        }
        
        return excluded;
    }
    
    /// <summary>
    /// Updates the excluded expressions on the provider (call after changing LipSyncCompatible).
    /// </summary>
    private void UpdateExcludedExpressions()
    {
        if (_facePoseProvider != null)
        {
            _facePoseProvider.SetExcludedExpressions(BuildExcludedExpressionsList());
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
    /// Expects JSON files with OVRPlugin.FaceState format or simple weight arrays.
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
                        Debug.Log($"[AvatarExpressionController] Loaded preset: {presetName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarExpressionController] Failed to load preset {asset.name}: {e.Message}");
            }
        }
        
        Debug.Log($"[AvatarExpressionController] Loaded {_presets.Count} expression presets.");
    }
    
    /// <summary>
    /// Extracts the preset name from the file name (removes suffix after underscore).
    /// </summary>
    private string ExtractPresetName(string fileName)
    {
        int underscoreIndex = fileName.IndexOf('_');
        return underscoreIndex > 0 ? fileName.Substring(0, underscoreIndex) : fileName;
    }
    
    /// <summary>
    /// Registers a custom expression preset at runtime.
    /// </summary>
    /// <param name="name">The name to identify this preset.</param>
    /// <param name="preset">The preset containing expression weights.</param>
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
    /// </summary>
    /// <param name="presetName">The name of the preset to apply.</param>
    /// <param name="transitionTime">Optional override for transition duration. Use -1 to use default.</param>
    public void ApplyExpression(string presetName, float transitionTime = -1f)
    {
        if (!_presets.TryGetValue(presetName, out var preset))
        {
            Debug.LogWarning($"[AvatarExpressionController] Expression preset '{presetName}' not found.");
            return;
        }
        
        ApplyExpression(preset, transitionTime);
    }
    
    /// <summary>
    /// Applies a face expression preset to the avatar.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <param name="transitionTime">Optional override for transition duration. Use -1 to use default.</param>
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
            Debug.Log($"[AvatarExpressionController] Applying expression with {targetWeights.Length} weights, duration: {duration}s");
        }
    }
    
    /// <summary>
    /// Applies raw expression weights to the avatar.
    /// </summary>
    /// <param name="weights">Array of expression weights (should be 72 elements for ovrAvatar2FaceExpression.Count).</param>
    /// <param name="transitionTime">Optional override for transition duration. Use -1 to use default.</param>
    public void ApplyWeights(float[] weights, float transitionTime = -1f)
    {
        if (!IsInitialized || weights == null) return;
        
        var preset = new FaceExpressionPreset(weights);
        ApplyExpression(preset, transitionTime);
    }
    
    /// <summary>
    /// Sets a single expression weight.
    /// </summary>
    /// <param name="expression">The expression to set.</param>
    /// <param name="weight">The weight value (0-1).</param>
    public void SetExpressionWeight(CAPI.ovrAvatar2FaceExpression expression, float weight)
    {
        if (!IsInitialized) return;
        _facePoseProvider!.SetWeight(expression, weight * _expressionIntensity);
    }
    
    /// <summary>
    /// Resets all expressions to neutral (all weights to 0).
    /// </summary>
    /// <param name="transitionTime">Optional override for transition duration. Use -1 to use default.</param>
    public void ResetToNeutral(float transitionTime = -1f)
    {
        if (!IsInitialized) return;
        
        float[] neutralWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        ApplyWeights(neutralWeights, transitionTime);
    }
    
    /// <summary>
    /// Applies intensity multiplier to weights.
    /// </summary>
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
    
    /// <summary>
    /// Smoothly transitions from current weights to target weights.
    /// </summary>
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
        
        // Ensure we end exactly at target
        _facePoseProvider.SetWeights(targetWeights);
        _transitionCoroutine = null;
    }
    
    #endregion

    #region Blending
    
    /// <summary>
    /// Blends between two expression presets.
    /// </summary>
    /// <param name="presetA">First preset name.</param>
    /// <param name="presetB">Second preset name.</param>
    /// <param name="blendFactor">Blend factor (0 = presetA, 1 = presetB).</param>
    public void BlendExpressions(string presetA, string presetB, float blendFactor)
    {
        if (!_presets.TryGetValue(presetA, out var a) || !_presets.TryGetValue(presetB, out var b))
        {
            Debug.LogWarning("[AvatarExpressionController] One or both presets not found for blending.");
            return;
        }
        
        BlendExpressions(a, b, blendFactor);
    }
    
    /// <summary>
    /// Blends between two expression presets.
    /// </summary>
    /// <param name="presetA">First preset.</param>
    /// <param name="presetB">Second preset.</param>
    /// <param name="blendFactor">Blend factor (0 = presetA, 1 = presetB).</param>
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
    /// <param name="displayDuration">How long to show each expression.</param>
    public void TestAllPresets(float displayDuration = 3f)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[AvatarExpressionController] Cannot test presets - not initialized.");
            return;
        }
        
        StartCoroutine(TestPresetsCoroutine(displayDuration));
    }
    
    private IEnumerator TestPresetsCoroutine(float displayDuration)
    {
        foreach (var kvp in _presets)
        {
            Debug.Log($"[AvatarExpressionController] Testing preset: {kvp.Key}");
            ApplyExpression(kvp.Value);
            yield return new WaitForSeconds(displayDuration);
        }
        
        ResetToNeutral();
        Debug.Log("[AvatarExpressionController] Preset testing complete.");
    }
    
    #endregion
}

/// <summary>
/// Custom face pose provider that allows setting expression weights externally.
/// Properly excludes mouth/lip expressions to allow lip sync to work.
/// </summary>
public class CustomFacePoseProvider : OvrAvatarFacePoseProviderBase
{
    private readonly float[] _currentWeights;
    private HashSet<CAPI.ovrAvatar2FaceExpression> _excludedExpressions;
    private readonly object _lock = new();
    private bool _isDisposed;
    
    public float[] CurrentWeights
    {
        get
        {
            lock (_lock)
            {
                return (float[])_currentWeights.Clone();
            }
        }
    }
    
    public CustomFacePoseProvider(HashSet<CAPI.ovrAvatar2FaceExpression>? excludedExpressions = null)
    {
        _currentWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        _excludedExpressions = excludedExpressions ?? new HashSet<CAPI.ovrAvatar2FaceExpression>();
    }
    
    /// <summary>
    /// Updates the set of excluded expressions.
    /// </summary>
    public void SetExcludedExpressions(HashSet<CAPI.ovrAvatar2FaceExpression> excluded)
    {
        lock (_lock)
        {
            _excludedExpressions = excluded ?? new HashSet<CAPI.ovrAvatar2FaceExpression>();
            
            // Clear weights for newly excluded expressions
            foreach (var expr in _excludedExpressions)
            {
                int index = (int)expr;
                if (index >= 0 && index < _currentWeights.Length)
                {
                    _currentWeights[index] = 0f;
                }
            }
        }
    }
    
    /// <summary>
    /// Sets all expression weights at once (respecting exclusions).
    /// </summary>
    public void SetWeights(float[] weights)
    {
        if (weights == null || _isDisposed) return;
        
        lock (_lock)
        {
            int count = Mathf.Min(weights.Length, _currentWeights.Length);
            for (int i = 0; i < count; i++)
            {
                if (!_excludedExpressions.Contains((CAPI.ovrAvatar2FaceExpression)i))
                {
                    _currentWeights[i] = weights[i];
                }
            }
        }
    }
    
    /// <summary>
    /// Sets a single expression weight (respecting exclusions).
    /// </summary>
    public void SetWeight(CAPI.ovrAvatar2FaceExpression expression, float weight)
    {
        if (_isDisposed) return;
        
        int index = (int)expression;
        if (index < 0 || index >= _currentWeights.Length) return;
        if (_excludedExpressions.Contains(expression)) return;
        
        lock (_lock)
        {
            _currentWeights[index] = weight;
        }
    }
    
    /// <summary>
    /// Called by the Avatar SDK to get current face pose data.
    /// For excluded expressions (mouth/lip for lip sync), we return 0 weight
    /// so the lip sync system's visemes can control them instead.
    /// </summary>
    protected override bool GetFacePose(OvrAvatarFacePose facePose)
    {
        if (_isDisposed) return false;
        
        lock (_lock)
        {
            for (int i = 0; i < _currentWeights.Length; i++)
            {
                // For excluded expressions, don't set any weight - let lip sync handle them
                if (_excludedExpressions.Contains((CAPI.ovrAvatar2FaceExpression)i))
                {
                    facePose.expressionWeights[i] = 0f;
                    facePose.expressionConfidence[i] = 0f; // Zero confidence = SDK ignores this
                }
                else
                {
                    facePose.expressionWeights[i] = _currentWeights[i];
                    facePose.expressionConfidence[i] = 1f;
                }
            }
        }
        
        return true;
    }
    
    public void Dispose()
    {
        _isDisposed = true;
    }
}

/// <summary>
/// MonoBehaviour wrapper for the custom face pose provider.
/// Required for the Avatar SDK's SetFacePoseProvider method.
/// </summary>
public class CustomFacePoseBehavior : OvrAvatarFacePoseBehavior
{
    private CustomFacePoseProvider? _provider;
    
    public override OvrAvatarFacePoseProviderBase FacePoseProvider => _provider!;
    
    public void Initialize(CustomFacePoseProvider provider)
    {
        _provider = provider;
    }
}

/// <summary>
/// Represents a set of face expression weights that can be applied to an avatar.
/// Can be loaded from JSON or created programmatically.
/// </summary>
[System.Serializable]
public class FaceExpressionPreset
{
    [SerializeField]
    private float[] _expressionWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
    
    /// <summary>
    /// The expression weights for this preset.
    /// Array of 72 floats corresponding to ovrAvatar2FaceExpression values.
    /// </summary>
    public float[] ExpressionWeights => _expressionWeights;
    
    public FaceExpressionPreset()
    {
        _expressionWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
    }
    
    public FaceExpressionPreset(float[] weights)
    {
        _expressionWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        if (weights != null)
        {
            int count = Mathf.Min(weights.Length, _expressionWeights.Length);
            Array.Copy(weights, _expressionWeights, count);
        }
    }
    
    /// <summary>
    /// Creates a preset from JSON data.
    /// Supports both direct weight arrays and OVRPlugin.FaceState format.
    /// </summary>
    public static FaceExpressionPreset? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        
        try
        {
            // Try parsing as OVRPlugin.FaceState format first (for backward compatibility)
            var faceStateWrapper = JsonUtility.FromJson<FaceStateWrapper>(json);
            if (faceStateWrapper?.ExpressionWeights != null && faceStateWrapper.ExpressionWeights.Length > 0)
            {
                return new FaceExpressionPreset(faceStateWrapper.ExpressionWeights);
            }
            
            // Try parsing as direct preset format
            var preset = JsonUtility.FromJson<FaceExpressionPreset>(json);
            if (preset != null)
            {
                return preset;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FaceExpressionPreset] Failed to parse JSON: {e.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Converts the preset to JSON format.
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// Sets a specific expression weight.
    /// </summary>
    public void SetWeight(CAPI.ovrAvatar2FaceExpression expression, float weight)
    {
        int index = (int)expression;
        if (index >= 0 && index < _expressionWeights.Length)
        {
            _expressionWeights[index] = weight;
        }
    }
    
    /// <summary>
    /// Gets a specific expression weight.
    /// </summary>
    public float GetWeight(CAPI.ovrAvatar2FaceExpression expression)
    {
        int index = (int)expression;
        if (index >= 0 && index < _expressionWeights.Length)
        {
            return _expressionWeights[index];
        }
        return 0f;
    }
    
    /// <summary>
    /// Helper class for parsing OVRPlugin.FaceState JSON format.
    /// </summary>
    [System.Serializable]
    private class FaceStateWrapper
    {
        public float[]? ExpressionWeights;
        public float[]? ExpressionWeightConfidences;
    }
}
