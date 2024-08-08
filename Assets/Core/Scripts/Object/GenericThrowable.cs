using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.XR;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Messaging;
using VaSiLi.Interfaces;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to make an object grabbable and throwable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GenericThrowable : MonoBehaviour, IGraspable, IOwnable
    {
        private static int VELOCITY_LENGTH = 5;
        private Vector3[] releaseVelocities = new Vector3[VELOCITY_LENGTH];
        private int updateCount = 0;
        private NetworkContext context;
        private Hand hand;
        private Collider[] _collider;
        private Rigidbody body;
        private Vector3 localGrabPoint;
        private Quaternion localGrabRotation;
        private Vector3 centerOfMass;

        private bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }

        private bool released;

        public bool enable_grasp = true; 


        public enum MessageType
        {
            Physics,
            ChangeOwner
        }

        public struct Message
        {
            public MessageType type;
            public bool released;
            public TransformMessage transform;
            public bool kinematic;

            public Message(MessageType type, bool released, Transform transform, bool kinematic)
            {
                this.type = type;
                this.released = released;
                this.transform = new TransformMessage(transform);
                this.kinematic = kinematic;
            }
        }

        protected void Start()
        {
            context = NetworkScene.Register(this);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            Owner = false;
            _collider = GetComponentsInChildren<Collider>();
        }

        public void Grasp(Hand controller)
        {
            if (!enable_grasp)
                return;

            // Save the grab points
            hand = controller;
            Transform handTransform = hand.transform;
            localGrabPoint = handTransform.InverseTransformPoint(transform.position);
            localGrabRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;

            // Set the body to be kinematic
            body.isKinematic = true;
            Owner = true;

            // If the object has a collider disable it so it can't move objects
            this.gameObject.layer = LayerMask.NameToLayer("GraspedObject");
            //context.SendJson(new Message(MessageType.ChangeOwner, false, transform, true));
            setLayer(true);
        }

        public void Release(Hand controller)
        {
            if (controller == hand)
            {
                // Reset the various settings
                body.isKinematic = false;
                body.useGravity = true;
                released = true;
                hand = null;

                // If the object has a collider enable it again
                if (_collider != null)
                    _collider.ToList().ForEach((collider) => collider.enabled = true);
                context.SendJson(new Message(MessageType.ChangeOwner, true, transform, false));
                setLayer(false);
            }
        }

        private void Update()
        {
            if (hand)
            {
                // If the object is in a hand update the positions
                var prevPosition = transform.position;
                transform.rotation = hand.transform.rotation * localGrabRotation;
                transform.position = hand.transform.TransformPoint(localGrabPoint);
                // Log the velocities to aid in throwing the object consistently
                releaseVelocities[updateCount % VELOCITY_LENGTH] = (transform.position - prevPosition) / Time.fixedDeltaTime;
                updateCount++;
            }
            if (released)
            {
                // Get the average of the last velocities
                Vector3 releaseVelocity = new Vector3(
                releaseVelocities.Average(x => x.x),
                releaseVelocities.Average(x => x.y),
                releaseVelocities.Average(x => x.z));
                // Add the force to the object
                body.AddForce(releaseVelocity, ForceMode.Impulse);
                released = false;
            }
            if (Owner)
                context.SendJson(new Message(MessageType.Physics, false, transform, body.isKinematic));

        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.type == MessageType.Physics)
            {
                transform.localPosition = msg.transform.position;
                transform.localRotation = msg.transform.rotation;
                body.isKinematic = msg.kinematic;
            }
            else if (msg.type == MessageType.ChangeOwner)
            {
                // If the object has a collider disable/enable it depending if it was released or not
                Owner = false;
                body.isKinematic = !msg.released;
                setLayer(!msg.released);
                body.useGravity = msg.released; //Stop obs from flying around ...
            }

        }

        private void setLayer(bool grabbed)
        {
            this.GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToList().ForEach(go => go.layer = grabbed ? LayerMask.NameToLayer("GraspedObject") : 0);
        }

        private void setOwner(bool value)
        {
            _owner = value;
            if (Owner)
            {
                if (context.Scene)
                    // Tell the other clients they lost ownership of this object
                    context.SendJson(new Message(MessageType.ChangeOwner, released, transform, body.isKinematic));
            }
            else
            {
                hand = null;
            }
        }
    }
}
