using UnityEngine;
using TMPro;

public class HintObject : MonoBehaviour
{
    [Header("Configuration")]
    public string hintName; // Le nom de l'indice (ex: "Tache de sang")
    public TextMeshProUGUI linkedText; // Glisse ici le texte UI correspondant
    
    public void OnFound(Color successColor)
    {
        if (linkedText != null)
        {
            linkedText.color = successColor;
            // Optionnel : barrer le texte ou ajouter un checkmark
            linkedText.text = "<s>" + linkedText.text + "</s>"; 
        }
        
        // On désactive l'indice pour qu'on ne puisse plus le photographier
        gameObject.SetActive(false);
    }
}