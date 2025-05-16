using System;
using Ubiq.Messaging;
using UnityEngine;

namespace VaSiLi.Misc
{
    public class ImageRenderer : MonoBehaviour
    {
        public MeshRenderer frame;
        public int index = 0;
        private NetworkContext context;

        private struct Message
        {
            public string[] images;
        }
        // Start is called before the first frame update
        void Start()
        {
            //SetImage([base64);
            context = NetworkScene.Register(this);
        }

        public void SetImage(string[] base64)
        {
            SetImage(base64, true);
        }

        public void SetImage(string[] base64, bool sendMessage)
        {
            byte[] imageBytes = Convert.FromBase64String(base64[index]);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            frame.material.mainTexture = tex;
            if (sendMessage)
                context.SendJson(new Message() {images = base64});
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            SetImage(msg.images, false);
        }
    }
}
