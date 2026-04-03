using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // INDISPENSABLE pour la séparation des contrôles
using TMPro;

public class PhotoCameraController : MonoBehaviour
{
    [Header("Configuration Joueur")]
    [Tooltip("0 pour Joueur 1 (J0), 1 pour Joueur 2 (J1)")]
    public int playerId = 0;

    [Header("Réglages")]
    public float moveSpeed = 5f;
    public float zoomSpeed = 5f;
    public float minZoom = 1.5f;
    public float maxZoom = 4f;

    [Header("Détection")]
    public float detectionRadius = 0.5f;
    public Color successColor = Color.green;

    [Header("Score & Fin de Partie")]
    private int hintsFound = 0;
    private int totalHintsToFind = 0;
    private bool hasWon = false;

    private Camera cam;
    private PlayerInput playerInput;
    private SpriteRenderer backgroundSprite;
    private Vector2 moveInput;
    private float zoomInput;

    void Awake()
    {
        cam = GetComponent<Camera>();
        playerInput = GetComponent<PlayerInput>();

        Debug.Log("<color=white>ÉVEIL : Script attaché sur " + gameObject.name + "</color>");
    }

    void Start()
    {
        int id = playerId;
        Debug.Log($"<color=cyan>JOUEUR ID : {id} (Prefab: {gameObject.name})</color>");

        // --- 1. RECONNEXION PHYSIQUE FORCÉE (Lobby -> Mini-jeu) ---
        if (GlobalPlayerManager.Instance != null && playerInput != null)
        {
            InputDevice sessionDevice = (id == 0) ?
                GlobalPlayerManager.Instance.Player1Device :
                GlobalPlayerManager.Instance.Player2Device;

            if (sessionDevice != null)
            {
                // CRUCIAL : On empêche le PlayerInput d'écouter tout le monde
                playerInput.user.UnpairDevices();

                // On force la liaison unique avec la manette (ou clavier) du manager
                InputUser.PerformPairingWithDevice(sessionDevice, playerInput.user);

                Debug.Log($"<color=green>[J{id}]</color> Liaison PHYSIQUE forcée avec : {sessionDevice.displayName}");
            }
            else
            {
                Debug.LogError($"<color=red>[J{id}]</color> Aucune manette de session trouvée !");
            }
        }

        // --- 2. RECHERCHE DU FOND ---
        string targetBG = (id == 0) ? "Background_P1" : "Background_P2";
        GameObject bgObj = GameObject.Find(targetBG);

        if (bgObj != null)
        {
            backgroundSprite = bgObj.GetComponent<SpriteRenderer>();
        }

        // --- 3. INITIALISATION DES SCORES ---
        AssignTextsToIndices();

        // On compte combien de Textes TMP sont enfants de ce joueur pour définir l'objectif
        totalHintsToFind = GetComponentsInChildren<TextMeshProUGUI>(true).Length;
        Debug.Log($"<color=orange>[J{playerId}] Objectif : {totalHintsToFind} indices à trouver.</color>");
    }

    public void AddScore()
    {
        hintsFound++;
        Debug.Log($"<color=green>[J{playerId}] Score : {hintsFound}/{totalHintsToFind}</color>");

        if (hintsFound >= totalHintsToFind && !hasWon)
        {
            DeclareWinner();
        }
    }

    void DeclareWinner()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log($"<color=gold>VICTOIRE ! Le Joueur {playerId + 1} a complété sa liste !</color>");

        // --- APPEL AU WIN MANAGER ---
        if (WinManager.Instance != null)
        {
            WinManager.Instance.ShowVictory(playerId);
        }
    }

    void Update()
    {
        if (playerInput != null && !hasWon)
        {
            // Lecture directe des actions depuis le PlayerInput appairé
            moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            zoomInput = playerInput.actions["Zoom"].ReadValue<Vector2>().y;

            if (playerInput.actions["Photo"].triggered)
            {
                CheckPhotoValidation();
            }
        }

        if (backgroundSprite == null) return;

        if (moveInput != Vector2.zero || Mathf.Abs(zoomInput) > 0.01f)
        {
            ApplyMovement();
        }
    }

    void ApplyMovement()
    {
        // Calcul du Zoom
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (zoomInput * zoomSpeed * Time.deltaTime), minZoom, maxZoom);

        // Calcul des limites du fond (Background)
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        Bounds b = backgroundSprite.bounds;

        float minX = b.min.x + camWidth;
        float maxX = b.max.x - camWidth;
        float minY = b.min.y + camHeight;
        float maxY = b.max.y - camHeight;

        // Déplacement
        Vector3 nextPos = transform.position + (Vector3)moveInput * moveSpeed * Time.deltaTime;
        nextPos.x = (minX > maxX) ? b.center.x : Mathf.Clamp(nextPos.x, minX, maxX);
        nextPos.y = (minY > maxY) ? b.center.y : Mathf.Clamp(nextPos.y, minY, maxY);

        transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
    }

    void AssignTextsToIndices()
    {
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        HintObject[] allIndices = Object.FindObjectsByType<HintObject>(FindObjectsSortMode.None);

        foreach (HintObject indice in allIndices)
        {
            // On vérifie si l'indice est du côté du joueur (X < 10 pour J1, X > 10 pour J2)
            bool distanceValide = (playerId == 0 && indice.transform.position.x < 10) || (playerId == 1 && indice.transform.position.x > 10);

            if (distanceValide)
            {
                foreach (TextMeshProUGUI txt in allTexts)
                {
                    if (txt.text.ToLower().Contains(indice.hintName.ToLower()))
                    {
                        indice.linkedText = txt;
                    }
                }
            }
        }
    }

    void CheckPhotoValidation()
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, detectionRadius, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("hint"))
        {
            HintObject h = hit.collider.GetComponent<HintObject>();
            if (h != null)
            {
                h.OnFound(successColor, this);
            }
        }
    }
}