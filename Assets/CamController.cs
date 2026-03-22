using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PhotoCameraController : MonoBehaviour
{
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
        
        // LOG DE DÉBUG INITIAL
        Debug.Log("<color=white>ÉVEIL : Script attaché sur " + gameObject.name + "</color>");

        // Activation obligatoire des écrans secondaires au démarrage global
        if (Display.displays.Length > 1) {
            Display.displays[1].Activate();
            Debug.Log("<color=orange>SYSTÈME : Écran secondaire détecté et activé.</color>");
        }
    }

    void Start()
    {
        int id = playerInput.playerIndex; 
        Debug.Log("<color=cyan>JOUEUR RECONNU : ID = " + id + "</color>");

        // --- GESTION DES DISPLAYS (ÉCRANS) ---
        if (id == 0) 
        {
            cam.targetDisplay = 0; // Display 1
            Debug.Log("<color=green>[J0]</color> Configuré sur DISPLAY 1");
        } 
        else if (id == 1) 
        {
            cam.targetDisplay = 1; // Display 2
            Debug.Log("<color=blue>[J1]</color> Configuré sur DISPLAY 2");
        }

        // --- RECHERCHE DU FOND ---
        string targetBG = (id == 0) ? "Background_P1" : "Background_P2";
        GameObject bgObj = GameObject.Find(targetBG);
        if (bgObj != null) {
            backgroundSprite = bgObj.GetComponent<SpriteRenderer>();
            Debug.Log("<color=green>[J" + id + "]</color> Fond lié avec succès : " + targetBG);
        } else {
            Debug.LogError("[J" + id + "] ERREUR CRITIQUE : Impossible de trouver " + targetBG + " dans la scène !");
        }

        AssignTextsToIndices();
    }

    void Update()
    {
        // On lit les inputs directement via le PlayerInput
        if (playerInput != null) {
            moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            zoomInput = playerInput.actions["Zoom"].ReadValue<Vector2>().y;
            
            if (playerInput.actions["Photo"].triggered) {
                Debug.Log("<color=yellow>[J" + playerInput.playerIndex + "] TOUCHE PHOTO PRESSÉE</color>");
                CheckPhotoValidation();
            }
        }

        if (backgroundSprite == null) return;

        // --- MOUVEMENT ---
        if (moveInput != Vector2.zero) {
            ApplyMovement();
        }
    }

    void ApplyMovement()
    {
        // Zoom
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (zoomInput * zoomSpeed * Time.deltaTime), minZoom, maxZoom);

        // Limites
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
        foreach (HintObject indice in allIndices) {
            foreach (TextMeshProUGUI txt in allTexts) {
                if (txt.text.ToLower().Contains(indice.hintName.ToLower())) {
                    indice.linkedText = txt;
                    Debug.Log("<color=white>[J" + playerInput.playerIndex + "] Texte lié : " + indice.hintName + "</color>");
                }
            }
        }
    }

    void CheckPhotoValidation()
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, detectionRadius, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("hint")) {
            HintObject h = hit.collider.GetComponent<HintObject>();
            if (h != null) {
                Debug.Log("<color=green>PHOTO VALIDE !</color>");
                h.OnFound(successColor);
            }
        }
    }
}