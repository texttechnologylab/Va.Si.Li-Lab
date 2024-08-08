using System.Collections;
using UnityEngine;
using Ubiq.XR;
using Ubiq.Avatars;

namespace VaSiLi.MetaAvatar
{
    [RequireComponent(typeof(AvatarManager))]
    public class AvatarFingerHintHelper : MonoBehaviour
    {

        private void Start()
        {
            var pcs = FindObjectsOfType<XRPlayerController>(includeInactive: true);

            if (pcs.Length == 0)
            {
                Debug.LogWarning("No Ubiq player controller found");
            }
            else if (pcs.Length > 1)
            {
                Debug.LogWarning("Multiple Ubiq player controllers found. Using: " + pcs[0].name);
            }

            var pc = pcs[0];

            var hcs = pc.GetComponentsInChildren<OVRSkeleton>();

            foreach (var hc in hcs)
            {
                Debug.Log("===" + hc.GetSkeletonType());
            }

            GetLeftHand(hcs, out var leftHand);
            //StartCoroutine(InitLeftHand(leftHand));
            //SetTransformProvider(leftHandPositionNode, leftHandRotationNode, leftHand);
            //SetTransformProvider(leftWristPositionNode, leftWristRotationNode, leftWrist);
            //SetGripProvider(leftGripNode, leftHc);

            GetRightHand(hcs, out var rightHand);
            //StartCoroutine(InitRightHand(rightHand));
            //SetTransformProvider(rightHandPositionNode, rightHandRotationNode, rightHand);
            //SetTransformProvider(rightWristPositionNode, rightWristRotationNode, rightWrist);
            //SetGripProvider(rightGripNode, rightHc);
        }

        private IEnumerator InitLeftHand(OVRSkeleton hand)
        {
            while (!hand.IsInitialized)
            {
                yield return new WaitForSeconds(.1f);
            }
            Debug.Log("===" + hand.Bones.Count + " - " + hand.Bones);
            foreach (var bone in hand.Bones)
            {
                SetTransformProvider($"Left{bone.Id}_Position", $"Left{bone.Id}_Rotation", bone.Transform);
            }
        }

        private IEnumerator InitRightHand(OVRSkeleton hand)
        {
            while (!hand.IsInitialized)
            {
                yield return new WaitForSeconds(.1f);
            }

            foreach (var bone in hand.Bones)
            {
                SetTransformProvider($"Right{bone.Id}_Position", $"Left{bone.Id}_Rotation", bone.Transform);
            }
        }

        private void GetLeftHand(OVRSkeleton[] handControllers,
            out OVRSkeleton hand)
        {
            if (handControllers != null && handControllers.Length > 0)
            {
                foreach (var hc in handControllers)
                {
                    if (hc.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft)
                    {
                        hand = hc;
                        return;
                    }
                }
            }
            hand = null;
        }

        private void GetRightHand(OVRSkeleton[] handControllers,
            out OVRSkeleton hand)
        {
            if (handControllers != null && handControllers.Length > 0)
            {
                foreach (var hc in handControllers)
                {
                    if (hc.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
                    {
                        hand = hc;
                        return;
                    }
                }
            }
            hand = null;
        }

        private void SetTransformProvider(string posNode, string rotNode, Transform transform)
        {
            if (posNode == string.Empty && rotNode == string.Empty)
            {
                return;
            }

            if (!transform)
            {
                Debug.LogWarning("Could not find a hint source. Has the Ubiq player prefab changed?");
                return;
            }

            var hp = gameObject.AddComponent<TransformAvatarHintProvider>();
            var manager = GetComponent<AvatarManager>();
            hp.hintTransform = transform;
            if (posNode != string.Empty)
            {
                manager.hints.SetProvider(posNode, AvatarHints.Type.Vector3, hp);
            }
            if (rotNode != string.Empty)
            {
                manager.hints.SetProvider(rotNode, AvatarHints.Type.Quaternion, hp);
            }
        }

        private void SetGripProvider(string node, HandController handController)
        {
            if (node == string.Empty)
            {
                return;
            }

            if (!handController)
            {
                Debug.LogWarning("Could not find a hint source. Has the Ubiq player prefab changed?");
                return;
            }

            var hp = gameObject.AddComponent<GripAvatarHintProvider>();
            var manager = GetComponent<AvatarManager>();
            hp.controller = handController;
            manager.hints.SetProvider(node, AvatarHints.Type.Float, hp);
        }
    }
}
