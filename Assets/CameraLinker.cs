using UnityEngine;

public class PlayerCameraLinker : MonoBehaviour
{
    public static PlayerCameraLinker Instance;

    [Header("Assignation des Caméras")]
    public Camera camJ0; // Glisse Camerap1 (Gauche) - Index 0
    public Camera camJ1; // Glisse Camerap2 (Droite) - Index 1

    void Awake()
    {
        Instance = this;
    }

    // Retourne la caméra correspondant à l'index du joueur
    public Camera GetCamera(int index)
    {
        return (index == 0) ? camJ0 : camJ1;
    }
}