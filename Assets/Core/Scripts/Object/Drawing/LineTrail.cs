using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Rooms;
using VaSiLi.Networking;
using static VaSiLi.Networking.ResponsiveNetworking;

namespace VaSiLi.Object.Drawing
{
    [RequireComponent(typeof(LineTrail))]
    public class LineTrail : MonoBehaviour, INetworkSpawnable
    {
        private NetworkContext context;
        private RoomClient roomClient;
        private TrailRenderer trail;
        private bool updateOnStart = false;
        public NetworkId NetworkId { get; set; }

        private enum MessageType
        {
            UpdateEmitting,
            UpdatePositions,
            UpdateColors,
        }

        private struct Message
        {
            public MessageType type;
            public Vector3[] positions;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public bool emitting;
            public Color start;
            public Color end;

            public Message(bool emitting)
            {
                this.type = MessageType.UpdateEmitting;
                this.emitting = emitting;
                this.positions = null;
                this.localPosition = Vector3.zero;
                this.localRotation = Quaternion.identity;
                this.start = Color.black;
                this.end = Color.black;
            }

            public Message(Vector3[] positions, Vector3 localPosition, Quaternion localRotation)
            {
                this.type = MessageType.UpdatePositions;
                this.emitting = false;
                this.positions = positions;
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.start = Color.black;
                this.end = Color.black;
            }

            public Message(Color start, Color end)
            {
                this.type = MessageType.UpdateColors;
                this.emitting = false;
                this.positions = null;
                this.localPosition = Vector3.zero;
                this.localRotation = Quaternion.identity;
                this.start = start;
                this.end = end;
            }
        }

        private void Awake()
        {
            trail = GetComponent<TrailRenderer>();
            roomClient = GameObject.Find("Social Network Scene").GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SynchronizeData);
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
            trail.startWidth = .05f;
            trail.endWidth = .05f;
            if (updateOnStart)
            {
                Vector3[] positions = new Vector3[trail.positionCount];
                trail.GetPositions(positions);
                ResponsiveNetworking.SendJson(context.Id, new Message(positions, transform.localPosition, transform.localRotation), SimpleMessageHandler);
                ResponsiveNetworking.SendJson(context.Id, new Message(trail.startColor, trail.endColor), SimpleMessageHandler);
            }
        }

        public void SimpleMessageHandler(CallbackResult result)
        {
            if (!result.success)
            {
                var resultContext = result.context;
                if (resultContext == null)
                {
                    resultContext = 1;
                }
                else
                {
                    int count = (int)resultContext;
                    if (count > 5)
                    {
                        Debug.LogError("A client couldn't process the message after 5 tries");
                        return;
                    }
                    count = count + 1;
                    resultContext = count;
                }

                ResponsiveNetworking.SendJson(context.Id, result.message, SimpleMessageHandler, resultContext);
            }

        }

        private void SynchronizeData(IPeer peer)
        {
            if (context.Scene)
            {
                SendPositions();
                SetColors(trail.startColor, trail.endColor, true);
            }
        }

        /// <summary>
        /// Sets the color of the trail making sure to synchronize it across the clients
        /// </summary>
        /// <param name="start">The color the beginning to the trail should have</param>
        /// <param name="end">The color the end of the trail should have</param>
        /// <param name="sendMessage">Weither to send the new colors to the user clients</param>
        public void SetColors(Color start, Color end, bool sendMessage)
        {
            trail.startColor = start;
            trail.endColor = end;
            if (sendMessage && context.Scene != null)
            {
                context.SendJson(new Message(start, end));
            }
            else if (sendMessage)
            {
                updateOnStart = true;
            }
        }

        /// <summary>
        /// Sets if the current trail should be emitting (drawn)
        /// </summary>
        /// <param name="emitting"></param>
        /// <param name="sendMessage"></param>
        public void SetEmitting(bool emitting, bool sendMessage)
        {
            trail.Clear();
            trail.emitting = emitting;
            if (sendMessage && context.Scene != null)
                context.SendJson(new Message(emitting));
        }

        /// <summary>
        /// Sets the points of the current trail
        /// </summary>
        /// <param name="positions">An array of points</param>
        /// <param name="localPosition">The local (root)-position the trail should have</param>
        /// <param name="localRotation">The local rotation the trail should have</param>
        /// <param name="sendMessage"></param>
        public void SetPositions(Vector3[] positions, Vector3 localPosition, Quaternion localRotation, bool sendMessage)
        {
            trail.Clear();
            // If we don't have enough positions to draw a line
            if (positions.Length <= 1)
                return;
            trail.AddPositions(positions);

            // Copy position and rotation
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            if (sendMessage && context.Scene != null)
            {
                context.SendJson(new Message(positions, localPosition, localRotation));
            }
            else if (sendMessage)
            {
                updateOnStart = true;
            }
        }

        /// <summary>
        /// Sends the positions of the trail to the other clients
        /// </summary>
        public void SendPositions()
        {
            Vector3[] positions = new Vector3[trail.positionCount];
            trail.GetPositions(positions);
            context.SendJson(new Message(positions, transform.localPosition, transform.localRotation));
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
        {
            Message data = msg.FromJson<Message>();
            if (data.type == MessageType.UpdateEmitting)
            {
                SetEmitting(data.emitting, false);
            }
            else if (data.type == MessageType.UpdatePositions)
            {
                SetPositions(data.positions, data.localPosition, data.localRotation, false);
            }
            else if (data.type == MessageType.UpdateColors)
            {
                SetColors(data.start, data.end, false);
            }
        }
    }
}