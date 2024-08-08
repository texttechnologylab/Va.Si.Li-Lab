using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;
using Ubiq.Spawning;
using VaSiLi.Interfaces;
using UnityEngine.UI;
using System.Collections.Generic;


namespace VaSiLi.VAnnotator
{
    /** README:
    *  This is the base class for all interactable VAnnotator objects in the scene.
    *  To create a new interactable object, create a new script that inherits from this class.
    *  The coresponding prefab needs to be added in the SpawnManager catalogue.
    */

    public class VAInteractableObject : Selectable, IGraspable, IOwnable, INetworkSpawnable, IDistanceUseable
    {
        public Transform spawnRelativeTransform;
        protected NetworkContext context;
        protected Hand controller;
        public bool disableGrab = false;
        public NetworkId NetworkId { get; set; }
        protected bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }


        private Vector3 localGrabPoint;
        private Quaternion localGrabRotation;
        private Quaternion grabHandRotation;

        private Collider obj_collider;

        public List<VAInteractableLink> linkList;
        private GameObject linkPrefab;
        public enum MessageType
        {
            Ownership,
            Position,
            Data
        }

        // Amend message to also store current drawing state
        protected struct Message
        {
            public MessageType type;
            public bool isOwner;
            public TransformMessage transform;
            public string jsonString; //This usually contaisn the data of the specific vannotator object

            public Message(MessageType type, bool owner, Transform transform, string jsonString)
            {
                this.type = type;
                this.isOwner = owner;
                this.transform = new TransformMessage(transform);
                this.jsonString = jsonString;
            }
        }

        protected override void Start()
        {
            base.Start();
            context = NetworkScene.Register(this);
            linkList = new List<VAInteractableLink>();
            linkPrefab = (GameObject)Resources.Load("VAInteractableObjects/VAInteractableLink");
            obj_collider = GetComponentInChildren<Collider>();
        }


        public void FixedUpdate()
        {
            if (Owner) //Only happens, when grabbing the object.
            {
                context.SendJson(new Message(MessageType.Position, false, transform, ""));
            }
        }

        private void Update()
        {
            if (controller)
            {
                transform.rotation = controller.transform.rotation * localGrabRotation;
                transform.position = controller.transform.TransformPoint(localGrabPoint);
                for (int i = 0; i < linkList.Count; i++)
                    linkList[i].UpdatePosition();
            }
        }

        void IGraspable.Grasp(Hand controller)
        {
            if (disableGrab)
                return;
            Owner = true;
            this.controller = controller;

            Transform handTransform = controller.transform;
            localGrabPoint = handTransform.InverseTransformPoint(transform.position); //transform.InverseTransformPoint(handTransform.position);
            localGrabRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
            grabHandRotation = handTransform.rotation;
        }

        void IGraspable.Release(Hand controller)
        {
            if (disableGrab)
                return;
            Owner = false;
            this.controller = null;
        }


        private void setOwner(bool value)
        {
            _owner = value;

            if (Owner)
            {
                // If the object was just spawned the context doesn't have a Scene yet
                if (context.Scene)
                    // Tell the other clients they lost ownership of this object
                    context.SendJson(new Message(MessageType.Ownership, false, transform, ""));
            }
            else
            {
                this.controller = null;
            }
        }

        public virtual void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            switch (msg.type)
            {
                case MessageType.Ownership:
                    Owner = msg.isOwner;
                    break;
                case MessageType.Position:
                    transform.localPosition = msg.transform.position;
                    transform.localRotation = msg.transform.rotation;
                    for (int i = 0; i < linkList.Count; i++)
                        linkList[i].UpdatePosition();
                    break;
            }
        }

        public void DistanceUse(Hand controller)
        {
            return;
        }

        public virtual void Request()
        {
            var cam = Camera.main.transform;
            transform.position = cam.TransformPoint(spawnRelativeTransform.localPosition);
            transform.rotation = cam.rotation * spawnRelativeTransform.localRotation;
            gameObject.SetActive(true);
        }

        public void DistanceLink(Hand controller, IDistanceUseable ground)
        {
            GameObject link = NetworkSpawnManager.Find(this).SpawnWithPeerScope(linkPrefab);
            VAInteractableLink link_obj = link.GetComponent<VAInteractableLink>();
            link_obj.Figure = this;
            link_obj.Ground = ground as VAInteractableObject;
            link_obj.UpdatePosition();

            StartCoroutine(link_obj.Init());
        }

        public Vector3 GetCenter()
        {
            if(obj_collider != null)
                return obj_collider.bounds.center;
            else
                return transform.position;
        }

        public void Delete()
        {
            NetworkSpawnManager.Find(this).Despawn(gameObject);
        }
    }


}