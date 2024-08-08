using System.Collections;
using Ubiq.Rooms;
using System;
using Oculus.Avatar2;
using UnityEngine;

namespace VaSiLi.MetaAvatar
{
    public class NetworkSampleAvatarEntity : SampleAvatarEntity
    {

        MetaAvatar avatar;

        protected override IEnumerator Start()
        {
            //transform.rotation = new Quaternion(0f, 1f, 0f, 180f);
            SetBodyTracking(null);
            SetFacePoseProvider(null);
            SetEyePoseProvider(null);
            SetIsLocal(false);

            avatar = GetComponentInParent<MetaAvatar>();
            if (avatar)
            {
                avatar.OnPeerUpdated.AddListener(UpdateTexture);
            }

            //gameObject.AddComponent<SampleAvatarGazeTargets>();

            StartCoroutine(AddHeadGaze());

            // Don't start base(), because it will initiallize a default char, but we want to load the network variante.
            return null;
            //return base.Start();
        }

        private IEnumerator AddHeadGaze()
        {
            yield return new WaitUntil(() => HasJoints);

            CreateGazeTarget("HeadGazeTarget", CAPI.ovrAvatar2JointType.Head, CAPI.ovrAvatar2GazeTargetType.AvatarHead);
            CreateGazeTarget("LeftHandGazeTarget", CAPI.ovrAvatar2JointType.LeftHandIndexProximal, CAPI.ovrAvatar2GazeTargetType.AvatarHand);
            CreateGazeTarget("RightHandGazeTarget", CAPI.ovrAvatar2JointType.RightHandIndexProximal, CAPI.ovrAvatar2GazeTargetType.AvatarHand);
        }

        private void CreateGazeTarget(string gameObjectName, CAPI.ovrAvatar2JointType jointType, CAPI.ovrAvatar2GazeTargetType targetType)
        {
            Transform jointTransform = GetSkeletonTransform(jointType);
            if (jointTransform)
            {
                var gazeTargetObj = new GameObject(gameObjectName);
                var gazeTarget = gazeTargetObj.AddComponent<OvrAvatarGazeTarget>();
                gazeTarget.TargetType = targetType;
                gazeTargetObj.transform.SetParent(jointTransform, false);
                OvrAvatarLog.LogInfo($"GazeTarget added for {gameObjectName}");
            }
            else
            {
                OvrAvatarLog.LogError($"No joint transform found for {gameObjectName}");
            }
        }


        private void UpdateTexture(IPeer peer)
        {
            var usrid = peer["ubiq.avatar.meta.userid"];
            string platform = peer["ubiq.avatar.meta.platform"];
            if (!String.IsNullOrWhiteSpace(usrid))
            {
                var longid = Convert.ToUInt64(usrid);
                //if(_userId != longid || CurrentState <= AvatarState.DefaultAvatar)
                //{
                //_userId = longid;
                Teardown();
                CreateEntity();

                //Arbitrary number. Presets are numbert up to 32 (currently, 08.02.23). Regular userids are just large ...
                if (longid < 48)
                {
                    LoadPreset(Convert.ToInt32(longid));
                }
                else
                {
                    try
                    {
                        if (platform == Application.platform.ToString())
                            LoadRemoteUserCdnAvatar(longid);
                    }
                    catch (Exception e)
                    {
                        LoadPreset(0);
                    }
                }
                ///}
            }
            else
            {
                Debug.LogWarning("Could not find meta user id. Take default.");
                Teardown();
                CreateEntity();
                LoadPreset(0);
            }

        }

    }

}