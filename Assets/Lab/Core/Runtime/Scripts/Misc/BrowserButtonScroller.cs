using TLab.WebView;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Enables scrolling a Browser window using Meta Quest controller buttons (A/B for vertical, X/Y for horizontal).
/// Optionally scrolls at the position the controller is pointing at.
/// Controller-only on purpose: with hand tracking the pinch is used to click/drag the page,
/// which already scrolls it, so mapping the pinch here as well would conflict.
/// </summary>
public class BrowserButtonScroller : MonoBehaviour
{
    [Header("Browser Reference")]
    [SerializeField]
    [Tooltip("The browser component to scroll. If not set, will try to find it on this GameObject.")]
    private Browser browser;

    [Header("Scroll Settings")]
    [SerializeField]
    [Tooltip("Scroll speed per second when button is held.")]
    private float scrollSpeed = 500f;

    [SerializeField]
    [Tooltip("Scroll amount per button tap when tap scrolling is enabled.")]
    private int scrollAmountPerTap = 50;

    [SerializeField]
    [Tooltip("If true, buttons only scroll once per press. If false, holding the button continuously scrolls.")]
    private bool tapScrollOnly = false;

    [SerializeField]
    [Tooltip("Delay before continuous scroll starts when holding a button (seconds).")]
    [Range(0f, 1f)]
    private float holdDelay = 0.3f;

    [Header("Button Configuration")]
    [SerializeField]
    [Tooltip("Controller to use for scrolling.")]
    private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [SerializeField]
    [Tooltip("Button to scroll up.")]
    private OVRInput.Button scrollUpButton = OVRInput.Button.One; // A button on right controller

    [SerializeField]
    [Tooltip("Button to scroll down.")]
    private OVRInput.Button scrollDownButton = OVRInput.Button.Two; // B button on right controller

    [SerializeField]
    [Tooltip("Enable horizontal scrolling with additional buttons.")]
    private bool enableHorizontalScroll = false;

    [SerializeField]
    [Tooltip("Button to scroll left (typically X on left controller).")]
    private OVRInput.Button scrollLeftButton = OVRInput.Button.Three; // X button

    [SerializeField]
    [Tooltip("Button to scroll right (typically Y on left controller).")]
    private OVRInput.Button scrollRightButton = OVRInput.Button.Four; // Y button

    [Header("Pointer-Based Scrolling (Optional)")]
    [SerializeField]
    [Tooltip("If set, scrolling will only work when the ray interactor is pointing at the browser collider.")]
    private XRRayInteractor rayInteractor;

    [SerializeField]
    [Tooltip("If true, scrolling only activates when pointing at the browser. If false, scrolling always works.")]
    private bool requirePointerOnBrowser = false;

    [SerializeField]
    [Tooltip("The collider representing the browser surface for raycast detection.")]
    private Collider browserCollider;

    private bool isInitialized = false;
    private float scrollUpHoldTime = 0f;
    private float scrollDownHoldTime = 0f;
    private float scrollLeftHoldTime = 0f;
    private float scrollRightHoldTime = 0f;

    private void Start()
    {
        if (browser == null)
        {
            browser = GetComponent<Browser>();
        }

        if (browserCollider == null && browser != null)
        {
            browserCollider = browser.GetComponent<Collider>();
        }

        if (browser == null)
        {
            Debug.LogError("[BrowserButtonScroller] No Browser component found. Please assign one in the inspector.");
            enabled = false;
            return;
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || browser == null)
            return;

        // Check if we require the pointer to be on the browser
        if (requirePointerOnBrowser && !IsPointerOnBrowser())
        {
            ResetHoldTimers();
            return;
        }

        // Handle vertical scrolling
        HandleVerticalScroll();

        // Handle horizontal scrolling if enabled
        if (enableHorizontalScroll)
        {
            HandleHorizontalScroll();
        }
    }

    private void HandleVerticalScroll()
    {
        bool scrollingUp = false;
        bool scrollingDown = false;

        // Scroll up
        if (OVRInput.Get(scrollUpButton, controller))
        {
            scrollingUp = true;
            scrollUpHoldTime += Time.deltaTime;

            if (tapScrollOnly)
            {
                // Only scroll once per press
                if (OVRInput.GetDown(scrollUpButton, controller))
                {
                    browser.ScrollBy(0, -scrollAmountPerTap);
                }
            }
            else
            {
                // Continuous scroll after hold delay
                if (scrollUpHoldTime >= holdDelay)
                {
                    int scrollAmount = (int)(scrollSpeed * Time.deltaTime);
                    browser.ScrollBy(0, -scrollAmount);
                }
                else if (scrollUpHoldTime >= Time.deltaTime) // First frame of press
                {
                    browser.ScrollBy(0, -scrollAmountPerTap);
                }
            }
        }
        else
        {
            scrollUpHoldTime = 0f;
        }

        // Scroll down
        if (OVRInput.Get(scrollDownButton, controller))
        {
            scrollingDown = true;
            scrollDownHoldTime += Time.deltaTime;

            if (tapScrollOnly)
            {
                // Only scroll once per press
                if (OVRInput.GetDown(scrollDownButton, controller))
                {
                    browser.ScrollBy(0, scrollAmountPerTap);
                }
            }
            else
            {
                // Continuous scroll after hold delay
                if (scrollDownHoldTime >= holdDelay)
                {
                    int scrollAmount = (int)(scrollSpeed * Time.deltaTime);
                    browser.ScrollBy(0, scrollAmount);
                }
                else if (scrollDownHoldTime >= Time.deltaTime) // First frame of press
                {
                    browser.ScrollBy(0, scrollAmountPerTap);
                }
            }
        }
        else
        {
            scrollDownHoldTime = 0f;
        }
    }

    private void HandleHorizontalScroll()
    {
        // Scroll left
        if (OVRInput.Get(scrollLeftButton, controller))
        {
            scrollLeftHoldTime += Time.deltaTime;

            if (tapScrollOnly)
            {
                // Only scroll once per press
                if (OVRInput.GetDown(scrollLeftButton, controller))
                {
                    browser.ScrollBy(-scrollAmountPerTap, 0);
                }
            }
            else
            {
                // Continuous scroll after hold delay
                if (scrollLeftHoldTime >= holdDelay)
                {
                    int scrollAmount = (int)(scrollSpeed * Time.deltaTime);
                    browser.ScrollBy(-scrollAmount, 0);
                }
                else if (scrollLeftHoldTime >= Time.deltaTime) // First frame of press
                {
                    browser.ScrollBy(-scrollAmountPerTap, 0);
                }
            }
        }
        else
        {
            scrollLeftHoldTime = 0f;
        }

        // Scroll right
        if (OVRInput.Get(scrollRightButton, controller))
        {
            scrollRightHoldTime += Time.deltaTime;

            if (tapScrollOnly)
            {
                // Only scroll once per press
                if (OVRInput.GetDown(scrollRightButton, controller))
                {
                    browser.ScrollBy(scrollAmountPerTap, 0);
                }
            }
            else
            {
                // Continuous scroll after hold delay
                if (scrollRightHoldTime >= holdDelay)
                {
                    int scrollAmount = (int)(scrollSpeed * Time.deltaTime);
                    browser.ScrollBy(scrollAmount, 0);
                }
                else if (scrollRightHoldTime >= Time.deltaTime) // First frame of press
                {
                    browser.ScrollBy(scrollAmountPerTap, 0);
                }
            }
        }
        else
        {
            scrollRightHoldTime = 0f;
        }
    }

    private void ResetHoldTimers()
    {
        scrollUpHoldTime = 0f;
        scrollDownHoldTime = 0f;
        scrollLeftHoldTime = 0f;
        scrollRightHoldTime = 0f;
    }

    private bool IsPointerOnBrowser()
    {
        if (rayInteractor == null || browserCollider == null)
            return true; // If not properly configured, allow scrolling

        // Check if the ray interactor is hitting our browser collider
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            return hit.collider == browserCollider;
        }

        return false;
    }

    /// <summary>
    /// Set the browser to scroll at runtime.
    /// </summary>
    public void SetBrowser(Browser newBrowser)
    {
        browser = newBrowser;
        if (browser != null && browserCollider == null)
        {
            browserCollider = browser.GetComponent<Collider>();
        }
    }

    /// <summary>
    /// Set the ray interactor for pointer-based scrolling at runtime.
    /// </summary>
    public void SetRayInteractor(XRRayInteractor interactor)
    {
        rayInteractor = interactor;
    }

    /// <summary>
    /// Enable or disable the requirement for the pointer to be on the browser.
    /// </summary>
    public void SetRequirePointerOnBrowser(bool require)
    {
        requirePointerOnBrowser = require;
    }

    /// <summary>
    /// Set the controller to use for button input.
    /// </summary>
    public void SetController(OVRInput.Controller newController)
    {
        controller = newController;
    }

    /// <summary>
    /// Set custom button mappings for scrolling.
    /// </summary>
    public void SetScrollButtons(OVRInput.Button up, OVRInput.Button down, OVRInput.Button left = OVRInput.Button.None, OVRInput.Button right = OVRInput.Button.None)
    {
        scrollUpButton = up;
        scrollDownButton = down;
        
        if (left != OVRInput.Button.None && right != OVRInput.Button.None)
        {
            scrollLeftButton = left;
            scrollRightButton = right;
            enableHorizontalScroll = true;
        }
    }
}
