using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // INDISPENSABLE pour la séparation des contrôles
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Sons")]
    public AudioClip shutterClip;
    public AudioClip zoomInClip;
    public AudioClip zoomOutClip;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Flash Photo")]
    public Image flashImage; // Image blanche plein écran dans le Canvas appareilPhoto

    [Header("Score & Fin de Partie")]
    private int hintsFound = 0;
    private int totalHintsToFind = 0;
    private bool hasWon = false;

    private Camera cam;
    private PlayerInput playerInput;
    private AudioSource audioSource;
    private AudioSource zoomAudioSource;
    private SpriteRenderer backgroundSprite;
    private Vector2 moveInput;
    private float zoomInput;

    void Awake()
    {
        cam = GetComponent<Camera>();
        playerInput = GetComponent<PlayerInput>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        zoomAudioSource = gameObject.AddComponent<AudioSource>();
        zoomAudioSource.loop        = false;
        zoomAudioSource.playOnAwake = false;
        zoomAudioSource.spatialBlend = 0f;
        zoomAudioSource.volume      = sfxVolume;

        if (flashImage != null) flashImage.color = new Color(1, 1, 1, 0);

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
        totalHintsToFind = GetComponentsInChildren<TextMeshProUGUI>().Length;
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
                if (shutterClip != null) audioSource.PlayOneShot(shutterClip, sfxVolume);
                if (flashImage != null) StartCoroutine(FlashEffect());
                CheckPhotoValidation();
            }

            UpdateZoomSound();
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
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
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

    void UpdateZoomSound()
    {
        if (zoomAudioSource == null) return;

        AudioClip target = null;
        if      (zoomInput >  0.1f) target = zoomInClip  != null ? zoomInClip  : zoomOutClip;
        else if (zoomInput < -0.1f) target = zoomOutClip != null ? zoomOutClip : zoomInClip;

        if (target != null && !zoomAudioSource.isPlaying)
            zoomAudioSource.PlayOneShot(target, sfxVolume);
    }

    IEnumerator FlashEffect()
    {
        flashImage.color = new Color(1, 1, 1, 1);
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            flashImage.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / 0.4f));
            yield return null;
        }
        flashImage.color = new Color(1, 1, 1, 0);
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