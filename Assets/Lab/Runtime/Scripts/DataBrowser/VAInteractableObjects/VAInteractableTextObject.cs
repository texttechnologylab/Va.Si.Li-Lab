using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.UI;

using System.Collections;


namespace VaSiLi.VAnnotator
{
    public class VAInteractableTextObject : VAInteractableObject
    {
        public Text titleTransform;
        public TextEditorInputField textTransform;

        public Button editButton;
        public GameObject keyboard;


        private Sprite editSprite;
        private Sprite saveSprite;
        private struct TextData
        {
            public string title;
            public string text;

            public TextData(string title, string text)
            {
                this.title = title;
                this.text = text;
            }
        }

        protected override void Start()
        {
            base.Start();
            keyboard.SetActive(false);

            editSprite = Resources.Load<Sprite>("VAInteractableObjects/edit_FILL1_wght700_GRAD0_opsz48");
            saveSprite = Resources.Load<Sprite>("VAInteractableObjects/save_FILL1_wght700_GRAD0_opsz48");

            textTransform.onEndEdit.AddListener(delegate { SendCurrentText(); });
            textTransform.onValueChanged.AddListener(delegate { SendCurrentText(); });
        }


        public IEnumerator LocalInit(string title, string text)
        {
            SetText(title, text);

            while (context.Scene == null)
                yield return new WaitForSeconds(0.1f);

            //For correct positioning when initializing
            context.SendJson(new Message(MessageType.Position, false, transform, ""));
            SendCurrentText();

        }

        public void SwitchMode()
        {
            if (textTransform.isFocused)
            {
                textTransform.DeactivateInputField();
                editButton.GetComponent<Image>().sprite = editSprite;
                keyboard.SetActive(false);
            }
            else
            {
                textTransform.ActivateInputField();
                editButton.GetComponent<Image>().sprite = saveSprite;
                keyboard.SetActive(true);
            }
        }

        public void SendCurrentText()
        {
            TextData data = new TextData(titleTransform.text, textTransform.text);
            Message msg = new Message(MessageType.Data, false, transform, JsonUtility.ToJson(data));
            context.SendJson(msg);
        }

        private void SetText(string title, string text)
        {
            titleTransform.text = title;
            textTransform.text = text;  
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
                    TextData msg_data = JsonUtility.FromJson<TextData>(msg.jsonString);
                    SetText(msg_data.title, msg_data.text);
                    break;
            }
        }
    }
}
