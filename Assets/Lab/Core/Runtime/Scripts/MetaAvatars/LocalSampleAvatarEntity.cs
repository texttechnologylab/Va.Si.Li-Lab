using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Oculus.Platform;
using Oculus.Platform.Models;

#if ENABLE_VR || ENABLE_AR
using UnityEngine.XR;
#endif

namespace VaSiLi.MetaAvatar
{
    public class LocalSampleAvatarEntity : SampleAvatarEntity
    {

        [Header("Expression Exaggeration")]
        [Tooltip("Multiplier applied to the tracked face blendshape weights. 1 = no change, " +
                 "higher values exaggerate facial expressions.")]
        [Range(1f, 3f)]
        [SerializeField] private float _expressionMultiplier = 1.3f;

        private RoomClient roomClient;
        //private DesktopPlayerController desktoplayer;
        private TrackedMetaAvatar tracker;
        private ExaggeratedFacePoseBehavior _exaggeratedFaceBehavior;
        private static List<XRInputSubsystem> tmpSubsystems = new List<XRInputSubsystem>();

        protected override IEnumerator Start()
        {
            roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();

            tracker = transform.GetComponentInParent<TrackedMetaAvatar>();

            GameObject metamanager = GameObject.Find("AvatarSdkManagerMeta");

            if (metamanager) //TODO: Filter Bots, set active view to first person
            {
                Debug.Log("Is Desktop: ?" + IsDesktopPlayer());
                if (IsDesktopPlayer())
                {
                    TransformTrackingInputManager desktopTransforms = transform.GetComponentInChildren<TransformTrackingInputManager>();
                    SetInputManager(desktopTransforms);
                }
                else
                {
                    SetInputManager(metamanager.GetComponentInChildren<SampleInputManager>());
                }

                SetEyePoseProvider(metamanager.GetComponent<OvrAvatarEyeTrackingBehaviorOvrPlugin>());

                var faceTracking = metamanager.GetComponent<OvrAvatarFaceTrackingBehaviorOvrPlugin>();
                SetFacePoseProvider(WrapWithExaggeration(faceTracking));
                //SetLipSync(player.GetComponent<OvrAvatarLipSyncBehavior>());
            }


            StartCoroutine(SetMetaID());
            return base.Start();
        }

        private IEnumerator SetMetaID()
        {
            Debug.Log("Wait for Loading");
            roomClient.Me["ubiq.avatar.meta.userid"] = "0";
            roomClient.Me["ubiq.avatar.meta.platform"] = UnityEngine.Application.platform.ToString();
            /*try
            {


                //OvrPlatformInit.InitializeOvrPlatform();
                Users.GetLoggedInUser().OnComplete(callback: (Message<User> msg) =>
                {
                    if (msg.IsError)
                    {
                        Debug.LogError("Failed to get logged in user: " + msg.GetError().Message);
                        //roomClient.Me["ubiq.avatar.meta.userid"] = "0";
                    }
                    else
                    {
                        roomClient.Me["ubiq.avatar.meta.userid"] = msg.Data.ID.ToString();
                        Debug.Log("User ID: " + msg.Data.ID);
                        Debug.Log("UserName: " + msg.Data.DisplayName);
                        Debug.Log("UserImageURL: " + msg.Data.ImageURL);
                        Debug.Log("UserId:  " + roomClient.Me["ubiq.avatar.meta.userid"]);
                    }
                });

            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to initialize Oculus Platform: " + e.Message);
                roomClient.Me["ubiq.avatar.meta.userid"] = "0";
            }*/
            while (CurrentState < AvatarState.UserAvatar)
            {
                Debug.Log(CurrentState);
                yield return new WaitForSeconds(1.0f);
            }
            //For now ....
            roomClient.Me["ubiq.avatar.meta.userid"] = _userId.ToString();

            // FIXME: Current workaround for OPENXR tracking issues
#if UNITY_EDITOR
            GameObject metamanager = GameObject.Find("AvatarSdkManagerMeta");
            if (metamanager)
                metamanager.GetComponentInChildren<SampleInputManager>().BodyTrackingMode = OvrAvatarBodyTrackingMode.Standalone;
#endif
        }

        private bool IsDesktopPlayer()
        {
            SubsystemManager.GetSubsystems<XRInputSubsystem>(tmpSubsystems);
            return tmpSubsystems.Count < 1;
        }

        /// <summary>
        /// Wraps the live face tracking behavior with an <see cref="ExaggeratedFacePoseBehavior"/>
        /// that multiplies the tracked blendshape weights to exaggerate facial expressions.
        /// Falls back to the original tracking behavior if no source provider is available.
        /// </summary>
        private OvrAvatarFacePoseBehavior WrapWithExaggeration(OvrAvatarFacePoseBehavior source)
        {
            if (source == null || _expressionMultiplier <= 1f)
            {
                return source;
            }

            _exaggeratedFaceBehavior = gameObject.AddComponent<ExaggeratedFacePoseBehavior>();
            if (_exaggeratedFaceBehavior.Initialize(source, _expressionMultiplier))
            {
                return _exaggeratedFaceBehavior;
            }

            // Source provider not ready (e.g. face tracking unavailable on this platform);
            // use the original behavior so tracking still works without exaggeration.
            Destroy(_exaggeratedFaceBehavior);
            _exaggeratedFaceBehavior = null;
            return source;
        }

        /// <summary>
        /// Adjusts the facial-expression exaggeration multiplier at runtime.
        /// </summary>
        public void SetExpressionMultiplier(float multiplier)
        {
            _expressionMultiplier = multiplier;
            if (_exaggeratedFaceBehavior != null)
            {
                _exaggeratedFaceBehavior.SetExpressionMultiplier(multiplier);
            }
        }

        public void UpdateTexture(int id)
        {
            tracker.dataBuffer.Dispose(); // Not needed anymore
            tracker.dataBuffer = default;

            id = Mathf.Max(id, 0);
            if (id <= 64)
            {
                roomClient.Me["ubiq.avatar.meta.userid"] = id.ToString();
                Teardown();
                CreateEntity();
                LoadPreset(id);
            }
            else
            {

                Teardown();
                CreateEntity();
                //LoadPreset(0);
                LoadUser();
            }

        }

        public void UpdateTexture(ulong id)
        {
            tracker.dataBuffer.Dispose(); // Not needed anymore
            tracker.dataBuffer = default;

            roomClient.Me["ubiq.avatar.meta.userid"] = id.ToString();
            LoadRemoteUserCdnAvatar(id);

        }
    }

}