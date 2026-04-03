using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneChanger : MonoBehaviour
{
    [Header("Paramètres de Scène")]
    public string sceneToLoad;

    [Header("Référence Transition")]
    [Tooltip("Glisse ici l'objet qui possède le script VideoTransitionHandler")]
    public VideoTransitionHandler transitionHandler;

    private bool isPlayerInside = false;

    void Update()
    {
        if (!isPlayerInside) return;

        // --- DÉTECTION DES INPUTS ---
        bool gamepadInteract = false;
        foreach (var gp in Gamepad.all)
        {
            if (gp.buttonSouth.wasPressedThisFrame) gamepadInteract = true;
        }

        bool keyboardInteract = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        // --- SI ACTION ---
        if (gamepadInteract || keyboardInteract)
        {
            Debug.Log($"<color=cyan>[Interaction]</color> Bouton pressé ! Tentative de transition vers : {sceneToLoad}");
            ExecuteChange();
        }
    }

    private void ExecuteChange()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("<color=red>[SceneChanger]</color> Nom de scène manquant sur " + gameObject.name);
            return;
        }

        // Si on a un handler de transition, on l'utilise
        if (transitionHandler != null)
        {
            transitionHandler.StartTransition(sceneToLoad);
        }
        else
        {
            // Sinon, on charge directement (sécurité)
            Debug.LogWarning("[SceneChanger] Aucun TransitionHandler trouvé, chargement direct.");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            Debug.Log("<color=green>[Trigger]</color> JOUEUR DÉTECTÉ ! Appuyez sur A ou E.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            Debug.Log("<color=red>[Trigger]</color> Le joueur a quitté la zone.");
        }
    }
}