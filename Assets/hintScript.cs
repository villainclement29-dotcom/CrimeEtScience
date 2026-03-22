using UnityEngine;
using TMPro;

public class HintObject : MonoBehaviour
{
    public string hintName; // Doit être EXACTEMENT le même que le texte UI (ex: "Tache de sang")
    [HideInInspector] public TextMeshProUGUI linkedText; 

    public void OnFound(Color colorToApply)
    {
        if (linkedText != null)
        {
            linkedText.color = colorToApply;
            linkedText.text = "<s>" + linkedText.text + "</s>";
        }
        gameObject.SetActive(false);
    }
}