using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Chrono général de 7 minutes. Singleton DontDestroyOnLoad.
/// Crée lui-même ses Canvas/UI sur les deux displays joueurs.
/// Quand le temps expire : affiche un overlay de fin de partie sur les deux écrans,
/// puis redirige vers le Lobby après un délai.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; set; }

    [Header("Durée du jeu (secondes)")]
    public float totalTime = 420f; // 7 minutes

    [Header("Navigation")]
    public string startMenuSceneName = "StartMenu";
    public string mainSceneName  = "MainScene";
    public float delayBeforeRedirection = 6f;

    // ── État ─────────────────────────────────────────────────────────
    float remaining;
    bool  gameOver;

    // ── UI chrono ────────────────────────────────────────────────────
    TextMeshProUGUI textJ1, textJ2;
    GameObject timerCanvasJ1, timerCanvasJ2;

    // ── UI overlay fin de partie ─────────────────────────────────────
    GameObject overlayJ1, overlayJ2;

    // ── Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameOver)
            {
                Instance.remaining = Instance.totalTime;
                Instance.gameOver  = false;
                Instance.HideOverlays();
                Instance.RefreshUI();
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        remaining = totalTime;
        gameOver  = false;

        CreateAllTimerUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ne recréer l'UI que si on arrive sur la scène principale
        if (scene.name != mainSceneName)
            return;

        if (timerCanvasJ1 == null && timerCanvasJ2 == null)
        {
            remaining = totalTime;
            gameOver  = false;
            CreateAllTimerUI();
        }
    }

    void Update()
    {
        if (gameOver) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            TriggerEnd();
            return;
        }

        RefreshUI();
    }

    // ── API publique ─────────────────────────────────────────────────

    public static void ResetAndStart()
    {
        if (Instance == null) return;
        Instance.remaining = Instance.totalTime;
        Instance.gameOver  = false;
        Instance.HideOverlays();
    }

    // ── Logique de fin ───────────────────────────────────────────────
    public void TriggerEnd()
    {
        if (gameOver) return;
        gameOver = true;

        // Bonus = secondes restantes offertes au joueur en tête
        int bonus = Mathf.RoundToInt(Mathf.Max(0f, remaining));
        remaining = 0f;
        if (WinManager.scoreJ1 > WinManager.scoreJ2)
            WinManager.scoreJ1 += bonus;
        else if (WinManager.scoreJ2 > WinManager.scoreJ1)
            WinManager.scoreJ2 += bonus;

        ShowEndOverlay(WinManager.scoreJ1, WinManager.scoreJ2);
        StartCoroutine(RedirectToStartMenu());
    }

    IEnumerator RedirectToStartMenu()
    {
        yield return new WaitForSeconds(delayBeforeRedirection);

        // Sauvegarde leaderboard
        WinManager.AddLeaderboardEntry(StartMenuController.PseudoJ1, WinManager.scoreJ1);
        WinManager.AddLeaderboardEntry(StartMenuController.PseudoJ2, WinManager.scoreJ2);

        // Reset scores et pseudos
        WinManager.scoreJ1 = 0;
        WinManager.scoreJ2 = 0;
        StartMenuController.PseudoJ1 = "Joueur 1";
        StartMenuController.PseudoJ2 = "Joueur 2";

        HideOverlays();
        DestroyTimerUI();
        InteractionPointRandomizer.ResetSavedPositions();

        // Destruction des singletons de gameplay
        if (WinManager.Instance != null)
        {
            WinManager.Instance.StopAllCoroutines();
            Destroy(WinManager.Instance.gameObject);
            WinManager.Instance = null;
        }
        if (PauseMenu.Instance != null)
        {
            Destroy(PauseMenu.Instance.gameObject);
            PauseMenu.Instance = null;
        }
        if (MinimapController.Instance != null)
        {
            Destroy(MinimapController.Instance.gameObject);
            MinimapController.Instance = null;
        }
        if (GlobalPlayerManager.Instance != null)
        {
            Destroy(GlobalPlayerManager.Instance.gameObject);
            GlobalPlayerManager.Instance = null;
        }

        SceneTransitionManager.LoadScene(startMenuSceneName);

        Instance = null;
        Destroy(gameObject);
    }

    void DestroyTimerUI()
    {
        if (timerCanvasJ1) Destroy(timerCanvasJ1);
        if (timerCanvasJ2) Destroy(timerCanvasJ2);
        timerCanvasJ1 = timerCanvasJ2 = null;
        textJ1 = textJ2 = null;
    }

    // ── Overlay fin de partie ────────────────────────────────────────
    void ShowEndOverlay(int scoreJ1, int scoreJ2)
    {
        // Cache les chrono panels
        if (timerCanvasJ1) timerCanvasJ1.SetActive(false);
        if (timerCanvasJ2) timerCanvasJ2.SetActive(false);

        string winner;
        if (scoreJ1 > scoreJ2)       winner = "Joueur 1 gagne !";
        else if (scoreJ2 > scoreJ1)  winner = "Joueur 2 gagne !";
        else                          winner = "Egalite !";

        string msgJ1 = BuildOverlayMessage(scoreJ1, scoreJ2, winner, isJ1: true);
        string msgJ2 = BuildOverlayMessage(scoreJ1, scoreJ2, winner, isJ1: false);

        overlayJ1 = BuildOverlayCanvas("EndOverlay_J1", targetDisplay: 0, msgJ1, scoreJ1 >= scoreJ2);
        overlayJ2 = BuildOverlayCanvas("EndOverlay_J2", targetDisplay: 1, msgJ2, scoreJ2 >= scoreJ1);
    }

    string BuildOverlayMessage(int scoreJ1, int scoreJ2, string winner, bool isJ1)
    {
        string myScore    = isJ1 ? scoreJ1.ToString() : scoreJ2.ToString();
        string theirScore = isJ1 ? scoreJ2.ToString() : scoreJ1.ToString();
        string myLabel    = isJ1 ? "Joueur 1" : "Joueur 2";
        string theirLabel = isJ1 ? "Joueur 2" : "Joueur 1";

        return $"FIN DE PARTIE\n\n{winner}\n\n{myLabel} : {myScore} pts\n{theirLabel} : {theirScore} pts";
    }

    GameObject BuildOverlayCanvas(string canvasName, int targetDisplay, string message, bool isWinner)
    {
        var canvasGO = new GameObject(canvasName);
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder  = 200;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Fond semi-transparent plein écran
        var bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);

        var bgImg   = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.82f);

        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Texte centré
        var textGO = new GameObject("OverlayText");
        textGO.transform.SetParent(bgGO.transform, false);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = message;
        tmp.fontSize  = 60f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = isWinner ? new Color(1f, 0.85f, 0f) : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return canvasGO;
    }

    void HideOverlays()
    {
        if (overlayJ1) Destroy(overlayJ1);
        if (overlayJ2) Destroy(overlayJ2);
        overlayJ1 = overlayJ2 = null;

        if (timerCanvasJ1) timerCanvasJ1.SetActive(true);
        if (timerCanvasJ2) timerCanvasJ2.SetActive(true);
    }

    // ── Affichage chrono ─────────────────────────────────────────────
    void RefreshUI()
    {
        string label = FormatTime(remaining);
        Color  c     = remaining <= 30f ? Color.red : Color.white;

        if (textJ1) { textJ1.text = label; textJ1.color = c; }
        if (textJ2) { textJ2.text = label; textJ2.color = c; }
    }

    static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    // ── Création de l'UI chrono ──────────────────────────────────────
    void CreateAllTimerUI()
    {
        textJ1 = BuildTimerCanvas("TimerCanvas_J1", targetDisplay: 0, out timerCanvasJ1);
        textJ2 = BuildTimerCanvas("TimerCanvas_J2", targetDisplay: 1, out timerCanvasJ2);
    }

    TextMeshProUGUI BuildTimerCanvas(string canvasName, int targetDisplay, out GameObject canvasGO)
    {
        canvasGO = new GameObject(canvasName);
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder  = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Panneau fond semi-transparent (bande en haut)
        var panelGO  = new GameObject("TimerPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelImg   = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.45f);

        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.35f, 0.93f);
        panelRect.anchorMax = new Vector2(0.65f, 1.00f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Texte du chrono
        var textGO = new GameObject("TimerText");
        textGO.transform.SetParent(panelGO.transform, false);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = FormatTime(totalTime);
        tmp.fontSize  = 52f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);

        return tmp;
    }
}
