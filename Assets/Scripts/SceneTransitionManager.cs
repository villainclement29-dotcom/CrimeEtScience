using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    static readonly float fadeDuration = 0.4f;

    CanvasGroup[] overlays = new CanvasGroup[2];
    bool isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("SceneTransitionManager");
        go.AddComponent<SceneTransitionManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlays();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void CreateOverlays()
    {
        for (int d = 0; d < 2; d++)
        {
            var canvasGO = new GameObject($"FadeCanvas_D{d}");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = d;
            canvas.sortingOrder = 9999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var cg = canvasGO.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var imgGO = new GameObject("Black");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var img = imgGO.AddComponent<Image>();
            img.color = Color.black;
            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            overlays[d] = cg;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        StopAllCoroutines();
        isTransitioning = false;
        StartCoroutine(Fade(1f, 0f));
    }

    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
        {
            if (!Instance.isTransitioning)
                Instance.StartCoroutine(Instance.DoTransition(sceneName));
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator DoTransition(string sceneName)
    {
        isTransitioning = true;
        yield return StartCoroutine(Fade(0f, 1f));
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        SetAlpha(from);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration)));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        for (int d = 0; d < 2; d++)
            if (overlays[d] != null)
                overlays[d].alpha = a;
    }
}
