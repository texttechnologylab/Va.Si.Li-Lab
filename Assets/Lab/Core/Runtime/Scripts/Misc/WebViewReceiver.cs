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

    private void SetText(string[] text)
    {
        Clear();
        
        // Filter out unwanted entries and check for blocking patterns
        var filteredEntries = new System.Collections.Generic.List<string>();
        
        foreach (string textEntry in text)
        {
            if (string.IsNullOrWhiteSpace(textEntry))
            {
                continue;
            }
            
            // Check for blocking patterns - if any are found, don't display anything
            if (textEntry.Contains("ZP gibt", System.StringComparison.OrdinalIgnoreCase) ||
                textEntry.Contains("Interviewer", System.StringComparison.OrdinalIgnoreCase) ||
                textEntry.Contains("ledig", System.StringComparison.OrdinalIgnoreCase) ||
                textEntry.Contains("ja", System.StringComparison.OrdinalIgnoreCase) || 
                textEntry.Contains("stunden", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Blocked pattern found in: {textEntry}");
                return; // Exit without displaying anything
            }
            
            // Skip unwanted entries
            if (textEntry.Contains("weiß nicht", System.StringComparison.OrdinalIgnoreCase) ||
                textEntry.Contains("undefined", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            filteredEntries.Add(textEntry);
        }
        
        // Don't display if more than 6 options (excluding numeric scales)
        if (filteredEntries.Count > 6 && !IsNumericScale(filteredEntries))
        {
            Debug.Log($"Too many options ({filteredEntries.Count}), not displaying.");
            return;
        }
        
        // Check if this is a 0-10 numeric scale
        if (IsNumericScale(filteredEntries) && filteredEntries.Count == 11)
        {
            DisplayNumericScale(filteredEntries);
        }
        else
        {
            DisplayNormalList(filteredEntries);
        }
    }
    
    private bool IsNumericScale(System.Collections.Generic.List<string> entries)
    {
        // Check if entries follow a 0-10 pattern
        if (entries.Count != 11) return false;
        
        for (int i = 0; i < entries.Count; i++)
        {
            // Check if the entry starts with the number matching its index
            if (!entries[i].TrimStart().StartsWith(i.ToString()))
            {
                return false;
            }
        }
        
        return true;
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
