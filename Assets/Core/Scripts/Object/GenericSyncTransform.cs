using Ubiq.XR;
using UnityEngine;
using Ubiq.Messaging;
using VaSiLi.Interfaces;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to synchronize the transform data of an object with another
    /// </summary>
    public class GenericSyncTransform : MonoBehaviour, IOwnable
    {
        private NetworkContext context;
        private bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }

        public enum MessageType
        {
            Physics,
            ChangeOwner
        }

        public struct Message
        {
            public MessageType type;
            public TransformMessage transform;

            public Message(MessageType type, Transform transform)
            {
                this.type = type;
                this.transform = new TransformMessage(transform);
            }
        }

        protected void Start()
        {
            context = NetworkScene.Register(this);
        }

        private void Awake()
        {
            Owner = false;
        }


        private void Update()
        {
            if (Owner)
                context.SendJson(new Message(MessageType.Physics, transform));

        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.type == MessageType.Physics)
            {
                transform.localPosition = msg.transform.position;
                transform.localRotation = msg.transform.rotation;
            }
            else if (msg.type == MessageType.ChangeOwner)
            {
                Owner = false;
            }
        }

        private void setOwner(bool value)
        {
            _owner = value;
            if (Owner)
            {
                if (context.Scene)
                    // Tell the other clients they lost ownership of this object
                    context.SendJson(new Message(MessageType.ChangeOwner, transform));
            }
        }
    }
}