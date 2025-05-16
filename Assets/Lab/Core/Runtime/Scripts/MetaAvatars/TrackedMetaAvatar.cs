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
        public PoseUpdateEvent OnAvatarUpdate;
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

            if (!IsInvalid(state[0].avatar))
            {
                // Make sure the avatar is in the same positon as the HMD
                this.transform.position = state[0].avatar.position;
                this.transform.rotation = state[0].avatar.rotation;
            }

            // The avatars themselves are solved via this.
            dataByteCount = avatar.GetActiveAvatarScript().RecordStreamData_AutoBuffer(StreamLOD.Full, ref dataBuffer);

            // Send it through network
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
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

            // Sending Meta Avatar stuff. 
            var message2 = ReferenceCountedSceneGraphMessage.Rent(dataBuffer.Length);
            dataBuffer.ToArray().CopyTo(new Span<byte>(message2.bytes, message2.start, message2.length));
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
                byte[] data = new ReadOnlySpan<byte>(message.bytes, message.start, message.length).ToArray();
                ReceivePacketData(data, StreamLOD.Full);
            }
        }

        // State has been set either remotely or locally so update listeners
        private void OnStateChange()
        {
            OnAvatarUpdate.Invoke(FromNetwork(state[0].avatar));
            OnHeadUpdate.Invoke(FromNetwork(state[0].head));
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
