using System.Collections;
using UnityEngine;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;
    private int resultId = 0;

    public void Start()
    {
        browser.eventCallback.onPageFinish.AddListener(OnBrowserEvent);
    }

    public void ButtonStuff()
    {
        Debug.Log($"ButtonStuff");
        resultId++;
        StartCoroutine(LogJSResult("resultId" + resultId, $"tlab.unitySendMessage('WebViewReceiver', 'OnJsResult', JSON.stringify(Object.values($(\".answer-text\").not(\".hidden\")).map((e) => e.innerHTML?.trim()))); tlab.postResult(resultId{resultId}, 1);"));
    
    }
    private void OnBrowserEvent(string message)
    {
        Debug.Log($"BrowserEvent!!!2: {message}");
        resultId++;
        StartCoroutine(LogJSResult("resultId" + resultId, $"tlab.unitySendMessage('WebViewReceiver', 'OnJsResult', JSON.stringify(Object.values($(\".answer-text\").not(\".hidden\")).map((e) => e.innerHTML?.trim())));"));
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

            // Once we get a real result:
            Debug.Log("✅ JS evaluation status: " + result.status);
            Debug.Log("✅ JS evaluation result: " + result.s);
            Debug.Log("✅ JS evaluation result (int): " + result.i);
            Debug.Log("✅ JS evaluation result (double): " + result.d);
            Debug.Log("✅ JS evaluation result (bool): " + result.b);
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