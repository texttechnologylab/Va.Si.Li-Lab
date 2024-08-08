using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;
using Ubiq.Spawning;
using VaSiLi.Helper;
using UnityEngine.UI;
using System.Collections;

namespace VaSiLi.VAnnotator
{
    /** README:
    *  This is the base class for all interactable VAnnotator objects in the scene.
    *  To create a new interactable object, create a new script that inherits from this class.
    *  The coresponding prefab needs to be added in the SpawnManager catalogue.
    */

    public class VAInteractableLink : MonoBehaviour, INetworkSpawnable
    {
        protected NetworkContext context;

        public NetworkId NetworkId { get; set; }
        public VAInteractableObject Figure
        {
            get { return _figure; }
            set
            {   if (_figure != null)
                    _figure.linkList.Remove(this);
                _figure = value;
                _figure.linkList.Add(this);
            }
        }

        public VAInteractableObject Ground
        {
            get { return _ground; }
            set
            {
                if (_ground != null)
                    _ground.linkList.Remove(this);
                _ground = value;
                _ground.linkList.Add(this);
            }
        }

        private VAInteractableObject _figure;
        private VAInteractableObject _ground;

        private LineRenderer lineRenderer;

        private struct LinkData
        {
            public NetworkId figure;
            public NetworkId ground;

            public LinkData(VAInteractableObject figure, VAInteractableObject ground)
            {
                this.figure = figure.NetworkId;
                this.ground = ground.NetworkId;
            }
        }

        public enum MessageType
        {
            Data
        }

        // Amend message to also store current drawing state
        protected struct Message
        {
            public MessageType type;
            public string jsonString; //This usually contaisn the data of the specific vannotator object

            public Message(MessageType type, string jsonString)
            {
                this.type = type;
                this.jsonString = jsonString;
            }
        }

        public IEnumerator Init()
        {
            while (context.Scene == null)
                yield return new WaitForSeconds(0.1f);
            LinkData linkData = new LinkData(Figure, Ground);
            context.SendJson(new Message(MessageType.Data, JsonUtility.ToJson(linkData)));
        }

        private void Awake()
        {
            lineRenderer = GetComponentInChildren<LineRenderer>();
            lineRenderer.positionCount = 6;
            lineRenderer.material.color = Color.blue;
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
        }

        public virtual void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            switch (msg.type)
            {
                case MessageType.Data:
                    LinkData data = JsonUtility.FromJson<LinkData>(msg.jsonString);
                    Figure = FindSpawnedObjWithNetworkID(data.figure);
                    Ground = FindSpawnedObjWithNetworkID(data.ground);
                    UpdatePosition();
                    break;
                default:
                    break;
            }
        }

        private VAInteractableObject FindSpawnedObjWithNetworkID(NetworkId nID)
        {
            foreach (VAInteractableObject obj in context.Scene.GetComponentsInChildren<VAInteractableObject>())
            {
                if (obj.NetworkId == nID)
                    return obj;
            }
            return null;
        }

        public void UpdatePosition()
        {
            if (Figure == null || Ground == null)
            {
                lineRenderer.enabled = false;
                return;
            }
            Vector3 lineRenderer_start = Figure.GetCenter();
            Vector3 lineRenderer_end = Ground.GetCenter();
            Vector3[] positions = BezierCurve.CalculateCurvePoints(lineRenderer_start, lineRenderer_end, 6);
            lineRenderer.SetPositions(positions);
            lineRenderer.enabled = true;

        }
    }
}