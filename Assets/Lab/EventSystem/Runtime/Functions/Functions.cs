using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VaSiLi.Helper;

public class Funktions : MonoBehaviour
{
    [Header("Root (nur das hier setzen)")]
    public Transform root;
    private Camera sourceCamera;

    [Header("Zoom Settings")]
    public float zoomFOV = 25f;
    public float normalFOV = 60f;

    private string renderTextureName = "RT_Zoom";
    private int rtDepth = 24;
    private GameObject scopeUI;
    private Camera zoomCamera;
    private RenderTexture zoomRT;
    private RawImage zoomRawImage;

    public bool zooming = false;
    public bool zoomGamepad = false;

    int lastW, lastH;

    void Awake()
    {
        if (root == null) root = transform;
        sourceCamera = root.GetComponent<Camera>();

        CreateScopeCanvasIfMissing();
        CreateZoomCameraIfMissing();
        CreateOrResizeRenderTexture(force: true);

        if (scopeUI != null) scopeUI.SetActive(false);
        if (zoomCamera != null) zoomCamera.enabled = false;
    }

    void Update()
    {
        zooming = LabInput.KeyHeld(Key.Space);

        // Left grip on controllers, left index pinch with hand tracking
        zoomGamepad = LabInput.SecondaryHeld();

        bool isZoom = zooming || zoomGamepad;

        CreateOrResizeRenderTexture();

        if (scopeUI != null)
            scopeUI.SetActive(isZoom);

        if (zoomCamera != null)
        {
            zoomCamera.enabled = isZoom;

            if (sourceCamera != null)
            {
                zoomCamera.transform.position = sourceCamera.transform.position;
                zoomCamera.transform.rotation = sourceCamera.transform.rotation;
            }

            zoomCamera.fieldOfView = isZoom ? zoomFOV : normalFOV;
        }
    }

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

        crt.localPosition = new Vector3(-0.27f, 0f, 1f);
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
        {
            zoomCamera.CopyFrom(sourceCamera);
        }
        else
        {
            zoomCamera.fieldOfView = zoomFOV;
        }
    }

    private void CreateOrResizeRenderTexture(bool force = false)
    {
        int w = 1024;//Screen.width;
        int h = 1024;//Screen.height;

        if (!force && zoomRT != null && w == lastW && h == lastH)
            return;

        lastW = w; lastH = h;

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
}
