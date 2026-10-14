using System.Collections;
using UnityEngine;
using Oculus.Avatar2;

public class NPCController : MonoBehaviour
{
   [Header("References")]
    [SerializeField] private OvrAvatarEntity _avatarEntity;
    [SerializeField] private OvrAvatarLipSyncContext _lipSyncContext;
    [SerializeField] private AudioSource _audioSource;
    
    private AvatarExpressionControllerV2 _expressionController;
    
    private void Start()
    {
        _expressionController = GetComponent<AvatarExpressionControllerV2>();
        StartCoroutine(InitializeWhenReady());
    }
    
    private IEnumerator InitializeWhenReady()
    {
        // Wait for avatar to load
        while (_avatarEntity.IsPendingAvatar)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        
        // Initialize - this handles BOTH expressions AND lip sync!
        _expressionController.Initialize(_avatarEntity, _lipSyncContext);
        _expressionController.MouthBlendMode = MouthBlendMode.Blend;

        //_expressionController.LipSyncBlendWeight = 0.4f;
        StartCoroutine(TestRoutine());
        Debug.Log("NPC Ready!");
    }

    private IEnumerator TestRoutine()
    {
        while (true)
        {
            Speak(_audioSource.clip, "happy");
            yield return new WaitForSeconds(5f);
            Speak(_audioSource.clip, "sad");
            yield return new WaitForSeconds(5f);
            Speak(_audioSource.clip, "angry");
            yield return new WaitForSeconds(5f);
            StopSpeaking();
            yield return new WaitForSeconds(5f);
        }
    }
    
    // Call these from dialogue system, AI, etc.
    
    public void SetHappy() => _expressionController.ApplyExpression(ExpressionPresets.CreateHappy());
    public void SetSad() => _expressionController.ApplyExpression(ExpressionPresets.CreateSad());
    public void SetAngry() => _expressionController.ApplyExpression(ExpressionPresets.CreateAngry());
    public void SetNeutral() => _expressionController.ResetToNeutral();
    
    public void Speak(AudioClip clip, string emotion = "neutral")
    {
        // Play audio - lip sync will automatically animate the mouth
        if (clip != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }
        
        // Set the emotion (eyes, brows, cheeks)
        switch (emotion.ToLower())
        {
            case "happy": SetHappy(); break;
            case "sad": SetSad(); break;
            case "angry": SetAngry(); break;
            default: SetNeutral(); break;
        }
    }
    
    public void StopSpeaking()
    {
        _audioSource.Stop();
        SetNeutral();
    }
}