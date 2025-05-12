using Ubiq.XR;
using UnityEngine;
using Ubiq.Messaging;
using VaSiLi.Interfaces;
using UnityEngine.XR.Interaction.Toolkit;
using System;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to make an object grabbable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    [Obsolete("Use the new GrabSyncTransform instead alongside Meta Interactables.")]
    public class GenericGrabbable : MonoBehaviour, IOwnable
    {
        public Transform targetTransform;
        private NetworkContext context;
        private bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable;

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
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            interactable.selectEntered.AddListener(Select);
            interactable.selectExited.AddListener(UnSelect);
            //interactable.activated.AddListener(XRGrabInteractable_Activated);
        }

        private void OnDestroy()
        {
            interactable.activated.RemoveListener(XRGrabInteractable_Activated);
        }

        private void Awake()
        {
            if (!targetTransform)
                targetTransform = transform;
            Owner = false;
        }
        private void Select(SelectEnterEventArgs eventArgs)
        {
            Owner = true;
            //SetLayer(true);
        }
        private void UnSelect(SelectExitEventArgs eventArgs)
        {
            Owner = false;
            //SetLayer(false);
        }
        public void XRGrabInteractable_Activated(ActivateEventArgs eventArgs)
        {
            Owner = !Owner;
            // Force the interactor(hand) to drop the firework
            /*var interactor = (XRBaseInteractor)eventArgs.interactorObject;
            interactor.allowSelect = false;
            var interactable = (XRGrabInteractable)eventArgs.interactableObject;
            interactable.enabled = false;
            interactor.allowSelect = true;*/
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
        }
    }
}
