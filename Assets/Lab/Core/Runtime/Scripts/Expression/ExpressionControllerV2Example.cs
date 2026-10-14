#nullable enable

using System.Collections;
using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Example script showing how to use AvatarExpressionControllerV2 with lip sync.
/// This version properly combines custom expressions with lip sync by manually
/// converting visemes to expressions and merging them.
/// </summary>
public class ExpressionControllerV2Example : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private OvrAvatarEntity? _avatarEntity;
    
    [SerializeField]
    private OvrAvatarLipSyncBehavior? _lipSyncBehavior;
    
    [SerializeField]
    private AudioSource? _audioSource;
    
    [Header("Settings")]
    [SerializeField] 
    private bool _initializeOnStart = true;
    
    [SerializeField] 
    private bool _runDemoOnStart = false;
    
    [Header("Test Audio")]
    [SerializeField]
    private AudioClip? _testAudioClip;
    
    private AvatarExpressionControllerV2? _expressionController;
    
    private void Start()
    {
        // Get or create the expression controller
        _expressionController = GetComponent<AvatarExpressionControllerV2>();
        if (_expressionController == null)
        {
            _expressionController = gameObject.AddComponent<AvatarExpressionControllerV2>();
        }
        
        // Try to find components if not assigned
        if (_avatarEntity == null)
        {
            _avatarEntity = GetComponent<OvrAvatarEntity>();
            if (_avatarEntity == null)
            {
                _avatarEntity = GetComponentInChildren<OvrAvatarEntity>();
            }
        }
        
        if (_lipSyncBehavior == null)
        {
            _lipSyncBehavior = GetComponent<OvrAvatarLipSyncBehavior>();
            if (_lipSyncBehavior == null)
            {
                _lipSyncBehavior = GetComponentInChildren<OvrAvatarLipSyncBehavior>();
            }
        }
        
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>();
            }
        }
        
        if (_initializeOnStart && _avatarEntity != null)
        {
            StartCoroutine(InitializeWhenReady());
        }
    }
    
    private IEnumerator InitializeWhenReady()
    {
        // Wait until the avatar is fully loaded
        while (_avatarEntity!.IsPendingAvatar)
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Initialize the V2 controller with lip sync
        // IMPORTANT: This controller handles both expressions AND lip sync internally!
        // Do NOT call _avatarEntity.SetLipSync() separately!
        _expressionController!.Initialize(_avatarEntity, _lipSyncBehavior);
        
        Debug.Log("[ExpressionControllerV2Example] Controller initialized!");
        Debug.Log("Lip sync is now handled internally by the combined face pose provider.");
        
        if (_runDemoOnStart)
        {
            StartCoroutine(DemoExpressionsWithSpeech());
        }
    }
    
    /// <summary>
    /// Demonstrates expressions while audio is playing.
    /// The mouth will animate from lip sync while eyes/brows show the expression!
    /// </summary>
    private IEnumerator DemoExpressionsWithSpeech()
    {
        yield return new WaitForSeconds(1f);
        
        // Start playing audio for lip sync
        if (_audioSource != null && _testAudioClip != null)
        {
            _audioSource.clip = _testAudioClip;
            _audioSource.loop = true;
            _audioSource.Play();
            Debug.Log("Playing audio for lip sync...");
        }
        
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("Demo: Happy - watch the eyes/brows while mouth moves with speech!");
        _expressionController!.ApplyExpression(ExpressionPresets.CreateHappy());
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Demo: Sad");
        _expressionController.ApplyExpression(ExpressionPresets.CreateSad());
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Demo: Angry");
        _expressionController.ApplyExpression(ExpressionPresets.CreateAngry());
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Demo: Surprised");
        _expressionController.ApplyExpression(ExpressionPresets.CreateSurprised());
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Demo: Thinking");
        _expressionController.ApplyExpression(ExpressionPresets.CreateThinking());
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Demo: Neutral");
        _expressionController.ResetToNeutral();
        yield return new WaitForSeconds(2f);
        
        // Stop audio
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
        
        Debug.Log("Demo complete! Lip sync should have worked throughout.");
    }
    
    // Public methods for testing
    
    [ContextMenu("Play Test Audio")]
    public void PlayTestAudio()
    {
        if (_audioSource != null && _testAudioClip != null)
        {
            _audioSource.clip = _testAudioClip;
            _audioSource.Play();
        }
    }
    
    [ContextMenu("Stop Audio")]
    public void StopAudio()
    {
        _audioSource?.Stop();
    }
    
    [ContextMenu("Apply Happy Expression")]
    public void ApplyHappy() => _expressionController?.ApplyExpression(ExpressionPresets.CreateHappy());
    
    [ContextMenu("Apply Sad Expression")]
    public void ApplySad() => _expressionController?.ApplyExpression(ExpressionPresets.CreateSad());
    
    [ContextMenu("Apply Angry Expression")]
    public void ApplyAngry() => _expressionController?.ApplyExpression(ExpressionPresets.CreateAngry());
    
    [ContextMenu("Apply Surprised Expression")]
    public void ApplySurprised() => _expressionController?.ApplyExpression(ExpressionPresets.CreateSurprised());
    
    [ContextMenu("Apply Neutral Expression")]
    public void ApplyNeutral() => _expressionController?.ResetToNeutral();
    
    [ContextMenu("Run Demo With Speech")]
    public void RunDemo() => StartCoroutine(DemoExpressionsWithSpeech());
    
    /// <summary>
    /// Speaks with an emotion - plays audio and sets expression.
    /// </summary>
    public void SpeakWithEmotion(AudioClip clip, string emotion)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }
        
        switch (emotion.ToLower())
        {
            case "happy":
                _expressionController?.ApplyExpression(ExpressionPresets.CreateHappy());
                break;
            case "sad":
                _expressionController?.ApplyExpression(ExpressionPresets.CreateSad());
                break;
            case "angry":
                _expressionController?.ApplyExpression(ExpressionPresets.CreateAngry());
                break;
            case "surprised":
                _expressionController?.ApplyExpression(ExpressionPresets.CreateSurprised());
                break;
            case "fear":
                _expressionController?.ApplyExpression(ExpressionPresets.CreateFear());
                break;
            default:
                _expressionController?.ResetToNeutral();
                break;
        }
    }
}
