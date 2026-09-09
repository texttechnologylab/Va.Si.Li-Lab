using TMPro;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.Events;

public class WebViewReceiver : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    public TMP_Text textMesh1;
    public TMP_Text textMesh2;

    [System.Serializable]
    public class AnswerOptionsEvent : UnityEvent<string[]> { }

    /// <summary>Fires whenever a non-empty filtered set of answer options is received from the iframe.</summary>
    public AnswerOptionsEvent OnAnswerOptionsReceived;

    // Track current data so we can synchronize to new peers
    private string[] currentStrings;

    public void Start()
    {
        context = NetworkScene.Register(this);
        roomClient = GameObject.Find("Social Network Scene").GetComponent<RoomClient>();
        roomClient.OnPeerAdded.AddListener(SynchronizeData);
    }

    private void SynchronizeData(IPeer peer)
    {
        if (currentStrings != null && currentStrings.Length > 0)
        {
            context.SendJson(new StringArrayWrapper { strings = currentStrings });
        }
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
        currentStrings = res.strings;
        context.SendJson(res);
        SetText(res.strings);
        OnAnswerOptionsReceived?.Invoke(res.strings);
    }

    /// <summary>Display-only entry point for options received over the Janus textroom
    /// (interviewee side). Does NOT re-broadcast — unlike OnJsResult, which is the
    /// interviewer's questionnaire-webview path.</summary>
    public void DisplayOptions(string[] options)
    {
        if (options == null) return;
        SetText(options);
    }

    private void SetText(string[] text)
    {
        Clear();

        // Filtering is shared with the interviewer side (and ported from the web
        // frontend) via AnswerOptionFilter, so the Quest renders exactly what the
        // website would. A null result means the batch resolves to "no concrete
        // options" (a block term matched, or too many non-scale entries) — the
        // free-answer case, where nothing is displayed.
        var filteredEntries = VaSiLi.WebCam.AnswerOptionFilter.Filter(text);
        if (filteredEntries == null)
        {
            Debug.Log("Answer options filtered out (free-answer / blocked) — not displaying.");
            return;
        }

        // Check if this is a 0-10 numeric scale
        if (VaSiLi.WebCam.AnswerOptionFilter.IsNumericScale(filteredEntries) && filteredEntries.Count == 11)
        {
            DisplayNumericScale(filteredEntries);
        }
        else
        {
            DisplayNormalList(filteredEntries);
        }
    }

    private void DisplayNumericScale(System.Collections.Generic.List<string> entries)
    {
        // For 0-10 scale, display in a compact format:
        // First and last entries with full text
        // Middle entries condensed
        
        // First entry (0)
        string firstLabel = FilterText(entries[0]);
        textMesh1.text = firstLabel + "\n";

        textMesh1.text += ".\n.\n";
        
        // Last entry (10)
        string lastLabel = FilterText(entries[10]);
        textMesh1.text += lastLabel;
    }
    
    private void DisplayNormalList(System.Collections.Generic.List<string> entries)
    {
        int i = 0;
        foreach (string textEntry in entries)
        {
            var filteredText = FilterText(textEntry);
            
            if (i < 6)
            {
                textMesh1.text += filteredText + "\n";
            }
            else if (i < 12)
            {
                textMesh2.text += filteredText + "\n";
            }
            i++;
        }
    }

    private string FilterText(string text)
    {
        if (text.Contains("\n")) {
            text = text[..text.IndexOf("\n")];
        }

        return text;
    }
    private void Clear()
    {
        textMesh1.text = "";
        textMesh2.text = "";
        currentStrings = null;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<StringArrayWrapper>();
        SetText(msg.strings);
        OnAnswerOptionsReceived?.Invoke(msg.strings);
    }
}
