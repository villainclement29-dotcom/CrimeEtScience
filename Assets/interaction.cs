using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Obligatoire pour le Gamepad

public class SceneChanger : MonoBehaviour
{
    public string sceneToLoad;
    private bool isPlayerInside = false;

    void Update()
    {
        // On vérifie s'il y a une manette et si le bouton South est pressé
        bool gamepadInteract = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        // On garde aussi le clavier (E) au cas où pour tes tests
        bool keyboardInteract = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (isPlayerInside && (gamepadInteract || keyboardInteract))
        {
            ChangeScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifie si l'objet a le tag Player
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            Debug.Log("Appuyez sur A (Manette) ou E (Clavier) pour entrer");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Nom de scène manquant sur " + gameObject.name);
        }
    }
}