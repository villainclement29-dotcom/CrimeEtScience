using UnityEngine;
using TMPro;

public class ScoreBoardDisplay : MonoBehaviour
{
    public TextMeshProUGUI textJ1;
    public TextMeshProUGUI textJ2;

    [Tooltip("Laissez vide pour auto-création au-dessus des scores.")]
    public TextMeshProUGUI textBestScore;

    void Start()
    {
        if (textBestScore == null)
            textBestScore = CreateBestScoreLabel();
    }

    void Update()
    {
        textJ1.text = "Score J1: " + WinManager.scoreJ1 + " pts";
        textJ2.text = "Score J2: " + WinManager.scoreJ2 + " pts";

        if (textBestScore != null)
            textBestScore.text = "TIC du mois : " + WinManager.GetBestScore() + " pts";
    }

    TextMeshProUGUI CreateBestScoreLabel()
    {
        // Cherche le canvas parent pour s'y accrocher
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        var go  = new GameObject("BestScoreLabel");
        go.transform.SetParent(canvas.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "TIC du mois : 0 pts";
        tmp.fontSize  = 40f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1f, 0.82f, 0f); // doré
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.outlineWidth    = 0.2f;
        tmp.outlineColor    = new Color32(0, 0, 0, 255);

        // Positionné en haut du canvas, au-dessus des scores joueurs
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.72f);
        rect.anchorMax = new Vector2(0.9f, 0.88f);
        rect.offsetMin = new Vector2(0f, -61f);
        rect.offsetMax = new Vector2(0f, -61f);

        return tmp;
    }
}
