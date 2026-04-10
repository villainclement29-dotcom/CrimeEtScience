using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class WinManager : MonoBehaviour
{
    public static WinManager Instance;

    [Header("Navigation")]
    public string mainSceneName = "MainScene";
    public float delayBeforeRedirection = 5f;

    // Scores statiques persistants
    public static int scoreJ1 = 0, scoreJ2 = 0;

    // ── Meilleur score all-time (PlayerPrefs) ─────────────────────────
    const string BEST_SCORE_KEY = "BestScore";
    public static int  GetBestScore() => PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
    public static void TrySaveBestScore(int score)
    {
        if (score > GetBestScore())
        {
            PlayerPrefs.SetInt(BEST_SCORE_KEY, score);
            PlayerPrefs.Save();
        }
    }

    // Retour après minijeu
    public static string returnScene = "";
    public static Vector3 savedPositionJ1 = Vector3.zero;
    public static Vector3 savedPositionJ2 = Vector3.zero;
    public static bool hasSavedPositions = false;

    private Canvas canvasJ1, canvasJ2;
    private TextMeshProUGUI textJ1, textJ2;
    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isGameOver = false;

        GameObject p1 = GameObject.Find("VictoryPanel");
        GameObject p2 = GameObject.Find("LoosePanel");

        Camera cam1 = GameObject.Find("Camerap1")?.GetComponent<Camera>();
        Camera cam2 = GameObject.Find("Camerap2")?.GetComponent<Camera>();

        if (p1 && cam1)
        {
            canvasJ1 = p1.GetComponent<Canvas>();
            canvasJ1.worldCamera = cam1;
            textJ1 = p1.GetComponentInChildren<TextMeshProUGUI>(true);
            p1.SetActive(false);
        }

        if (p2 && cam2)
        {
            canvasJ2 = p2.GetComponent<Canvas>();
            canvasJ2.worldCamera = cam2;
            textJ2 = p2.GetComponentInChildren<TextMeshProUGUI>(true);
            p2.SetActive(false);
        }

        if (hasSavedPositions && scene.name == returnScene)
            StartCoroutine(RestorePlayerPositions());
    }

    IEnumerator RestorePlayerPositions()
    {
        yield return null; // attendre un frame que la scène soit initialisée

        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.name.Contains("1")) p.transform.position = savedPositionJ1;
            else if (p.name.Contains("2")) p.transform.position = savedPositionJ2;
        }

        hasSavedPositions = false;
        savedPositionJ1 = savedPositionJ2 = Vector3.zero;
        returnScene = "";
    }

    public void ShowVictory(int winnerId)
    {
        if (isGameOver) return;
        isGameOver = true;

        // --- CHANGEMENT ICI : +100 au lieu de +1 ---
        if (winnerId == 0) scoreJ1 += 100; else scoreJ2 += 100;

        if (canvasJ1) canvasJ1.gameObject.SetActive(true);
        if (canvasJ2) canvasJ2.gameObject.SetActive(true);

        string winnerName = "Joueur " + (winnerId + 1);
        string winMsg = $"BRAVO !\nScore Global : {scoreJ1} - {scoreJ2}";
        string loseMsg = $"PERDU...\n{winnerName} a gagné !";

        if (winnerId == 0)
        {
            if (textJ1) textJ1.text = winMsg;
            if (textJ2) textJ2.text = loseMsg;
        }
        else
        {
            if (textJ1) textJ1.text = loseMsg;
            if (textJ2) textJ2.text = winMsg;
        }

        StartCoroutine(WaitAndRedirect());
    }

    IEnumerator WaitAndRedirect()
    {
        yield return new WaitForSeconds(delayBeforeRedirection);
        if (canvasJ1) canvasJ1.gameObject.SetActive(false);
        if (canvasJ2) canvasJ2.gameObject.SetActive(false);
        string target = string.IsNullOrEmpty(returnScene) ? mainSceneName : returnScene;
        // returnScene est conservé pour la comparaison dans OnSceneLoaded, il sera vidé après restauration
        SceneManager.LoadScene(target);
    }
}