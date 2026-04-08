using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Nombre d'écrans détectés : {Display.displays.Length}");

        // Écran 1 (Joueur 1) : Toujours actif par défaut
        // Écran 2 (Joueur 2) :
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();

        // Écran 3 (ScoreBoard / Monitoring) :
        if (Display.displays.Length > 2)
            Display.displays[2].Activate();
    }
}