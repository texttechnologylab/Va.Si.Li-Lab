using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oculus.Avatar2;
using Oculus.Skinning;
using OVRSimpleJSON;
using Ubiq.Samples;
using UnityEngine;
using UnityEngine.Assertions.Must;
using static OvrAvatarSkinnedRenderable;
using static Oculus.Avatar2.OvrAvatarEntity;

public class ExpressionHandler : MonoBehaviour
{

    private OvrAvatarSkinnedRenderable _skinnedRenderable; 
    private SkinnedMeshRenderer _skinnedRenderer;
    private Transform _jawJoint;
    private Vector3 _jawDefault;
    private Dictionary<string, OVRPlugin.FaceState> faceStates = new();
    private string[] files;
    [SerializeField]
    private float factor;

    [SerializeField]
    private ExpressionMapping _mapping;

    /// <summary>
    /// Initializes the system and loads all available FaceStates from the Resources.
    /// </summary>
    void Start()
    {
        TextAsset[] facestates = Resources.LoadAll<TextAsset>("FaceStates");
        foreach(var fs in facestates) {
            faceStates.Add(fs.name.Substring(0, fs.name.IndexOf('_')), JsonUtility.FromJson<OVRPlugin.FaceState>(fs.text));
        }
        Debug.Log($"Facestates found: {faceStates.Count}");
    }

    /// <summary>
    /// Retrieves the SkinnedRenderable object and stops any animations if needed.
    /// </summary>
    public void GetSkinnedRenderable()
    {
        _skinnedRenderable = GetComponentInChildren<OvrAvatarSkinnedRenderable>(); //Script zu NPC, skinnedrenderable in dessen Kindern suchen

        var property = typeof(OvrAvatarSkinnedRenderable).GetProperty("IsAnimationEnabled"); //Reflection, weil set internal ist 
        if (property != null && property.CanWrite) 
        {
            property.SetValue(_skinnedRenderable, false); //property in skinnedRenderable Instanz zu false ändern um Animation zu stoppen
        }

        GetSkinnedMeshRenderer();

        // Looks for the jaw joint ("Jaw_jnt") and stores its default position
        foreach(var bone in _skinnedRenderer.bones){
                    if(bone != null && bone.name== "Jaw_jnt"){
                        _jawJoint = bone;
                        _jawDefault = bone.localPosition;
                    }
                }

        StartCoroutine(Test()); // Starts the test for applying FaceStates
    }

    /// <summary>
    /// Retrieves the SkinnedMeshRenderer from the child objects.
    /// </summary>
    public void GetSkinnedMeshRenderer() {
        _skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    /// <summary>
    /// Loads FaceState data from JSON files in the specified directory (not used currently)
    /// </summary>
    /// <param name="dirPath">The path to the directory containing the JSON files.</param>
    public void GetFaceStates(string dirPath){

        files = Directory.GetFiles(dirPath);
        foreach (string f in files){
            string[] splitFile = f.Split('.');
            string[] splitPath = f.Split('\\');
            if(splitFile[^1] != "json"){
                continue;
            }
            JObject jObj = JObject.Parse(File.ReadAllText(f));

            OVRPlugin.FaceState faceState = GetFaceState(jObj);
            faceStates.Add(splitPath[^1].Split('_')[0], faceState);
        }
    }

    /// <summary>
    /// Converts a JObject into a FaceState object (not used currently)
    /// </summary>
    /// <param name="obj">The JObject containing the FaceState data.</param>
    /// <returns>The converted FaceState object.</returns>
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
    /// Applies the FaceState of a given emotion to the avatar.
    /// </summary>
    /// <param name="emotion">The name of the emotion.</param>
    public void ApplyFaceState(string emotion){
        if(!faceStates.ContainsKey(emotion)) {
            Debug.Log($"Emotion {emotion} not found");
            return;
        }
        Debug.Log($"Selected Emotion: {emotion}");
        ApplyFaceState(faceStates[emotion]);
    }

    /// <summary>
    /// Applies a given FaceState to the avatar.
    /// </summary>
    /// <param name="state">The FaceState to apply.</param>
    public void ApplyFaceState(OVRPlugin.FaceState state) {
        GetSkinnedMeshRenderer();

        if(_skinnedRenderer == null) {
            Debug.LogError("Trying to apply FaceState while no SkinnedMeshRenderer exists.");
            return;
        }

        int numShapes = _skinnedRenderer.sharedMesh.blendShapeCount;
        for(int i = 0; i < numShapes; i++) {
            var (expression, mappingFactor) = _mapping.Get(_skinnedRenderer.sharedMesh.GetBlendShapeName(i));
            if(expression >= OVRFaceExpressions.FaceExpression.Max || expression < 0) {
                continue;
            }
            float weight = state.ExpressionWeights[(int)expression]*mappingFactor*factor;
            _skinnedRenderer.SetBlendShapeWeight(i, weight);
            if(expression == OVRFaceExpressions.FaceExpression.JawDrop){
                Vector3 pos = _jawDefault;
                pos.x -= 0.03f*weight;
                _jawJoint.transform.localPosition= pos;
            }
        }
    }

    /// <summary>
    /// Tests all the FaceStates by applying them one after another.
    /// </summary>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator Test(){
        foreach(KeyValuePair<string, OVRPlugin.FaceState> pair in faceStates){
            ApplyFaceState(pair.Value);
            Debug.Log(pair.Key);
            yield return new WaitForSeconds(3);
        }
    }
}