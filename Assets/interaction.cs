using UnityEngine;
using UnityEngine.SceneManagement; // Obligatoire pour changer de scène

public class SceneChanger : MonoBehaviour
{
    public string sceneToLoad; // Tape le nom de ta scène dans l'Inspector
    public KeyCode interactionKey = KeyCode.E; // Touche par défaut : E

    private bool isPlayerInside = false;

    void Update()
    {
        // Si le joueur est dans la zone ET qu'il appuie sur la touche
        if (isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            ChangeScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            Debug.Log("Appuyez sur " + interactionKey + " pour entrer");
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
            Debug.LogError("Nom de scène manquant sur l'objet " + gameObject.name);
        }
    }
}