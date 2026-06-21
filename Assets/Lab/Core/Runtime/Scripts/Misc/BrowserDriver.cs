using UnityEngine;
using TLab.WebView;

/// <summary>
/// Drop-in replacement for the package's <see cref="BrowserSample"/> that throttles the expensive
/// per-frame web-frame pull.
///
/// In ByteBuffer capture mode, <c>browser.UpdateFrame()</c> calls the native <c>GetFrameBuffer</c>
/// over JNI (copying the entire webview framebuffer into a fresh managed byte[] every call) and then
/// re-uploads it to a Texture2D. The package's BrowserSample runs that every Unity frame (72-90 Hz),
/// which on Quest cost ~8.5 ms/frame plus multi-MB/frame of GC. The web content itself only renders
/// at the native SetFps rate (~10 Hz), so pulling it faster is wasted work.
///
/// This driver pulls frames at <see cref="frameUpdateFps"/> (match it to SetFps) while still draining
/// the message queue every frame so page/JS events stay responsive. It disables any BrowserSample on
/// the same GameObject in Awake so the two don't both drive UpdateFrame.
/// </summary>
public class BrowserDriver : MonoBehaviour
{
    [Tooltip("BrowserContainer to drive. Auto-resolved from this GameObject if left empty.")]
    [SerializeField] private BrowserContainer m_container;

    [Tooltip("How often (Hz) to pull a new web frame into the Unity texture. Match the native " +
             "SetFps value (e.g. 10). Lower = cheaper. Does not affect input/event responsiveness.")]
    [SerializeField] private float frameUpdateFps = 10f;

    [Tooltip("Optional OVROverlayCanvas displaying this browser. If set, it is marked dirty after " +
             "each web-frame pull so it re-renders only when the content actually changes. The " +
             "canvas should have Manual Redraw enabled — otherwise it ignores this and keeps " +
             "rendering every frame (see OVROverlayCanvas.ShouldRender).")]
    [SerializeField] private OVROverlayCanvas m_overlayCanvas;

    private float _nextFrameTime;

    void Awake()
    {
        if (m_container == null)
            m_container = GetComponent<BrowserContainer>();
        if (m_container == null)
        {
            Debug.LogError("[BrowserDriver] No BrowserContainer assigned or found on this GameObject — disabling.");
            enabled = false;
            return;
        }

        // Stop the package's BrowserSample from also calling UpdateFrame every frame.
        // Disabling it in Awake means its Start() (which calls Init()) won't run, so we
        // take over initialization below.
        var sample = GetComponent<BrowserSample>();
        if (sample != null)
            sample.enabled = false;
    }

    void Start() => m_container.browser.Init();

    void Update()
    {
        var browser = m_container.browser;

        // Drain page/JS events every frame — cheap relative to the framebuffer pull and keeps
        // callbacks (e.g. OnJsResult) responsive.
        browser.DispatchMessageQueue();

        // The expensive GetFrameBuffer pull + texture upload only at content rate.
        if (Time.time >= _nextFrameTime)
        {
            _nextFrameTime = Time.time + 1f / Mathf.Max(1f, frameUpdateFps);
            browser.UpdateFrame();

            // Tell the overlay a new frame landed so it re-renders the quad at content rate
            // (~frameUpdateFps) instead of every app frame. Only effective with Manual Redraw on.
            if (m_overlayCanvas != null)
                m_overlayCanvas.SetFrameDirty();
        }
    }
}
