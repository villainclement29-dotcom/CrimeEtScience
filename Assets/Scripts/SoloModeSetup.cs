using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

/// <summary>
/// S'exécute en premier dans la scène principale (ordre -200, avant PlayerInput à -1).
/// En mode solo : désactive le joueur non sélectionné et redirige la caméra du joueur
/// actif sur le Display 0. Ne fait rien en mode duo.
/// </summary>
[DefaultExecutionOrder(-200)]
public class SoloModeSetup : MonoBehaviour
{
    void Awake()
    {
        if (!StartMenuController.IsSoloMode) return;

        int chosen      = CharacterSelectionData.J1CharacterIndex;
        int activePlayer = (chosen == 1) ? 1 : 0; // 0 = terrain, 1 = labo

        DeactivateUnusedPlayer(activePlayer);
        SetupCamera(activePlayer);
    }

    void DeactivateUnusedPlayer(int activePlayerId)
    {
        var players = FindObjectsByType<PoliceManMovements>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.playerId != activePlayerId)
                p.gameObject.SetActive(false);
        }
    }

    void SetupCamera(int activePlayerId)
    {
        Camera camP1 = null;
        Camera camP2 = null;

        // Les caméras sont tagguées MainCameraJ1 / MainCameraJ2
        foreach (var cam in Camera.allCameras)
        {
            if (cam.CompareTag("MainCameraJ1")) camP1 = cam;
            else if (cam.CompareTag("MainCameraJ2")) camP2 = cam;
        }

        if (activePlayerId == 0)
        {
            // Terrain : CameraP1 déjà sur Display 0, on désactive CameraP2
            if (camP2 != null) camP2.gameObject.SetActive(false);
        }
        else
        {
            // Labo : on redirige CameraP2 vers Display 0, on désactive CameraP1
            if (camP2 != null) camP2.targetDisplay = 0;
            if (camP1 != null) camP1.gameObject.SetActive(false);
        }
    }
}
