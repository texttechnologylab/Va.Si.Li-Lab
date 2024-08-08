using Ubiq.XR;
using UnityEngine;
using Ubiq.Messaging;
using VaSiLi.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms;
using Ubiq.Avatars;
using System;
using System.Threading.Tasks;
using VaSiLi.Misc;
using VaSiLi.Networking;
using static VaSiLi.Networking.ResponsiveNetworking;
using Ubiq.Spawning;
using System.Collections;

namespace VaSiLi.Object
{
    /// <summary>
    /// Simple script to make an object grabbable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MultiGrabbable : MonoBehaviour, IGraspable, IOwnable, IUseable
    {
        private RoomClient roomClient;
        private HandController handController;
        public Transform targetTransform;
        public GameObject deletionPopOver;
        private NetworkContext context;
        private AvatarManager avatarManager;
        private List<GameObject> hands = new List<GameObject>();
        private Dictionary<GameObject, Joint> joints = new Dictionary<GameObject, Joint>();
        private Dictionary<GameObject, GameObject> spheres = new Dictionary<GameObject, GameObject>();
        private bool isDeleting;
        public float breakForce;
        public float breakTorque;
        public bool _owner;
        public bool Owner { get => _owner; set => setOwner(value); }
        public bool isOwned;
        public bool startupSync = false;
        public bool disableGravityOnPlace = true;
        public enum MessageType
        {
            Physics,
            ChangeOwner,
            Grab,
            LostGrab
        }

        [Serializable]
        public struct Message
        {
            public MessageType type;
            public TransformMessage transform;
            public bool isOwned;

            public string peerUUID;
            public bool grasp;
            public bool left;
            public Message(MessageType type, bool isOwned, Transform transform, string peerUUID = null, bool grasp = false, bool left = false)
            {
                this.type = type;
                this.isOwned = isOwned;
                this.transform = new TransformMessage(transform);
                this.peerUUID = peerUUID;
                this.grasp = grasp;
                this.left = left;
            }
        }

        protected void Start()
        {
            context = NetworkScene.Register(this);
            avatarManager = GameObject.Find("Avatar Manager").GetComponent<AvatarManager>();
        }

        private void Awake()
        {
            if (!targetTransform)
                targetTransform = transform;
            Owner = false;
            roomClient = GameObject.Find("Social Network Scene").GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SynchronizeData);
            avatarManager = GameObject.Find("Avatar Manager").GetComponent<AvatarManager>();
        }

        private void SynchronizeData(IPeer peer)
        {
            // Tell the other peers that someone is owning this object
            if (Owner)
            {
                context.SendJson(new Message(MessageType.ChangeOwner, true, targetTransform));
            }
            context.SendJson(new Message(MessageType.Physics, true, targetTransform));
        }

        public void Grasp(Hand hand)
        {
            var controller = hand.gameObject;
            startupSync = false;
            AddDebugSphere(controller);
            if (isOwned)
            {
                context.SendJson(new Message(MessageType.Grab, true, targetTransform, roomClient.Me.uuid, true));
            }
            else
            {
                Grasp(controller);
            }
        }
        private void Grasp(GameObject controller)
        {
            if (hands.Contains(controller))
                return;
            // TODO: Broadcast this
            this.gameObject.layer = LayerMask.NameToLayer("GraspedObject");
            hands.Add(controller);
            var localGrabPoint = transform.InverseTransformPoint(controller.transform.position);
            Joint joint = CreateJoint(gameObject);
            joint.anchor = localGrabPoint;
            joint.connectedBody = controller.GetComponent<Rigidbody>();
            joints[controller] = joint;
            Owner = true;
            transform.GetComponent<Rigidbody>().useGravity = true;
        }

        private void AddDebugSphere(GameObject controller)
        {
            if (spheres.ContainsKey(controller))
            {
                GameObject.Destroy(spheres[controller]);
                spheres.Remove(controller);
            }
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;

            sphere.transform.parent = this.transform;
            sphere.transform.rotation = this.targetTransform.rotation;
            sphere.transform.localScale = new Vector3((1 / this.targetTransform.localScale.x) * 0.05f, (1 / this.targetTransform.localScale.y) * 0.05f, (1 / this.targetTransform.localScale.z) * 0.05f);
            var localGrabPoint = transform.InverseTransformPoint(controller.transform.position);
            sphere.transform.localPosition = localGrabPoint;
            spheres.Add(controller, sphere);
        }


        private ConfigurableJoint CreateJoint(GameObject jointObject)
        {

            ConfigurableJoint joint = jointObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = true;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.breakForce = breakForce;
            joint.breakTorque = breakTorque;
            joint.angularXLimitSpring = new SoftJointLimitSpring { spring = 10f, damper = 5f };

            joint.angularYZLimitSpring = new SoftJointLimitSpring { spring = 10f, damper = 5f };
            joint.angularYLimit = new SoftJointLimit { limit = 3f };
            joint.angularZLimit = new SoftJointLimit { limit = 3f };
            joint.linearLimitSpring = new SoftJointLimitSpring { spring = 1000f, damper = 5f };
            joint.linearLimit = new SoftJointLimit { limit = 0.0001f };
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive = new JointDrive { positionSpring = 10f, positionDamper = 5f, maximumForce = 10f };

            return joint;
        }

        public void Release(Hand hand)
        {
            var controller = hand.gameObject;
            if (this == null)
                return;
            if (spheres.ContainsKey(controller))
            {
                GameObject.Destroy(spheres[controller]);
                spheres.Remove(controller);
            }
            if (isOwned)
            {
                context.SendJson(new Message(MessageType.Grab, true, targetTransform, roomClient.Me.uuid, false));
            }
            else
            {
                Release(controller);
            }

        }

        private void Release(GameObject controller)
        {
            Debug.Log("Release");
            if (hands.Contains(controller))
            {
                hands.Remove(controller);
                GameObject.Destroy(joints[controller]);
                joints.Remove(controller);
            }
            if (hands.Count == 0)
            {
                this.gameObject.layer = 0;
                this.Owner = false;
                transform.GetComponent<Rigidbody>().useGravity = disableGravityOnPlace ? false : true;
            }
        }

        private void Update()
        {
            var keys = new List<GameObject>();
            foreach (var key in joints.Keys)
            {
                if (joints[key] == null)
                {
                    keys.Add(key);
                }

            }
            foreach (var key in keys)
            {
                Debug.Log("Force-Release");
                joints.Remove(key);
                hands.Remove(key);
                if (spheres.ContainsKey(key))
                {
                    GameObject.Destroy(spheres[key]);
                    spheres.Remove(key);
                }
                else
                {
                    var avatar = key.GetComponentInParent<Ubiq.Avatars.Avatar>();
                    var left = key.GetComponent<HandIdentifier>().isLeftHand;
                    // If we have found an avatar that means this is a remote user grabbing the item
                    if (avatar)
                        context.SendJson(new Message(MessageType.LostGrab, true, targetTransform, avatar.Peer.uuid, false, left));
                }
            }
            if (Owner || startupSync)
            {
                context.SendJson(new Message(MessageType.Physics, true, targetTransform));
            }
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
                startupSync = false;
                if (msg.isOwned)
                {
                    this.gameObject.layer = LayerMask.NameToLayer("GraspedObject");
                }
                else
                {
                    this.gameObject.layer = 0;
                }
                //transform.GetComponent<Rigidbody>().isKinematic = msg.isOwned;
                transform.GetComponent<Rigidbody>().useGravity = disableGravityOnPlace ? msg.isOwned : true;
                isOwned = msg.isOwned;
                Owner = false;
            }
            else if (msg.type == MessageType.Grab)
            {
                if (Owner)
                {
                    RemoteGrab(msg);
                }
            }
            else if (msg.type == MessageType.LostGrab)
            {
                if (msg.peerUUID == roomClient.Me.uuid)
                {
                    var sphere = spheres.FirstOrDefault(s => s.Key.GetComponent<HandIdentifier>().isLeftHand == msg.left).Value;
                    if (sphere)
                    {
                        GameObject.Destroy(sphere);
                        spheres.Remove(sphere);
                    }
                }
            }
        }

        private async void RemoteGrab(Message msg)
        {
            var avatar = avatarManager.Avatars.FirstOrDefault(a => a != null && a.Peer.uuid == msg.peerUUID);
            GameObject remoteHand = null;
            if (avatar is MetaAvatar.MetaAvatar)
                remoteHand = avatar.transform.Find(msg.left ? "MetaAvatar/Joint LeftHandIndexProximal" : "MetaAvatar/Joint RightHandIndexProximal").gameObject;
            else
                remoteHand = avatar.transform.Find(msg.left ? "Body/Floating_LeftHand_A" : "Body/Floating_RightHand_A").gameObject;

            if (msg.grasp)
            {
                //TODO: Only delay if remote user is desktop
                await Task.Delay(200);
                Grasp(remoteHand);
            }
            else
            {
                Release(remoteHand);
            }
        }

        public void setOwner(bool value)
        {
            if (Owner || value)
            {
                if (context.Scene)
                    // Tell the other clients that someone owns this object
                    context.SendJson(new Message(MessageType.ChangeOwner, value, transform));
            }
            _owner = value;
        }

        public IEnumerator DeletionProgress(GameObject controller)
        {
            GameObject popOver = null;
            int count = 0;

            yield return new WaitForSeconds(7f);
            if (isDeleting)
                popOver = AddDeletionPopUp(controller);

            while (isDeleting && count < 3)
            {
                yield return new WaitForSeconds(1f);
                count++;
            }
            if (popOver != null)
            {
                GameObject.Destroy(popOver);
            }
            if (isDeleting)
            {
                isDeleting = false;
                Despawn();
            }

        }

        public void Use(Hand controller)
        {
            if (isDeleting || deletionPopOver == null)
                return;
            isDeleting = true;
            StartCoroutine(DeletionProgress(controller.gameObject));
        }

        public void Despawn()
        {
            NetworkSpawnManager.Find(this).Despawn(this.transform.parent.gameObject);
        }

        private GameObject AddDeletionPopUp(GameObject controller)
        {
            var popOver = GameObject.Instantiate(deletionPopOver);
            popOver.transform.parent = controller.transform;
            popOver.transform.localPosition = new Vector3();
            return popOver;
        }

        public void UnUse(Hand controller)
        {
            isDeleting = false;
        }
    }
}
