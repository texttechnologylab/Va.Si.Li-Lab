#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using Oculus.Avatar2;
using UnityEngine;
using static OvrAvatarSkinnedRenderable;

/// <summary>
/// Work in progress and alternative to: ExpressionHandler.cs .
/// Represents an NPC (Non-Player Character) whose facial expressions are controlled using data from external face states.
/// This class handles the mapping of expression data to avatar morph targets and applies facial expressions to the avatar with smooth transitions.
/// </summary>
public class MetaNPC : SampleAvatarEntity
{
    #region Variables
    [Header("MetaNPC")]
    [SerializeField] private ExpressionMapping? _defaultMapping;
    [SerializeField] private float _transitionTime = 0.2f;
    [SerializeField] private float _weightMultFactor = 1.0f;

    private Transform _jawJoint;
    private Vector3 _jawDefault;
    private float[] _currentWeights = new float[103];
    private Coroutine? _transitionCoroutine = null;
    private bool hasRunThisFrame = false;
    private Dictionary<string, OVRPlugin.FaceState> _faceStates = new();
    private const string _dir = "Assets/facialtracking/PythonScripts/averaged_facestates";
    private string[]? _files;
    #endregion

    #region Functions
    /// <summary>
    /// Initializes the face states by loading them from the directory.
    /// Starts the base Start method from SampleAvatarEntity and begins loading face states.
    /// </summary>
    new void Start() {
        StartCoroutine(base.Start());
        GetFaceStates(_dir);
    }

    /// <summary>
    /// Called when the avatar is loaded. Starts the test sequence to apply facial expressions.
    /// </summary>
    public void OnAvatarLoaded() {
        StartCoroutine(Test());
    }

    /// <summary>
    /// Loads face state files from a specified directory and adds them to the _faceStates dictionary.
    /// </summary>
    /// <param name="dirPath">The directory path to search for face state files.</param>
    public void GetFaceStates(string dirPath){
        _files = Directory.GetFiles(dirPath);
        foreach (string f in _files){
            string[] splitFile = f.Split('.');
            string[] splitPath = f.Split('\\');
            if(splitFile[^1] != "json"){
                continue;
            }
            JObject jObj = JObject.Parse(File.ReadAllText(f));

            OVRPlugin.FaceState faceState = GetFaceState(jObj);
            _faceStates.Add(splitPath[^1].Split('_')[0], faceState);
        }
    }

    /// <summary>
    /// Converts a JSON object to an OVRPlugin.FaceState.
    /// </summary>
    /// <param name="obj">The JSON object containing face state data.</param>
    /// <returns>The corresponding FaceState.</returns>
    public OVRPlugin.FaceState GetFaceState(JObject obj){
        OVRPlugin.FaceState faceState = new OVRPlugin.FaceState();
        faceState.ExpressionWeights = obj["ExpressionWeights"]?.ToObject<float[]>() ?? Array.Empty<float>();
        faceState.ExpressionWeightConfidences = obj["ExpressionWeightConfidences"]?.ToObject<float[]>() ?? Array.Empty<float>();
        faceState.Status.IsValid = obj["Status"]["IsValid"]?.Value<bool>() ?? false;
        faceState.Status.IsEyeFollowingBlendshapesValid = obj["Status"]["IsEyeFollowingBlendshapesValid"]?.Value<bool>() ?? false;
        faceState.Time= obj["Time"]?.Value<double>() ?? 0;
        return faceState;
    }
    
    /// <summary>
    /// Tests all the FaceStates by applying them one after another.
    /// </summary>
    /// <returns>An enumerator for the coroutine.</returns>

    private IEnumerator Test(){
        foreach(KeyValuePair<string, OVRPlugin.FaceState> pair in _faceStates){
            ApplyFaceState(pair.Value, 0.1f);
            Debug.Log($"Expression {pair.Key}");
            yield return new WaitForSeconds(3);
        }
    }
    #endregion

    #region Facial Expression
    private readonly List<PrimitiveRenderData> animationFrameCache = new();
    
    /// <summary>
    /// Applies the face state associated with a given emotion.
    /// </summary>
    /// <param name="emotion">The emotion string to apply the corresponding face state.</param>
    public void ApplyFaceState(string emotion){
        Debug.Log(emotion);
        if(!_faceStates.ContainsKey(emotion)){
            Debug.Log($"Facestate zu {emotion} ist nicht vorhanden");
            return;
        }
        ApplyFaceState(_faceStates[emotion], 0f);
    }
    
    /// <summary>
    /// Applies the given face state to the avatar, optionally with a transition over a specified duration.
    /// </summary>
    /// <param name="fstate">The face state to apply.</param>
    /// <param name="duration">The transition duration for the face state, in seconds.</param>
    public void ApplyFaceState(OVRPlugin.FaceState fstate, float duration) {
        animationFrameCache.Clear();
        MethodInfo method = typeof(OvrAvatarEntity).GetMethod("AppendPrimitivesWithAnimationEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(this, new object[]{null, animationFrameCache});

        foreach(var primRenderable in animationFrameCache) {
            var skinnedRenderable = primRenderable.skinnedRenderable;
            var primitive = primRenderable.primitive;

            if(primitive.morphTargetCount == 0) { continue; }

            uint morphTargetCount = primitive.morphTargetCount;

            float[] targetWeights = FaceStateToWeights(morphTargetCount, fstate); // Convert face state to weights for morph targets
            if(duration > 0) {
                if(_transitionCoroutine != null) {
                    StopCoroutine(_transitionCoroutine);
                }
                _transitionCoroutine = StartCoroutine(SmoothMorph(targetWeights, duration, skinnedRenderable));
                return;
            }
            CopyMorphWeightsToBuffer(targetWeights, skinnedRenderable);
        }
    }

    /// <summary>
    /// Converts a face state into a set of morph target weights.
    /// </summary>
    /// <param name="morphTargetCount">The number of morph targets.</param>
    /// <param name="fstate">The face state data.</param>
    /// <returns>An array of morph target weights.</returns>
    private float[] FaceStateToWeights(uint morphTargetCount, OVRPlugin.FaceState fstate) {
        float[] weights = new float[morphTargetCount];
        if(_defaultMapping.Keys.Count != morphTargetCount) {
            Debug.LogError($"Expected mapping to have size {morphTargetCount}, got {_defaultMapping.Keys.Count} instead.");
            return weights;
        }

        for(int i=0; i < weights.Length; i++) {
            var expression = _defaultMapping.Values[i];
            if(expression >= OVRFaceExpressions.FaceExpression.Max || expression < 0) {
                continue;
            }

            float weight = fstate.ExpressionWeights[(int)expression];
            weights[i] = weight * _weightMultFactor;
        }

        return weights;
    }

    /// <summary>
    /// Copies the morph target weights to the skinned renderable buffer.
    /// </summary>
    /// <param name="weights">The morph target weights.</param>
    /// <param name="skinnedRenderable">The skinned renderable to update.</param>
    private void CopyMorphWeightsToBuffer(float[] weights, OvrAvatarSkinnedRenderable skinnedRenderable) {
        /*IDisposableBuffer bufferHandle = skinnedRenderable.CheckoutMorphTargetBuffer((uint)weights.Length);
        Marshal.Copy(weights, 0, bufferHandle.BufferPtr, weights.Length);
        skinnedRenderable.MorphTargetBufferUpdated(bufferHandle);
        bufferHandle.Dispose();*/
    }


    /// <summary>
    /// Smoothly interpolates the morph target weights from the current weights to the target weights.
    /// </summary>
    /// <param name="target">The target weights.</param>
    /// <param name="duration">The duration of the transition.</param>
    /// <param name="skinnedRenderable">The skinned renderable to update.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator SmoothMorph(float[] target, float duration, OvrAvatarSkinnedRenderable skinnedRenderable) {
        float elapsed = 0f;
        float[] startWeights = (float[])_currentWeights.Clone();

        // Smoothly interpolate the weights over time
        while(elapsed<duration) {
            elapsed+=Time.deltaTime;
            float t = Mathf.Clamp01(elapsed/duration);

            for (int i = 0; i < target.Length; i++)
            {
                _currentWeights[i] = Mathf.Lerp(startWeights[i], target[i], t);
            }

            CopyMorphWeightsToBuffer(_currentWeights, skinnedRenderable);
            yield return null;
        }

        // Ensure the final weights are set precisely to the target
        Array.Copy(target, _currentWeights, target.Length);
        CopyMorphWeightsToBuffer(_currentWeights, skinnedRenderable);

        _transitionCoroutine = null;
    }
    
    #endregion
}
