using System.Collections;
using UnityEngine;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;

    public void Start()
    {
        browser.eventCallback.onPageFinish.AddListener(OnBrowserEvent);
    }


    public void ButtonStuff()
    {
        StartCoroutine(ExecuteJS("let options = Object.values($(\".answer-text\").not(\".hidden\")).map((e) => e.innerHTML);"));
    }
    private void OnBrowserEvent(string message)
    {
        Debug.Log($"BrowserEvent!!!2: {message}");
        StartCoroutine(ExecuteJS("Object.values($(\".answer-text\").not(\".hidden\")).map((e) => e.innerHTML);"));
    }

    public IEnumerator ExecuteJS(string jsCode)
    {
        if (browser != null)
        {
            var res = browser.EvaluateJSForResult("options", jsCode);
            foreach (var result in res) // Iterate over the yielded values
            {
                if (result != null) // Ignore the null yields
                {
                    Debug.Log("JS Result: " + result);
                }
                yield return result; // Keep coroutine timing consistent
            }
        }
        else
        {
            Debug.LogWarning("Browser reference is not set.");
        }

        yield break;
    }

}