using Ubiq.XR;
using UnityEngine;
using Ubiq.Messaging;
using VaSiLi.Interfaces;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to synchronize the transform data of an object with another
    /// </summary>
    public class GrabSyncTransform : MonoBehaviour, IOwnable
    {
        private HandGrabInteractable[] interactables;

        private NetworkContext context;
        private bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }
        [Tooltip("The transform that will be updated.")]
        [SerializeField, Optional]
        public Transform targetTransform;
        [Tooltip("The rigidbody to set the correct (kinematic) state.")]
        [SerializeField, Optional]
        public Rigidbody targetRigidbody;
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
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
            if (targetRigidbody == null)
            {
                TryGetComponent(out targetRigidbody);
            }
        }

        private void Awake()
        {
            Owner = false;
            interactables = GetComponentsInChildren<HandGrabInteractable>();
            foreach (var interactable in interactables)
            {
                interactable.WhenSelectingInteractorAdded.Action += (HandGrabInteractor i) => OnSelectEntered(i, interactable);
            }
        }

        private void OnSelectEntered(HandGrabInteractor interactor, HandGrabInteractable interactable)
        {
            Owner = true;
        }

        private void Update()
        {
            if (Owner)
                context.SendJson(new Message(MessageType.Physics, targetTransform));

        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.type == MessageType.Physics)
            {
                if (targetRigidbody != null)
                {
                    targetRigidbody.isKinematic = true;
                }
                targetTransform.localPosition = msg.transform.position;
                targetTransform.localRotation = msg.transform.rotation;
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
                    context.SendJson(new Message(MessageType.ChangeOwner, targetTransform));
            }
        }
    }
}