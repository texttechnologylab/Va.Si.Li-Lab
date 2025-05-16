using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;
using Ubiq.Spawning;
using VaSiLi.Interfaces;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using Org.BouncyCastle.Crypto.Modes;

namespace VaSiLi.Object.Drawing
{
    /// <summary>
    /// An obejct that allows the user to grab it and draw lines freely wherever they want
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class Pen : MonoBehaviour, INetworkSpawnable, IOwnable//, IGraspable, IUseable, IOwnable, INetworkSpawnable
    {
        private NetworkContext context;
        public GameObject drawingPrefab;
        private Transform nib;
        public GameObject localDrawing;
        private GameObject currentDrawing;
        private bool isDrawing;
        public NetworkId NetworkId { get; set; }
        private bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable;
        public enum MessageType
        {
            Ownership,
            Position
        }
        // Amend message to also store current drawing state
        private struct Message
        {
            public MessageType type;
            public bool isOwner;
            public Vector3 position;
            public Quaternion rotation;

            public Message(MessageType type, bool owner, Transform transform)
            {
                this.type = type;
                this.isOwner = owner;
                this.position = transform.position;
                this.rotation = transform.rotation;
            }
        }

        private void Start()
        {
            // Get the tip of the pen
            nib = transform.Find("Grip/Nib");
            context = NetworkScene.Register(this);
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            interactable.selectEntered.AddListener(Select);
            interactable.selectExited.AddListener(UnSelect);
            interactable.activated.AddListener(Use);
            interactable.deactivated.AddListener(UnUse);
        }

        private void FixedUpdate()
        {
            if (Owner)
            {
                context.SendJson(new Message(MessageType.Position, false, transform));
            }
        }

        private void Select(SelectEnterEventArgs eventArgs)
        {
            Owner = true;
            SetLayer(true);
        }
        private void UnSelect(SelectExitEventArgs eventArgs)
        {
            Owner = false;
            SetLayer(false);
        }
        void Use(ActivateEventArgs interactor)
        {
            BeginDrawing();
        }
        void UnUse(DeactivateEventArgs args)
        {
            EndDrawing();
        }

        private void BeginDrawing()
        {
            localDrawing.GetComponent<LineTrail>().SetEmitting(true, true);
            var startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            var endColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            localDrawing.GetComponent<LineTrail>().SetColors(startColor, endColor, true);

            // Spawn the drawing that will persist
            currentDrawing = NetworkSpawnManager.Find(this).SpawnWithPeerScope(drawingPrefab);
            // Set the colors
            currentDrawing.GetComponent<LineTrail>().SetColors(startColor, endColor, true);
        }

        private void EndDrawing()
        {
            if (!currentDrawing)
                return;

            var trailRenderer = localDrawing.GetComponent<TrailRenderer>();
            Vector3[] positions = new Vector3[trailRenderer.positionCount];
            trailRenderer.GetPositions(positions);

            // Make sure to stop the current element
            localDrawing.GetComponent<LineTrail>().SetEmitting(false, true);
            trailRenderer.Clear();

            // Set the position the new drawing
            currentDrawing.GetComponent<LineTrail>().SetPositions(positions, localDrawing.transform.position, localDrawing.transform.rotation, true);
            currentDrawing = null;
        }

        private void setOwner(bool value)
        {
            _owner = value;

            if (Owner)
            {
                // If the object was just spawned the context doesn't have a Scene yet
                if (context.Scene)
                    // Tell the other clients they lost ownership of this object
                    context.SendJson(new Message(MessageType.Ownership, false, transform));
            }
            else
            {
                // If we lost ownership, end the drawing (if applicable) and unasssign the controller
                if (localDrawing.GetComponent<TrailRenderer>().emitting)
                {
                    EndDrawing();
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
        {
            var data = msg.FromJson<Message>();
            if (data.type == MessageType.Position)
            {
                transform.position = data.position;
                transform.rotation = data.rotation;
            }
            else if (data.type == MessageType.Ownership)
            {
                SetLayer(data.isOwner);
                Owner = data.isOwner;
            }
        }

        private void SetLayer(bool grabbed)
        {
            GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToList().ForEach(go => go.layer = grabbed ? LayerMask.NameToLayer("GraspedObject") : 0);
        }

    }
}