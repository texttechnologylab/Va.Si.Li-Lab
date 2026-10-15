#nullable enable

using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Factory for creating common facial expression presets programmatically.
/// Uses the Meta Avatar SDK's ovrAvatar2FaceExpression enum for expression indices.
/// </summary>
public static class ExpressionPresets
{
    /// <summary>
    /// Creates a happy/smiling expression.
    /// </summary>
    public static FaceExpressionPreset CreateHappy(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Smile - pull lip corners up
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerL, 0.8f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerR, 0.8f * intensity);
        
        // Cheek raise (Duchenne smile indicator)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.CheekRaiserL, 0.5f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.CheekRaiserR, 0.5f * intensity);
        
        // Slight eye narrowing (genuine smile)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerR, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a sad expression.
    /// </summary>
    public static FaceExpressionPreset CreateSad(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Inner brow raise (sadness indicator)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserL, 0.6f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserR, 0.6f * intensity);
        
        // Lip corner depressor (frown)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerDepressorL, 0.5f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerDepressorR, 0.5f * intensity);
        
        // Slight chin raise
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.ChinRaiserL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.ChinRaiserR, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates an angry expression.
    /// </summary>
    public static FaceExpressionPreset CreateAngry(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Brow lowerer (furrowed brow)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererL, 0.8f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererR, 0.8f * intensity);
        
        // Lid tightener
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerL, 0.5f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerR, 0.5f * intensity);
        
        // Lip tightener
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipTightenerL, 0.4f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipTightenerR, 0.4f * intensity);
        
        // Nose wrinkler
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerL, 0.4f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerR, 0.4f * intensity);
        
        // Upper lip raise
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLipRaiserL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLipRaiserR, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a surprised expression.
    /// </summary>
    public static FaceExpressionPreset CreateSurprised(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Raise outer brows
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserL, 0.8f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserR, 0.8f * intensity);
        
        // Raise inner brows
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserL, 0.7f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserR, 0.7f * intensity);
        
        // Raise upper lids (wide eyes)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLidRaiserL, 0.7f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLidRaiserR, 0.7f * intensity);
        
        // Open mouth
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.JawDrop, 0.5f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a fearful expression.
    /// </summary>
    public static FaceExpressionPreset CreateFear(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Raise inner brows
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserL, 0.8f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserR, 0.8f * intensity);
        
        // Raise outer brows
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserL, 0.5f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserR, 0.5f * intensity);
        
        // Wide eyes
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLidRaiserL, 0.6f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLidRaiserR, 0.6f * intensity);
        
        // Stretch lips horizontally
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipStretcherL, 0.6f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipStretcherR, 0.6f * intensity);
        
        // Slight jaw drop
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.JawDrop, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a disgusted expression.
    /// </summary>
    public static FaceExpressionPreset CreateDisgust(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Nose wrinkle
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerL, 0.8f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerR, 0.8f * intensity);
        
        // Upper lip raise
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLipRaiserL, 0.7f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.UpperLipRaiserR, 0.7f * intensity);
        
        // Brow lowerer
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererL, 0.4f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererR, 0.4f * intensity);
        
        // Chin raise
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.ChinRaiserL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.ChinRaiserR, 0.3f * intensity);
        
        // Lower lip depressor
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LowerLipDepressorL, 0.4f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LowerLipDepressorR, 0.4f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a contempt expression (asymmetric lip corner pull).
    /// </summary>
    public static FaceExpressionPreset CreateContempt(float intensity = 1.0f, bool leftSide = true)
    {
        var preset = new FaceExpressionPreset();
        
        // Asymmetric lip corner pull (smirk)
        if (leftSide)
        {
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerL, 0.6f * intensity);
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.DimplerL, 0.4f * intensity);
        }
        else
        {
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerR, 0.6f * intensity);
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.DimplerR, 0.4f * intensity);
        }
        
        // Slight brow lower
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererL, 0.2f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererR, 0.2f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a thinking/pondering expression.
    /// </summary>
    public static FaceExpressionPreset CreateThinking(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Asymmetric brow raise
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserL, 0.5f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.InnerBrowRaiserR, 0.3f * intensity);
        
        // Slight eye squint
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerL, 0.2f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerR, 0.3f * intensity);
        
        // Lips pressed together
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipPressorL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipPressorR, 0.3f * intensity);
        
        // Look to the side slightly (eyes look up-right)
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesLookUpL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesLookUpR, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesLookRightL, 0.2f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesLookRightR, 0.2f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a skeptical/doubtful expression.
    /// </summary>
    public static FaceExpressionPreset CreateSkeptical(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Asymmetric brow - one raised, one lowered
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.OuterBrowRaiserL, 0.7f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.BrowLowererR, 0.4f * intensity);
        
        // Slight lip press
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipTightenerL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipTightenerR, 0.3f * intensity);
        
        // Slight squint
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LidTightenerR, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a wink expression.
    /// </summary>
    public static FaceExpressionPreset CreateWink(bool leftEye = true, float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        if (leftEye)
        {
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedL, 0.9f * intensity);
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.CheekRaiserL, 0.3f * intensity);
        }
        else
        {
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedR, 0.9f * intensity);
            preset.SetWeight(CAPI.ovrAvatar2FaceExpression.CheekRaiserR, 0.3f * intensity);
        }
        
        // Add a slight smile
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipCornerPullerR, 0.3f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a blink expression.
    /// </summary>
    public static FaceExpressionPreset CreateBlink(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedL, 1.0f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedR, 1.0f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a yawning expression.
    /// </summary>
    public static FaceExpressionPreset CreateYawn(float intensity = 1.0f)
    {
        var preset = new FaceExpressionPreset();
        
        // Wide jaw drop
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.JawDrop, 0.9f * intensity);
        
        // Eyes closing/squinting
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedL, 0.6f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.EyesClosedR, 0.6f * intensity);
        
        // Nose wrinkle
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerL, 0.3f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.NoseWrinklerR, 0.3f * intensity);
        
        // Lip stretcher
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipStretcherL, 0.4f * intensity);
        preset.SetWeight(CAPI.ovrAvatar2FaceExpression.LipStretcherR, 0.4f * intensity);
        
        return preset;
    }
    
    /// <summary>
    /// Creates a neutral/relaxed expression (all weights at zero).
    /// </summary>
    public static FaceExpressionPreset CreateNeutral()
    {
        return new FaceExpressionPreset();
    }
    
    /// <summary>
    /// Creates a custom expression by specifying individual weights.
    /// </summary>
    public static FaceExpressionPreset CreateCustom(params (CAPI.ovrAvatar2FaceExpression expression, float weight)[] weights)
    {
        var preset = new FaceExpressionPreset();
        
        foreach (var (expression, weight) in weights)
        {
            preset.SetWeight(expression, weight);
        }
        
        return preset;
    }
}
