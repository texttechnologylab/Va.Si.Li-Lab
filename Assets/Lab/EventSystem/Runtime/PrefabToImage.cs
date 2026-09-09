using System.Collections;
using UnityEngine;
using UnityEngine.UI;


/*
PrefabToImage

@date 17.02.2026

@author Nick Korjakin
@version 1.0

Within this class, the Prefabs get Images to show in the Inventory.
When the Scene starts, the Prefabs will be in a layer of the scene. In that Layer, the picture will be taken and can be used for the Inventory pictures.
*/
public class PrefabToImage : MonoBehaviour
{
    [Header("Input")]
    public GameObject prefab;
    public RawImage rawImage;

    [Header("Preview Settings")]
    public int textureSize = 256;

    [Range(0, 31)]
    public int previewLayerIndex = 30;

    [Range(1.0f, 2.0f)]
    public float framingPadding = 1.15f;
    public Vector3 previewRotationEuler = new Vector3(20f, -30f, 0f);

    public bool cleanupAfterRender = true;

    private GameObject camGO;
    private Camera cam;
    private GameObject lightGO;
    private GameObject inst;
    private RenderTexture rt;

    private Texture2D bakedTexture;

    private static bool isRendering = false;

    void Awake()
    {
        if (!rawImage) rawImage = GetComponent<RawImage>();
    }

    void Start()
    {
        StartCoroutine(RenderQueued());
    }

    /**
    * one prefab will be rendered, the other prefabs are in queue and wait for the first prefab to be rendered
    */
    public IEnumerator RenderQueued()
    {
        if (!prefab || !rawImage) yield break;

        yield return null;

        while (isRendering)
            yield return null;

        isRendering = true;

        RenderNow();

        yield return new WaitForEndOfFrame();

        isRendering = false;
    }

    /**
    * an image will be rendered of the prefab
    */
    [ContextMenu("Render Now")]
    public void RenderNow()
    {
        if (!prefab || !rawImage)
        {
            Debug.LogWarning("Prefab oder RawImage fehlt.");
            return;
        }

        if (bakedTexture)
        {
            Destroy(bakedTexture);
            bakedTexture = null;
        }

        CleanupPreviewOnly();

        int layerMask = 1 << previewLayerIndex;

        rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2
        };
        rt.Create();

        camGO = new GameObject("~PreviewCam");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        cam = camGO.AddComponent<Camera>();
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = false;
        cam.fieldOfView = 30f;
        cam.cullingMask = layerMask;
        cam.targetTexture = rt;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        lightGO = new GameObject("~PreviewLight");
        lightGO.hideFlags = HideFlags.HideAndDontSave;
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = Color.white;
        light.cullingMask = layerMask;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        inst = Instantiate(prefab);
        inst.hideFlags = HideFlags.HideAndDontSave;

        SetLayerRecursively(inst, previewLayerIndex);

        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.Euler(previewRotationEuler);

        FrameObject(inst, cam, framingPadding);

        cam.Render();

        bakedTexture = BakeRTToTexture2D(rt, textureSize);

        rawImage.texture = bakedTexture;
        rawImage.color = Color.white;

        if (cleanupAfterRender)
        {
            CleanupPreviewOnly();
        }
    }

    /**
    * image will be adjusted
    * @return adjusted image
    */
    private static Texture2D BakeRTToTexture2D(RenderTexture sourceRT, int size)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = sourceRT;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        return tex;
    }

    /**
    * only one gameobject per layer
    */
    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    /**
    * camera gets positioned to take image from gameobject
    */
    static void FrameObject(GameObject go, Camera cam, float padding)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);

        Vector3 center = b.center;
        float radius = b.extents.magnitude * padding;

        float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float dist = radius / Mathf.Sin(fovRad * 0.5f);

        cam.transform.position = center + new Vector3(0, 0, -dist);
        cam.transform.LookAt(center);
    }

    void OnDestroy()
    {
        CleanupAll();
    }

    private void CleanupPreviewOnly()
    {
        if (inst) Destroy(inst);
        inst = null;

        if (camGO) Destroy(camGO);
        camGO = null;
        cam = null;

        if (lightGO) Destroy(lightGO);
        lightGO = null;

        if (rt)
        {
            rt.Release();
            Destroy(rt);
        }
        rt = null;
    }

    private void CleanupAll()
    {
        CleanupPreviewOnly();

        if (bakedTexture)
        {
            Destroy(bakedTexture);
            bakedTexture = null;
        }
    }
}

