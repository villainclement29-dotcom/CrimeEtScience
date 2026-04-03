using UnityEngine;
using TMPro;

public class HintObject : MonoBehaviour
{
    public string hintName; // Doit correspondre au texte UI
    [HideInInspector] public TextMeshProUGUI linkedText;

    private bool isFound = false; // Sécurité pour ne compter qu'une seule fois

    // On passe désormais la référence du joueur (PhotoCameraController) en paramètre
    public void OnFound(Color colorToApply, PhotoCameraController player)
    {
        if (isFound) return; // Si déjà trouvé, on ne fait rien

        isFound = true;

        // 1. Mise à jour visuelle de la liste UI
        if (linkedText != null)
        {
            linkedText.color = colorToApply;
            // On utilise les balises TMP pour barrer le texte proprement
            linkedText.text = "<s>" + linkedText.text + "</s>";
        }

        // 2. Notification au joueur pour augmenter son score
        if (player != null)
        {
            player.AddScore();
        }

        // 3. Désactivation de l'indice sur la scène
        gameObject.SetActive(false);
    }
}