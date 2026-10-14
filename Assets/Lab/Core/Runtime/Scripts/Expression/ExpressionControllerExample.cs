#nullable enable

using System.Collections;
using UnityEngine;
using Oculus.Avatar2;

/// <summary>
/// Example script demonstrating how to use the AvatarExpressionController with Lip Sync.
/// Attach this to the same GameObject as your OvrAvatarEntity or a parent.
/// </summary>
public class ExpressionControllerExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private OvrAvatarEntity? _avatarEntity;
    
    [SerializeField]
    private OvrAvatarLipSyncBehavior? _lipSyncBehavior;
    
    [Header("Settings")]
    [SerializeField] 
    private bool _initializeOnStart = true;
    
    [SerializeField] 
    private bool _runDemoOnStart = false;
    
    [SerializeField, Tooltip("Enable to allow lip sync to control mouth expressions")]
    private bool _enableLipSync = true;
    
    private AvatarExpressionController? _expressionController;
    
    private void Start()
    {
        // Get or create the expression controller
        _expressionController = GetComponent<AvatarExpressionController>();
        if (_expressionController == null)
        {
            _expressionController = gameObject.AddComponent<AvatarExpressionController>();
        }
        
        // Try to find the avatar entity if not assigned
        if (_avatarEntity == null)
        {
            _avatarEntity = GetComponent<OvrAvatarEntity>();
            if (_avatarEntity == null)
            {
                _avatarEntity = GetComponentInChildren<OvrAvatarEntity>();
            }
        }
        
        // Try to find lip sync behavior if not assigned
        if (_lipSyncBehavior == null)
        {
            _lipSyncBehavior = GetComponent<OvrAvatarLipSyncBehavior>();
            if (_lipSyncBehavior == null)
            {
                _lipSyncBehavior = GetComponentInChildren<OvrAvatarLipSyncBehavior>();
            }
        }
        
        if (_initializeOnStart && _avatarEntity != null)
        {
            // Wait for avatar to load before initializing
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
        
        // Additional wait to ensure everything is set up
        yield return new WaitForSeconds(0.5f);
        
        // IMPORTANT: Set lip sync compatible mode BEFORE initializing
        _expressionController!.LipSyncCompatible = _enableLipSync;
        
        // Initialize the expression controller
        _expressionController.Initialize(_avatarEntity);
        
        // Set up lip sync on the avatar entity (this is separate from face expressions)
        if (_enableLipSync && _lipSyncBehavior != null)
        {
            _avatarEntity.SetLipSync(_lipSyncBehavior);
            Debug.Log("[ExpressionControllerExample] Lip sync enabled!");
        }
        
        Debug.Log("[ExpressionControllerExample] Expression controller initialized!");
        
        if (_runDemoOnStart)
        {
            StartCoroutine(DemoExpressions());
        }
    }
    
    /// <summary>
    /// Demonstrates various expressions.
    /// Note: Mouth will still move with lip sync while these expressions are applied!
    /// </summary>
    private IEnumerator DemoExpressions()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Demo: Happy (lip sync still works for mouth!)");
        _expressionController!.ApplyExpression(ExpressionPresets.CreateHappy());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Sad");
        _expressionController.ApplyExpression(ExpressionPresets.CreateSad());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Angry");
        _expressionController.ApplyExpression(ExpressionPresets.CreateAngry());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Surprised");
        _expressionController.ApplyExpression(ExpressionPresets.CreateSurprised());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Fear");
        _expressionController.ApplyExpression(ExpressionPresets.CreateFear());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Disgust");
        _expressionController.ApplyExpression(ExpressionPresets.CreateDisgust());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Thinking");
        _expressionController.ApplyExpression(ExpressionPresets.CreateThinking());
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Demo: Wink");
        _expressionController.ApplyExpression(ExpressionPresets.CreateWink());
        yield return new WaitForSeconds(1.5f);
        
        Debug.Log("Demo: Neutral");
        _expressionController.ResetToNeutral();
        
        Debug.Log("Demo complete!");
    }
    
    // Public methods for testing from inspector or other scripts
    
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
    
    [ContextMenu("Test All Presets")]
    public void TestAllPresets() => _expressionController?.TestAllPresets();
    
    [ContextMenu("Run Demo")]
    public void RunDemo() => StartCoroutine(DemoExpressions());
    
    /// <summary>
    /// Apply an expression by name (for use with UI buttons, events, etc.)
    /// </summary>
    public void ApplyExpressionByName(string presetName)
    {
        _expressionController?.ApplyExpression(presetName);
    }
    
    /// <summary>
    /// Blend between two expressions.
    /// </summary>
    public void BlendHappySad(float blend)
    {
        _expressionController?.BlendExpressions(
            ExpressionPresets.CreateHappy(),
            ExpressionPresets.CreateSad(),
            blend
        );
    }
}
