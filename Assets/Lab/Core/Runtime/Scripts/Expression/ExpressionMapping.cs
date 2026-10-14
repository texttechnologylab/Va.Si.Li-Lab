using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ubiq.Samples;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// ScriptableObject for mapping avatar blendshape names (as strings) to OVR face expressions and a scaling factor.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "New Mapping", menuName = "MetaNPC/BlendShapeMapping", order = 1)]
public class ExpressionMapping : ScriptableObject
{
    [HideInInspector]
    public List<string> Keys = new(); //Blendshape names
    [HideInInspector]
    public List<OVRFaceExpressions.FaceExpression> Values = new(); //OVRFaceExpressions
    [HideInInspector]
    public List<float> Factors = new(); //multiplicative factor for each mapping

    /// <summary>
    /// Retrieves the mapped OVRFaceExpression and its factor for a given key.
    /// </summary>
    public (OVRFaceExpressions.FaceExpression, float) Get(string key) {
        int idx = Keys.IndexOf(key);
        
        if(idx == -1) {
            return (OVRFaceExpressions.FaceExpression.Invalid, -1f);
        }

        return (Values[idx],Factors[idx]);
    }

    /// <summary>
    /// Adds a new mapping entry if the key is not already present.
    /// </summary>
    public void AddEntry(string key, OVRFaceExpressions.FaceExpression value, float factor) {
        if(Keys.Contains(key)) {
            return;
        }

        Keys.Add(key);
        Values.Add(value);
        Factors.Add(factor);
    }

    /// <summary>
    /// Removes an entry by key if it exists.
    /// </summary>
    public void RemoveEntry(string key) {
        int idx = Keys.IndexOf(key);
        if(idx == -1) {
            return;
        }

        Keys.RemoveAt(idx);
        Values.RemoveAt(idx);
        Factors.RemoveAt(idx);
    }

    /// <summary>
    /// Updates a key in the mapping.
    /// </summary>
    public void UpdateKey(string oldKey, string newKey) {
        int idx = Keys.IndexOf(oldKey);
        if(idx == -1 || Keys.Contains(newKey)) {
            return;
        }

        Keys[idx] = newKey;
    }

    /// <summary>
    /// Updates the value (OVR expression) for a given key.
    /// </summary>
    public void UpdateValue(string key, OVRFaceExpressions.FaceExpression newValue) {
        int idx = Keys.IndexOf(key);
        if(idx == -1) {
            return;
        }

        Values[idx] = newValue;
    }

    /// <summary>
    /// Updates the factor for a given key.
    /// </summary>
    public void UpdateFactor(string key, float newFactor){
        int idx = Keys.IndexOf(key);
        if(idx == -1) {
            return;
        }
        Factors[idx] = newFactor;
    }

    /// <summary>
    /// Clears all mapping entries.
    /// </summary>
    public void ClearAllEntries() {
        Keys.Clear();
        Values.Clear();
        Factors.Clear();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ExpressionMapping))]
public class ExpressionMappingEditor : Editor
{
    private readonly List<string> givenBlendshapes = new()
    {
        "anim_chinRaiser",
        "anim_jawDrop",
        "anim_jawThrust",
        "anim_leftBottomLipFunneler",
        "anim_leftBottomLipSuck",
        "anim_leftBottomMouthSideways",
        "anim_leftBrowInward",
        "anim_leftBrowLowerer",
        "anim_leftCheekPuff",
        "anim_leftCheekRaiser",
        "anim_leftCheekSuck",
        "anim_leftDimpler",
        "anim_leftEyeLookDown",
        "anim_leftEyeLookIn",
        "anim_leftEyeLookOut",
        "anim_leftEyeLookUp",
        "anim_leftInnerBrowRaiser",
        "anim_leftInnerBrowRotateDown",
        "anim_leftInnerBrowRotateUp",
        "anim_leftInnerEyeClose",
        "anim_leftInnerEyeLowerLidClose",
        "anim_leftInnerEyeOpenExtra",
        "anim_leftInnerLidTightener",
        "anim_leftIrisScale",
        "anim_leftIrisScaleInverse",
        "anim_leftJawSideways",
        "anim_leftLipCornerDepressor",
        "anim_leftLipCornerPuller",
        "anim_leftLipCornerRounder",
        "anim_leftLipPucker",
        "anim_leftLipStretcher",
        "anim_leftLipTightener",
        "anim_leftLowerLipDepressor",
        "anim_leftMidBrowInward",
        "anim_leftMidBrowRaiser",
        "anim_leftMouthSideways",
        "anim_leftNoseWrinkler",
        "anim_leftNostrilDilator",
        "anim_leftOuterBrowRaiser",
        "anim_leftOuterEyeClose",
        "anim_leftOuterEyeLowerLidClose",
        "anim_leftOuterEyeOpenExtra",
        "anim_leftOuterLidTightener",
        "anim_leftPupilScale",
        "anim_leftPupilScaleInverse",
        "anim_leftTopLipFunneler",
        "anim_leftTopLipSuck",
        "anim_leftTopMouthSideways",
        "anim_leftUpperLipRaiser",
        "anim_midLowerLipDepressor",
        "anim_midUpperLipRaiser",
        "anim_mouthRotateClock",
        "anim_mouthRotateCounterClock",
        "anim_rightBottomLipFunneler",
        "anim_rightBottomLipSuck",
        "anim_rightBottomMouthSideways",
        "anim_rightBrowInward",
        "anim_rightBrowLowerer",
        "anim_rightCheekPuff",
        "anim_rightCheekRaiser",
        "anim_rightCheekSuck",
        "anim_rightDimpler",
        "anim_rightEyeLookDown",
        "anim_rightEyeLookIn",
        "anim_rightEyeLookOut",
        "anim_rightEyeLookUp",
        "anim_rightInnerBrowRaiser",
        "anim_rightInnerBrowRotateDown",
        "anim_rightInnerBrowRotateUp",
        "anim_rightInnerEyeClose",
        "anim_rightInnerEyeLowerLidClose",
        "anim_rightInnerEyeOpenExtra",
        "anim_rightInnerLidTightener",
        "anim_rightIrisScale",
        "anim_rightIrisScaleInverse",
        "anim_rightJawSideways",
        "anim_rightLipCornerDepressor",
        "anim_rightLipCornerPuller",
        "anim_rightLipCornerRounder",
        "anim_rightLipPucker",
        "anim_rightLipStretcher",
        "anim_rightLipTightener",
        "anim_rightLowerLipDepressor",
        "anim_rightMidBrowInward",
        "anim_rightMidBrowRaiser",
        "anim_rightMouthSideways",
        "anim_rightNoseWrinkler",
        "anim_rightNostrilDilator",
        "anim_rightOuterBrowRaiser",
        "anim_rightOuterEyeClose",
        "anim_rightOuterEyeLowerLidClose",
        "anim_rightOuterEyeOpenExtra",
        "anim_rightOuterLidTightener",
        "anim_rightPupilScale",
        "anim_rightPupilScaleInverse",
        "anim_rightTopLipFunneler",
        "anim_rightTopLipSuck",
        "anim_rightTopMouthSideways",
        "anim_rightUpperLipRaiser",
        "anim_teethWedge",
        "anim_tongueCurl",
        "anim_tonguePress",
        "anim_tongueRoll"
    };

    /**
    Custom inspector for editing ExpressionMapping in the Unity Editor.
    **/
    public override void OnInspectorGUI() {
        ExpressionMapping EM = (ExpressionMapping)target;


        bool modified = false;
        
        if(GUILayout.Button("Auto Populate Blendshapes")) {
            AutoPopulateMapping();
            modified = true;
        }

        if(GUILayout.Button("Clear all Entries")) {
            EM.ClearAllEntries();
            modified = true;
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Blendshape Mapping", EditorStyles.boldLabel);

        for(int i = 0; i < EM.Keys.Count; i++) {
            string currentKey = EM.Keys[i];
            OVRFaceExpressions.FaceExpression currentValue = EM.Values[i];
            float currentFactor = EM.Factors[i];
            
            
            EditorGUILayout.BeginHorizontal();

            string newKey = EditorGUILayout.TextField(currentKey, GUILayout.Width(200));

            if(newKey != currentKey) {
                EM.UpdateKey(currentKey, newKey);
                modified = true;
            }

            OVRFaceExpressions.FaceExpression newValue = (OVRFaceExpressions.FaceExpression)EditorGUILayout.EnumPopup(currentValue);

            if(newValue != currentValue) {
                EM.UpdateValue(currentKey, newValue);
                modified = true;
            }

            float newFactor = EditorGUILayout.FloatField(currentFactor, GUILayout.Width(50));
            if(newFactor != currentFactor) {
                EM.UpdateFactor(currentKey, newFactor);
                modified = true;
            }

            if(GUILayout.Button("Remove", GUILayout.Width(60))) {
                EM.RemoveEntry(currentKey);
                modified = true;
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if(GUILayout.Button("Add new Entry")) {
            string key = $"Entry {EM.Keys.Count+1}";
            EM.AddEntry(key, OVRFaceExpressions.FaceExpression.Invalid, 1.0f);
            modified = true;
        }


        if(modified) {
            EditorUtility.SetDirty(EM);
        }
    }

    /**
    Uses reflection and heuristics from OVR code to populate the mapping with best-guess face expression matches.
    The majority of this logic is derived from <cref="OVRCustomFaceEditor.cs">.
    **/ 
    private void AutoPopulateMapping() {
        ExpressionMapping EM = (ExpressionMapping)target;
        
        Type myType = typeof(OVRCustomFace);

        MethodInfo tokenizeString = myType.GetMethod("TokenizeString", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo findBestMatch = myType.GetMethod("FindBestMatch", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo isLipsToward = myType.GetMethod("IsLipsToward", BindingFlags.NonPublic | BindingFlags.Static);

        if(tokenizeString != null && findBestMatch != null && isLipsToward != null) {
            EM.ClearAllEntries();

            string[] blendShapeNames = Enum.GetNames(typeof(OVRFaceExpressions.FaceExpression));
            OVRFaceExpressions.FaceExpression[] faceExpressions =
                (OVRFaceExpressions.FaceExpression[])Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression));
            bool allowDuplicateMapping = false;

            // <cref="OVRCustomFaceEditor.cs"> content starts here//
            var result = new OVRFaceExpressions.FaceExpression[givenBlendshapes.Count];
            var expressionTokens = new HashSet<string>[blendShapeNames.Length];
            
            for (int i = 0; i < blendShapeNames.Length; ++i)
            {
                expressionTokens[i] = (HashSet<string>)tokenizeString.Invoke(null, new object[] {blendShapeNames[i]});
            }

            var usedBlendshapes = new HashSet<OVRFaceExpressions.FaceExpression>();
            for (int i = 0; i < givenBlendshapes.Count; ++i)
            {
                var blendShapeName = givenBlendshapes[i];
                var bestMatchFound = (OVRFaceExpressions.FaceExpression)findBestMatch.Invoke(
                    null,
                    new object[] {
                        expressionTokens,
                        blendShapeName,
                        faceExpressions,
                        OVRFaceExpressions.FaceExpression.Invalid
                    });
                // If not allowing duplicates, make an exception for liptowards.
                if (!allowDuplicateMapping &&
                    (usedBlendshapes.Contains(bestMatchFound) &&
                    !(bool)isLipsToward.Invoke(null, new object[] {blendShapeName})))
                {
                    result[i] = OVRFaceExpressions.FaceExpression.Invalid;
                }
                else
                {
                    result[i] = bestMatchFound;
                    usedBlendshapes.Add(bestMatchFound);
                }
            }
            // <cref="OVRCustomFaceEditor.cs"> content ends here//
            EM.Keys = givenBlendshapes;
            EM.Values = result.ToList();
            EM.Factors = Enumerable.Repeat(1.0f, givenBlendshapes.Count).ToList();
        }
    }
}
#endif