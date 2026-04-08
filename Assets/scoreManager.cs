using UnityEngine;
using TMPro;

public class ScoreBoardDisplay : MonoBehaviour
{
    public TextMeshProUGUI textJ1;
    public TextMeshProUGUI textJ2;

    void Update()
    {
        // Accès direct aux variables static du WinManager
        textJ1.text = "Score J1: " + WinManager.scoreJ1 + "pts";
        textJ2.text = "Score J2: " + WinManager.scoreJ2 + "pts";
    }
}