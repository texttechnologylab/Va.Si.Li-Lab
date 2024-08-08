using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Unity.Collections;
using static Oculus.Avatar2.OvrAvatarEntity;
using Oculus.Avatar2;


namespace VaSiLi.MetaAvatar
{
    [RequireComponent(typeof(MetaAvatar))]
    public class TrackedMetaAvatar : MonoBehaviour
    {
        private struct PositionRotation
        {
            public Vector3 position;
            public Quaternion rotation;

            public static PositionRotation identity
            {
                get
                {
                    return new PositionRotation
                    {
                        position = Vector3.zero,
                        rotation = Quaternion.identity
                    };
                }
            }
        }

        [Serializable]
        public class TransformUpdateEvent : UnityEvent<Vector3, Quaternion> { }
        public TransformUpdateEvent OnAvatarUpdate;
        public TransformUpdateEvent OnHeadUpdate;
        public TransformUpdateEvent OnLeftHandUpdate;
        public TransformUpdateEvent OnRightHandUpdate;

        [Serializable]
        public class GripUpdateEvent : UnityEvent<float> { }
        public GripUpdateEvent OnLeftGripUpdate;
        public GripUpdateEvent OnRightGripUpdate;

        private NetworkContext context;
        private Transform networkSceneRoot;
        private State[] state = new State[1];
        private MetaAvatar avatar;
        private float lastTransmitTime;

        // I left it default for now. But tehoretically currently only the HeadUpdater is usedtr
        [Serializable]
        private struct State
        {
            public PositionRotation avatar;
            public PositionRotation head;
            public PositionRotation leftHand;
            public PositionRotation rightHand;
            public float leftGrip;
            public float rightGrip;
        }

        protected void Start()
        {
            avatar = GetComponent<MetaAvatar>();
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, "ThreePointTracked"));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;

            AvatarLODManager.Instance.firstPersonAvatarLod = avatar.GetActiveAvatarScript().AvatarLOD;
            AvatarLODManager.Instance.enableDynamicStreaming = true;

            //localCam = 
        }

        public NativeArray<byte> dataBuffer;// = default; // = new byte[16 * 1024];
        public uint dataByteCount;
        private void Update()
        {
            if (avatar.IsLocal && avatar.GetActiveAvatarScript().CurrentState >= AvatarState.Skeleton)
            {

                // Update state from hints
                // I left it default for now. But tehoretically currently only the HeadUpdater is usedtr
                state[0].avatar = GetPosRotHint("AvatarPosition", "AvatarRotation");
                state[0].head = GetPosRotHint("HeadPosition", "HeadRotation");
                state[0].leftHand = GetPosRotHint("LeftHandPosition", "LeftHandRotation");
                state[0].rightHand = GetPosRotHint("RightHandPosition", "RightHandRotation");
                state[0].leftGrip = GetFloatHint("LeftGrip");
                state[0].rightGrip = GetFloatHint("RightGrip");

                // I left it default for now. But tehoretically currently only the HeadUpdater is usedtr 
                // The avatars themselves are solved via this.
                //dataBuffer = new NativeArray<byte>();
                dataByteCount = avatar.GetActiveAvatarScript().RecordStreamData_AutoBuffer(StreamLOD.Full, ref dataBuffer);
                
                // Send it through network
                if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
                {
                    lastTransmitTime = Time.time;
                    Send();
                }

                // Update local listeners
                OnRecv();
            }
        }

        // Local to world space
        private PositionRotation TransformPosRot(PositionRotation local, Transform root)
        {
            var world = new PositionRotation();
            world.position = root.TransformPoint(local.position);
            world.rotation = root.rotation * local.rotation;
            return world;
        }

        // World to local space
        private PositionRotation InverseTransformPosRot(PositionRotation world, Transform root)
        {
            var local = new PositionRotation();
            local.position = root.InverseTransformPoint(world.position);
            local.rotation = Quaternion.Inverse(root.rotation) * world.rotation;
            return local;
        }

        private PositionRotation GetPosRotHint(string position, string rotation)
        {
            if (avatar == null || avatar.hints == null)
            {
                return PositionRotation.identity;
            }

            var posrot = PositionRotation.identity;
            if (avatar.hints.TryGetVector3(position, out var pos))
            {
                posrot.position = pos;
            }
            if (avatar.hints.TryGetQuaternion(rotation, out var rot))
            {
                posrot.rotation = rot;
            }
            return InverseTransformPosRot(posrot, networkSceneRoot);
        }

        private float GetFloatHint(string node)
        {
            if (avatar == null || avatar.hints == null)
            {
                return 0.0f;
            }

            if (avatar.hints.TryGetFloat(node, out var f))
            {
                return f;
            }
            return 0.0f;
        }

 
        private void Send()
        {
            // Co-ords from hints are already in local to our network scene
            // so we can send them without any changes
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<State>(state));
            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));
            context.Send(message);

            // Sending Meta Avatar stuff. 
            var message2 = ReferenceCountedSceneGraphMessage.Rent(dataBuffer.Length);
            dataBuffer.ToArray().CopyTo(new Span<byte>(message2.bytes, message2.start, message2.length));
            context.Send(message2);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // Not pretty, but it works. 
            // It is better to create a separate tracker so that the two parts are solved independently in different Scrips.
            try
            {
                MemoryMarshal.Cast<byte, State>(
                    new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                    .CopyTo(new Span<State>(state));
                OnRecv();
            }
            catch (Exception)
            {
                byte[] data = new ReadOnlySpan<byte>(message.bytes, message.start, message.length).ToArray();
                ReceivePacketData(data, StreamLOD.Full);
            }
        }

        // State has been set either remotely or locally so update listeners
        private void OnRecv()
        {
            // Transform with our network scene root to get world position/rotation
            var avatar = TransformPosRot(state[0].avatar, networkSceneRoot);
            var head = TransformPosRot(state[0].head, networkSceneRoot);
            var leftHand = TransformPosRot(state[0].leftHand, networkSceneRoot);
            var rightHand = TransformPosRot(state[0].rightHand, networkSceneRoot);
            var leftGrip = state[0].leftGrip;
            var rightGrip = state[0].rightGrip;

            OnAvatarUpdate.Invoke(avatar.position, avatar.rotation);
            OnHeadUpdate.Invoke(head.position, head.rotation);
            OnLeftHandUpdate.Invoke(leftHand.position, leftHand.rotation);
            OnRightHandUpdate.Invoke(rightHand.position, rightHand.rotation);
            OnLeftGripUpdate.Invoke(leftGrip);
            OnRightGripUpdate.Invoke(rightGrip);

        }

        private void ReceivePacketData(byte[] data, StreamLOD lod)
        {
            OvrAvatarEntity entity = avatar.GetActiveAvatarScript();
            if (entity.CurrentState >= AvatarState.Skeleton)
            {
                entity.ApplyStreamData(data);
            }
        }

        private void OnDestroy()
        {
            if(dataBuffer.GetBufferSize() > 0)
            {
                dataBuffer.Dispose();
            }
            
        }
    }
}
