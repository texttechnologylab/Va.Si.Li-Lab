using TLab.WebView;
using UnityEngine;

public class BrowserReload : MonoBehaviour
{
    [Header("Browser Reference")]
    [SerializeField]
    [Tooltip("The browser component to reload.")]
    private Browser browser;


    public void ReloadBrowser()
    {
        if (browser != null)
        {
            browser.LoadUrl(browser.GetUrl());
        }
        else
        {
            Debug.LogWarning("Browser reference not set on BrowserReload component.");
        }
    }

}