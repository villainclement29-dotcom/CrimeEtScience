using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TutorialReadySystem : MonoBehaviour
{
    [Header("UI Status")]
    public TextMeshProUGUI statusJ1;
    public TextMeshProUGUI statusJ2;

    [Header("Navigation")]
    public string nextSceneName;
    public float delayAfterReady = 1.5f;

    static bool dialogueCreated;
    static TutorialReadySystem primary;

    enum Phase { Dialogue, Ready }
    Phase phase = Phase.Dialogue;

    string[] dialogueLines;
    int currentLine;
    bool isJ1Ready, isJ2Ready, isStarting;
    bool isTyping;
    string fullLineText;
    int typedChars;
    float typeTimer;
    float typeSpeed = 0.03f;

    Sprite chiefSprite;
    GameObject[] canvases = new GameObject[2];
    CanvasGroup[] overlayGroups = new CanvasGroup[2];
    Image[] chiefImages = new Image[2];
    GameObject[] bubbleRoots = new GameObject[2];
    TextMeshProUGUI[] dialogueTexts = new TextMeshProUGUI[2];
    TextMeshProUGUI[] hintTexts = new TextMeshProUGUI[2];
    GameObject[] readyPanels = new GameObject[2];
    TextMeshProUGUI[] readyTexts = new TextMeshProUGUI[2];
    Image[] recDots = new Image[2];

    void Start()
    {
        if (dialogueCreated && primary != this)
        {
            enabled = false;
            return;
        }
        dialogueCreated = true;
        primary = this;

        SetDialogueForScene(SceneManager.GetActiveScene().name);
        LoadChiefSprite();
        HideExistingTutoUI();

        for (int d = 0; d < 2; d++)
            BuildCanvas(d);

        ShowLine();
    }

    void OnDestroy()
    {
        if (primary == this)
        {
            dialogueCreated = false;
            primary = null;
            for (int d = 0; d < 2; d++)
                if (canvases[d] != null) Destroy(canvases[d]);
        }
    }

    void SetDialogueForScene(string sceneName)
    {
        if (sceneName.Contains("TutoMiniGame1"))
        {
            dialogueLines = new[]
            {
                "Ecoutez-moi bien, agents. La police scientifique est souvent amenee a faire de la dactyloscopie, c'est-a-dire l'etude des empreintes digitales.",
                "Votre but est d'identifier des paternes communs aux empreintes, appeles minuties, sur l'empreinte retrouvee dans l'appartement de Clara.",
                "Appuyez sur A pour ouvrir la liste des minuties a selectionner.",
                "Utilisez le joystick gauche pour vous deplacer de minutie en minutie.",
                "Une fois vos choix faits, scannez l'empreinte pour les verifier. Bonne chance !"
            };
        }
        else
        {
            dialogueLines = new[]
            {
                "Agents, en arrivant dans l'appartement de Clara, vous avez remarque plusieurs elements suspects...",
                "Votre mission : etre le premier a photographier tous ces elements. Soyez rapide et precis !",
                "Utilisez le joystick gauche pour deplacer votre appareil photo dans la scene.",
                "Appuyez sur A pour prendre une photo. Attention a ce que l'element soit bien au centre du viseur !",
                "Vous pouvez zoomer et dezoomer avec le joystick droit. Allez, au travail !"
            };
        }
    }

    void LoadChiefSprite()
    {
        Sprite[] all = Resources.LoadAll<Sprite>("police-chief");
        if (all != null && all.Length > 0)
            chiefSprite = all[0];
    }

    void HideExistingTutoUI()
    {
        string[] namesToHide = { "tutoContainer", "content-1", "key-tuto-container", "Title" };
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            foreach (string n in namesToHide)
            {
                if (t.name == n || t.name.StartsWith(n + " ("))
                {
                    if (t.GetComponentInParent<Canvas>() != null)
                        t.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    // ── UI Construction ──────────────────────────────────────────────

    void BuildCanvas(int d)
    {
        var go = new GameObject($"DialogueCanvas_D{d}");
        canvases[d] = go;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = d;
        canvas.sortingOrder = 800;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(go.transform, false);
        var overlayRT = overlay.AddComponent<RectTransform>();
        Stretch(overlayRT);
        overlayGroups[d] = overlay.AddComponent<CanvasGroup>();
        overlayGroups[d].blocksRaycasts = false;

        MakeImage(overlay.transform, "Dim", new Color(0.15f, 0.15f, 0.15f, 0.45f),
            V2(0, 0), V2(1, 1));

        BuildRecBadge(overlay.transform, d);
        BuildChief(overlay.transform, d);
        BuildBubble(overlay.transform, d);
        BuildReadyPanel(overlay.transform, d);

        readyPanels[d].SetActive(false);
    }

    void BuildRecBadge(Transform parent, int d)
    {
        var badge = new GameObject("RecBadge");
        badge.transform.SetParent(parent, false);
        var badgeBg = badge.AddComponent<Image>();
        badgeBg.color = new Color(0f, 0f, 0f, 0.45f);
        badgeBg.raycastTarget = false;
        var badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = V2(0.02f, 0.90f);
        badgeRT.anchorMax = V2(0.14f, 0.96f);
        badgeRT.offsetMin = badgeRT.offsetMax = Vector2.zero;

        // Red dot
        var dotGo = new GameObject("Dot");
        dotGo.transform.SetParent(badge.transform, false);
        var dotImg = dotGo.AddComponent<Image>();
        dotImg.color = new Color(1f, 0.15f, 0.15f);
        dotImg.raycastTarget = false;
        var dotRT = dotGo.GetComponent<RectTransform>();
        dotRT.anchorMin = V2(0.06f, 0.2f);
        dotRT.anchorMax = V2(0.06f, 0.8f);
        dotRT.pivot = V2(0.5f, 0.5f);
        dotRT.sizeDelta = new Vector2(18f, 18f);
        dotRT.offsetMin = Vector2.zero;
        dotRT.offsetMax = Vector2.zero;
        dotRT.anchoredPosition = new Vector2(16f, 0f);
        dotRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 18f);
        dotRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 18f);
        recDots[d] = dotImg;

        // "REC" label
        var recGo = new GameObject("RecLabel");
        recGo.transform.SetParent(badge.transform, false);
        var recTmp = recGo.AddComponent<TextMeshProUGUI>();
        recTmp.text = "REC";
        recTmp.fontSize = 24f;
        recTmp.fontStyle = FontStyles.Bold;
        recTmp.color = new Color(1f, 1f, 1f, 0.9f);
        recTmp.alignment = TextAlignmentOptions.MidlineLeft;
        recTmp.raycastTarget = false;
        var recRT = recGo.GetComponent<RectTransform>();
        recRT.anchorMin = V2(0.28f, 0f);
        recRT.anchorMax = V2(1f, 1f);
        recRT.offsetMin = recRT.offsetMax = Vector2.zero;
    }

    void BuildChief(Transform parent, int d)
    {
        var go = new GameObject("Chief");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (chiefSprite != null)
            img.sprite = chiefSprite;
        else
            img.color = Color.clear;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = V2(0.68f, -0.25f);
        rt.anchorMax = V2(1.0f, 0.6f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        chiefImages[d] = img;
    }

    void BuildBubble(Transform parent, int d)
    {
        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(parent, false);
        var bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        var bubbleRT = bubble.GetComponent<RectTransform>();
        bubbleRT.anchorMin = V2(0.03f, 0.10f);
        bubbleRT.anchorMax = V2(0.66f, 0.37f);
        bubbleRT.offsetMin = bubbleRT.offsetMax = Vector2.zero;
        bubbleRoots[d] = bubble;

        // Name label
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(bubble.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Chef de la Police";
        nameTmp.fontSize = 44f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color(1f, 0.82f, 0f);
        nameTmp.alignment = TextAlignmentOptions.TopLeft;
        nameTmp.raycastTarget = false;
        var nameRT = nameGo.GetComponent<RectTransform>();
        nameRT.anchorMin = V2(0.04f, 0.78f);
        nameRT.anchorMax = V2(0.96f, 0.98f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        // Separator line
        MakeImage(bubble.transform, "Sep", new Color(1f, 0.82f, 0f, 0.4f),
            V2(0.04f, 0.74f), V2(0.96f, 0.76f));

        // Dialogue text
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(bubble.transform, false);
        var txtTmp = txtGo.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 32f;
        txtTmp.fontStyle = FontStyles.Normal;
        txtTmp.color = Color.white;
        txtTmp.alignment = TextAlignmentOptions.TopLeft;
        txtTmp.raycastTarget = false;
        txtTmp.enableWordWrapping = true;
        var txtRT = txtGo.GetComponent<RectTransform>();
        txtTmp.margin = new Vector4(12f, 8f, 12f, 8f);
        txtRT.anchorMin = V2(0.05f, 0.12f);
        txtRT.anchorMax = V2(0.95f, 0.70f);
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        dialogueTexts[d] = txtTmp;

        // Hint
        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(bubble.transform, false);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        hintTmp.fontSize = 18f;
        hintTmp.fontStyle = FontStyles.Italic;
        hintTmp.color = new Color(0.6f, 0.6f, 0.65f);
        hintTmp.alignment = TextAlignmentOptions.BottomRight;
        hintTmp.raycastTarget = false;
        var hintRT = hintGo.GetComponent<RectTransform>();
        hintRT.anchorMin = V2(0.70f, 0.02f);
        hintRT.anchorMax = V2(0.98f, 0.14f);
        hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;
        hintTexts[d] = hintTmp;
    }

    void BuildReadyPanel(Transform parent, int d)
    {
        var panel = new GameObject("ReadyPanel");
        panel.transform.SetParent(parent, false);
        var panelRT = panel.AddComponent<RectTransform>();
        Stretch(panelRT);
        readyPanels[d] = panel;

        MakeImage(panel.transform, "ReadyBg", new Color(0f, 0f, 0f, 0.7f),
            V2(0, 0), V2(1, 1));

        var go = new GameObject("ReadyText");
        go.transform.SetParent(panel.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 48f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = V2(0.1f, 0.35f);
        rt.anchorMax = V2(0.9f, 0.65f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        readyTexts[d] = tmp;
    }

    // ── Dialogue Logic ───────────────────────────────────────────────

    void ShowLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        fullLineText = dialogueLines[currentLine];
        typedChars = 0;
        typeTimer = 0f;
        isTyping = true;

        for (int d = 0; d < 2; d++)
        {
            if (dialogueTexts[d] != null) dialogueTexts[d].text = "";
            if (hintTexts[d] != null) hintTexts[d].text = "";
        }
    }

    void FinishTyping()
    {
        isTyping = false;
        typedChars = fullLineText.Length;
        bool isLast = currentLine >= dialogueLines.Length - 1;
        string hint = isLast ? "A  Terminer" : "A  Suivant  >";

        for (int d = 0; d < 2; d++)
        {
            if (dialogueTexts[d] != null) dialogueTexts[d].text = fullLineText;
            if (hintTexts[d] != null) hintTexts[d].text = hint;
        }
    }

    void AdvanceDialogue()
    {
        currentLine++;
        if (currentLine >= dialogueLines.Length)
        {
            EnterReadyPhase();
            return;
        }
        ShowLine();
    }

    void EnterReadyPhase()
    {
        phase = Phase.Ready;
        isJ1Ready = false;
        isJ2Ready = false;

        for (int d = 0; d < 2; d++)
        {
            if (chiefImages[d] != null) chiefImages[d].gameObject.SetActive(false);
            if (bubbleRoots[d] != null) bubbleRoots[d].SetActive(false);
            readyPanels[d].SetActive(true);
        }
        UpdateReadyUI();
    }

    void UpdateReadyUI()
    {
        string txtJ1 = isJ1Ready
            ? "<color=green>PRET</color>"
            : "<color=red>APPUYEZ SUR A</color>";
        string txtJ2 = isJ2Ready
            ? "<color=green>PRET</color>"
            : "<color=red>APPUYEZ SUR A</color>";

        if (readyTexts[0] != null) readyTexts[0].text = $"J1\n{txtJ1}";
        if (readyTexts[1] != null) readyTexts[1].text = $"J2\n{txtJ2}";

        if (statusJ1 != null)
            statusJ1.text = isJ1Ready ? "<color=green>PRET</color>" : "<color=red>APPUYEZ SUR A</color>";
        if (statusJ2 != null)
            statusJ2.text = isJ2Ready ? "<color=green>PRET</color>" : "<color=red>APPUYEZ SUR A</color>";
    }

    // ── Update ───────────────────────────────────────────────────────

    void Update()
    {
        if (isStarting) return;

        // Blink REC dot
        float blinkAlpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f) > 0.5f ? 1f : 0.2f;
        for (int d = 0; d < 2; d++)
            if (recDots[d] != null)
                recDots[d].color = new Color(1f, 0.15f, 0.15f, blinkAlpha);

        // Typewriter tick
        if (isTyping && fullLineText != null)
        {
            typeTimer += Time.unscaledDeltaTime;
            while (typeTimer >= typeSpeed && typedChars < fullLineText.Length)
            {
                typedChars++;
                typeTimer -= typeSpeed;
            }
            string visible = fullLineText.Substring(0, typedChars);
            for (int d = 0; d < 2; d++)
                if (dialogueTexts[d] != null) dialogueTexts[d].text = visible;

            if (typedChars >= fullLineText.Length)
                FinishTyping();
        }

        InputDevice dev1 = GlobalPlayerManager.Instance != null ? GlobalPlayerManager.Instance.Player1Device : null;
        InputDevice dev2 = GlobalPlayerManager.Instance != null ? GlobalPlayerManager.Instance.Player2Device : null;

        if (phase == Phase.Dialogue)
        {
            bool pressed = WasButtonSouthPressed(dev1) || WasButtonSouthPressed(dev2);
            if (pressed && isTyping)
                FinishTyping();
            else if (pressed && !isTyping)
                AdvanceDialogue();
        }
        else
        {
            if (!isJ1Ready && WasButtonSouthPressed(dev1)) isJ1Ready = true;
            if (!isJ2Ready && WasButtonSouthPressed(dev2)) isJ2Ready = true;
            UpdateReadyUI();

            if (isJ1Ready && isJ2Ready)
                StartCoroutine(StartGameRoutine());
        }
    }

    bool WasButtonSouthPressed(InputDevice device)
    {
        if (device is Gamepad pad) return pad.buttonSouth.wasPressedThisFrame;
        if (device is Keyboard kb) return kb.spaceKey.wasPressedThisFrame;
        return false;
    }

    IEnumerator StartGameRoutine()
    {
        isStarting = true;
        yield return new WaitForSeconds(delayAfterReady);
        SceneTransitionManager.LoadScene(nextSceneName);
    }

    // ── UI Helpers ───────────────────────────────────────────────────

    Vector2 V2(float x, float y) => new(x, y);

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Image MakeImage(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }
}
