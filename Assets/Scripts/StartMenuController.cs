using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    [Header("Vidéo de fond (optionnelle)")]
    [Tooltip("Glissez un VideoClip ici pour un fond vidéo en boucle sur les deux écrans")]
    public VideoClip backgroundVideoClip;

    [Header("Navigation")]
    public string lobbySceneName = "Lobby";
    public int maxPseudoLength = 12;

    // ── État par joueur ───────────────────────────────────────────────
    enum MenuState { Main, PseudoInput, Settings, Leaderboard }
    MenuState[] pState = { MenuState.Main, MenuState.Main };
    Gamepad[]   pPad   = new Gamepad[2];

    // ── Main menu (par joueur) ────────────────────────────────────────
    int[]     pMainIdx   = new int[2];
    bool[]    pMainStick = new bool[2];
    Image[][] pMainBtns  = new Image[2][];
    const int MAIN_COUNT = 3;

    // ── Settings (par joueur) ─────────────────────────────────────────
    int[]     pSetIdx   = new int[2];
    bool[]    pSetStick = new bool[2];
    Image[][] pSetBtns  = new Image[2][];
    Slider[]  pSlider   = new Slider[2];
    const int SET_COUNT = 2;

    // ── Clavier virtuel (par joueur) ──────────────────────────────────
    static readonly string[][] KB =
    {
        new[] { "A", "Z", "E", "R", "T", "Y", "U", "I", "O", "P" },
        new[] { "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M" },
        new[] { "W", "X", "C", "V", "B", "N", "0", "1", "2", "3" },
        new[] { "ESPACE", "EFFACER", "VALIDER" }
    };

    int[]             kbRow   = new int[2];
    int[]             kbCol   = new int[2];
    string[]          kbTexts = { "", "" };
    bool[]            kbDone  = new bool[2];
    bool[]            kbStick = new bool[2];
    Image[][][]       kbKeys  = new Image[2][][];
    TextMeshProUGUI[] kbDisp  = new TextMeshProUGUI[2];
    GameObject[]      kbConfirm = new GameObject[2];

    // ── Panels par joueur ─────────────────────────────────────────────
    Canvas[]          pCanvas   = new Canvas[2];
    GameObject[]      pMain     = new GameObject[2];
    GameObject[]      pSettings = new GameObject[2];
    GameObject[]      pLeader   = new GameObject[2];
    GameObject[]      pPseudo   = new GameObject[2];
    GameObject[]      pWait     = new GameObject[2];
    TextMeshProUGUI[] pLeaderTxt = new TextMeshProUGUI[2];

    // ── Vidéo partagée ────────────────────────────────────────────────
    VideoPlayer  videoPlayer;
    RenderTexture videoRT;

    // ── Pseudos (accessibles partout) ─────────────────────────────────
    public static string PseudoJ1 = "Joueur 1";
    public static string PseudoJ2 = "Joueur 2";

    // ── Couleurs ──────────────────────────────────────────────────────
    static readonly Color colBtn    = new Color(0.18f, 0.18f, 0.25f, 0.92f);
    static readonly Color colSel    = new Color(1f, 0.82f, 0f);
    static readonly Color colPanel  = new Color(0.06f, 0.06f, 0.1f, 0.96f);
    static readonly Color colKeyBg  = new Color(0.14f, 0.14f, 0.22f);
    static readonly Color colKeySel = new Color(1f, 0.82f, 0f);

    // ═══════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════

    void Start()
    {
        if (Display.displays.Length > 1) Display.displays[1].Activate();

        SetupVideo();

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        for (int p = 0; p < 2; p++)
        {
            BuildPlayerCanvas(p);
            ShowPlayerMain(p);
        }
    }

    void OnDestroy()
    {
        if (videoRT != null) { videoRT.Release(); Destroy(videoRT); }
    }

    void Update()
    {
        for (int p = 0; p < 2; p++)
        {
            if (pPad[p] == null && Gamepad.all.Count > p)
            {
                pPad[p] = Gamepad.all[p];
                GO(pWait[p], false);
            }
            if (pPad[p] == null) continue;

            switch (pState[p])
            {
                case MenuState.Main:        HandleMain(p);    break;
                case MenuState.PseudoInput:  HandlePseudo(p);  break;
                case MenuState.Settings:     HandleSet(p);     break;
                case MenuState.Leaderboard:  HandleLead(p);    break;
            }
        }

        if (pState[0] == MenuState.PseudoInput && pState[1] == MenuState.PseudoInput
            && kbDone[0] && kbDone[1])
        {
            PseudoJ1 = kbTexts[0].Length > 0 ? kbTexts[0] : "Joueur 1";
            PseudoJ2 = kbTexts[1].Length > 0 ? kbTexts[1] : "Joueur 2";
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // MAIN MENU
    // ═══════════════════════════════════════════════════════════════════

    void HandleMain(int p)
    {
        Gamepad gp = pPad[p];
        int nav = NavV(gp, ref pMainStick[p]);
        if (nav != 0)
        {
            pMainIdx[p] = (pMainIdx[p] - nav + MAIN_COUNT) % MAIN_COUNT;
            HL(pMainBtns[p], pMainIdx[p]);
        }
        if (gp.buttonSouth.wasPressedThisFrame)
        {
            switch (pMainIdx[p])
            {
                case 0: ShowPlayerPseudo(p); break;
                case 1: ShowPlayerSet(p);    break;
                case 2: ShowPlayerLead(p);   break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLAVIER VIRTUEL
    // ═══════════════════════════════════════════════════════════════════

    void HandlePseudo(int p)
    {
        Gamepad gp = pPad[p];

        if (gp.buttonEast.wasPressedThisFrame)
        {
            if (kbDone[p]) { kbDone[p] = false; GO(kbConfirm[p], false); }
            else             ShowPlayerMain(p);
            return;
        }
        if (kbDone[p]) return;

        int navH = 0, navV = 0;
        if (gp.dpad.left.wasPressedThisFrame)  navH = -1;
        if (gp.dpad.right.wasPressedThisFrame) navH =  1;
        if (gp.dpad.up.wasPressedThisFrame)    navV = -1;
        if (gp.dpad.down.wasPressedThisFrame)  navV =  1;

        if (navH == 0 && navV == 0)
        {
            Vector2 s = gp.leftStick.ReadValue();
            if (s.magnitude > 0.5f && !kbStick[p])
            {
                kbStick[p] = true;
                if (Mathf.Abs(s.x) > Mathf.Abs(s.y)) navH = s.x > 0 ? 1 : -1;
                else                                   navV = s.y > 0 ? -1 : 1;
            }
            if (s.magnitude <= 0.3f) kbStick[p] = false;
        }

        if (navH != 0)
        {
            int cols = KB[kbRow[p]].Length;
            kbCol[p] = (kbCol[p] + navH + cols) % cols;
            RefreshHL(p);
        }
        if (navV != 0)
        {
            int oldCols = KB[kbRow[p]].Length;
            kbRow[p] = (kbRow[p] + navV + KB.Length) % KB.Length;
            int newCols = KB[kbRow[p]].Length;
            if (oldCols != newCols)
            {
                float r = oldCols > 1 ? (float)kbCol[p] / (oldCols - 1) : 0f;
                kbCol[p] = Mathf.RoundToInt(r * (newCols - 1));
            }
            RefreshHL(p);
        }

        if (gp.buttonSouth.wasPressedThisFrame)
            PressKey(p, KB[kbRow[p]][kbCol[p]]);
    }

    void PressKey(int p, string key)
    {
        switch (key)
        {
            case "ESPACE":
                if (kbTexts[p].Length < maxPseudoLength) kbTexts[p] += " ";
                break;
            case "EFFACER":
                if (kbTexts[p].Length > 0) kbTexts[p] = kbTexts[p][..^1];
                break;
            case "VALIDER":
                kbDone[p] = true;
                GO(kbConfirm[p], true);
                break;
            default:
                if (kbTexts[p].Length < maxPseudoLength) kbTexts[p] += key;
                break;
        }
        RefreshDisp(p);
    }

    void RefreshHL(int p)
    {
        if (kbKeys[p] == null) return;
        for (int r = 0; r < KB.Length; r++)
            for (int c = 0; c < KB[r].Length; c++)
            {
                if (kbKeys[p][r] == null || kbKeys[p][r][c] == null) continue;
                bool sel = r == kbRow[p] && c == kbCol[p];
                kbKeys[p][r][c].color = sel ? colKeySel : colKeyBg;
                var t = kbKeys[p][r][c].GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.color = sel ? Color.black : Color.white;
            }
    }

    void RefreshDisp(int p)
    {
        if (kbDisp[p] != null)
            kbDisp[p].text = kbTexts[p] + "<color=#666>│</color>";
    }

    // ═══════════════════════════════════════════════════════════════════
    // PARAMÈTRES
    // ═══════════════════════════════════════════════════════════════════

    void HandleSet(int p)
    {
        Gamepad gp = pPad[p];
        if (gp.buttonEast.wasPressedThisFrame) { ShowPlayerMain(p); return; }

        int nav = NavV(gp, ref pSetStick[p]);
        if (nav != 0)
        {
            pSetIdx[p] = (pSetIdx[p] - nav + SET_COUNT) % SET_COUNT;
            HL(pSetBtns[p], pSetIdx[p]);
        }
        if (pSetIdx[p] == 0) HandleSlider(gp);
        if (gp.buttonSouth.wasPressedThisFrame && pSetIdx[p] == 1) ShowPlayerMain(p);
    }

    void HandleSlider(Gamepad gp)
    {
        float axis = gp.leftStick.x.ReadValue();
        if (gp.dpad.right.isPressed) axis += 1f;
        if (gp.dpad.left.isPressed)  axis -= 1f;
        if (Mathf.Abs(axis) > 0.1f)
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + axis * Time.unscaledDeltaTime * 0.8f);
            for (int i = 0; i < 2; i++)
                if (pSlider[i] != null) pSlider[i].SetValueWithoutNotify(AudioListener.volume);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // LEADERBOARD
    // ═══════════════════════════════════════════════════════════════════

    void HandleLead(int p)
    {
        Gamepad gp = pPad[p];
        if (gp.buttonEast.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame)
            ShowPlayerMain(p);
    }

    // ═══════════════════════════════════════════════════════════════════
    // INPUT HELPER
    // ═══════════════════════════════════════════════════════════════════

    int NavV(Gamepad gp, ref bool stickUsed)
    {
        if (gp.dpad.up.wasPressedThisFrame)   return  1;
        if (gp.dpad.down.wasPressedThisFrame) return -1;
        float y = gp.leftStick.y.ReadValue();
        if (Mathf.Abs(y) > 0.5f && !stickUsed) { stickUsed = true; return y > 0 ? 1 : -1; }
        if (Mathf.Abs(y) <= 0.3f) stickUsed = false;
        return 0;
    }

    // ═══════════════════════════════════════════════════════════════════
    // TRANSITIONS (par joueur)
    // ═══════════════════════════════════════════════════════════════════

    void ShowPlayerMain(int p)
    {
        pState[p] = MenuState.Main;
        pMainIdx[p] = 0; pMainStick[p] = false;
        GO(pMain[p], true); GO(pPseudo[p], false);
        GO(pSettings[p], false); GO(pLeader[p], false);
        GO(kbConfirm[p], false);
        HL(pMainBtns[p], 0);
        kbRow[p] = 0; kbCol[p] = 0; kbTexts[p] = ""; kbDone[p] = false; kbStick[p] = false;
    }

    void ShowPlayerPseudo(int p)
    {
        pState[p] = MenuState.PseudoInput;
        GO(pMain[p], false); GO(pPseudo[p], true);
        kbRow[p] = 0; kbCol[p] = 0; kbTexts[p] = ""; kbDone[p] = false;
        RefreshHL(p); RefreshDisp(p);
    }

    void ShowPlayerSet(int p)
    {
        pState[p] = MenuState.Settings;
        pSetIdx[p] = 0; pSetStick[p] = false;
        GO(pMain[p], false); GO(pSettings[p], true);
        if (pSlider[p] != null) pSlider[p].value = AudioListener.volume;
        HL(pSetBtns[p], 0);
    }

    void ShowPlayerLead(int p)
    {
        pState[p] = MenuState.Leaderboard;
        GO(pMain[p], false); GO(pLeader[p], true);
        RefreshLead(p);
    }

    void RefreshLead(int p)
    {
        if (pLeaderTxt[p] == null) return;
        int best = PlayerPrefs.GetInt("BestScore", 0);
        string t = "<color=#FFD700>★ MEILLEUR SCORE ★</color>\n\n";
        t += $"<size=48>{best} pts</size>\n\n";
        t += "<color=#888>────────────────</color>\n\n";
        t += "<color=#AAA>Dernière partie :</color>\n";
        t += $"<color=#66CCFF>{PseudoJ1} : {WinManager.scoreJ1} pts</color>\n";
        t += $"<color=#FF9933>{PseudoJ2} : {WinManager.scoreJ2} pts</color>";
        pLeaderTxt[p].text = t;
    }

    // ═══════════════════════════════════════════════════════════════════
    // VIDÉO DE FOND (partagée)
    // ═══════════════════════════════════════════════════════════════════

    void SetupVideo()
    {
        if (backgroundVideoClip == null) return;
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip            = backgroundVideoClip;
        videoPlayer.isLooping       = true;
        videoPlayer.playOnAwake     = false;
        videoPlayer.renderMode      = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoRT = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = videoRT;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += src => src.Play();
    }

    // ═══════════════════════════════════════════════════════════════════
    // CONSTRUCTION UI — CANVAS PAR JOUEUR
    // ═══════════════════════════════════════════════════════════════════

    void BuildPlayerCanvas(int p)
    {
        var go = new GameObject($"Canvas_J{p + 1}");
        go.transform.SetParent(transform, false);
        pCanvas[p] = go.AddComponent<Canvas>();
        pCanvas[p].renderMode    = RenderMode.ScreenSpaceOverlay;
        pCanvas[p].targetDisplay = p;
        pCanvas[p].sortingOrder  = 100;

        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // Fond vidéo ou couleur unie
        var vGO = new GameObject("VideoBg");
        vGO.transform.SetParent(go.transform, false);
        vGO.transform.SetAsFirstSibling();
        var raw = vGO.AddComponent<RawImage>();
        Fill(vGO.GetComponent<RectTransform>());
        if (videoRT != null) { raw.texture = videoRT; raw.color = Color.white; }
        else                   raw.color = new Color(0.04f, 0.04f, 0.07f);

        Color accent = p == 0 ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.6f, 0.3f);

        pMain[p]     = BuildMainPanel(go.transform, p, accent);
        pPseudo[p]   = BuildPseudoPanel(go.transform, p, accent);
        pSettings[p] = BuildSettingsPanel(go.transform, p);
        pLeader[p]   = BuildLeaderboardPanel(go.transform, p);

        // Overlay « manette non connectée »
        var wGO = new GameObject("WaitOverlay");
        wGO.transform.SetParent(go.transform, false);
        wGO.AddComponent<RectTransform>(); Fill(wGO.GetComponent<RectTransform>());
        Img(wGO.transform, "Dim", new Color(0, 0, 0, 0.85f), V(0, 0), V(1, 1));
        Lbl(wGO.transform, "Txt", "CONNECTEZ UNE MANETTE",
            V(0.1f, 0.42f), V(0.9f, 0.58f), 42f, FontStyles.Bold, accent);
        pWait[p] = wGO;
    }

    // ── MAIN PANEL ────────────────────────────────────────────────────

    GameObject BuildMainPanel(Transform parent, int p, Color accent)
    {
        var root = Pnl(parent, "MainPanel");
        Img(root.transform, "Ov", new Color(0, 0, 0, 0.4f), V(0, 0), V(1, 1));

        Lbl(root.transform, "Title", "CRIME & SCIENCE",
            V(0.1f, 0.72f), V(0.9f, 0.92f), 90f, FontStyles.Bold, Color.white);
        Lbl(root.transform, "Sub", $"Joueur {p + 1}",
            V(0.3f, 0.63f), V(0.7f, 0.72f), 32f, FontStyles.Bold, accent);

        float bL = 0.30f, bR = 0.70f;
        pMainBtns[p] = new Image[MAIN_COUNT];
        int pi = p;
        pMainBtns[p][0] = Btn(root.transform, "BtnJ", "JOUER",       V(bL, 0.44f), V(bR, 0.56f), colBtn, () => ShowPlayerPseudo(pi));
        pMainBtns[p][1] = Btn(root.transform, "BtnP", "PARAMÈTRES",  V(bL, 0.30f), V(bR, 0.42f), colBtn, () => ShowPlayerSet(pi));
        pMainBtns[p][2] = Btn(root.transform, "BtnL", "LEADERBOARD", V(bL, 0.16f), V(bR, 0.28f), colBtn, () => ShowPlayerLead(pi));

        Lbl(root.transform, "H", "↑↓ Naviguer    A Confirmer",
            V(0.15f, 0.04f), V(0.85f, 0.12f), 18f, FontStyles.Normal, new Color(0.5f, 0.5f, 0.55f));
        return root;
    }

    // ── PSEUDO PANEL (CLAVIER VIRTUEL) ────────────────────────────────

    GameObject BuildPseudoPanel(Transform parent, int p, Color accent)
    {
        var root = Pnl(parent, "PseudoPanel");
        Img(root.transform, "Bg", new Color(0.03f, 0.03f, 0.06f), V(0, 0), V(1, 1));

        Lbl(root.transform, "Title", $"JOUEUR {p + 1} — PSEUDO",
            V(0.05f, 0.89f), V(0.95f, 0.98f), 48f, FontStyles.Bold, accent);

        Img(root.transform, "DispBg", new Color(0.08f, 0.08f, 0.14f),
            V(0.12f, 0.76f), V(0.88f, 0.87f));

        var dGO = new GameObject("DispTxt");
        dGO.transform.SetParent(root.transform, false);
        kbDisp[p]           = dGO.AddComponent<TextMeshProUGUI>();
        kbDisp[p].fontSize  = 42f;
        kbDisp[p].color     = Color.white;
        kbDisp[p].alignment = TextAlignmentOptions.Center;
        kbDisp[p].richText  = true;
        Anc(dGO.GetComponent<RectTransform>(), V(0.14f, 0.76f), V(0.86f, 0.87f));

        Lbl(root.transform, "Count", $"0/{maxPseudoLength}",
            V(0.74f, 0.87f), V(0.88f, 0.93f), 18f, FontStyles.Normal, new Color(0.4f, 0.4f, 0.45f));

        var kb = new GameObject("KB");
        kb.transform.SetParent(root.transform, false);
        kb.AddComponent<RectTransform>();
        Anc(kb.GetComponent<RectTransform>(), V(0.06f, 0.12f), V(0.94f, 0.73f));
        BuildKeyGrid(kb.transform, p);

        // Overlay confirmation
        var cRoot = new GameObject("Confirm");
        cRoot.transform.SetParent(root.transform, false);
        cRoot.AddComponent<RectTransform>(); Fill(cRoot.GetComponent<RectTransform>());
        Img(cRoot.transform, "Dim", new Color(0, 0, 0, 0.75f), V(0, 0), V(1, 1));
        Lbl(cRoot.transform, "R", "PRÊT !",
            V(0.1f, 0.48f), V(0.9f, 0.68f), 80f, FontStyles.Bold, new Color(0.2f, 0.95f, 0.3f));
        Lbl(cRoot.transform, "W", "En attente de l'autre joueur...\n\nB pour modifier",
            V(0.1f, 0.28f), V(0.9f, 0.46f), 24f, FontStyles.Italic, new Color(0.6f, 0.6f, 0.65f));
        kbConfirm[p] = cRoot;
        cRoot.SetActive(false);

        Lbl(root.transform, "H", "↑↓←→  Naviguer     A  Sélectionner     B  Retour",
            V(0.05f, 0.02f), V(0.95f, 0.09f), 18f, FontStyles.Normal, new Color(0.4f, 0.4f, 0.45f));

        root.SetActive(false);
        return root;
    }

    void BuildKeyGrid(Transform parent, int p)
    {
        kbKeys[p] = new Image[KB.Length][];
        float rowH = 0.21f, rowGap = 0.05f;

        for (int r = 0; r < KB.Length; r++)
        {
            int n = KB[r].Length;
            kbKeys[p][r] = new Image[n];
            float top = 1f - r * (rowH + rowGap);
            float bot = top - rowH;
            float gap = r < 3 ? 0.008f : 0.02f;
            float kw  = (1f - (n + 1) * gap) / n;

            for (int c = 0; c < n; c++)
            {
                float x = gap + c * (kw + gap);
                var kGO = new GameObject($"K{r}_{c}");
                kGO.transform.SetParent(parent, false);
                var img = kGO.AddComponent<Image>(); img.color = colKeyBg;
                Anc(kGO.GetComponent<RectTransform>(), V(x, bot), V(x + kw, top));
                Lbl(kGO.transform, "L", KB[r][c], V(0, 0), V(1, 1),
                    r < 3 ? 30f : 22f, FontStyles.Bold, Color.white);
                kbKeys[p][r][c] = img;
            }
        }
    }

    // ── SETTINGS PANEL ────────────────────────────────────────────────

    GameObject BuildSettingsPanel(Transform parent, int p)
    {
        var root = Pnl(parent, "SettingsPanel");
        Img(root.transform, "Ov", new Color(0, 0, 0, 0.5f), V(0, 0), V(1, 1));

        var box = new GameObject("Box");
        box.transform.SetParent(root.transform, false);
        box.AddComponent<Image>().color = colPanel;
        Anc(box.GetComponent<RectTransform>(), V(0.25f, 0.18f), V(0.75f, 0.82f));

        Lbl(box.transform, "T", "PARAMÈTRES",
            V(0, 0.82f), V(1, 1), 56f, FontStyles.Bold, Color.white);
        Lbl(box.transform, "VL", "Volume sonore",
            V(0.06f, 0.64f), V(0.94f, 0.76f), 26f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f));

        pSetBtns[p] = new Image[SET_COUNT];

        var sc = new GameObject("SC");
        sc.transform.SetParent(box.transform, false);
        var scI = sc.AddComponent<Image>(); scI.color = colBtn;
        Anc(sc.GetComponent<RectTransform>(), V(0.06f, 0.48f), V(0.94f, 0.64f));
        pSetBtns[p][0] = scI;

        pSlider[p] = BuildSlider(sc.transform);
        pSlider[p].value = AudioListener.volume;

        int pi = p;
        pSetBtns[p][1] = Btn(box.transform, "BtnR", "RETOUR",
            V(0.25f, 0.12f), V(0.75f, 0.28f), colBtn, () => ShowPlayerMain(pi));

        Lbl(box.transform, "H", "↑↓ Naviguer   ←→ Volume   B Retour",
            V(0.02f, 0.02f), V(0.98f, 0.10f), 16f, FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));
        root.SetActive(false);
        return root;
    }

    // ── LEADERBOARD PANEL ─────────────────────────────────────────────

    GameObject BuildLeaderboardPanel(Transform parent, int p)
    {
        var root = Pnl(parent, "LeaderPanel");
        Img(root.transform, "Ov", new Color(0, 0, 0, 0.5f), V(0, 0), V(1, 1));

        var box = new GameObject("Box");
        box.transform.SetParent(root.transform, false);
        box.AddComponent<Image>().color = colPanel;
        Anc(box.GetComponent<RectTransform>(), V(0.20f, 0.10f), V(0.80f, 0.90f));

        Lbl(box.transform, "T", "LEADERBOARD",
            V(0, 0.85f), V(1, 1), 56f, FontStyles.Bold, Color.white);

        var cGO = new GameObject("C");
        cGO.transform.SetParent(box.transform, false);
        pLeaderTxt[p]           = cGO.AddComponent<TextMeshProUGUI>();
        pLeaderTxt[p].fontSize  = 30f;
        pLeaderTxt[p].color     = Color.white;
        pLeaderTxt[p].alignment = TextAlignmentOptions.Center;
        pLeaderTxt[p].richText  = true;
        Anc(cGO.GetComponent<RectTransform>(), V(0.05f, 0.15f), V(0.95f, 0.82f));

        Lbl(box.transform, "H", "A / B → Retour",
            V(0.02f, 0.02f), V(0.98f, 0.12f), 18f, FontStyles.Italic, new Color(0.5f, 0.5f, 0.5f));
        root.SetActive(false);
        return root;
    }

    // ═══════════════════════════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════════════════════════

    void HL(Image[] a, int idx)
    {
        for (int i = 0; i < a.Length; i++)
            if (a[i] != null) a[i].color = i == idx ? colSel : colBtn;
    }

    void GO(GameObject o, bool on) { if (o != null) o.SetActive(on); }
    Vector2 V(float x, float y) => new(x, y);

    GameObject Pnl(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Fill(go.GetComponent<RectTransform>());
        return go;
    }

    void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void Anc(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Image Img(Transform parent, string name, Color col, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = col;
        Anc(go.GetComponent<RectTransform>(), aMin, aMax);
        return img;
    }

    void Lbl(Transform parent, string name, string text,
        Vector2 aMin, Vector2 aMax, float size, FontStyles style, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = col;
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        Anc(go.GetComponent<RectTransform>(), aMin, aMax);
    }

    Image Btn(Transform parent, string name, string label,
        Vector2 aMin, Vector2 aMax, Color bg, UnityEngine.Events.UnityAction click)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = bg;
        Anc(go.GetComponent<RectTransform>(), aMin, aMax);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Lbl(go.transform, "T", label, V(0, 0), V(1, 1), 32f, FontStyles.Bold, Color.white);
        return img;
    }

    Slider BuildSlider(Transform parent)
    {
        var sGO = new GameObject("Slider");
        sGO.transform.SetParent(parent, false);
        var sl = sGO.AddComponent<Slider>(); sl.minValue = 0f; sl.maxValue = 1f;
        var rt = sGO.GetComponent<RectTransform>();
        rt.anchorMin = V(0.03f, 0.1f); rt.anchorMax = V(0.97f, 0.9f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(sGO.transform, false);
        bgGO.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var bgR = bgGO.GetComponent<RectTransform>();
        bgR.anchorMin = V(0, 0.2f); bgR.anchorMax = V(1, 0.8f);
        bgR.offsetMin = bgR.offsetMax = Vector2.zero;

        var faGO = new GameObject("FA");
        faGO.transform.SetParent(sGO.transform, false);
        var faR = faGO.AddComponent<RectTransform>();
        faR.anchorMin = V(0, 0.2f); faR.anchorMax = V(1, 0.8f);
        faR.offsetMin = V(5, 0); faR.offsetMax = V(-15, 0);

        var fGO = new GameObject("F");
        fGO.transform.SetParent(faGO.transform, false);
        fGO.AddComponent<Image>().color = new Color(0.2f, 0.55f, 1f);
        var fR = fGO.GetComponent<RectTransform>();
        fR.anchorMin = Vector2.zero; fR.anchorMax = Vector2.one;
        fR.offsetMin = fR.offsetMax = Vector2.zero;
        sl.fillRect = fR;

        var haGO = new GameObject("HA");
        haGO.transform.SetParent(sGO.transform, false);
        var haR = haGO.AddComponent<RectTransform>();
        haR.anchorMin = Vector2.zero; haR.anchorMax = Vector2.one;
        haR.offsetMin = V(10, 0); haR.offsetMax = V(-10, 0);

        var hGO = new GameObject("H");
        hGO.transform.SetParent(haGO.transform, false);
        hGO.AddComponent<Image>().color = Color.white;
        var hR = hGO.GetComponent<RectTransform>();
        hR.anchorMin = V(0, 0); hR.anchorMax = V(0, 1);
        hR.sizeDelta = V(20, 0);
        sl.handleRect = hR;

        return sl;
    }
}
