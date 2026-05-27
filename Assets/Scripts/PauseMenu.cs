using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu pause singleton (DontDestroyOnLoad).
/// Start / Échap    → ouvre ou ferme le menu
/// B                → ferme le menu  (ou revient en arrière depuis l'overlay Score)
/// ↑↓ stick/dpad    → navigue entre les éléments
/// ←→ stick/dpad    → ajuste le volume (slider sélectionné)
/// A / Entrée       → valide l'élément sélectionné
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; set; }

    [Header("Navigation")]
    public string startMenuSceneName = "StartMenu";

    // ── Index des éléments navigables ────────────────────────────────
    const int ITEM_SLIDER = 0;
    const int ITEM_MENU   = 1;
    const int ITEM_SCORE  = 2;
    const int ITEM_RETOUR = 3;
    const int ITEM_COUNT  = 4;

    // ── State ─────────────────────────────────────────────────────────
    bool isPaused       = false;
    bool isShowingScore = false;
    bool syncingSliders = false;
    int  selectedIndex  = ITEM_SLIDER;
    bool navStickUsed   = false;

    // ── Couleurs ──────────────────────────────────────────────────────
    static readonly Color colorBtn      = new Color(0.3f,  0.3f,  0.3f);
    static readonly Color colorSliderBg = new Color(0.25f, 0.25f, 0.3f);
    static readonly Color colorSelected = new Color(1f,    0.82f, 0f);

    // ── Refs UI ───────────────────────────────────────────────────────
    Image[][] itemBg  = new Image[2][];
    Slider[]  sliders = new Slider[2];

    // Overlays Score (un par écran)
    GameObject[] scoreOverlays = new GameObject[2];
    // Contenu principal du menu pause (fond + panel), caché quand le score est affiché
    GameObject[] pauseContents = new GameObject[2];

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPaused) { isPaused = false; Time.timeScale = 1f; }
        isShowingScore = false;
        DestroyMenuUI();
    }

    void Update()
    {
        InputDevice dev1 = GlobalPlayerManager.Instance?.Player1Device;
        InputDevice dev2 = GlobalPlayerManager.Instance?.Player2Device;

        bool startPressed = IsStart(dev1) || IsStart(dev2)
            || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        bool bPressed = IsB(dev1) || IsB(dev2);

        // ── Jeu normal ────────────────────────────────────────────────
        if (!isPaused)
        {
            if (startPressed) Pause();
            return;
        }

        // ── Overlay Score ouvert ──────────────────────────────────────
        if (isShowingScore)
        {
            if (bPressed)     HideScore();          // retour au menu pause
            if (startPressed) Resume();             // ferme tout
            return;
        }

        // ── Menu pause ouvert ─────────────────────────────────────────
        if (bPressed || startPressed) { Resume(); return; }

        // Navigation ↑↓
        int navV = GetVerticalNav(dev1, dev2);
        if (navV != 0)
        {
            selectedIndex = (selectedIndex - navV + ITEM_COUNT) % ITEM_COUNT;
            RefreshHighlight();
        }

        // Slider ←→
        if (selectedIndex == ITEM_SLIDER)
            HandleSliderInput(dev1, dev2);

        // Confirmation A
        bool confirm = IsA(dev1) || IsA(dev2)
            || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame);
        if (confirm) ConfirmSelected();
    }

    // ── Boutons physiques ─────────────────────────────────────────────

    bool IsStart(InputDevice d) => d is Gamepad gp && gp.startButton.wasPressedThisFrame;
    bool IsB(InputDevice d)     => d is Gamepad gp && gp.buttonEast.wasPressedThisFrame;
    bool IsA(InputDevice d)     => d is Gamepad gp && gp.buttonSouth.wasPressedThisFrame;

    // ── Navigation ────────────────────────────────────────────────────

    int GetVerticalNav(InputDevice dev1, InputDevice dev2)
    {
        if (dev1 is Gamepad g1)
        {
            if (g1.dpad.up.wasPressedThisFrame)   return  1;
            if (g1.dpad.down.wasPressedThisFrame)  return -1;
        }
        if (dev2 is Gamepad g2)
        {
            if (g2.dpad.up.wasPressedThisFrame)   return  1;
            if (g2.dpad.down.wasPressedThisFrame)  return -1;
        }

        float stickY = 0f;
        if (dev1 is Gamepad gs1) stickY += gs1.leftStick.y.ReadValue();
        if (dev2 is Gamepad gs2) stickY += gs2.leftStick.y.ReadValue();

        if (Mathf.Abs(stickY) > 0.5f && !navStickUsed)
        { navStickUsed = true; return stickY > 0 ? 1 : -1; }
        if (Mathf.Abs(stickY) <= 0.3f) navStickUsed = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)   return  1;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)  return -1;
        }
        return 0;
    }

    void HandleSliderInput(InputDevice dev1, InputDevice dev2)
    {
        float axis = 0f;
        if (dev1 is Gamepad g1) { axis += g1.leftStick.x.ReadValue(); if (g1.dpad.right.isPressed) axis += 1f; if (g1.dpad.left.isPressed) axis -= 1f; }
        if (dev2 is Gamepad g2) { axis += g2.leftStick.x.ReadValue(); if (g2.dpad.right.isPressed) axis += 1f; if (g2.dpad.left.isPressed) axis -= 1f; }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed)  axis -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) axis += 1f;
        }
        if (Mathf.Abs(axis) > 0.1f)
            OnVolumeChanged(Mathf.Clamp01(AudioListener.volume + axis * Time.unscaledDeltaTime * 0.8f));
    }

    void ConfirmSelected()
    {
        switch (selectedIndex)
        {
            case ITEM_RETOUR: Resume();      break;
            case ITEM_MENU:   GoToStartMenu();   break;
            case ITEM_SCORE:  ShowScore();   break;
        }
    }

    // ── API publique ──────────────────────────────────────────────────

    public void Pause()
    {
        if (isPaused) return;
        isPaused      = true;
        selectedIndex = ITEM_SLIDER;
        navStickUsed  = false;
        Time.timeScale = 0f;
        CreateMenuUI();
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused       = false;
        isShowingScore = false;
        Time.timeScale = 1f;
        DestroyMenuUI();
    }

    // ── Actions ───────────────────────────────────────────────────────

    void GoToStartMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;

        WinManager.AddLeaderboardEntry(StartMenuController.PseudoJ1, WinManager.scoreJ1);
        WinManager.AddLeaderboardEntry(StartMenuController.PseudoJ2, WinManager.scoreJ2);

        WinManager.scoreJ1 = 0;
        WinManager.scoreJ2 = 0;
        StartMenuController.PseudoJ1 = "Joueur 1";
        StartMenuController.PseudoJ2 = "Joueur 2";

        DestroyMenuUI();
        InteractionPointRandomizer.ResetSavedPositions();

        if (WinManager.Instance != null)
        {
            WinManager.Instance.StopAllCoroutines();
            Destroy(WinManager.Instance.gameObject);
            WinManager.Instance = null;
        }
        if (GameTimer.Instance != null)
        {
            Destroy(GameTimer.Instance.gameObject);
            GameTimer.Instance = null;
        }
        if (MinimapController.Instance != null)
        {
            Destroy(MinimapController.Instance.gameObject);
            MinimapController.Instance = null;
        }
        if (GlobalPlayerManager.Instance != null)
        {
            Destroy(GlobalPlayerManager.Instance.gameObject);
            GlobalPlayerManager.Instance = null;
        }

        SceneTransitionManager.LoadScene(startMenuSceneName);

        Instance = null;
        Destroy(gameObject);
    }

    void ShowScore()
    {
        isShowingScore = true;
        for (int d = 0; d < 2; d++)
        {
            if (pauseContents[d] != null) pauseContents[d].SetActive(false);
            if (scoreOverlays[d]  != null) scoreOverlays[d].SetActive(true);
        }
    }

    void HideScore()
    {
        isShowingScore = false;
        for (int d = 0; d < 2; d++)
        {
            if (scoreOverlays[d]  != null) scoreOverlays[d].SetActive(false);
            if (pauseContents[d] != null) pauseContents[d].SetActive(true);
        }
    }

    // ── Volume ────────────────────────────────────────────────────────

    void OnVolumeChanged(float value)
    {
        if (syncingSliders) return;
        syncingSliders = true;
        AudioListener.volume = value;
        if (sliders[0] != null) sliders[0].value = value;
        if (sliders[1] != null) sliders[1].value = value;
        syncingSliders = false;
    }

    // ── Highlight ─────────────────────────────────────────────────────

    void RefreshHighlight()
    {
        for (int d = 0; d < 2; d++)
        {
            if (itemBg[d] == null) continue;
            for (int i = 0; i < ITEM_COUNT; i++)
            {
                if (itemBg[d][i] == null) continue;
                itemBg[d][i].color = GetItemColor(i, i == selectedIndex);
            }
        }
    }

    Color GetItemColor(int idx, bool selected)
    {
        if (selected) return colorSelected;
        return idx == ITEM_SLIDER ? colorSliderBg : colorBtn;
    }

    // ── Construction UI ───────────────────────────────────────────────

    void CreateMenuUI()
    {
        itemBg[0] = new Image[ITEM_COUNT];
        itemBg[1] = new Image[ITEM_COUNT];

        for (int d = 0; d < 2; d++)
            BuildMenuCanvas($"PauseMenu_J{d + 1}", targetDisplay: d, displayIdx: d);

        RefreshHighlight();
    }

    void DestroyMenuUI()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("PauseMenu")) Destroy(child.gameObject);
        }
        sliders[0]       = sliders[1]       = null;
        itemBg[0]        = itemBg[1]        = null;
        scoreOverlays[0]  = scoreOverlays[1]  = null;
        pauseContents[0] = pauseContents[1] = null;
    }

    void BuildMenuCanvas(string canvasName, int targetDisplay, int displayIdx)
    {
        // ── Canvas ────────────────────────────────────────────────────
        var canvasGO = new GameObject(canvasName);
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder  = 500;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Contenu pause (fond + panel) — caché quand score affiché ──
        var pauseContent = new GameObject("PauseContent");
        pauseContent.transform.SetParent(canvasGO.transform, false);
        pauseContent.AddComponent<RectTransform>(); // nécessaire pour SetParent
        StretchFull(pauseContent.GetComponent<RectTransform>());
        pauseContents[displayIdx] = pauseContent;

        // Fond
        CreateImage(pauseContent.transform, "Background", new Color(0f, 0f, 0f, 0.75f),
            Vector2.zero, Vector2.one);

        // ── Panneau central ───────────────────────────────────────────
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(pauseContent.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
        SetAnchors(panelGO.GetComponent<RectTransform>(),
            new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.83f));

        // Titre
        AddLabel(panelGO.transform, "Title", "PAUSE",
            new Vector2(0f, 0.84f), new Vector2(1f, 1f),
            72f, FontStyles.Bold, Color.white);

        // Label volume
        AddLabel(panelGO.transform, "VolumeLabel", "Volume sonore",
            new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.78f),
            26f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f));

        // Slider (item 0)
        var scGO  = new GameObject("SliderContainer");
        scGO.transform.SetParent(panelGO.transform, false);
        var scImg = scGO.AddComponent<Image>();
        scImg.color = GetItemColor(ITEM_SLIDER, selectedIndex == ITEM_SLIDER);
        SetAnchors(scGO.GetComponent<RectTransform>(),
            new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.67f));
        itemBg[displayIdx][ITEM_SLIDER] = scImg;

        var slider = BuildSlider(scGO.transform);
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(OnVolumeChanged);
        sliders[displayIdx] = slider;

        // Hint
        AddLabel(panelGO.transform, "Hint", "↑↓ naviguer   ←→ volume   A confirmer   B / Start fermer",
            new Vector2(0.02f, 0.44f), new Vector2(0.98f, 0.53f),
            16f, FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));

        // Bouton Menu (item 1) — haut
        itemBg[displayIdx][ITEM_MENU] = AddButton(panelGO.transform, "BtnMenu", "Menu",
            new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.43f),
            GetItemColor(ITEM_MENU, selectedIndex == ITEM_MENU), GoToStartMenu);

        // Bouton Score (item 2) — milieu
        itemBg[displayIdx][ITEM_SCORE] = AddButton(panelGO.transform, "BtnScore", "Score",
            new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.29f),
            GetItemColor(ITEM_SCORE, selectedIndex == ITEM_SCORE), ShowScore);

        // Bouton Retour (item 3) — bas
        itemBg[displayIdx][ITEM_RETOUR] = AddButton(panelGO.transform, "BtnRetour", "Retour",
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.15f),
            GetItemColor(ITEM_RETOUR, selectedIndex == ITEM_RETOUR), Resume);

        // ── Overlay Score (caché par défaut) ──────────────────────────
        scoreOverlays[displayIdx] = BuildScoreOverlay(canvasGO.transform);
        scoreOverlays[displayIdx].SetActive(false);
    }

    GameObject BuildScoreOverlay(Transform canvasTransform)
    {
        var root = new GameObject("ScoreOverlay");
        root.transform.SetParent(canvasTransform, false);
        root.AddComponent<RectTransform>();
        StretchFull(root.GetComponent<RectTransform>());

        // Fond noir
        CreateImage(root.transform, "Background", new Color(0f, 0f, 0f, 0.88f),
            Vector2.zero, Vector2.one);

        // Panneau central — plus grand pour respirer
        var panelGO = new GameObject("ScorePanel");
        panelGO.transform.SetParent(root.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
        SetAnchors(panelGO.GetComponent<RectTransform>(),
            new Vector2(0.20f, 0.22f), new Vector2(0.80f, 0.80f));

        // Titre
        AddLabel(panelGO.transform, "Title", "SCORES",
            new Vector2(0f, 0.78f), new Vector2(1f, 1f),
            64f, FontStyles.Bold, Color.white);

        // Score J1
        AddLabel(panelGO.transform, "ScoreJ1",
            $"Joueur 1  :  {WinManager.scoreJ1} pts",
            new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.76f),
            42f, FontStyles.Normal, new Color(0.4f, 0.8f, 1f));

        // Score J2
        AddLabel(panelGO.transform, "ScoreJ2",
            $"Joueur 2  :  {WinManager.scoreJ2} pts",
            new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.50f),
            42f, FontStyles.Normal, new Color(1f, 0.6f, 0.3f));

        // Hint bas
        AddLabel(panelGO.transform, "Hint",
            "B → Retour au menu     Start → Fermer le menu",
            new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.20f),
            20f, FontStyles.Italic, new Color(0.55f, 0.55f, 0.55f));

        return root;
    }

    // ── Helpers UI ────────────────────────────────────────────────────

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return img;
    }

    void AddLabel(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, FontStyles style, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.fontStyle = style; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
    }

    Image AddButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;
        SetAnchors(btnGO.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        AddLabel(btnGO.transform, "Text", label,
            Vector2.zero, Vector2.one, 28f, FontStyles.Bold, Color.white);
        return img;
    }

    Slider BuildSlider(Transform parent)
    {
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(parent, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f;

        var rt = sliderGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.03f, 0.1f);
        rt.anchorMax = new Vector2(0.97f, 0.9f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Background
        var bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.22f);
        var bgRect  = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.2f); bgRect.anchorMax = new Vector2(1f, 0.8f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        slider.targetGraphic = bgImg;

        // Fill area
        var fillAreaGO   = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.2f); fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);   fillAreaRect.offsetMax = new Vector2(-15f, 0f);

        var fillGO   = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillImg  = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.55f, 1f);
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        // Handle
        var handleAreaGO   = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero; handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f); handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handleGO   = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleImg  = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f); handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(20f, 0f);
        slider.handleRect = handleRect;

        return slider;
    }
}
