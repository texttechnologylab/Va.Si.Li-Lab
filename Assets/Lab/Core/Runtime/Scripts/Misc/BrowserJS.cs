using System.Collections;
using UnityEngine;
using VaSiLi.WebCam;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;
    private int resultId = 0;

    private bool isInitialized = false;
    private bool nativeSetupDone = false;
    private string loadedToken;            // the token reflected in the currently-loaded URL, or null if nothing has loaded
    private InterviewTokenManager subscribedManager;

    public void Start()
    {
        browser.eventCallback.onPageStart.AddListener(OnBrowserEvent);
    }

    public void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.TokenChanged -= OnTokenChanged;
    }

    public void Update()
    {
        // One-time native setup as soon as the WebView is ready. URL loading is gated separately
        // on the token being available so we never show a token-less page if SetToken hasn't
        // landed yet (the race we were hitting: browser initialises before the token arrives).
        if (!nativeSetupDone && browser.IsInitialized())
        {
            nativeSetupDone = true;
            browser.ClearCookie();
            //browser.ClearCache(true);
            browser.SetUserAgent("Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", true);
        }

        // Lazily attach to the manager — it may live in a different scene that loads after us,
        // and Awake order isn't guaranteed even within one scene.
        if (subscribedManager == null && InterviewTokenManager.Instance != null)
        {
            subscribedManager = InterviewTokenManager.Instance;
            subscribedManager.TokenChanged += OnTokenChanged;
        }

        TryLoadTokenUrl();
    }

    private void OnTokenChanged(string _) => TryLoadTokenUrl();

    private void TryLoadTokenUrl()
    {
        if (!nativeSetupDone) return;
        var tokenMgr = subscribedManager ?? InterviewTokenManager.Instance;
        if (tokenMgr == null || string.IsNullOrEmpty(tokenMgr.Token))
        {
            if (!isInitialized)
                Debug.Log("[BrowserJS] Waiting for interview token before loading URL.");
            return;
        }
        if (loadedToken == tokenMgr.Token) return; // already showing this token's page

        loadedToken = tokenMgr.Token;
        isInitialized = true;
        Debug.Log($"[BrowserJS] Loading URL with token='{tokenMgr.Token}'");
        browser.LoadUrl(tokenMgr.BrowserUrl);
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