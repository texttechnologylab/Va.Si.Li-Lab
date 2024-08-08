using Ubiq.XR;
using UnityEngine;
using Ubiq.Messaging;
using VaSiLi.Interfaces;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to make an object grabbable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GenericGrabbable : MonoBehaviour, IGraspable, IOwnable
    {
        public float maxVelocity = Mathf.Infinity;
        public Transform targetTransform;
        private NetworkContext context;
        private Hand hand;
        private Rigidbody body;
        private Vector3 localGrabPoint;
        private Quaternion localGrabRotation;
        private Vector3 centerOfMass;
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
            body = GetComponent<Rigidbody>();
            if (!targetTransform)
                targetTransform = transform;
            Owner = false;
        }

        public void Grasp(Hand controller)
        {
            hand = controller;
            Transform handTransform = hand.transform;
            localGrabPoint = handTransform.InverseTransformPoint(targetTransform.position);
            localGrabRotation = Quaternion.Inverse(handTransform.rotation) * targetTransform.rotation;

            Owner = true;
        }

        public void Release(Hand controller)
        {
            if (controller == hand)
            {
                hand = null;
                context.SendJson(new Message(MessageType.ChangeOwner, targetTransform));
            }
        }

        private void Update()
        {
            if (hand)
            {
                //TODO: Consider using Joints instead to follow the players hand
                var prevPosition = targetTransform.position;
                var prevRotation = targetTransform.rotation;
                var newRotation = hand.transform.rotation * localGrabRotation;
                var newPosition = hand.transform.TransformPoint(localGrabPoint);

                var velocity = newPosition - prevPosition;
                if (velocity.sqrMagnitude >= maxVelocity * maxVelocity)
                {
                    Owner = false;
                }

                // Apply velocities
                body.AddForce(velocity, ForceMode.Impulse);
                body.AddTorque(newRotation.eulerAngles - prevRotation.eulerAngles, ForceMode.Impulse);

            }
            if (Owner)
                context.SendJson(new Message(MessageType.Physics, targetTransform));

        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.type == MessageType.Physics)
            {
                targetTransform.localPosition = msg.transform.position;
                targetTransform.localRotation = msg.transform.rotation;
            }
            else if (msg.type == MessageType.ChangeOwner)
            {
                Owner = false;
            }
        }

        public void setOwner(bool value)
        {
            _owner = value;
            if (Owner)
            {
                if (context.Scene)
                    // Tell the other clients they lost ownership of this object
                    context.SendJson(new Message(MessageType.ChangeOwner, transform));
            }
            else
            {
                hand = null;
            }
        }
    }
}
