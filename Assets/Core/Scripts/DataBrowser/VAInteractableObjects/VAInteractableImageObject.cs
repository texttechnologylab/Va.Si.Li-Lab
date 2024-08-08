using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.UI;

using System.Collections;

namespace VaSiLi.VAnnotator
{
    public class VAInteractableImageObject : VAInteractableObject
    {
        public Text titleTransform;
        public RawImage imageTransform;


        private struct ImageData
        {
            public string title;
            public byte[] img;

            public ImageData(string title, byte[] img)
            {
                this.title = title;
                this.img = img;
            }
        }


        public IEnumerator Init(string title, byte[] img)
        {
            SetImage(title, img);
            while (context.Scene == null)
                yield return new WaitForSeconds(0.1f);
            context.SendJson(new Message(MessageType.Position, false, transform, ""));
            context.SendJson(new Message(MessageType.Data, false, transform, JsonUtility.ToJson(new ImageData(title, img))));
        }

        
        public void ChangeAndSendImage(string title, byte[] img)
        {
            SetImage(title, img);
            ImageData data = new ImageData(title, img);
            Message msg = new Message(MessageType.Data, false, transform, JsonUtility.ToJson(data));
            context.SendJson(msg);
        }

        private void SetImage(string title, byte[] img)
        {
            titleTransform.text = title;

            Texture2D tempPic = new Texture2D(1, 1); //mock size 1x1
            tempPic.LoadImage(img);
            imageTransform.texture = tempPic;
            imageTransform.SizeToParent();
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
                    ImageData msg_data = JsonUtility.FromJson<ImageData>(msg.jsonString);
                    Debug.Log("!!!!!!!!!!Received text data: " + msg_data.title);
                    SetImage(msg_data.title, msg_data.img);
                    break;
            }
        }
    }

    static class CanvasExtensions
    {
        /**
         * https://forum.unity.com/threads/code-snippet-size-rawimage-to-parent-keep-aspect-ratio.381616/
         * 
         * Scale the image nicely to fit into the parent
         */
        public static Vector2 SizeToParent(this RawImage image, float padding = 0)
        {
            float w = 0, h = 0;
            var parent = image.GetComponentInParent<RectTransform>();
            var imageTransform = image.GetComponent<RectTransform>();

            // check if there is something to do
            if (image.texture != null)
            {
                if (!parent) { return imageTransform.sizeDelta; } //if we don't have a parent, just return our current width;
                padding = 1 - padding;
                float ratio = image.texture.width / (float)image.texture.height;
                var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
                if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90)
                {
                    //Invert the bounds if the image is rotated
                    bounds.size = new Vector2(bounds.height, bounds.width);
                }
                //Size by height first
                h = bounds.height * padding;
                w = h * ratio;
                if (w > bounds.width * padding)
                { //If it doesn't fit, fallback to width;
                    w = bounds.width * padding;
                    h = w / ratio;
                }
            }
            imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            return imageTransform.sizeDelta;
        }
    }
}
