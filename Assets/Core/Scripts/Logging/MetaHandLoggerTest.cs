using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Voip;
using System;
using Ubiq.XR;
using VaSiLi.SceneManagement;
using Ubiq.Messaging;
using SIPSorceryMedia.Abstractions;
using System.Threading.Tasks;
using Ubiq.Voip.Implementations.Dotnet;
using VaSiLi.Networking;
using System.Security.Cryptography;
using System.Text;
using Ubiq.Spawning;
using System.Linq;
using System.IO;
using TinyJson;
using Newtonsoft.Json;
using static OVRSkeleton;
using System.Collections;

namespace VaSiLi.Logging
{
    /// <summary>
    /// Class to aid in logging various messages to the api
    /// All messages are first queued to be sent and then handled asynchronisly 
    /// </summary>
    /// 

    public class MetaHandLoggerTest : MonoBehaviour
    {
        [SerializeField]
        private OVRHand hand;
        [SerializeField]
        private OVRSkeleton handSkeleton;

        StreamWriter writer;
        string save_path = "Assets/Resources/logs.txt";

        private void Awake()
        {
            if (!hand) hand = GetComponent<OVRHand>();
            if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();

            writer = new StreamWriter(save_path);
        }



        private void Update()
        {
            //IOVRSkeletonDataProvider skeletonProvider = hand;
            //SkeletonPoseData poseData = skeletonProvider.GetSkeletonPoseData();
            //Debug.Log("!!!" + poseData.ToJson());
            SaveBoneInfo();
        }

        private void SaveBoneInfo()
        {
            Dictionary<string, Dictionary<string, float>> handpos = new Dictionary<string, Dictionary<string, float>>();
            //string dict_str = "{";
            //Debug.Log("???????" + handSkeleton.GetSkeletonType());
            //Debug.Log("???????" + handSkeleton.GetCurrentNumBones());
            //Debug.Log("???????" + handSkeleton.GetCurrentNumSkinnableBones());
            //Debug.Log("???????" + handSkeleton.GetCurrentStartBoneId());
            //Debug.Log("???????" + handSkeleton.GetCurrentEndBoneId());
            foreach (var bone in handSkeleton.Bones)
            {
                Debug.Log("???" + bone.Id.ToString());
                Dictionary<string, float> pos_dict = new Dictionary<string, float>
                {
                    { "x", bone.Transform.position.x },
                    { "y", bone.Transform.position.y },
                    { "z", bone.Transform.position.z }
                };
                handpos.Add(bone.Id.ToString(), pos_dict);
                //Vector3 pos = bone.Transform.position;
                //dict_str += '"' + bone.Id.ToString() + ": [" + pos.x + ", " + pos.y + ", " + pos.z + "]";
                //Debug.Log($"!!: bone.Id -> {bone.Id} Pose -> {bone.Transform.position}");
            }
            //dict_str += "}";
            string test  = JsonConvert.SerializeObject(handpos, Formatting.Indented);
            //writer.WriteLine(JsonConvert.ToJson(handpos) + "\n");
            Debug.Log("!!!" + test);
        }


        void OnApplicationQuit()
        {
            // Make sure the file logger finishes writing all the files
            FileLogger.OnApplicationQuit();
            writer.Close();
        }
    }

}
