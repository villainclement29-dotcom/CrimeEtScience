using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GlobalPlayerManager : MonoBehaviour
{
    public static GlobalPlayerManager Instance;

    [Header("Stockage des Manettes")]
    public InputDevice Player1Device; // Index 0
    public InputDevice Player2Device; // Index 1

    [Header("Gestion du 3ème Écran")]
    [Tooltip("Le nom exact de la scène de score (ex: Scene_ScoreBoard)")]
    public string scoreboardSceneName = "Scene_ScoreBoard";

    void Awake()
    {
        // --- 1. GESTION DU SINGLETON ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("<color=cyan><b>[GlobalPlayerManager]</b> : Initialisé et Persistant.</color>");

            // --- 2. ACTIVATION PHYSIQUE DES ÉCRANS ---
            // On active les écrans supplémentaires au démarrage du jeu
            if (Display.displays.Length > 1) Display.displays[1].Activate();
            if (Display.displays.Length > 2) Display.displays[2].Activate();

            // --- 3. PREMIER CHARGEMENT DU SCOREBOARD ---
            LoadScoreBoard();
        }
        else
        {
            Debug.LogWarning("<color=orange><b>[GlobalPlayerManager]</b> : Double détecté, destruction du doublon.</color>");
            Destroy(gameObject);
        }
    }

    // On s'abonne à l'événement de chargement de scène
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Cette fonction est appelée AUTOMATIQUEMENT à chaque fois qu'une scène finit de charger
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si la scène est chargée en mode Single (ce qui arrive quand on change de mini-jeu),
        // Unity a déchargé le ScoreBoard. On doit donc le remettre.
        if (mode == LoadSceneMode.Single)
        {
            LoadScoreBoard();
        }
    }

    private void LoadScoreBoard()
    {
        if (!string.IsNullOrEmpty(scoreboardSceneName))
        {
            // On vérifie si la scène n'est pas déjà présente pour éviter de la charger 2 fois
            Scene sbScene = SceneManager.GetSceneByName(scoreboardSceneName);
            if (!sbScene.isLoaded)
            {
                // On charge en ADDITIVE pour qu'elle s'ajoute à la scène actuelle
                SceneManager.LoadScene(scoreboardSceneName, LoadSceneMode.Additive);
                Debug.Log($"<color=magenta><b>[GlobalPlayerManager]</b> : Ré-injection de {scoreboardSceneName} sur l'écran 3.</color>");
            }
        }
    }

    // Fonction appelée par le LobbyController pour enregistrer la manette
    public void AssignPlayer(int index, InputDevice device)
    {
        if (device == null) return;

        if (index == 0) Player1Device = device;
        else if (index == 1) Player2Device = device;

        Debug.Log($"<color=green><b>[GlobalPlayerManager]</b> : J{index} lié à {device.displayName}</color>");
    }

    public void DebugCurrentPlayers()
    {
        string p1 = (Player1Device != null) ? Player1Device.displayName : "AUCUN";
        string p2 = (Player2Device != null) ? Player2Device.displayName : "AUCUN";
        Debug.Log($"<color=yellow>J0: {p1} | J1: {p2}</color>");
    }
}