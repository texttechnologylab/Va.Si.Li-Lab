using System.Collections;
using UnityEngine;
using VaSiLi.WebCam;

public class BrowserJS : MonoBehaviour
{
    [SerializeField]
    private TLab.WebView.WebView browser;
    private int resultId = 0;

    private bool nativeSetupDone = false;
    private bool firstLoadStarted = false;
    private bool cookiesClearedAndReloaded = false;
    private InterviewTokenManager subscribedManager;

    private static readonly WaitForSeconds WaitSettle = new(0.5f);
    private static readonly WaitForSeconds WaitReload = new(1.0f);

    public void Start()
    {
        browser.eventCallback.onPageStart.AddListener(OnPageStart);
        browser.eventCallback.onPageFinish.AddListener(OnPageFinish);
    }

    public void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.TokenChanged -= OnTokenChanged;
    }

    public void Update()
    {
        if (!nativeSetupDone && browser.IsInitialized())
        {
            nativeSetupDone = true;
            browser.SetUserAgent("Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", false);
        }

        if (subscribedManager == null && InterviewTokenManager.Instance != null)
        {
            subscribedManager = InterviewTokenManager.Instance;
            subscribedManager.TokenChanged += OnTokenChanged;
        }

        TryFirstLoad();
    }

    private void OnTokenChanged(string _) => TryFirstLoad();

    private void TryFirstLoad()
    {
        if (!nativeSetupDone) return;
        if (firstLoadStarted) return;

        var tokenMgr = subscribedManager;
        if (tokenMgr == null) tokenMgr = InterviewTokenManager.Instance;
        if (tokenMgr == null || string.IsNullOrEmpty(tokenMgr.Token)) return;

        firstLoadStarted = true;
        Debug.Log($"[BrowserJS] First load — URL: {tokenMgr.BrowserUrl}");
        browser.LoadUrl(tokenMgr.BrowserUrl);
    }

    private void OnPageStart(string url)
    {
        Debug.Log($"[BrowserJS] onPageStart: {url}");
    }

    private void OnPageFinish(string url)
    {
        Debug.Log($"[BrowserJS] onPageFinish: {url}");

        if (!cookiesClearedAndReloaded)
        {
            // First page has finished loading — now clear cookies and reload with the token URL.
            StartCoroutine(ClearAndReload());
            return;
        }

        // Second load (post-cookie-clear) is done — install the JS message listener.
        OnBrowserReady();
    }

    private IEnumerator ClearAndReload()
    {
        cookiesClearedAndReloaded = true;

        // Give the browser a moment to settle before touching it again.
        yield return WaitSettle;

        browser.ClearCookie();
        Debug.Log("[BrowserJS] Cookies cleared, waiting before reload...");

        yield return WaitReload;

        var tokenMgr = subscribedManager;
        if (tokenMgr == null) tokenMgr = InterviewTokenManager.Instance;
        if (tokenMgr == null || string.IsNullOrEmpty(tokenMgr.Token))
        {
            Debug.LogWarning("[BrowserJS] Token missing at reload time — aborting reload.");
            yield break;
        }

        Debug.Log($"[BrowserJS] Reloading with token URL: {tokenMgr.BrowserUrl}");
        browser.LoadUrl(tokenMgr.BrowserUrl);
    }

    private void OnBrowserReady()
    {
        resultId++;
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
        var enumerable = browser.EvaluateJSForResult(varName, jsCode);
        foreach (var result in enumerable)
        {
            if (result == null)
            {
                yield return null;
                continue;
            }
        }
    }

    public IEnumerator ExecuteJS(string jsCode)
    {
        if (browser != null)
        {
            var res = browser.EvaluateJSForResult("options", jsCode);
            foreach (var result in res)
            {
                if (result != null)
                {
                    Debug.Log("JS Result: " + result);
                }
                yield return result;
            }
        }
        else
        {
            Debug.LogWarning("Browser reference is not set.");
        }

        yield break;
    }
}
