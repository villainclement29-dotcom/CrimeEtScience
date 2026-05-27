using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScoreBoardDisplay : MonoBehaviour
{
    [HideInInspector] public TextMeshProUGUI textJ1;
    [HideInInspector] public TextMeshProUGUI textJ2;

    TextMeshProUGUI listTmp;
    TextMeshProUGUI currentGameTmp;

    void Start()
    {
        CleanOldUI();
        BuildLeaderboardUI();
    }

    void Update()
    {
        RefreshLeaderboard();
    }

    void CleanOldUI()
    {
        var children = new List<Transform>();
        foreach (Transform c in transform)
            children.Add(c);
        foreach (var c in children)
            Destroy(c.gameObject);

        // Garde l'image de fond du Panel

        textJ1 = null;
        textJ2 = null;
    }

    void BuildLeaderboardUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform root = canvas.transform;

        // Titre
        var titleGO = new GameObject("LB_Title");
        titleGO.transform.SetParent(root, false);
        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "LEADERBOARD";
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.82f, 0f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.outlineWidth = 0.2f;
        titleTmp.outlineColor = new Color32(0, 0, 0, 255);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.87f);
        titleRect.anchorMax = new Vector2(0.95f, 0.97f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        // Liste (ancree en haut, le texte descend naturellement)
        var listGO = new GameObject("LB_List");
        listGO.transform.SetParent(root, false);
        listTmp = listGO.AddComponent<TextMeshProUGUI>();
        listTmp.fontSize = 40f;
        listTmp.color = Color.white;
        listTmp.alignment = TextAlignmentOptions.Top;
        listTmp.richText = true;
        listTmp.enableWordWrapping = false;
        listTmp.overflowMode = TextOverflowModes.Truncate;
        var listRect = listGO.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.05f, 0.12f);
        listRect.anchorMax = new Vector2(0.95f, 0.85f);
        listRect.offsetMin = listRect.offsetMax = Vector2.zero;

        // Scores partie en cours (bas d'ecran)
        var curGO = new GameObject("LB_CurrentGame");
        curGO.transform.SetParent(root, false);
        currentGameTmp = curGO.AddComponent<TextMeshProUGUI>();
        currentGameTmp.fontSize = 32f;
        currentGameTmp.color = new Color(0.8f, 0.8f, 0.85f);
        currentGameTmp.alignment = TextAlignmentOptions.Center;
        currentGameTmp.richText = true;
        var curRect = curGO.GetComponent<RectTransform>();
        curRect.anchorMin = new Vector2(0.05f, 0.02f);
        curRect.anchorMax = new Vector2(0.95f, 0.10f);
        curRect.offsetMin = curRect.offsetMax = Vector2.zero;
    }

    void RefreshLeaderboard()
    {
        if (listTmp == null) return;

        List<WinManager.LeaderboardEntry> entries = WinManager.GetLeaderboard();

        string txt = "";
        if (entries.Count == 0)
        {
            txt = "\n\n<color=#888><i>Aucune partie enregistree</i></color>";
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                string rankLabel;
                string color;
                switch (i)
                {
                    case 0: rankLabel = "1er"; color = "#FFD700"; break;
                    case 1: rankLabel = "2e";  color = "#C0C0C0"; break;
                    case 2: rankLabel = "3e";  color = "#CD7F32"; break;
                    default: rankLabel = (i + 1) + "e"; color = "#FFFFFF"; break;
                }

                string pseudo = entries[i].pseudo;
                if (pseudo.Length > 14) pseudo = pseudo.Substring(0, 14) + "..";

                txt += $"<color={color}>{rankLabel}   {pseudo}  —  {entries[i].score} pts</color>\n";
            }
        }
        listTmp.text = txt;

        if (currentGameTmp != null)
        {
            currentGameTmp.text =
                $"<color=#66CCFF>{StartMenuController.PseudoJ1} : {WinManager.scoreJ1} pts</color>      " +
                $"<color=#FF9933>{StartMenuController.PseudoJ2} : {WinManager.scoreJ2} pts</color>";
        }
    }
}
