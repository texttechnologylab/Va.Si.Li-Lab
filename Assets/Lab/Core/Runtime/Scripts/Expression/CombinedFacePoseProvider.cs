#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Defines how mouth expressions are blended between custom expressions and lip sync.
/// </summary>
public enum MouthBlendMode
{
    /// <summary>Only lip sync controls mouth (custom mouth expressions ignored)</summary>
    LipSyncOnly,
    /// <summary>Only custom expressions control mouth (lip sync ignored for mouth)</summary>
    CustomOnly,
    /// <summary>Blend between custom and lip sync using LipSyncBlendWeight</summary>
    Blend,
    /// <summary>Take the maximum of custom and lip sync for each expression</summary>
    Max,
    /// <summary>Add custom and lip sync together (clamped to 1.0)</summary>
    Additive,
    /// <summary>Add custom and lip sync together with being able to adjust the percentage of custom face state value effect</summary>
    AdditivePercentage
}

/// <summary>
/// A face pose provider that combines custom expressions with lip sync visemes.
/// This provider queries the lip sync system and merges mouth movements with 
/// custom expressions for eyes, brows, and other non-mouth areas.
/// Mouth expressions can be blended between custom and lip sync using a configurable ratio.
/// </summary>
public class CombinedFacePoseProvider : OvrAvatarFacePoseProviderBase
{
    private readonly float[] _customWeights;
    private readonly float[] _visemeToExpressionWeights;
    private readonly HashSet<int> _mouthExpressionIndices;
    private readonly object _lock = new();
    private bool _isDisposed;
    
    // Reference to lip sync for querying visemes
    private OvrAvatarLipSyncBehavior? _lipSyncBehavior;
    private OvrAvatarEntity? _avatarEntity;
    
    // Blend settings
    private float _lipSyncBlendWeight = 0.7f; // Default: 70% lip sync, 30% custom for mouth
    private MouthBlendMode _blendMode = MouthBlendMode.Blend;
    
    /// <summary>
    /// Gets or sets how mouth expressions are blended between custom and lip sync.
    /// </summary>
    public MouthBlendMode BlendMode
    {
        get => _blendMode;
        set => _blendMode = value;
    }
    
    /// <summary>
    /// Gets or sets the blend weight between custom expressions and lip sync for mouth.
    /// 0 = 100% custom expressions, 1 = 100% lip sync.
    /// Default is 0.7 (70% lip sync, 30% custom).
    /// Only applies when BlendMode is set to Blend.
    /// </summary>
    public float LipSyncBlendWeight
    {
        get => _lipSyncBlendWeight;
        set => _lipSyncBlendWeight = Mathf.Clamp01(value);
    }
    
    // Viseme to expression mapping weights
    private static readonly Dictionary<CAPI.ovrAvatar2Viseme, Dictionary<CAPI.ovrAvatar2FaceExpression, float>> VisemeToExpressionMap = new()
    {
        // Silent - neutral mouth
        { CAPI.ovrAvatar2Viseme.sil, new Dictionary<CAPI.ovrAvatar2FaceExpression, float>() },
        
        // PP (p, b, m) - lips pressed together
        { CAPI.ovrAvatar2Viseme.PP, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.LipPressorL, 0.8f },
            { CAPI.ovrAvatar2FaceExpression.LipPressorR, 0.8f },
        }},
        
        // FF (f, v) - lower lip tucked under teeth
        { CAPI.ovrAvatar2Viseme.FF, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.LipSuckLB, 0.6f },
            { CAPI.ovrAvatar2FaceExpression.LipSuckRB, 0.6f },
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.1f },
        }},
        
        // TH (th) - tongue between teeth
        { CAPI.ovrAvatar2Viseme.TH, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.15f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLT, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRT, 0.3f },
        }},
        
        // DD (t, d) - tongue touches roof
        { CAPI.ovrAvatar2Viseme.DD, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.2f },
        }},
        
        // kk (k, g) - back of tongue up
        { CAPI.ovrAvatar2Viseme.kk, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.25f },
        }},
        
        // CH (ch, j, sh) - lips rounded forward
        { CAPI.ovrAvatar2Viseme.CH, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.LipPuckerL, 0.5f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerR, 0.5f },
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.2f },
        }},
        
        // SS (s, z) - teeth together, lips slightly parted
        { CAPI.ovrAvatar2Viseme.SS, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.LipStretcherL, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipStretcherR, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.1f },
        }},
        
        // nn (n, l) - tongue tip up
        { CAPI.ovrAvatar2Viseme.nn, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.15f },
        }},
        
        // RR (r) - lips slightly rounded
        { CAPI.ovrAvatar2Viseme.RR, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.LipPuckerL, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerR, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.2f },
        }},
        
        // aa (a as in "car") - mouth wide open
        { CAPI.ovrAvatar2Viseme.aa, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.7f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLB, 0.2f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRB, 0.2f },
        }},
        
        // E (e as in "bed") - mouth medium open, spread
        { CAPI.ovrAvatar2Viseme.E, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.35f },
            { CAPI.ovrAvatar2FaceExpression.LipStretcherL, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipStretcherR, 0.4f },
        }},
        
        // ih (i as in "tip") - mouth slightly open, spread
        { CAPI.ovrAvatar2Viseme.ih, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.2f },
            { CAPI.ovrAvatar2FaceExpression.LipStretcherL, 0.5f },
            { CAPI.ovrAvatar2FaceExpression.LipStretcherR, 0.5f },
        }},
        
        // oh (o as in "toe") - lips rounded
        { CAPI.ovrAvatar2Viseme.oh, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerL, 0.5f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerR, 0.5f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLT, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRT, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLB, 0.3f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRB, 0.3f },
        }},
        
        // ou (oo as in "book") - lips tightly rounded
        { CAPI.ovrAvatar2Viseme.ou, new Dictionary<CAPI.ovrAvatar2FaceExpression, float> {
            { CAPI.ovrAvatar2FaceExpression.JawDrop, 0.25f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerL, 0.7f },
            { CAPI.ovrAvatar2FaceExpression.LipPuckerR, 0.7f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLT, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRT, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerLB, 0.4f },
            { CAPI.ovrAvatar2FaceExpression.LipFunnelerRB, 0.4f },
        }},
    };
    
    public float[] CurrentWeights
    {
        get
        {
            lock (_lock)
            {
                return (float[])_customWeights.Clone();
            }
        }
    }
    
    public CombinedFacePoseProvider()
    {
        _customWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        _visemeToExpressionWeights = new float[(int)CAPI.ovrAvatar2FaceExpression.Count];
        _mouthExpressionIndices = GetMouthExpressionIndices();
    }
    
    /// <summary>
    /// Sets the lip sync behavior to query for viseme data.
    /// </summary>
    public void SetLipSyncSource(OvrAvatarLipSyncBehavior? lipSync, OvrAvatarEntity? entity)
    {
        _lipSyncBehavior = lipSync;
        _avatarEntity = entity;
    }
    
    /// <summary>
    /// Gets all expression indices that are controlled by mouth/lip movements.
    /// </summary>
    private static HashSet<int> GetMouthExpressionIndices()
    {
        return new HashSet<int>
        {
            (int)CAPI.ovrAvatar2FaceExpression.JawDrop,
            (int)CAPI.ovrAvatar2FaceExpression.JawSidewaysLeft,
            (int)CAPI.ovrAvatar2FaceExpression.JawSidewaysRight,
            (int)CAPI.ovrAvatar2FaceExpression.JawThrust,
            (int)CAPI.ovrAvatar2FaceExpression.LipCornerDepressorL,
            (int)CAPI.ovrAvatar2FaceExpression.LipCornerDepressorR,
            (int)CAPI.ovrAvatar2FaceExpression.LipCornerPullerL,
            (int)CAPI.ovrAvatar2FaceExpression.LipCornerPullerR,
            (int)CAPI.ovrAvatar2FaceExpression.LipFunnelerLB,
            (int)CAPI.ovrAvatar2FaceExpression.LipFunnelerLT,
            (int)CAPI.ovrAvatar2FaceExpression.LipFunnelerRB,
            (int)CAPI.ovrAvatar2FaceExpression.LipFunnelerRT,
            (int)CAPI.ovrAvatar2FaceExpression.LipPressorL,
            (int)CAPI.ovrAvatar2FaceExpression.LipPressorR,
            (int)CAPI.ovrAvatar2FaceExpression.LipPuckerL,
            (int)CAPI.ovrAvatar2FaceExpression.LipPuckerR,
            (int)CAPI.ovrAvatar2FaceExpression.LipStretcherL,
            (int)CAPI.ovrAvatar2FaceExpression.LipStretcherR,
            (int)CAPI.ovrAvatar2FaceExpression.LipSuckLB,
            (int)CAPI.ovrAvatar2FaceExpression.LipSuckLT,
            (int)CAPI.ovrAvatar2FaceExpression.LipSuckRB,
            (int)CAPI.ovrAvatar2FaceExpression.LipSuckRT,
            (int)CAPI.ovrAvatar2FaceExpression.LipTightenerL,
            (int)CAPI.ovrAvatar2FaceExpression.LipTightenerR,
            (int)CAPI.ovrAvatar2FaceExpression.LipsTowardLB,
            (int)CAPI.ovrAvatar2FaceExpression.LipsTowardLT,
            (int)CAPI.ovrAvatar2FaceExpression.LipsTowardRB,
            (int)CAPI.ovrAvatar2FaceExpression.LipsTowardRT,
            (int)CAPI.ovrAvatar2FaceExpression.LowerLipDepressorL,
            (int)CAPI.ovrAvatar2FaceExpression.LowerLipDepressorR,
            (int)CAPI.ovrAvatar2FaceExpression.MouthLeft,
            (int)CAPI.ovrAvatar2FaceExpression.MouthRight,
            (int)CAPI.ovrAvatar2FaceExpression.UpperLipRaiserL,
            (int)CAPI.ovrAvatar2FaceExpression.UpperLipRaiserR,
            (int)CAPI.ovrAvatar2FaceExpression.ChinRaiserL,
            (int)CAPI.ovrAvatar2FaceExpression.ChinRaiserR,
        };
    }
    
    /// <summary>
    /// Sets custom expression weights (including mouth expressions for blending).
    /// </summary>
    public void SetWeights(float[] weights)
    {
        if (weights == null || _isDisposed) return;
        
        lock (_lock)
        {
            int count = Mathf.Min(weights.Length, _customWeights.Length);
            for (int i = 0; i < count; i++)
            {
                _customWeights[i] = weights[i];
            }
        }
    }
    
    /// <summary>
    /// Sets a single expression weight (including mouth expressions for blending).
    /// </summary>
    public void SetWeight(CAPI.ovrAvatar2FaceExpression expression, float weight)
    {
        if (_isDisposed) return;
        
        int index = (int)expression;
        if (index < 0 || index >= _customWeights.Length) return;
        
        lock (_lock)
        {
            _customWeights[index] = weight;
        }
    }
    
    /// <summary>
    /// Converts current viseme values to expression weights.
    /// </summary>
    private void UpdateVisemeExpressions()
    {
        // Clear previous viseme weights
        Array.Clear(_visemeToExpressionWeights, 0, _visemeToExpressionWeights.Length);
        
        if (_lipSyncBehavior == null) return;
        
        // Get current lip sync state
        var lipSyncContext = _lipSyncBehavior.LipSyncContext;
        if (lipSyncContext == null) return;
        
        var lipSyncState = lipSyncContext.DebugQueryLipSyncState();
        if (lipSyncState == null) return;
        
        // Convert visemes to expression weights
        for (int visemeIdx = 0; visemeIdx < (int)CAPI.ovrAvatar2Viseme.Count; visemeIdx++)
        {
            var viseme = (CAPI.ovrAvatar2Viseme)visemeIdx;
            float visemeWeight = lipSyncState.visemes[visemeIdx];
            
            if (visemeWeight <= 0f) continue;
            
            if (VisemeToExpressionMap.TryGetValue(viseme, out var expressionWeights))
            {
                foreach (var kvp in expressionWeights)
                {
                    int exprIdx = (int)kvp.Key;
                    // Additive blending for visemes
                    _visemeToExpressionWeights[exprIdx] += visemeWeight * kvp.Value;
                }
            }
        }
        
        // Clamp all weights to 0-1
        for (int i = 0; i < _visemeToExpressionWeights.Length; i++)
        {
            _visemeToExpressionWeights[i] = Mathf.Clamp01(_visemeToExpressionWeights[i]);
        }
    }
    
    /// <summary>
    /// Called by the Avatar SDK to get combined face pose data.
    /// </summary>
    protected override bool GetFacePose(OvrAvatarFacePose facePose)
    {
        if (_isDisposed) return false;
        
        // Update viseme-based expressions
        UpdateVisemeExpressions();
        
        lock (_lock)
        {
            for (int i = 0; i < _customWeights.Length; i++)
            {
                float finalWeight;
                
                if (_mouthExpressionIndices.Contains(i))
                {
                    // Apply blend mode for mouth expressions
                    float customWeight = _customWeights[i];
                    float lipSyncWeight = _visemeToExpressionWeights[i];
                    
                    finalWeight = _blendMode switch
                    {
                        MouthBlendMode.LipSyncOnly => lipSyncWeight,
                        MouthBlendMode.CustomOnly => customWeight,
                        MouthBlendMode.Blend => Mathf.Lerp(customWeight, lipSyncWeight, _lipSyncBlendWeight),
                        MouthBlendMode.Max => Mathf.Max(customWeight, lipSyncWeight),
                        MouthBlendMode.Additive => Mathf.Clamp01(customWeight + lipSyncWeight),
                        MouthBlendMode.AdditivePercentage => Mathf.Clamp01(customWeight + lipSyncWeight - (lipSyncWeight * _lipSyncBlendWeight)),
                        _ => lipSyncWeight
                    };
                }
                else
                {
                    // Non-mouth expressions come directly from custom weights
                    finalWeight = _customWeights[i];
                }
                
                facePose.expressionWeights[i] = finalWeight;
                facePose.expressionConfidence[i] = 1f;
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
/// MonoBehaviour wrapper for the combined face pose provider.
/// </summary>
public class CombinedFacePoseBehavior : OvrAvatarFacePoseBehavior
{
    private CombinedFacePoseProvider? _provider;
    
    public override OvrAvatarFacePoseProviderBase FacePoseProvider => _provider!;
    
    public CombinedFacePoseProvider? Provider => _provider;
    
    public void Initialize(CombinedFacePoseProvider provider)
    {
        _provider = provider;
    }
}
