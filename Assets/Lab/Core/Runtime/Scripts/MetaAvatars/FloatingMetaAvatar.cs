using UnityEngine;

namespace VaSiLi.MetaAvatar
{
    /// <summary>
    /// TODO: Cleanup
    /// </summary>
    public class FloatingMetaAvatar : MonoBehaviour
    {
        public Transform head;
        public Transform torso;
        public Transform leftHand;
        public Transform rightHand;

        public Renderer headRenderer;
        public Renderer torsoRenderer;
        public Renderer leftHandRenderer;
        public Renderer rightHandRenderer;

        public Transform baseOfNeckHint;

        public AnimationCurve torsoFootCurve;

        public AnimationCurve torsoFacingCurve;

        private TrackedMetaAvatar trackedAvatar;


        private void OnEnable()
        {
            trackedAvatar = GetComponentInParent<TrackedMetaAvatar>();

            if (trackedAvatar)
            {
                trackedAvatar.OnAvatarHeadUpdate.AddListener(ThreePointTrackedAvatar_OnAvatarUpdate);
            }

        }

        private void OnDisable()
        {
            if (trackedAvatar && trackedAvatar != null)
            {
                trackedAvatar.OnAvatarHeadUpdate.RemoveListener(ThreePointTrackedAvatar_OnAvatarUpdate);
            }
        }

        private void ThreePointTrackedAvatar_OnAvatarUpdate(Ubiq.InputVar<Pose> headPose, Ubiq.InputVar<Pose> avatarPose)
        {
            if (!headPose.valid || !avatarPose.valid)
                return;
            transform.position = avatarPose.value.position;
            transform.rotation = avatarPose.value.rotation * Quaternion.Inverse(Quaternion.Euler(0.0f, headPose.value.rotation.eulerAngles.y, 0.0f));
        }
    }
}