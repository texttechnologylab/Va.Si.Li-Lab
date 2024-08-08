using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.XR;
using static OVRPlugin;

#if ENABLE_VR || ENABLE_AR
using UnityEngine.XR;
#endif

namespace VaSiLi.MetaAvatar
{
    public class LocalSampleAvatarEntity : SampleAvatarEntity
    {

        private RoomClient roomClient;
        private DesktopPlayerController desktoplayer;
        private TrackedMetaAvatar tracker;

        private static List<XRInputSubsystem> tmpSubsystems = new List<XRInputSubsystem>();
        protected override IEnumerator Start()
        {
            //transform.rotation = Quaternion.identity;
            roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
            desktoplayer = GameObject.Find("MetaPlayer").GetComponent<DesktopPlayerController>();
            tracker = transform.GetComponentInParent<TrackedMetaAvatar>();

            GameObject metamanager = GameObject.Find("AvatarSdkManagerMeta");
            GameObject player = GameObject.Find("MetaPlayer"); //I Bet there is a better variante ....

            if (metamanager) //TODO: Filter Bots, set active view to first person
            {
                Debug.Log("Is Desktop: ?" + IsDesktopPlayer());
                if (IsDesktopPlayer())
                {
                    TransformTrackingInputManager desktopTransforms = transform.GetComponentInChildren<TransformTrackingInputManager>();
                    SetBodyTracking(desktopTransforms);
                }
                else
                {
                    SetBodyTracking(metamanager.GetComponentInChildren<OvrAvatarInputManager>());
                }

                SetEyePoseProvider(metamanager.GetComponent<OvrAvatarEyeTrackingBehaviorOvrPlugin>());
                SetFacePoseProvider(metamanager.GetComponent<OvrAvatarFaceTrackingBehaviorOvrPlugin>());

                SetLipSync(player.GetComponent<OvrAvatarLipSyncBehavior>());
            }


            //SetIsLocal(true);
            //UpdateTexture(0);
            //return null;
            StartCoroutine(SetMetaID());
            return base.Start();

        }

        private IEnumerator SetMetaID()
        {
            Debug.Log("Wait for Loading");
            roomClient.Me["ubiq.avatar.meta.userid"] = "0";
            roomClient.Me["ubiq.avatar.meta.platform"] = UnityEngine.Application.platform.ToString();
            while (CurrentState < AvatarState.UserAvatar)
            {
                Debug.Log(CurrentState);
                yield return new WaitForSeconds(1.0f);
            }
            //For now ....
            roomClient.Me["ubiq.avatar.meta.userid"] = _userId.ToString();
        }


        private bool IsDesktopPlayer()
        {
            SubsystemManager.GetSubsystems<XRInputSubsystem>(tmpSubsystems);
            return tmpSubsystems.Count < 1;
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
                //LoadLoggedInUserCdnAvatar();
            }

        }
    }

}