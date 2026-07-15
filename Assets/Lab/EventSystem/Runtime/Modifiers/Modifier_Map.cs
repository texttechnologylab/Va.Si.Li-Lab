using System.Collections.Generic;
using Lab.Core.Runtime.Scripts.LLM;
using UnityEngine;
using UnityEngine.UI;

/*
Modifier_Map

@date 12.02.2026

@author Nick Korjakin
@version 1.0

On the top left screen of the Users vision, will be a minimap which shows the location of the user
*/
public class Modifier_Map : MonoBehaviour, IModifier, IAgentContextInfo
{
    [SerializeField] private Transform root;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 mapOffset = new Vector3(0f, 30f, 0f);
    [SerializeField] private Vector3 mapRotationEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] private float mapOrthoSize = 25f;
    [SerializeField] private Vector3 mapPos = new Vector3(0.2f, 0f, 0.65f);
    [SerializeField] private Camera sourceCamera;

    [Header("Player Dots")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector2 dotSize = new Vector2(14f, 14f);
    [SerializeField] private Color dotColor = Color.red;
    [SerializeField] private bool clampDotToMap = true;
    [SerializeField] private float refindPlayersEverySeconds = 0.5f;

    private string renderTextureName = "RT_Map";
    private int rtDepth = 24;
    private int rtSize = 1024;

    private GameObject mapUI;
    private Camera mapCamera;
    private RenderTexture mapRT;
    private RawImage mapRawImage;

    private RectTransform mapCanvasRT;
    private RectTransform mapViewRT;

    private readonly List<Transform> playerTfs = new List<Transform>();
    private readonly Dictionary<Transform, RectTransform> dotByPlayer = new Dictionary<Transform, RectTransform>();

    private bool isSetup;
    public bool IsMapActive;

    private float nextRefindTime;

    public string GetContextInfo() {
        return "{\\\"description\\\":\\\"Dies ist eine tragbare Karte, die den Standort von Spielern anzeigt.\\\"}";
    }

    void Awake()
    {
        SetupIfNeeded();
        Deactivate();
    }
    /**
    * If there is no camera, the camera will be placed on top of the player
    * If there is no canvas to show the minimap, the canvas will be created
    */
    private void SetupIfNeeded()
    {
        if (isSetup) return;

        if (root == null) {
            root = target.transform.Find("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
        }

        if (target == null) target = root;

        if (sourceCamera == null)
            sourceCamera = root.GetComponent<Camera>();

        if (sourceCamera == null)
            sourceCamera = Camera.main;

        CreateMapCanvasIfMissing();
        CreateMapCameraIfMissing();
        CreateOrResizeRenderTexture(true);

        isSetup = true;
    }

    /**
    * Canvas of minimap will be activated or deactivated
    */
    public void Toggle(GameObject t = null, float value = -1f)
    {
        SetupIfNeeded();

        if (t != null) target = t.transform;

        if (!IsMapActive) Activate(value);
        else Deactivate();
    }

    public void Activate() => Activate(-1f);

    /**
    * activates the canvas for map and positions everything where it should be
    */
    private void Activate(float value)
    {
        SetupIfNeeded();

        if (value > 0f) mapOrthoSize = value;

        IsMapActive = true;

        if (mapUI != null) mapUI.SetActive(true);
        if (mapCamera != null) mapCamera.enabled = true;
        mapUI.GetComponentInChildren<RawImage>(true).texture = mapRT;

        nextRefindTime = 0f;

        UpdateMapCameraPose(true);
        UpdatePlayerDots(true);
    }

    /**
    * deactivates the canvas of map
    */
    public void Deactivate()
    {
        SetupIfNeeded();

        IsMapActive = false;

        if (mapUI != null) mapUI.SetActive(false);
        if (mapCamera != null) mapCamera.enabled = false;

        foreach (var kv in dotByPlayer)
        {
            if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        }
    }

    /**
    * Updates camera position
    */
    void LateUpdate()
    {
        if (!IsMapActive) return;
        if (mapCamera == null || target == null) return;

        UpdateMapCameraPose(false);
        UpdatePlayerDots(false);
    }

    /**
    * Updates camera position
    */
    private void UpdateMapCameraPose(bool forceCopyFromSource)
    {
        if (mapCamera == null || target == null) return;

        if (forceCopyFromSource && sourceCamera != null)
            mapCamera.CopyFrom(sourceCamera);

        mapCamera.orthographic = true;
        mapCamera.orthographicSize = Mathf.Max(0.01f, mapOrthoSize);
        mapCamera.fieldOfView = 60f;

        if (mapCanvasRT != null) mapCanvasRT.localPosition = mapPos;

        mapCamera.transform.position = target.position + mapOffset;
        mapCamera.transform.rotation = Quaternion.Euler(mapRotationEuler);

        mapCamera.nearClipPlane = 0.01f;
        mapCamera.farClipPlane = 500f;

        mapCamera.targetTexture = mapRT;
        mapCamera.enabled = true;
    }

    /**
    * Updates on the minimap the exact position of the User
    */
    private void UpdatePlayerDots(bool forceRefind)
    {
        if (mapCamera == null || mapViewRT == null) return;

        if (forceRefind || Time.time >= nextRefindTime)
        {
            nextRefindTime = Time.time + Mathf.Max(0.05f, refindPlayersEverySeconds);
            RefindPlayers();
        }

        for (int i = playerTfs.Count - 1; i >= 0; i--)
        {
            var tf = playerTfs[i];
            if (tf == null)
            {
                playerTfs.RemoveAt(i);
                continue;
            }

            if (!dotByPlayer.TryGetValue(tf, out RectTransform dotRT) || dotRT == null)
            {
                dotRT = CreateDotFor(tf);
            }

            Vector3 vp = mapCamera.WorldToViewportPoint(tf.position);

            if (vp.z <= 0f)
            {
                dotRT.gameObject.SetActive(false);
                continue;
            }

            if (clampDotToMap)
            {
                vp.x = Mathf.Clamp01(vp.x);
                vp.y = Mathf.Clamp01(vp.y);
            }

            Vector2 size = mapViewRT.rect.size;
            Vector2 anchored = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);

            dotRT.anchoredPosition = anchored;
            dotRT.gameObject.SetActive(true);
        }

        var toRemove = new List<Transform>();
        foreach (var kv in dotByPlayer)
        {
            if (kv.Key == null || !playerTfs.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var key = toRemove[i];
            if (key == null) continue;
            if (dotByPlayer.TryGetValue(key, out var rt) && rt != null) Destroy(rt.gameObject);
            dotByPlayer.Remove(key);
        }
    }
    /**
    * Updates player position on map
    */
    private void RefindPlayers()
    {
        playerTfs.Clear();

        var gos = GameObject.FindGameObjectsWithTag(playerTag);
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i] != null) playerTfs.Add(gos[i].transform);
        }
    }

    /**
    * Creates a Dot on the map for every object with a tag
    */
    private RectTransform CreateDotFor(Transform tf)
    {
        if (mapViewRT == null) return null;

        GameObject dotGO = new GameObject("PlayerDot_" + tf.GetInstanceID(), typeof(RectTransform), typeof(Image));
        dotGO.transform.SetParent(mapViewRT, false);

        var rt = dotGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = dotSize;
        rt.anchoredPosition = Vector2.zero;

        var img = dotGO.GetComponent<Image>();
        img.color = dotColor;
        img.raycastTarget = false;

        dotByPlayer[tf] = rt;
        return rt;
    }

    /**
    * Creates the map canvas if missing
    */
    private void CreateMapCanvasIfMissing()
    {
        Transform existing = root.Find("MapCanvas");
        if (existing != null)
        {
            mapUI = existing.gameObject;
            mapCanvasRT = existing.GetComponent<RectTransform>();
            mapRawImage = existing.GetComponentInChildren<RawImage>(true);
            mapViewRT = mapRawImage != null ? mapRawImage.rectTransform : null;
            return;
        }

        GameObject canvasGO = new GameObject(
            "MapCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasGO.transform.SetParent(root, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = null;
        canvas.planeDistance = 100f;

        mapCanvasRT = canvasGO.GetComponent<RectTransform>();
        mapCanvasRT.anchorMin = new Vector2(0f, 0f);
        mapCanvasRT.anchorMax = new Vector2(0f, 0f);
        mapCanvasRT.pivot = new Vector2(0.5f, 0.5f);

        mapCanvasRT.localPosition = mapPos;
        mapCanvasRT.localRotation = Quaternion.identity;
        mapCanvasRT.localScale = Vector3.one;
        mapCanvasRT.sizeDelta = new Vector2(0.5f, 0.5f);

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GameObject rawGO = new GameObject("MapView", typeof(RectTransform), typeof(RawImage));
        rawGO.transform.SetParent(canvasGO.transform, false);

        mapViewRT = rawGO.GetComponent<RectTransform>();
        mapViewRT.anchorMin = Vector2.zero;
        mapViewRT.anchorMax = Vector2.one;
        mapViewRT.offsetMin = Vector2.zero;
        mapViewRT.offsetMax = Vector2.zero;
        mapViewRT.localRotation = Quaternion.identity;
        mapViewRT.localScale = Vector3.one;

        mapRawImage = rawGO.GetComponent<RawImage>();
        mapRawImage.raycastTarget = false;

        mapUI = canvasGO;
    }
    /**
    * Creates the camera on top of the player for the map if missing
    */
    private void CreateMapCameraIfMissing()
    {
        Transform existing = root.Find("MapCamera");
        if (existing != null)
        {
            mapCamera = existing.GetComponent<Camera>();
            if (mapCamera == null) mapCamera = existing.gameObject.AddComponent<Camera>();
            return;
        }

        GameObject camGO = new GameObject("MapCamera");
        camGO.transform.SetParent(root, false);

        mapCamera = camGO.AddComponent<Camera>();

        if (sourceCamera != null)
            mapCamera.CopyFrom(sourceCamera);

        mapCamera.enabled = false;
    }
    /**
    * Resizes the picture which will be places on the canvas
    */
    private void CreateOrResizeRenderTexture(bool force = false)
    {
        int w = rtSize;
        int h = rtSize;

        if (!force && mapRT != null && mapRT.width == w && mapRT.height == h)
            return;

        if (mapRT != null)
        {
            if (mapCamera != null && mapCamera.targetTexture == mapRT)
                mapCamera.targetTexture = null;

            mapRT.Release();
            Destroy(mapRT);
        }

        mapRT = new RenderTexture(w, h, rtDepth, RenderTextureFormat.ARGB32);
        mapRT.name = renderTextureName;
        mapRT.Create();

        if (mapCamera != null)
            mapCamera.targetTexture = mapRT;

        if (mapRawImage != null)
            mapRawImage.texture = mapRT;
    }

    void OnDestroy()
    {
        if (mapRT != null)
        {
            if (mapCamera != null && mapCamera.targetTexture == mapRT)
                mapCamera.targetTexture = null;

            mapRT.Release();
            Destroy(mapRT);
        }

        foreach (var kv in dotByPlayer)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        dotByPlayer.Clear();
        playerTfs.Clear();
    }

    public void Toggle(float zoomFov = -1f) => Toggle(null, zoomFov);

    public string getName()
    {
        return "Modifier_Map";
    }
    public bool IsActive()
    {
        return IsMapActive;
    }

    public string getDiscription()
    {
        return "Verleiht dem Spieler eine Minimap";
    }
}
