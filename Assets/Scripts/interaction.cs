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
        if (!isPlayerInside) return;

        // Détection des touches
        bool interactPressed = false;

        foreach (var gp in Gamepad.all)
        {
            if (gp.buttonSouth.wasPressedThisFrame) interactPressed = true;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            interactPressed = true;

        if (interactPressed)
        {
            AddPointsToInteractingPlayer();
            Debug.Log($"<color=cyan>[Interaction]</color> +{pointsToGive} pts. Vers : {sceneToLoad}");
            ExecuteChange();
        }
    }

    private void AddPointsToInteractingPlayer()
    {
        if (playerIndexInside == 0) WinManager.scoreJ1 += pointsToGive;
        else if (playerIndexInside == 1) WinManager.scoreJ2 += pointsToGive;
    }

    private void ExecuteChange()
    {
        if (string.IsNullOrEmpty(sceneToLoad)) return;

        if (transitionHandler != null)
            transitionHandler.StartTransition(sceneToLoad);
        else
            SceneManager.LoadScene(sceneToLoad);
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