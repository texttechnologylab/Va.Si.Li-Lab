using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab.Core.Runtime.Scripts.LLM {
    public class ToolWhitelistUI : MonoBehaviour {
        public Button plus, minus;
        public GameObject toolSelector;
        public Transform toolMenuRect;
        public Transform toolSelectorRect;
        public AgentInstance agent;
        public Image exampleSprite;
        
        private AgentToolProvider _toolProvider;

        private bool _removeMode;

        public void SelectorActivate() {
            toolSelector.SetActive(!toolSelector.activeSelf);

            plus.GetComponentInChildren<TextMeshProUGUI>().text = toolSelector.activeSelf ? "OK" : "+";
        }

        public void RemoveMode() {
            _removeMode = !_removeMode;
            
            minus.interactable = !minus.interactable;
            plus.interactable = !plus.interactable;
            
            if(toolSelector.activeSelf)
                SelectorActivate();
        }
        
        public void GenerateActiveButtons() {
            foreach (Transform child in toolMenuRect)
                Destroy(child.gameObject);
            
            int y = 140;

            foreach (string tool in agent.allowedTools.Keys) {
                GameObject buttonObj = new GameObject(tool);
                RectTransform rect = buttonObj.AddComponent<RectTransform>();
                
                rect.SetParent(toolMenuRect, false);
                
                rect.anchorMin = exampleSprite.GetComponent<RectTransform>().anchorMin;
                rect.anchorMax = exampleSprite.GetComponent<RectTransform>().anchorMax;
                rect.pivot = exampleSprite.GetComponent<RectTransform>().pivot;
                rect.sizeDelta = exampleSprite.GetComponent<RectTransform>().sizeDelta;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.Euler(0, 0, 0);
                rect.anchoredPosition = new Vector2(-50, y);
                
                Image image = buttonObj.AddComponent<Image>();
                image.sprite = exampleSprite.sprite;
                image.type = Image.Type.Sliced;
                Button button = buttonObj.AddComponent<Button>();
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.targetGraphic = image;
                
                GameObject textObj = new GameObject("Text");
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.SetParent(rect);
                textRect.localPosition = new Vector3(0, 0, 0);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.fontSize = 10;
                text.text = tool;
                text.color = Color.black;
                text.alignment = TextAlignmentOptions.Center;
                textRect.localRotation = Quaternion.Euler(0, 0, 0);
                textRect.localScale = Vector3.one;
                
                button.onClick.AddListener(() => RemoveFromActive(tool));

                y -= 13;
            }
        }
        
        public void GenerateSelectorButtons() {
            int y = 140;

            foreach (string tool in _toolProvider.GetAvailableTools().Keys) {
                string toolName = tool;
                
                GameObject buttonObj = new GameObject(tool);
                RectTransform rect = buttonObj.AddComponent<RectTransform>();
                
                rect.SetParent(toolSelectorRect, false);
                
                rect.anchorMin = exampleSprite.GetComponent<RectTransform>().anchorMin;
                rect.anchorMax = exampleSprite.GetComponent<RectTransform>().anchorMax;
                rect.pivot = exampleSprite.GetComponent<RectTransform>().pivot;
                rect.sizeDelta = exampleSprite.GetComponent<RectTransform>().sizeDelta;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.Euler(0, 0, 0);
                rect.anchoredPosition = new Vector2(0, y);
                
                Image image = buttonObj.AddComponent<Image>();
                image.sprite = exampleSprite.sprite;
                image.type = Image.Type.Sliced;
                Button button = buttonObj.AddComponent<Button>();
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.targetGraphic = image;
                
                GameObject textObj = new GameObject("Text");
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.SetParent(rect);
                textRect.localPosition = new Vector3(0, 0, 0);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.fontSize = 10;
                text.text = tool;
                text.color = Color.black;
                text.alignment = TextAlignmentOptions.Center;
                textRect.localRotation = Quaternion.Euler(0, 0, 0);
                textRect.localScale = Vector3.one;
                
                button.onClick.AddListener(() => AddToActive(toolName));

                y -= 13;
            }
        }
    
        private void Awake() {
            plus.onClick.AddListener(SelectorActivate);
            minus.onClick.AddListener(RemoveMode);
            _toolProvider = FindFirstObjectByType<AgentToolProvider>();
            
            GenerateActiveButtons();
        }

        private async void AddToActive(string tool) {
            if (agent.allowedTools.Keys.Contains(tool))
                return;
            
            Debug.LogWarning("Trying to add tool " + tool);
            
            await agent.AddTool(tool, _toolProvider.GetAvailableTools()[tool]);
            GenerateActiveButtons();
        }

        private async void RemoveFromActive(string tool) {
            if (!_removeMode)
                return;

            RemoveMode();

            foreach (string toolName in _toolProvider.GetAvailableTools().Keys) {
                print(toolName);
            }
            
            await agent.RemoveTool(tool);
            GenerateActiveButtons();
            
            foreach (string toolName in _toolProvider.GetAvailableTools().Keys) {
                print(toolName);
            }
        }
    }
}
