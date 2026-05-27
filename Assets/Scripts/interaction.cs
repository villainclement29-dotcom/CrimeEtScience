using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneChanger : MonoBehaviour
{
    [Header("Paramètres de Scène")]
    public string sceneToLoad;

    [Header("Référence Transition")]
    public VideoTransitionHandler transitionHandler;

    [Header("Points")]
    public int pointsToGive = 50;

    [Header("SFX")]
    public AudioClip interactSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private bool isPlayerInside = false;
    private int playerIndexInside = -1; // 0 = J1, 1 = J2
    private InteractionPointVisibility visibilityScript;

    void Start()
    {
        // On récupère le script de visibilité sur le même objet
        visibilityScript = GetComponent<InteractionPointVisibility>();
    }

    void Update()
    {
        if (!isPlayerInside || playerIndexInside < 0) return;

        if (IsInteractPressed(playerIndexInside))
        {
            AddPointsToInteractingPlayer();
            Debug.Log($"<color=cyan>[Interaction]</color> +{pointsToGive} pts. Vers : {sceneToLoad}");
            if (interactSFX != null)
                AudioSource.PlayClipAtPoint(interactSFX, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
            ExecuteChange();
        }
    }

    private bool IsInteractPressed(int playerIndex)
    {
        InputDevice device = null;
        if (GlobalPlayerManager.Instance != null)
            device = playerIndex == 0
                ? GlobalPlayerManager.Instance.Player1Device
                : GlobalPlayerManager.Instance.Player2Device;

        if (device is Gamepad gp)
            return gp.buttonSouth.wasPressedThisFrame;

        if (device is Keyboard kb)
            return kb.eKey.wasPressedThisFrame;

        // Fallback si pas de GlobalPlayerManager : on accepte n'importe quel clavier
        if (device == null && Keyboard.current != null)
            return Keyboard.current.eKey.wasPressedThisFrame;

        return false;
    }

    private void AddPointsToInteractingPlayer()
    {
        if (playerIndexInside == 0) WinManager.scoreJ1 += pointsToGive;
        else if (playerIndexInside == 1) WinManager.scoreJ2 += pointsToGive;
    }

    private void ExecuteChange()
    {
        if (string.IsNullOrEmpty(sceneToLoad)) return;

        // Sauvegarde la scène courante pour y revenir après le minijeu
        WinManager.returnScene = SceneManager.GetActiveScene().name;

        // Sauvegarde les positions des joueurs
        WinManager.hasSavedPositions = false;
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.name.Contains("1")) { WinManager.savedPositionJ1 = p.transform.position; WinManager.hasSavedPositions = true; }
            else if (p.name.Contains("2")) { WinManager.savedPositionJ2 = p.transform.position; WinManager.hasSavedPositions = true; }
        }

        // Marque ce point d'interaction comme utilisé et le désactive
        InteractionPointRandomizer.MarkPointUsed(transform.position);
        gameObject.SetActive(false);

        if (transitionHandler != null)
            transitionHandler.StartTransition(sceneToLoad);
        else
            SceneTransitionManager.LoadScene(sceneToLoad);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;

            // On rend l'objet visible via le script de visibilité
            if (visibilityScript != null) visibilityScript.SetPlayerNearby(true);

            // Identification du joueur par son nom
            if (other.gameObject.name.Contains("1")) playerIndexInside = 0;
            else if (other.gameObject.name.Contains("2")) playerIndexInside = 1;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerIndexInside = -1;

            // On repasse en mode "caché" (sauf si la lampe torche l'éclaire encore)
            if (visibilityScript != null) visibilityScript.SetPlayerNearby(false);
        }
    }
}