using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections;
using Cainos.PixelArtTopDown_Basic;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance;

    [Header("Minimap")]
    public float minimapSize = 280f;
    public float edgePadding = 15f;
    public int rtResolution = 512;
    public float dotSize = 14f;

    string mainScene = "MainScene";

    Camera mmCam;
    RenderTexture mmRT;
    GameObject[] canvasGOs = new GameObject[2];
    RectTransform[] dotsJ1 = new RectTransform[2];
    RectTransform[] dotsJ2 = new RectTransform[2];

    Transform pJ1, pJ2;
    bool isActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainScene) StartCoroutine(Setup());
        else Teardown();
    }

    IEnumerator Setup()
    {
        yield return null;

        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            var pm = go.GetComponent<PoliceManMovements>();
            if (pm == null) continue;
            if (pm.playerId == 0) pJ1 = go.transform;
            else                   pJ2 = go.transform;
        }

        Bounds bounds = ComputeMapBounds();
        CreateCamera(bounds);
        for (int d = 0; d < 2; d++) CreateUI(d);
        isActive = true;
    }

    Bounds ComputeMapBounds()
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;

        foreach (var r in Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (first) { b = r.bounds; first = false; }
            else         b.Encapsulate(r.bounds);
        }

        if (first)
        {
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (first) { b = r.bounds; first = false; }
                else         b.Encapsulate(r.bounds);
            }
        }
        return b;
    }

    void CreateCamera(Bounds bounds)
    {
        var go = new GameObject("MinimapCam");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(bounds.center.x, bounds.center.y, -50f);

        mmCam = go.AddComponent<Camera>();
        mmCam.orthographic     = true;
        mmCam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.05f;
        mmCam.clearFlags       = CameraClearFlags.SolidColor;
        mmCam.backgroundColor  = Color.black;
        mmCam.cullingMask      = ~(1 << 5);
        mmCam.depth            = -100;

        mmRT = new RenderTexture(rtResolution, rtResolution, 0);
        mmCam.targetTexture = mmRT;
    }

    void CreateUI(int display)
    {
        var go = new GameObject($"MinimapCanvas_D{display}");
        go.transform.SetParent(transform);
        canvasGOs[display] = go;

        var cv = go.AddComponent<Canvas>();
        cv.renderMode    = RenderMode.ScreenSpaceOverlay;
        cv.targetDisplay = display;
        cv.sortingOrder  = 90;

        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // Container bas-droite
        var container = new GameObject("Container");
        container.transform.SetParent(go.transform, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(1, 0);
        cRT.pivot     = new Vector2(1, 0);
        cRT.anchoredPosition = new Vector2(-edgePadding, edgePadding);
        cRT.sizeDelta = new Vector2(minimapSize, minimapSize);

        var border = container.AddComponent<Image>();
        border.color = Color.white;

        // Image de la carte
        var mapGO = new GameObject("Map");
        mapGO.transform.SetParent(container.transform, false);
        var raw = mapGO.AddComponent<RawImage>();
        raw.texture = mmRT;
        var mRT = mapGO.GetComponent<RectTransform>();
        mRT.anchorMin = Vector2.zero;
        mRT.anchorMax = Vector2.one;
        mRT.offsetMin = new Vector2(3, 3);
        mRT.offsetMax = new Vector2(-3, -3);

        // Points joueurs
        dotsJ1[display] = MakeDot(container.transform, "J1", new Color(0.3f, 0.75f, 1f));
        dotsJ2[display] = MakeDot(container.transform, "J2", new Color(1f, 0.55f, 0.2f));
    }

    RectTransform MakeDot(Transform parent, string name, Color color)
    {
        var go = new GameObject($"Dot_{name}");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(dotSize, dotSize);
        rt.anchorMin  = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot      = new Vector2(0.5f, 0.5f);
        return rt;
    }

    void Update()
    {
        if (!isActive || mmCam == null) return;

        float viewSize = 2f * mmCam.orthographicSize;
        Vector3 camPos = mmCam.transform.position;
        float inner = minimapSize - 6f;

        for (int d = 0; d < 2; d++)
        {
            MoveDot(dotsJ1[d], pJ1, camPos, viewSize, inner);
            MoveDot(dotsJ2[d], pJ2, camPos, viewSize, inner);
        }
    }

    void MoveDot(RectTransform dot, Transform player,
        Vector3 camPos, float viewSize, float inner)
    {
        if (dot == null || player == null) return;
        float x = ((player.position.x - camPos.x) / viewSize) * inner;
        float y = ((player.position.y - camPos.y) / viewSize) * inner;
        dot.anchoredPosition = new Vector2(x, y);
    }

    void Teardown()
    {
        isActive = false;
        pJ1 = pJ2 = null;
        for (int i = 0; i < 2; i++)
        {
            if (canvasGOs[i] != null) { Destroy(canvasGOs[i]); canvasGOs[i] = null; }
            dotsJ1[i] = dotsJ2[i] = null;
        }
        if (mmCam != null) { Destroy(mmCam.gameObject); mmCam = null; }
        if (mmRT  != null) { mmRT.Release(); Destroy(mmRT); mmRT = null; }
    }
}
