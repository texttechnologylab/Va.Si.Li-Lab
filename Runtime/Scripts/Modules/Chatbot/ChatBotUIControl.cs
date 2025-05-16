
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VaSiLi.Modules.Chatbot {
    public class ChatBotUIControl : MonoBehaviour {
        #if !UNITY_WEBGL
        public Toggle playSelf;
        public Toggle playResult;
        public TMP_Text selfText;
        public TMP_Text responseText;
        public GameObject loadingIndicator;
        public GameObject chatBotEntry;
        public GameObject chatBotContentBar;
        private List<GameObject> availableChatbots = new List<GameObject>();

        public async void Awake()
        {
            var chatbots = await Util.GetChatBots(ChatBotManager.baseUrl + "/chatbots");
            if (!chatbots.HasValue)
                return;
            PopulateUI(chatbots.Value.result);
        }

        private void PopulateUI(string[] chatbots)
        {
            int chatbotId = 0;
            foreach(string chatbot in chatbots) {
                if (availableChatbots.Count <= chatbotId)
                    availableChatbots.Add(Instantiate(chatBotEntry, chatBotContentBar.transform));

                AssignChatBot(availableChatbots[chatbotId], chatbot);
                chatbotId++;

            }

            // Cleanup
            while (availableChatbots.Count > chatbotId)
            {
                Destroy(availableChatbots[chatbotId].gameObject);
                availableChatbots.RemoveAt(chatbotId);
            }
        }

        private void AssignChatBot(GameObject go, string name)
        {
            var button = go.GetComponent<ChatBotSelectButton>();
            button.SetName(name);
            go.SetActive(true);
        }

        public void Record()
        {
            ChatBotManager.GetInstance().RecordAudio();
        }

        public async void StopRecording()
        {
            loadingIndicator.SetActive(true);
            await ChatBotManager.GetInstance().EndAudio();
            loadingIndicator.SetActive(false);
        }

        public void StopRequest()
        {
            ChatBotManager.GetInstance().StopRequest();
            loadingIndicator.SetActive(false);
        }
        #endif
    }
}