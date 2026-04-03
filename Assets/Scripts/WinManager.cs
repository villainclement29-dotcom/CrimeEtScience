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

    public static int scoreJ1 = 0, scoreJ2 = 0;

    private Canvas canvasJ1, canvasJ2;
    private TextMeshProUGUI textJ1, textJ2;
    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Appelé à chaque début de mini-jeu
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isGameOver = false;

        // 1. On cherche les Canvas nommés "VictoryPanel" et "LoosePanel" dans la scène
        GameObject p1 = GameObject.Find("VictoryPanel");
        GameObject p2 = GameObject.Find("LoosePanel");

        // 2. On cherche les caméras par leur NOM exact dans la scène
        Camera cam1 = GameObject.Find("Camerap1")?.GetComponent<Camera>();
        Camera cam2 = GameObject.Find("Camerap2")?.GetComponent<Camera>();

        if (p1 && cam1)
        {
            canvasJ1 = p1.GetComponent<Canvas>();
            canvasJ1.worldCamera = cam1; // On lie l'UI à la caméra J1
            textJ1 = p1.GetComponentInChildren<TextMeshProUGUI>(true);
            p1.SetActive(false); // On cache au départ
        }

        if (p2 && cam2)
        {
            canvasJ2 = p2.GetComponent<Canvas>();
            canvasJ2.worldCamera = cam2; // On lie l'UI à la caméra J2
            textJ2 = p2.GetComponentInChildren<TextMeshProUGUI>(true);
            p2.SetActive(false); // On cache au départ
        }
    }

    public void ShowVictory(int winnerId)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (winnerId == 0) scoreJ1++; else scoreJ2++;

        // On affiche les deux panels
        canvasJ1.gameObject.SetActive(true);
        canvasJ2.gameObject.SetActive(true);

        // On génère les textes
        string winnerName = "Joueur " + (winnerId + 1);
        string winMsg = $"BRAVO !\nScore : {scoreJ1}-{scoreJ2}";
        string loseMsg = $"PERDU...\n{winnerName} a gagné !";

        // Attribution dynamique selon qui a gagné
        if (winnerId == 0)
        {
            textJ1.text = winMsg;
            textJ2.text = loseMsg;
        }
        else
        {
            textJ1.text = loseMsg;
            textJ2.text = winMsg;
        }

        StartCoroutine(WaitAndRedirect());
    }

    IEnumerator WaitAndRedirect()
    {
        yield return new WaitForSeconds(delayBeforeRedirection);
        SceneManager.LoadScene(mainSceneName);
    }
}