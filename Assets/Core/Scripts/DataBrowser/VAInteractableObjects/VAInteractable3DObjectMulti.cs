using Ubiq.Messaging;

using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Globalization;
using VaSiLi.Object;
using System.IO;

namespace VaSiLi.VAnnotator
{
    public class VAInteractable3DObjectMulti : VAInteractable3DObject
    {
        private GameObject child;
        public GameObject deletionPopOver;
        protected override void OnShapeNetObjLoaded(string path)
        {
            string shapeNetID = shapeNetData["id"].ToString();

            GameObject _object = ObjectLoader.LoadObject(Path.Combine(path, shapeNetID + ".obj"), Path.Combine(path, shapeNetID + ".mtl"));
            _object.SetActive(false);
            Debug.Log(shapeNetData);

            Vector3 up_vec;
            if (shapeNetData["up"] != null)
            {
                string up_string = shapeNetData["up"].ToString();
                string[] up = up_string.Substring(1, up_string.Length - 2).Split(",");
                up_vec = new Vector3(float.Parse(up[0], CultureInfo.InvariantCulture), float.Parse(up[1], CultureInfo.InvariantCulture), float.Parse(up[2], CultureInfo.InvariantCulture));
            }
            else
            {
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
            child = oriented_obj;
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


            float maxSize = 2f;
            float max = Mathf.Max(dims_vec.x, dims_vec.y, dims_vec.z);
            if (max / 100 > maxSize)
            {
                float size = (100 * maxSize) / max;
                this.transform.localScale = this.transform.localScale * size;
                if (size <= 0.1)
                {
                    GameObject.Destroy(this.gameObject);
                }
            }

            oriented_obj.transform.position = transform.position;
            transform.position = new Vector3();

            BoxCollider _collider = oriented_obj.AddComponent<BoxCollider>();
            _collider.size = dims_vec / 100;
            _collider.enabled = true;

            Rigidbody _body = oriented_obj.AddComponent<Rigidbody>();
            _body.drag = 2;
            _body.angularDrag = 1;
            _body.mass = 5;

            MultiGrabbable grab = oriented_obj.AddComponent<MultiGrabbable>();
            grab.breakForce = 2000;
            grab.breakTorque = 1000;
            grab.startupSync = true;
            grab.disableGravityOnPlace = true;
            grab.deletionPopOver = deletionPopOver;
            //transform.position = new Vector3(0, 0.5f * _collider.size.y, 0);
            _object.SetActive(true);

            isLoaded = true;
        }

        public override void Request()
        {
            var cam = Camera.main.transform;
            Vector3 relativePos = cam.TransformPoint(spawnRelativeTransform.localPosition);
            relativePos.y = transform.position.y;
            child.transform.position = relativePos;
            //transform.rotation = cam.rotation * spawnRelativeTransform.localRotation;
            gameObject.SetActive(true);
        }


        public override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();

            switch (msg.type)
            {
                case MessageType.Data:
                    shapeNetData = JToken.Parse(msg.jsonString);
                    string shapeNetID = shapeNetData["id"].ToString();
                    StartCoroutine(ShapeNetInterface.DownloadModel(shapeNetID, OnShapeNetObjLoaded));
                    break;
            }
        }
    }
}
