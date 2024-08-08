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
                trackedAvatar.OnAvatarUpdate.AddListener(ThreePointTrackedAvatar_OnAvatarUpdate);
            }

        }

        private void OnDisable()
        {
            if (trackedAvatar && trackedAvatar != null)
            {
                trackedAvatar.OnAvatarUpdate.RemoveListener(ThreePointTrackedAvatar_OnAvatarUpdate);
            }
        }

        private void ThreePointTrackedAvatar_OnAvatarUpdate(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }
}