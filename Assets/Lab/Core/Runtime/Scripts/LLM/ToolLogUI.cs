using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab.Core.Runtime.Scripts.LLM {
    public class ToolLogUI : MonoBehaviour
    {
        public Transform chatContainer;
        public float messageLifetime = 10f;

        public void AddMessage(string text)
        {
            GameObject msg = new GameObject("Message");
            msg.transform.SetParent(chatContainer, false);
            
            RectTransform rect = msg.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0, 0, 0);
            
            TMP_Text tmp = msg.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            
            StartCoroutine(RemoveAfterTime(msg, messageLifetime));
        }

        private IEnumerator RemoveAfterTime(GameObject msg, float time)
        {
            yield return new WaitForSeconds(time);
            Destroy(msg);
        }
    }
}
