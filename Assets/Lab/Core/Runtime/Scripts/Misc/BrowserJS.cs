using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;
    private int resultId = 0;

    private bool isInitialized = false;
    public void Start()
    {
        browser.eventCallback.onPageFinish.AddListener(OnBrowserEvent);
        
    }

    public void Update()
    {
        if (browser.IsInitialized() && !isInitialized)
        {
            isInitialized = true;
            browser.ClearCookie();
            browser.ClearCache(true);
            browser.SetUserAgent("Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", true);
        }
    }

    public void ButtonStuff()
    {
        resultId++;
        StartCoroutine(LogJSResult("resultId" + resultId, $"tlab.unitySendMessage('WebViewReceiver', 'OnJsResult', JSON.stringify(Object.values($(\".answer-text\").not(\".hidden\")).map((e) => e.innerHTML?.trim()))); tlab.postResult(resultId{resultId}, 1);"));
    
    }
    private void OnBrowserEvent(string message)
    {
        resultId++;
        StartCoroutine(LogJSResult("resultId" + resultId, $"tlab.unitySendMessage('WebViewReceiver', 'OnJsResult', JSON.stringify(Object.values($('.answer-text').not('.hidden')).map((e) => e.id?.split('-')[1] + ': ' + e.innerHTML?.trim()))); tlab.postResult(resultId{resultId}, 1);"));
    }

    private IEnumerator LogJSResult(string varName, string jsCode)
    {

        // Call the function
        var enumerable = browser.EvaluateJSForResult(varName, jsCode);

        // Iterate through the coroutine it returns
        foreach (var result in enumerable)
        {
            // Because it's a coroutine, some iterations will yield null (waiting)
            if (result == null)
            {
                yield return null;
                continue;
            }

            //break; // Exit the loop after processing the result
        }
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