using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using Ubiq.Messaging;
public class ChatLogPanel : MonoBehaviour
{
    public TMP_Text textPanel;
    public static UnityAction<string, string> setText = delegate {};
    // Start is called before the first frame update
    private NetworkContext context;
    void Start()
    {
        setText += PersonalListener;
        context = NetworkScene.Register(this);
    }

    private struct Message
    {
        public string userName;
        public string message;
    }
    private bool once = false;
    void PersonalListener(string username, string message)
    {   
        updateText(username, message, true);
    }

    void updateText(string username, string message, bool sendUpdate)
    {
        string oldtext = textPanel.text;
        DateTime timeNow = DateTime.Now;
        string timeI = timeNow.ToString("HH:mm:ss\\Z");
        string text = $"{oldtext}{timeI}\n{username}: {message}\n\n";
        if (once == false && username == "chatbot") {
            textPanel.SetText(text);
            once = true;
        } else {
            textPanel.SetText(text);
        }
        if (sendUpdate)
            context.SendJson(new Message() { userName = username, message = message });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        updateText(msg.userName, msg.message, false);
    }
}
