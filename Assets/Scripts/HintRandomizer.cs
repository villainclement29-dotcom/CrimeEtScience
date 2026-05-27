using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class HintRandomizer : MonoBehaviour
{
    public int maxHints = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Contains("MiniGame2")) return;
        var go = new GameObject("HintRandomizer");
        go.AddComponent<HintRandomizer>();
    }

    void Awake()
    {
        var hints = Object.FindObjectsByType<HintObject>(FindObjectsSortMode.None);

        var groups = new Dictionary<string, List<HintObject>>();
        foreach (var h in hints)
        {
            if (!groups.ContainsKey(h.hintName))
                groups[h.hintName] = new List<HintObject>();
            groups[h.hintName].Add(h);
        }

        var types = new List<string>(groups.Keys);
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }

        var selected = new HashSet<string>();
        for (int i = 0; i < Mathf.Min(maxHints, types.Count); i++)
            selected.Add(types[i]);

        foreach (var kv in groups)
            if (!selected.Contains(kv.Key))
                foreach (var h in kv.Value)
                    h.gameObject.SetActive(false);

        var fallback = new Dictionary<string, string>
        {
            { "empreinte", "Empreinte" },
            { "verre", "Eclat de verre" },
            { "sang", "Tache de sang" },
            { "carnet", "Carnet" },
            { "cheveu", "Cheveu" },
            { "megot", "Megot" },
            { "fibre", "Fibre textile" },
            { "trace", "Trace de pas" },
        };

        var labelMap = new Dictionary<string, string>();
        foreach (var kv in groups)
        {
            HintObject first = kv.Value[0];
            if (!string.IsNullOrEmpty(first.displayName))
                labelMap[kv.Key] = first.displayName;
            else if (fallback.ContainsKey(kv.Key))
                labelMap[kv.Key] = fallback[kv.Key];
            else
                labelMap[kv.Key] = char.ToUpper(kv.Key[0]) + kv.Key.Substring(1);
        }

        RebuildUI(selected, labelMap);
    }

    void RebuildUI(HashSet<string> selected, Dictionary<string, string> labelMap)
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.name.Contains("appareilPhoto")) continue;

            var children = new List<Transform>();
            foreach (Transform c in canvas.transform)
                children.Add(c);
            foreach (var c in children)
                if (c.GetComponent<TextMeshProUGUI>() != null)
                    DestroyImmediate(c.gameObject);

            float yStart = 170f;
            int idx = 0;
            foreach (var type in selected)
            {
                string label = labelMap.ContainsKey(type) ? labelMap[type] : type;

                var go = new GameObject(type);
                go.transform.SetParent(canvas.transform, false);
                go.layer = 5;

                var rt = go.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(186f, yStart - idx * 32f);
                rt.sizeDelta = new Vector2(303f, 32f);

                go.AddComponent<CanvasRenderer>();

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = $" - {label}";
                tmp.fontSize = 36;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

                idx++;
            }
        }
    }
}
