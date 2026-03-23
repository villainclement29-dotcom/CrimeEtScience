using UnityEngine;
using UnityEngine.InputSystem;
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

        // --- 1. RECONNEXION À LA MANETTE DE LA SESSION ---
        if (GlobalPlayerManager.Instance != null)
        {
            InputDevice sessionDevice = (id == 0) ?
                GlobalPlayerManager.Instance.Player1Device :
                GlobalPlayerManager.Instance.Player2Device;

            if (sessionDevice != null)
            {
                playerInput.SwitchCurrentControlScheme(sessionDevice);
                Debug.Log($"<color=yellow>[J{id}]</color> Manette session associée : {sessionDevice.displayName}");
            }
            else
            {
                Debug.LogError($"<color=red>[J{id}]</color> Aucune manette de session trouvée !");
            }
        }
        else
        {
            Debug.LogError($"[J{id}] <color=red>ATTENTION :</color> GlobalPlayerManager introuvable.");
        }

        // --- 2. RECHERCHE DU FOND ---
        string targetBG = (id == 0) ? "Background_P1" : "Background_P2";
        GameObject bgObj = GameObject.Find(targetBG);

        if (bgObj != null)
        {
            backgroundSprite = bgObj.GetComponent<SpriteRenderer>();
            Debug.Log("<color=green>[J" + id + "]</color> Fond lié : " + targetBG);
        }
        else
        {
            Debug.LogError("[J" + id + "] ERREUR : Impossible de trouver " + targetBG);
        }

        AssignTextsToIndices();
    }

    void Update()
    {
        if (playerInput != null)
        {
            moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            zoomInput = playerInput.actions["Zoom"].ReadValue<Vector2>().y;

            if (playerInput.actions["Photo"].triggered)
            {
                Debug.Log($"<color=yellow>[J{playerId}] PHOTO !</color>");
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
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (zoomInput * zoomSpeed * Time.deltaTime), minZoom, maxZoom);

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        Bounds b = backgroundSprite.bounds;

        float minX = b.min.x + camWidth;
        float maxX = b.max.x - camWidth;
        float minY = b.min.y + camHeight;
        float maxY = b.max.y - camHeight;

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
            // Vérification de distance pour séparer les indices J1 et J2
            bool distanceValide = (playerId == 0 && indice.transform.position.x < 10) || (playerId == 1 && indice.transform.position.x > 10);

            if (distanceValide)
            {
                foreach (TextMeshProUGUI txt in allTexts)
                {
                    if (txt.text.ToLower().Contains(indice.hintName.ToLower()))
                    {
                        indice.linkedText = txt;
                        Debug.Log($"<color=white>[J{playerId}] UI liée : {indice.hintName}</color>");
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
                Debug.Log($"<color=green>J{playerId} PHOTO VALIDE !</color>");
                h.OnFound(successColor);
            }
        }
    }
}