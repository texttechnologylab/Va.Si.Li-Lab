using TMPro;
using Ubiq.Messaging;
using UnityEngine;

public class WebViewReceiver : MonoBehaviour
{
    private NetworkContext context;
    public TMP_Text textMesh1;
    public TMP_Text textMesh2;

    public void Start()
    {
        context = NetworkScene.Register(this);
    }

    [System.Serializable]
    public class StringArrayWrapper
    {
        public string[] strings;
    }

    public void OnJsResult(string message)
    {
        // Message is just a json array with strings
        Debug.Log($"OnJsResult: {message}");
        var res = JsonUtility.FromJson<StringArrayWrapper>("{\"strings\":" + message + "}");
        context.SendJson(res);
        SetText(res.strings);
    }
    /* 
    NICHT:
    - ZP gibt an /nicht
    - Interviewer avatar, video...
    - ledig etc
    Auf jeden fall:
    - 0-10
    - 
    */
    private void SetText(string[] text)
    {
        Clear();
        int i = 0;
        foreach (string textEntry in text)
        {
            if (string.IsNullOrWhiteSpace(textEntry))
            {
                continue;
            }
            if (textEntry.Contains("weiß nicht", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (textEntry.Contains("undefined", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var filteredText = FilterText(textEntry);
            
            if (i <= 6)
            {
                textMesh1.text += filteredText + "\n";
            }
            else
            {
                textMesh2.text += filteredText + "\n";
            }
            i++;
        }
        if (i >=7)
        {
            Clear();
        }
    }

    private string FilterText(string text)
    {
        text = text.Replace("\n", "");
        text = text.Replace("\r", "");
        text = text.Replace("\t", "");
        text = text.Replace("<br>", " ");
        // With the help of regex match all numbers and remove them
        //text = System.Text.RegularExpressions.Regex.Replace(text, @"\d+", "");
        return text;
    }
    private void Clear()
    {
        textMesh1.text = "";
        textMesh2.text = "";
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<StringArrayWrapper>();
        SetText(msg.strings);
    }
}
