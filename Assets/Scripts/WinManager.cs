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
        SceneManager.LoadScene(mainSceneName);
    }
}