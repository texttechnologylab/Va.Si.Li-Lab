using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine.UIElements;
using System.Globalization;
using VaSiLi.Networking;
using static VaSiLi.Networking.ResponsiveNetworking;

namespace VaSiLi.VAnnotator
{
    public class VAInteractable3DObject : VAInteractableObject
    {
        protected JToken shapeNetData;
        protected bool isLoaded = false;

        protected override void Start()
        {
            base.Start();
        }

        public IEnumerator Init(JToken shapeNetData)
        {
            this.shapeNetData = shapeNetData;
            string shapeNetID = shapeNetData["id"].ToString();

            StartCoroutine(ShapeNetInterface.DownloadModel(shapeNetID, OnShapeNetObjLoaded));

            while (context.Scene == null || !isLoaded)
                yield return new WaitForSeconds(0.1f);
            Request();
            ResponsiveNetworking.SendJson(context.Id, new Message(MessageType.Data, false, transform, shapeNetData.ToString()), SimpleMessageHandler);
            ResponsiveNetworking.SendJson(context.Id, new Message(MessageType.Position, false, transform, ""), SimpleMessageHandler);

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

        protected virtual void OnShapeNetObjLoaded(string path)
        {
            string shapeNetID = shapeNetData["id"].ToString();

            GameObject _object = ObjectLoader.LoadObject(Path.Combine(path, shapeNetID + ".obj"), Path.Combine(path, shapeNetID + ".mtl"));
            _object.SetActive(false);
            Debug.Log(shapeNetData);

            Vector3 up_vec;
            if (shapeNetData["up"] != null) {
                string up_string = shapeNetData["up"].ToString();
                string[] up = up_string.Substring(1, up_string.Length - 2).Split(",");
                up_vec = new Vector3(float.Parse(up[0], CultureInfo.InvariantCulture), float.Parse(up[1], CultureInfo.InvariantCulture), float.Parse(up[2], CultureInfo.InvariantCulture));
            }
            else {
                up_vec = new Vector3(0, 0, 1f);
            }

            Vector3 front_vec;
            if (shapeNetData["front"] != null)
            {
                string fr_string = shapeNetData["front"].ToString();
                string[] front = fr_string.Substring(1, fr_string.Length - 2).Split(",");
                front_vec = new Vector3(float.Parse(front[0], CultureInfo.InvariantCulture), float.Parse(front[1], CultureInfo.InvariantCulture), float.Parse(front[2], CultureInfo.InvariantCulture));
            }
            else
            {
                front_vec = new Vector3(0, -1f, 0);
            }

            
            //string to float
            float scale = shapeNetData["unit"].ToObject<float>();

            GameObject oriented_obj = ObjectLoader.Reorientate_Obj(_object, up_vec, front_vec, scale);
            oriented_obj.transform.SetParent(transform, false);


            string[] dims;
            if (shapeNetData["aligned.dims"] != null)
            {
                dims = shapeNetData["aligned.dims"].ToString().Split(",");
            }
            else
            {
                string dim_string = shapeNetData["alignedDims"].ToString();
                dims = dim_string.Substring(1, dim_string.Length - 2).Split(",");
            }
            Vector3 dims_vec = new Vector3(float.Parse(dims[0], CultureInfo.InvariantCulture), float.Parse(dims[1], CultureInfo.InvariantCulture), float.Parse(dims[2], CultureInfo.InvariantCulture));

            BoxCollider _collider = oriented_obj.AddComponent<BoxCollider>();
            _collider.size = dims_vec / 100;
            _collider.enabled = true;

            //transform.position = new Vector3(0, 0.5f * _collider.size.y, 0);
            _object.SetActive(true);

            isLoaded = true;
        }

        public override void Request()
        {
            var cam = Camera.main.transform;
            Vector3 relativePos = cam.TransformPoint(spawnRelativeTransform.localPosition);
            relativePos.y = transform.position.y + 0.5f;
            transform.position = relativePos;
            //transform.rotation = cam.rotation * spawnRelativeTransform.localRotation;
            gameObject.SetActive(true);
        }


        public override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {   
            var msg = message.FromJson<Message>();

            switch (msg.type)
            {
                case MessageType.Ownership:
                case MessageType.Position:
                    base.ProcessMessage(message);
                    break;
                case MessageType.Data:
                    shapeNetData = JToken.Parse(msg.jsonString);
                    string shapeNetID = shapeNetData["id"].ToString();
                    StartCoroutine(ShapeNetInterface.DownloadModel(shapeNetID, OnShapeNetObjLoaded));
                    break;
            }
        }
    }
}
