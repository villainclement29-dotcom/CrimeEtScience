using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyManager : MonoBehaviour
{
    public GameObject player2Prefab;

    // Cette fonction est appelée par le composant Player Input Manager 
    // via l'événement "Player Joined"
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // Si c'est le premier joueur, on ne fait rien (il utilise le prefab par défaut)
        // Si c'est le deuxième joueur (index 1), on change son prefab
        if (playerInput.playerIndex == 1)
        {
            Debug.Log("Joueur 2 connecté ! Changement de caméra...");
            // Tu peux aussi changer manuellement le Target Display ici
            Camera cam = playerInput.GetComponentInChildren<Camera>();
            if(cam != null) cam.targetDisplay = 1; // Display 2
        }
    }
}