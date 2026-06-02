using TLab.WebView;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Enables scrolling a Browser window using the Meta Quest right joystick (SecondaryThumbstick).
/// Optionally scrolls at the position the controller is pointing at.
/// </summary>
public class BrowserJoystickScroller : MonoBehaviour
{
    [Header("Browser Reference")]
    [SerializeField]
    [Tooltip("The browser component to scroll. If not set, will try to find it on this GameObject.")]
    private Browser browser;

    [Header("Scroll Settings")]
    [SerializeField]
    [Tooltip("Scroll speed multiplier for vertical scrolling.")]
    private float verticalScrollSpeed = 300f;

    [SerializeField]
    [Tooltip("Scroll speed multiplier for horizontal scrolling.")]
    private float horizontalScrollSpeed = 300f;

    [SerializeField]
    [Tooltip("Deadzone threshold for joystick input to prevent drift.")]
    [Range(0f, 0.5f)]
    private float joystickDeadzone = 0.1f;

    [SerializeField]
    [Tooltip("Invert vertical scroll direction.")]
    private bool invertVertical = false;

    [SerializeField]
    [Tooltip("Invert horizontal scroll direction.")]
    private bool invertHorizontal = false;

    [SerializeField]
    [Tooltip("Enable horizontal scrolling with the joystick X-axis.")]
    private bool enableHorizontalScroll = true;

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
            Debug.LogError("[BrowserJoystickScroller] No Browser component found. Please assign one in the inspector.");
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
            return;

        // Get right joystick input using OVRInput (SecondaryThumbstick = right controller)
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // Apply deadzone
        if (Mathf.Abs(joystickInput.x) < joystickDeadzone)
            joystickInput.x = 0f;
        if (Mathf.Abs(joystickInput.y) < joystickDeadzone)
            joystickInput.y = 0f;

        // Only scroll if there's input
        if (joystickInput.sqrMagnitude > 0f)
        {
            PerformScroll(joystickInput);
        }
    }

    private void PerformScroll(Vector2 joystickInput)
    {
        // Calculate scroll amounts (inverted Y because joystick up should scroll up/content down)
        int scrollX = 0;
        int scrollY = 0;
        Debug.Log("Joystick Input: " + joystickInput);
        // Vertical scrolling (joystick Y-axis)
        // Joystick up (positive Y) should scroll content up (negative scroll value)
        float verticalInput = invertVertical ? joystickInput.y : -joystickInput.y;
        scrollY = (int)(verticalInput * verticalScrollSpeed * Time.deltaTime);

        // Horizontal scrolling (joystick X-axis)
        if (enableHorizontalScroll)
        {
            float horizontalInput = invertHorizontal ? -joystickInput.x : joystickInput.x;
            scrollX = (int)(horizontalInput * horizontalScrollSpeed * Time.deltaTime);
        }

        // Perform the scroll
        if (scrollX != 0 || scrollY != 0)
        {
            browser.ScrollBy(scrollX, scrollY);
        }
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
}
