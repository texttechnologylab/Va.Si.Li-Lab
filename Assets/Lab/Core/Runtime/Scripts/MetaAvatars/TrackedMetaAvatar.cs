using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Unity.Collections;
using static Oculus.Avatar2.OvrAvatarEntity;
using Oculus.Avatar2;
using Ubiq;
using Ubiq.Geometry;


namespace VaSiLi.MetaAvatar
{
    [RequireComponent(typeof(MetaAvatar))]
    public class TrackedMetaAvatar : MonoBehaviour
    {
        [Tooltip("The Avatar to use as the source of input. If null, will try to find an Avatar among parents at start.")]
        [SerializeField] private MetaAvatar avatar;

        [Serializable]
        public class PoseUpdateEvent : UnityEvent<InputVar<Pose>> { }
        [Serializable]
        public class PosePoseUpdateEvent : UnityEvent<InputVar<Pose>, InputVar<Pose>> { }
        public PoseUpdateEvent OnAvatarUpdate;
        public PosePoseUpdateEvent OnAvatarHeadUpdate;
        public PoseUpdateEvent OnHeadUpdate;
        public PoseUpdateEvent OnLeftHandUpdate;
        public PoseUpdateEvent OnRightHandUpdate;

        [Serializable]
        public class GripUpdateEvent : UnityEvent<InputVar<float>> { }
        public GripUpdateEvent OnLeftGripUpdate;
        public GripUpdateEvent OnRightGripUpdate;

        [Serializable]
        private struct State
        {
            public Pose avatar;
            public Pose head;
            public Pose leftHand;
            public Pose rightHand;
            public float leftGrip;
            public float rightGrip;
        }
        private State[] state = new State[1];
        private NetworkContext context;
        private Transform networkSceneRoot;
        private float lastTransmitTime;
        public NativeArray<byte> dataBuffer;
        public uint dataByteCount;

        // Reusable buffers so Send()/ProcessMessage() don't allocate a byte[] per network tick.
        private byte[] _sendScratch;
        private byte[] _recvScratch;

        protected void Start()
        {
            //TODO: 
            /*if (!avatar)
            {
                avatar = GetComponentInParent<MetaAvatar>();
                if (!avatar)
                {
                    Debug.LogWarning("No Avatar could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }*/
            avatar = GetComponent<MetaAvatar>();
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, nameof(TrackedMetaAvatar)));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;

            AvatarLODManager.Instance.firstPersonAvatarLod = avatar.GetActiveAvatarScript().AvatarLOD;
            AvatarLODManager.Instance.enableDynamicStreaming = true;

        }


        private void Update()
        {
            if (!avatar.IsLocal || avatar.GetActiveAvatarScript().CurrentState < AvatarState.Skeleton)
            {
                return;
            }

            // Update state from hints
            // I left it default for now. But tehoretically currently only the HeadUpdater is used
            state[0] = avatar.input.TryGet(out IMetaOVRInput src)
            ? new State
            {
                avatar = src.avatar,
                head = src.head,
                leftHand = src.leftHand,
                rightHand = src.rightHand,
                leftGrip = src.leftGrip,
                rightGrip = src.rightGrip
            }
            : new State
            {
                avatar = ToNetwork(InputVar<Pose>.invalid),
                head = ToNetwork(InputVar<Pose>.invalid),
                leftHand = ToNetwork(InputVar<Pose>.invalid),
                rightHand = ToNetwork(InputVar<Pose>.invalid),
                leftGrip = ToNetwork(InputVar<float>.invalid),
                rightGrip = ToNetwork(InputVar<float>.invalid)
            };

            // Record + send the avatar stream at UpdateRate, not every frame. The recorded
            // buffer is only consumed by Send(), so recording on non-transmit frames is wasted
            // work (and re-runs the SDK's per-frame record path). Caps avatar recording at
            // UpdateRate once the app runs faster than it.
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                // Medium LOD is cheaper to record (CPU) and produces smaller packets than Full,
                // and is visually indistinguishable for a networked remote representation. Full
                // detail isn't perceptible on another participant's avatar at interaction distance.
                dataByteCount = avatar.GetActiveAvatarScript().RecordStreamData_AutoBuffer(StreamLOD.Medium, ref dataBuffer);
                Send();
            }

            // Update local listeners
            OnStateChange();
        }

        private Pose ToNetwork(InputVar<Pose> input)
        {
            return input.valid
                ? Transforms.ToLocal(input.value, networkSceneRoot)
                : GetInvalidPose();
        }

        private float ToNetwork(InputVar<float> input)
        {
            return input.valid ? input.value : GetInvalidFloat();
        }

        private InputVar<Pose> FromNetwork(Pose net)
        {
            return !IsInvalid(net)
                ? new InputVar<Pose>(Transforms.ToWorld(net, networkSceneRoot))
                : InputVar<Pose>.invalid;
        }

        private InputVar<float> FromNetwork(float net)
        {
            return !IsInvalid(net)
                ? new InputVar<float>(net)
                : InputVar<float>.invalid;
        }


        private void Send()
        {
            // Co-ords from hints are already in local to our network scene
            // so we can send them without any changes
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<State>(state));
            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));
            context.Send(message);

            // Sending Meta Avatar stuff. Copy the NativeArray into the pooled message via a
            // reusable scratch buffer instead of allocating with ToArray() every transmit.
            if (_sendScratch == null || _sendScratch.Length != dataBuffer.Length)
                _sendScratch = new byte[dataBuffer.Length];
            dataBuffer.CopyTo(_sendScratch);
            var message2 = ReferenceCountedSceneGraphMessage.Rent(dataBuffer.Length);
            Array.Copy(_sendScratch, 0, message2.bytes, message2.start, message2.length);
            context.Send(message2);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // Consider creating a separate tracker for meta avatar things
            if (message.length == Marshal.SizeOf<State>())
            {
                MemoryMarshal.Cast<byte, State>(
                    message.bytes.AsSpan(message.start, message.length))
                .CopyTo(state);
                OnStateChange();
            }
            else if (message.length > 0)
            {
                // Reuse an exact-length buffer (avatar stream size is stable per LOD, so this
                // reallocates only when the size actually changes) instead of ToArray() per packet.
                if (_recvScratch == null || _recvScratch.Length != message.length)
                    _recvScratch = new byte[message.length];
                Array.Copy(message.bytes, message.start, _recvScratch, 0, message.length);
                ReceivePacketData(_recvScratch, StreamLOD.Full);
            }
        }

        // State has been set either remotely or locally so update listeners
        private void OnStateChange()
        {
            OnAvatarUpdate.Invoke(FromNetwork(state[0].avatar));
            OnHeadUpdate.Invoke(FromNetwork(state[0].head));
            OnAvatarHeadUpdate.Invoke(FromNetwork(state[0].head), FromNetwork(state[0].avatar));
            OnLeftHandUpdate.Invoke(FromNetwork(state[0].leftHand));
            OnRightHandUpdate.Invoke(FromNetwork(state[0].rightHand));
            OnLeftGripUpdate.Invoke(FromNetwork(state[0].leftGrip));
            OnRightGripUpdate.Invoke(FromNetwork(state[0].rightGrip));
        }

        private void ReceivePacketData(byte[] data, StreamLOD lod)
        {
            OvrAvatarEntity entity = avatar.GetActiveAvatarScript();
            if (entity.CurrentState >= AvatarState.Skeleton)
            {
                entity.ApplyStreamData(data);
            }
        }
        private static Pose GetInvalidPose()
        {
            return new Pose(new Vector3 { x = float.NaN }, Quaternion.identity);
        }

        private static float GetInvalidFloat()
        {
            return float.NaN;
        }

        private static bool IsInvalid(Pose p)
        {
            return float.IsNaN(p.position.x);
        }

        private static bool IsInvalid(float f)
        {
            return float.IsNaN(f);
        }
        private void OnDestroy()
        {
            if (dataBuffer.GetBufferSize() > 0)
            {
                dataBuffer.Dispose();
            }
        }
    }
}
