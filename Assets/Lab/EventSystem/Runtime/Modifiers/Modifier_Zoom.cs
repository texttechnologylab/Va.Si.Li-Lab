using Lab.Core.Runtime.Scripts.LLM;
using UnityEngine;
using UnityEngine.UI;

/*
Modifier_Map

@date 06.01.2026

@author Nick Korjakin
@version 1.0

Gives the player a modifier to zoom in
*/
public class Modifier_Zoom : MonoBehaviour, IModifier, IAgentContextInfo
{
    [SerializeField] private GameObject target;
    [SerializeField] private Transform root;
    [SerializeField] private float defaultZoomFOV = 25f;

    [SerializeField] private Camera sourceCamera;
    private string renderTextureName = "RT_Zoom";
    private int rtDepth = 24;
    private int rtSize = 1024;
    private GameObject scopeUI;
    private Camera zoomCamera;
    private RenderTexture zoomRT;
    private RawImage zoomRawImage;
    private float normalFOV;
    private bool isSetup;
    public bool IsZoomed { get; private set; }

    void Awake()
    {
        SetupIfNeeded();
        Deactivate();
    }
    
    public string GetContextInfo() {
        return "{\\\"description\\\":\\\"Dies ist ein Fernrohr, mit dem man Dinge aus der Nähe sehen kann.\\\"}";
    }
    
    /**
    * Setups the camera and the canvas, with which the user will be able to see the zoom
    */
    private void SetupIfNeeded()
    {
        if (isSetup) return;

        if (root == null)
        {
            root = target.transform.Find("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
        }

        if (sourceCamera == null)
            sourceCamera = root.GetComponent<Camera>();

        if (sourceCamera == null)
            sourceCamera = Camera.main;

        if (sourceCamera != null)
            normalFOV = sourceCamera.fieldOfView;

        CreateScopeCanvasIfMissing();
        CreateZoomCameraIfMissing();
        CreateOrResizeRenderTexture(true);

        isSetup = true;
    }
    /**
    * enables Zoom, if modifier zoom is active, disables if not
    */
    public void Toggle(GameObject target = null, float zoomFov = -1f)
    {
        SetupIfNeeded();

        if (!IsZoomed) Activate(zoomFov);
        else Deactivate();
    }

    public void Activate() => Activate(defaultZoomFOV);

    /**
    * activates zoom for user
    */
    private void Activate(float zoomFov)
    {
        SetupIfNeeded();

        IsZoomed = true;

        if (scopeUI != null) scopeUI.SetActive(true);
        if (zoomCamera != null)
        {
            zoomCamera.enabled = true;

            if (sourceCamera != null)
            {
                zoomCamera.transform.position = sourceCamera.transform.position;
                zoomCamera.transform.rotation = sourceCamera.transform.rotation;
            }

            zoomCamera.fieldOfView = (zoomFov > 0f) ? zoomFov : defaultZoomFOV;
        }
    }

    /**
    * deactivates zoom for player
    */
    public void Deactivate()
    {
        SetupIfNeeded();

        IsZoomed = false;

        if (scopeUI != null) scopeUI.SetActive(false);
        if (zoomCamera != null) zoomCamera.enabled = false;

        if (sourceCamera != null)
            sourceCamera.fieldOfView = normalFOV;
    }

    /**
    * @return "Modifier_Zoom"
    */
    public string getName()
    {
        return "Modifier_Zoom";
    }

    /**
    * Creates the canvas in which the zoom will be showed to the User
    */
    private void CreateScopeCanvasIfMissing()
    {
        Transform existing = root.Find("ScopeCanvas");
        if (existing != null)
        {
            scopeUI = existing.gameObject;
            zoomRawImage = existing.GetComponentInChildren<RawImage>(true);
            return;
        }

        GameObject canvasGO = new GameObject(
            "ScopeCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasGO.transform.SetParent(root, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = null;
        canvas.planeDistance = 100f;

        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
        canvas.overrideSorting = false;
        canvas.pixelPerfect = false;

        var crt = canvasGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(0f, 0f);
        crt.pivot = new Vector2(0.5f, 0.5f);

        crt.localPosition = new Vector3(-0.2f, 0f, 0.65f);
        crt.localRotation = Quaternion.identity;
        crt.localScale = Vector3.one;
        crt.sizeDelta = new Vector2(1f, 1f);

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GameObject rawGO = new GameObject("ZoomView", typeof(RectTransform), typeof(RawImage));
        rawGO.transform.SetParent(canvasGO.transform, false);

        var rt = rawGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        zoomRawImage = rawGO.GetComponent<RawImage>();
        zoomRawImage.raycastTarget = false;

        scopeUI = canvasGO;
    }

    /**
    * creates the camera, which includes the zoom picture
    */
    private void CreateZoomCameraIfMissing()
    {
        Transform existing = root.Find("ZoomCamera");
        if (existing != null)
        {
            zoomCamera = existing.GetComponent<Camera>();
            if (zoomCamera == null) zoomCamera = existing.gameObject.AddComponent<Camera>();
            return;
        }

        GameObject camGO = new GameObject("ZoomCamera");
        camGO.transform.SetParent(root, false);

        zoomCamera = camGO.AddComponent<Camera>();

        if (sourceCamera != null)
            zoomCamera.CopyFrom(sourceCamera);

        zoomCamera.enabled = false;
    }

    /**
    * Resizes so the user can see the zoom on the canvas without issues
    */
    private void CreateOrResizeRenderTexture(bool force = false)
    {
        int w = rtSize;
        int h = rtSize;

        if (!force && zoomRT != null && zoomRT.width == w && zoomRT.height == h)
            return;

        if (zoomRT != null)
        {
            if (zoomCamera != null && zoomCamera.targetTexture == zoomRT)
                zoomCamera.targetTexture = null;

            zoomRT.Release();
            Destroy(zoomRT);
        }

        zoomRT = new RenderTexture(w, h, rtDepth, RenderTextureFormat.ARGB32);
        zoomRT.name = renderTextureName;
        zoomRT.Create();

        if (zoomCamera != null)
            zoomCamera.targetTexture = zoomRT;

        if (zoomRawImage != null)
            zoomRawImage.texture = zoomRT;
    }
        public void Toggle(float zoomFov = -1f) => Toggle(null, zoomFov);

        public bool IsActive()
        {
            return IsZoomed;
        }

        public string getDiscription()
        {
            return "Ermöglicht das Zoomen für den Spieler";
        }
    }
