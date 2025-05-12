using UnityEngine;
using UnityEngine.UI;
using VaSiLi.Modules.Chatbot;

public class ChatBotSelectButton : MonoBehaviour {
    

    public Toggle toggle;
    public Text text;
    private string chatBotName;
    #if !UNITY_WEBGL
    public void Start()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }
    
    private void OnValueChanged(bool boolean)
    {
        if (boolean)
        {
            ChatBotManager.SetChatbot(chatBotName);
        }
    }
    #endif
    public void SetName(string name) {
        chatBotName = name;
        text.text = name;
    }
}