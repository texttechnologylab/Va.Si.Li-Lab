using UnityEngine;
using Ubiq.Avatars;
using Ubiq.MotionMatching;

public interface IMetaOVRInput : AvatarInput.IInput
{
    /// <summary>
    /// Head position and rotation in world space.
    /// </summary>
    Pose avatar { get; }
    /// <summary>
    /// Head position and rotation in world space.
    /// </summary>
    Pose head { get; }

    /// <summary>
    /// Left hand position and rotation in world space.
    /// </summary>
    Pose leftHand { get; }

    /// <summary>
    /// Right hand position and rotation in world space.
    /// </summary>
    Pose rightHand { get; }

    /// <summary>
    /// How tightly the left hand is making a fist in [0,1] where 0 is released.
    /// </summary>
    float leftGrip { get; }

    /// <summary>
    /// How tightly the right hand is making a fist in [0,1] where 0 is released.
    /// </summary>
    float rightGrip { get; }
}