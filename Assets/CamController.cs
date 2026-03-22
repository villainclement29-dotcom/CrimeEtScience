using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PhotoCameraController : MonoBehaviour
{
    [Header("Réglage des Limites")]
    [Tooltip("Glisse ici l'image de décor qui sert de fond")]
    public SpriteRenderer backgroundSprite;

    [Header("Déplacement")]
    public float moveSpeed = 5f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 5f; // Réduit un peu pour éviter de sortir du cadre trop vite

    [Header("Détection Photo")]
    public float detectionRadius = 0.5f;
    public Color successColor = Color.green;

    private Camera cam;
    private Vector2 moveInput;
    private float zoomInput;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (backgroundSprite == null)
        {
            Debug.LogError("<color=red>⚠ ERREUR :</color> Tu as oublié de glisser l'image de fond dans l'Inspector !");
        }
    }

    // --- ENTRÉES (Player Input) ---

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    
    public void OnZoom(InputValue value)
    {
        // On récupère le Vector2 et on utilise l'axe Y
        Vector2 inputVector = value.Get<Vector2>();
        zoomInput = inputVector.y;
    }

    public void OnPhoto() => CheckPhotoValidation();

    // --- LOGIQUE ---

    void Update()
    {
        if (backgroundSprite == null) return;

        // 1. GESTION DU ZOOM
        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            float targetSize = cam.orthographicSize - (zoomInput * zoomSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
        }

        // 2. CALCUL DES LIMITES DYNAMIQUES
        // Taille du sprite de fond
        float bgWidth = backgroundSprite.bounds.size.x;
        float bgHeight = backgroundSprite.bounds.size.y;
        Vector3 bgPos = backgroundSprite.transform.position;

        // Ce que la caméra voit actuellement (dépend du zoom)
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // Calcul des points d'arrêt pour que les bords de la cam ne dépassent pas le décor
        float minX = bgPos.x - (bgWidth / 2f) + camWidth;
        float maxX = bgPos.x + (bgWidth / 2f) - camWidth;
        float minY = bgPos.y - (bgHeight / 2f) + camHeight;
        float maxY = bgPos.y + (bgHeight / 2f) - camHeight;

        // 3. DÉPLACEMENT ET SÉCURITÉ
        Vector3 newPos = transform.position + (Vector3)moveInput * moveSpeed * Time.deltaTime;

        // Si l'image est plus petite que la vue (trop dézoomé), on centre sur l'axe concerné
        if (minX > maxX) newPos.x = bgPos.x;
        else newPos.x = Mathf.Clamp(newPos.x, minX, maxX);

        if (minY > maxY) newPos.y = bgPos.y;
        else newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        // Debug Clavier (E)
        if (Keyboard.current.eKey.wasPressedThisFrame) CheckPhotoValidation();
    }

    void CheckPhotoValidation()
    {
        Debug.Log("📸 Photo prise !");
        
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y);
        RaycastHit2D hit = Physics2D.CircleCast(rayOrigin, detectionRadius, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("hint"))
            {
                HintObject hint = hit.collider.GetComponent<HintObject>();
                if (hint != null)
                {
                    Debug.Log("<color=green>✅ Indice trouvé : </color>" + hint.hintName);
                    hint.OnFound(successColor);
                }
            }
            else
            {
                Debug.Log("Objet touché : " + hit.collider.name + " (n'est pas un indice)");
            }
        }
    }

    // Affiche le cercle de détection rouge dans la vue SCENE
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}