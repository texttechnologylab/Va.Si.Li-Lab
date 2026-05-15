using System.Collections;
using UnityEngine;
using VaSiLi.WebCam;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;
    private int resultId = 0;

    private bool isInitialized = false;
    public void Start()
    {
        browser.eventCallback.onPageStart.AddListener(OnBrowserEvent);

    }

    public void Update()
    {
        if (browser.IsInitialized() && !isInitialized)
        {
            isInitialized = true;
            browser.ClearCookie();
            //browser.ClearCache(true);
            browser.SetUserAgent("Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", true);

            // Use the token-aware URL when an interview token has been set.
            var tokenMgr = InterviewTokenManager.Instance;
            var url = (tokenMgr != null && !string.IsNullOrEmpty(tokenMgr.Token))
                ? tokenMgr.BrowserUrl
                : browser.GetUrl();
            browser.LoadUrl(url);
        }
    }

    private void OnBrowserEvent(string message)
    {
        resultId++;
        // Guard with a window flag so each page load only installs one listener.
        // Without the guard, every onPageStart adds a new listener and label_listing fires N times.
        string jsCode = $@"
            if (!window.__vasiliListenerInstalled) {{
                window.__vasiliListenerInstalled = true;
                window.addEventListener('message', function(event) {{
                    if (event.data && event.data.event === 'label_listing' && event.data.data) {{
                        tlab.unitySendMessage('WebViewReceiver', 'OnJsResult', JSON.stringify(event.data.data));
                    }}
                }});
            }}
            tlab.postResult('resultId{resultId}', 1);
        ";
        StartCoroutine(LogJSResult("resultId" + resultId, jsCode));
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