using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;
using VaSiLi.SceneManagement;

namespace VaSiLi.Misc
{
    public class TrafficLight : MonoBehaviour
    {
        private NetworkContext context;
        public Button buttonclick;
        public Button buttondeactivate;
        public Button buttondeactivate2;
        public Dictionary<string, Queue<string>> infosButton;
        Queue infos = new Queue();
        uint qsize = 5;

        public enum MessageType
        {
            lightUpdate
        }

        public struct Message
        {
            public string userName;
            public string ampel;
            public string pressed;
            public MessageType messageType;

            public Message(MessageType messageType, string userName, string ampel, string pressed)
            {
                this.messageType = messageType;
                this.userName = userName;
                this.ampel = ampel;
                this.pressed = pressed;
            }
        }
        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
            infosButton = new Dictionary<string, Queue<string>>()
        {
            { "red", new Queue<string>() },
            { "orange", new Queue<string>() },
            { "green", new Queue<string>() }

        };
        }

        public void HighlightTrafficLight()
        {
            string messagePressed = "pressed";
            Color temp = buttonclick.image.color;
            if (temp.a == 1f)
            {
                temp.a = 0.2f;
                messagePressed = "removed";
            }
            else
            {
                temp.a = 1f;
            }
            buttonclick.image.color = temp;
            temp = buttondeactivate.image.color;
            temp.a = 0.2f;
            buttondeactivate.image.color = temp;
            temp = buttondeactivate2.image.color;
            temp.a = 0.2f;
            buttondeactivate2.image.color = temp;
            string userNametemp = "A User";
            if (SceneManager.CurrentScene.HasValue)
            {
                if (RoleManager.CurrentRole.HasValue)
                {
                    userNametemp = RoleManager.CurrentRole.Value.name;
                }
            }
            string buttonName = buttonclick.gameObject.name;
            context.SendJson(new Message(MessageType.lightUpdate, userNametemp, buttonName, messagePressed));
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.messageType == MessageType.lightUpdate)
            {
                if (SceneManager.CurrentScene.HasValue)
                {

                    if (RoleManager.CurrentRole?.mode == "spectator" && RoleManager.CurrentRole?.name == "hidden")
                    {
                        Debug.Log(msg.userName + " " + msg.ampel + " " + msg.pressed + "\n");
                        infos.Enqueue(msg.userName + " " + msg.ampel + " " + msg.pressed + "\n");
                        while (infos.Count > qsize)
                        {
                            infos.Dequeue();
                        }
                        if (infosButton.ContainsKey(msg.ampel))
                        {
                            infosButton[msg.ampel].Enqueue(msg.userName + " " + msg.ampel + " " + msg.pressed);
                            while (infosButton[msg.ampel].Count > qsize)
                            {
                                infosButton[msg.ampel].Dequeue();
                            }
                        }
                    }

                }
            }
        }

        void OnGUI()
        {
            if (SceneManager.CurrentScene.HasValue)
            {
                if (RoleManager.CurrentRole.HasValue)
                {
                    if (RoleManager.CurrentRole.Value.mode == Mode.Spectator)
                    {
                        int i = 0;
                        foreach (var buttonI in infosButton)
                        {
                            GUILayout.BeginArea(new Rect(Screen.width - (250 + i), 0, 250, Screen.height));
                            GUILayout.Label("\n" + string.Join("\n", buttonI.Value.ToArray()));
                            GUILayout.EndArea();
                            i = i + 400;
                        }
                    }
                }
            }
        }
    }
}

