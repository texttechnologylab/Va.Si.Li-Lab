using Ubiq;
using Ubiq.Avatars;
using UnityEngine;

namespace VaSiLi.MetaAvatar
{
    public class MetaAvatarInput : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority;
        /*[Tooltip("The lower body source to use for input. If null, will try to find a lower body source among child objects at start.")]
        [SerializeField] private MMVRLowerBody lowerBodySource;*/
        [Tooltip("The transform to use as an offset for the neck from the head.")]
        [SerializeField] private Transform avatar;
        [Tooltip("The transform to use as world pose for the left hand in case no left hand input is found. This may happen in case of tracking failure for controllers or hands.")]
        [SerializeField] private Transform head;
        [Tooltip("The transform containing the latest leftHand pose. Will be frequently overwritten.")]
        [SerializeField] private Transform leftHand;
        [Tooltip("The transform containing the latest rightHand pose. Will be frequently overwritten.")]
        [SerializeField] private Transform rightHand;

        private class MetaOVRInput : IMetaOVRInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;

            public Pose avatar => owner.Avatar();
            public Pose head => owner.Head();
            public Pose leftHand => owner.LeftHand();
            public Pose rightHand => owner.RightHand();
            public float leftGrip => owner.LeftGrip();
            public float rightGrip => owner.RightGrip();

            private MetaAvatarInput owner;

            public MetaOVRInput(MetaAvatarInput owner)
            {
                this.owner = owner;
            }

            private static InputVar<Pose> GetVar(Transform transform)
            {
                return transform
                    ? new InputVar<Pose>(
                        new Pose(transform.position, transform.rotation))
                    : InputVar<Pose>.invalid;
            }
        }

        private MetaOVRInput input;

        private void Start()
        {
            if (!avatarManager)
            {
                avatarManager = FindAnyObjectByType<AvatarManager>();

                if (!avatarManager)
                {
                    Debug.LogWarning("No AvatarManager could be found in this Unity scene. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }

            /*if (!lowerBodySource)
            {
                lowerBodySource = GetComponentInChildren<MMVRLowerBody>();

                if (!lowerBodySource)
                {
                    Debug.LogWarning("No LowerBodySource could be found among child objects. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }*/

            input = new MetaOVRInput(this);
            avatarManager.input.Add((IMetaOVRInput)input);
        }


        private Pose Avatar()
        {
            return new Pose(avatar.position, avatar.rotation);
        }

        private Pose Head()
        {
            return new Pose(head.position, head.rotation);
        }
        private Pose LeftHand()
        {
            return new Pose(leftHand.position, leftHand.rotation);
        }

        private Pose RightHand()
        {
            return new Pose(rightHand.position, rightHand.rotation);
        }
        // TODO:
        private float LeftGrip()
        {
            return 0;//lowerBodySource.LeftPose;
        }

        private float RightGrip()
        {
            return 0;//lowerBodySource.RightPose;
        }



        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input?.Remove((IMetaOVRInput)input);
            }
        }
    }
}

