using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    [Header("UI Status")]
    public TextMeshProUGUI statusTextJ1_D1;
    public TextMeshProUGUI statusTextJ2_D1;
    public TextMeshProUGUI statusTextJ1_D2;
    public TextMeshProUGUI statusTextJ2_D2;

    [Header("Personnages jouables")]
    public CharacterData[] characters;

    [Header("Vidéos de Transition (Double Écran)")]
    public GameObject videoContainer_D1;
    public RawImage   videoDisplay_D1;
    private VideoPlayer videoPlayer_D1;

    [Space]
    public GameObject videoContainer_D2;
    public RawImage   videoDisplay_D2;
    private VideoPlayer videoPlayer_D2;

    [Header("Navigation")]
    public string nextSceneName      = "MainScene";
    public float  vitesseClignotement = 3f;

    // ── Devices ──────────────────────────────────────────────────────
    private InputDevice deviceJ1;
    private InputDevice deviceJ2;
    private bool loadingStarted = false;

    // ── Sélection de personnage ───────────────────────────────────────
    private int  indexJ1 = 0,  indexJ2 = 0;
    private bool selectedJ1 = false, selectedJ2 = false;
    private bool canSelectJ1 = false, canSelectJ2 = false; // bloque la sélection le frame de connexion
    private bool stickUsedJ1 = false, stickUsedJ2 = false;
    private const float stickThreshold = 0.5f;

    // ── UI Carousel ───────────────────────────────────────────────────
    private GameObject carouselJ1, carouselJ2;
    private Image          spriteJ1,  spriteJ2;
    private TextMeshProUGUI nameJ1,   nameJ2;
    private TextMeshProUGUI statusSelJ1, statusSelJ2;

    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        if (videoContainer_D1 != null)
        {
            videoPlayer_D1 = videoContainer_D1.GetComponentInChildren<VideoPlayer>(true);
            videoContainer_D1.SetActive(false);
        }
        if (videoContainer_D2 != null)
        {
            videoPlayer_D2 = videoContainer_D2.GetComponentInChildren<VideoPlayer>(true);
            videoContainer_D2.SetActive(false);
        }

        string attenteMsg = "<color=white>APPUYEZ SUR A POUR COMMENCER...</color>";
        if (statusTextJ1_D1) { statusTextJ1_D1.text = "J1 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ1_D1)); }
        if (statusTextJ1_D2) { statusTextJ1_D2.text = "J1 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ1_D2)); }
        if (statusTextJ2_D1) { statusTextJ2_D1.text = "J2 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ2_D1)); }
        if (statusTextJ2_D2) { statusTextJ2_D2.text = "J2 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ2_D2)); }

        CharacterSelectionData.Reset();

        // Initialise les indices pour qu'ils ne soient pas identiques au départ
        indexJ1 = 0;
        indexJ2 = (characters != null && characters.Length > 1) ? 1 : 0;
    }

    void Update()
    {
        if (loadingStarted) return;

        // ── Phase 1 : Détection des manettes ──────────────────────────
        if (deviceJ1 == null)
        {
            DetectDevice(out deviceJ1, null);
            if (deviceJ1 != null)
            {
                UpdatePlayerUI(1, deviceJ1.displayName);
                BuildCarousel(1);
                StartCoroutine(EnableSelectionNextFrame(1)); // évite que le A de connexion sélectionne direct
            }
        }
        else if (deviceJ2 == null)
        {
            DetectDevice(out deviceJ2, deviceJ1);
            if (deviceJ2 != null)
            {
                UpdatePlayerUI(2, deviceJ2.displayName);
                BuildCarousel(2);
                StartCoroutine(EnableSelectionNextFrame(2));
            }
        }

        // ── Phase 2 : Sélection du personnage ─────────────────────────
        HandleSelectionInput();
    }

    // ── CAROUSEL ─────────────────────────────────────────────────────

    void BuildCarousel(int playerNum)
    {
        // Masque l'ancien UI de connexion pour ce display
        string menuName = playerNum == 1 ? "Menu" : "Menu2";
        var oldMenu = GameObject.Find(menuName);
        if (oldMenu != null) oldMenu.SetActive(false);

        int display = playerNum == 1 ? 0 : 1;

        var root = new GameObject($"CarouselCanvas_J{playerNum}");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = display;
        canvas.sortingOrder  = 50;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Fond semi-transparent
        CreateImg(root.transform, "BG", new Color(0f, 0f, 0f, 0.78f), Vector2.zero, Vector2.one);

        // Titre
        var title = CreateTMP(root.transform, "Title",
            $"J{playerNum} — CHOISISSEZ VOTRE PERSONNAGE", 46f,
            new Vector2(0.1f, 0.80f), new Vector2(0.9f, 0.92f));
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        // Flèche gauche
        var lArrow = CreateTMP(root.transform, "ArrowL", "◀", 90f,
            new Vector2(0.05f, 0.28f), new Vector2(0.22f, 0.72f));
        lArrow.color = Color.white;

        // Flèche droite
        var rArrow = CreateTMP(root.transform, "ArrowR", "▶", 90f,
            new Vector2(0.78f, 0.28f), new Vector2(0.95f, 0.72f));
        rArrow.color = Color.white;

        // Sprite du personnage
        var charImg = CreateImg(root.transform, "CharSprite", Color.white,
            new Vector2(0.28f, 0.26f), new Vector2(0.72f, 0.76f));
        charImg.preserveAspect = true;

        // Nom du personnage
        var nameText = CreateTMP(root.transform, "CharName", "", 50f,
            new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.26f));
        nameText.color     = Color.white;
        nameText.fontStyle = FontStyles.Bold;

        // Statut (sélectionner / déjà pris)
        var statusText = CreateTMP(root.transform, "CharStatus",
            "Appuyez sur  A  pour sélectionner", 34f,
            new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.15f));
        statusText.color = new Color(0.85f, 0.85f, 0.85f);

        if (playerNum == 1)
        {
            carouselJ1   = root;
            spriteJ1     = charImg;
            nameJ1       = nameText;
            statusSelJ1  = statusText;
        }
        else
        {
            carouselJ2   = root;
            spriteJ2     = charImg;
            nameJ2       = nameText;
            statusSelJ2  = statusText;
        }

        RefreshCarousel(playerNum);
    }

    void RefreshCarousel(int playerNum)
    {
        int    myIdx      = playerNum == 1 ? indexJ1    : indexJ2;
        int    otherIdx   = playerNum == 1 ? indexJ2    : indexJ1;
        bool   otherSel   = playerNum == 1 ? selectedJ2 : selectedJ1;
        bool   mySel      = playerNum == 1 ? selectedJ1 : selectedJ2;
        Image  img        = playerNum == 1 ? spriteJ1   : spriteJ2;
        TextMeshProUGUI nm  = playerNum == 1 ? nameJ1     : nameJ2;
        TextMeshProUGUI st  = playerNum == 1 ? statusSelJ1 : statusSelJ2;

        if (img == null) return;

        bool taken = otherSel && otherIdx == myIdx;

        // Sprite carousel (placeholder coloré si non assigné)
        CharacterData cd = (characters != null && myIdx < characters.Length) ? characters[myIdx] : null;
        if (cd != null && cd.spriteCarousel != null)
        {
            img.sprite = cd.spriteCarousel;
            img.color  = taken && !mySel ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
        }
        else
        {
            img.sprite = null;
            img.color  = taken && !mySel
                ? new Color(0.2f, 0.2f, 0.35f)
                : (playerNum == 1 ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.4f, 0.2f));
        }

        // Nom
        if (nm != null)
        {
            string n = (cd != null && !string.IsNullOrEmpty(cd.characterName))
                ? cd.characterName
                : $"Personnage {myIdx + 1}";
            nm.text = n;
            nm.color = taken && !mySel ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }

        // Statut
        if (st != null)
        {
            if (mySel)
            {
                st.text  = "✓  PERSONNAGE SÉLECTIONNÉ";
                st.color = new Color(0.2f, 0.95f, 0.3f);
            }
            else if (taken)
            {
                st.text  = "✗  Déjà choisi par l'autre joueur";
                st.color = new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                st.text  = "Appuyez sur  A  pour sélectionner";
                st.color = new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }

    // ── INPUT SÉLECTION ──────────────────────────────────────────────

    void HandleSelectionInput()
    {
        // J1
        if (deviceJ1 != null && !selectedJ1 && canSelectJ1)
        {
            int nav = GetNav(deviceJ1, ref stickUsedJ1);
            if (nav != 0 && characters != null)
            {
                indexJ1 = (indexJ1 + nav + characters.Length) % characters.Length;
                RefreshCarousel(1);
                if (carouselJ2 != null) RefreshCarousel(2);
            }
            if (IsConfirm(deviceJ1))
            {
                bool taken = selectedJ2 && indexJ2 == indexJ1;
                if (!taken) ConfirmSelection(1);
            }
        }

        // J2
        if (deviceJ2 != null && !selectedJ2 && canSelectJ2)
        {
            int nav = GetNav(deviceJ2, ref stickUsedJ2);
            if (nav != 0 && characters != null)
            {
                indexJ2 = (indexJ2 + nav + characters.Length) % characters.Length;
                RefreshCarousel(2);
                if (carouselJ1 != null) RefreshCarousel(1);
            }
            if (IsConfirm(deviceJ2))
            {
                bool taken = selectedJ1 && indexJ1 == indexJ2;
                if (!taken) ConfirmSelection(2);
            }
        }
    }

    IEnumerator EnableSelectionNextFrame(int playerNum)
    {
        yield return null;
        if (playerNum == 1) canSelectJ1 = true;
        else                canSelectJ2 = true;
    }

    void ConfirmSelection(int playerNum)
    {
        if (playerNum == 1)
        {
            selectedJ1 = true;
            CharacterSelectionData.J1CharacterIndex = indexJ1;
            CharacterSelectionData.J1Data = (characters != null && indexJ1 < characters.Length)
                ? characters[indexJ1] : null;
        }
        else
        {
            selectedJ2 = true;
            CharacterSelectionData.J2CharacterIndex = indexJ2;
            CharacterSelectionData.J2Data = (characters != null && indexJ2 < characters.Length)
                ? characters[indexJ2] : null;
        }

        RefreshCarousel(playerNum);
        RefreshCarousel(playerNum == 1 ? 2 : 1);

        if (selectedJ1 && selectedJ2 && !loadingStarted)
        {
            loadingStarted = true;
            if (carouselJ1 != null) Destroy(carouselJ1);
            if (carouselJ2 != null) Destroy(carouselJ2);
            if (GlobalPlayerManager.Instance != null)
            {
                GlobalPlayerManager.Instance.AssignPlayer(0, deviceJ1);
                GlobalPlayerManager.Instance.AssignPlayer(1, deviceJ2);
            }
            StartCoroutine(PlayVideosAndLoad());
        }
    }

    int GetNav(InputDevice device, ref bool stickUsed)
    {
        if (device is Gamepad pad)
        {
            Vector2 s = pad.leftStick.ReadValue();
            if (s.magnitude > stickThreshold && !stickUsed)
            {
                stickUsed = true;
                return s.x >= 0 ? 1 : -1;
            }
            if (s.magnitude <= stickThreshold) stickUsed = false;
            if (pad.dpad.right.wasPressedThisFrame) return  1;
            if (pad.dpad.left.wasPressedThisFrame)  return -1;
        }
        else if (device is Keyboard kb)
        {
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return  1;
            if (kb.leftArrowKey.wasPressedThisFrame  || kb.qKey.wasPressedThisFrame) return -1;
        }
        return 0;
    }

    bool IsConfirm(InputDevice device)
    {
        if (device is Gamepad gp) return gp.buttonSouth.wasPressedThisFrame;
        if (device is Keyboard kb) return kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
        return false;
    }

    // ── VIDÉO + CHARGEMENT (inchangé) ────────────────────────────────

    IEnumerator PlayVideosAndLoad()
    {
        if (videoPlayer_D1 == null || videoPlayer_D2 == null)
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        videoContainer_D1.SetActive(true);
        videoContainer_D2.SetActive(true);
        videoPlayer_D1.gameObject.SetActive(true);
        videoPlayer_D2.gameObject.SetActive(true);

        videoPlayer_D1.Prepare();
        videoPlayer_D2.Prepare();

        while (!videoPlayer_D1.isPrepared || !videoPlayer_D2.isPrepared) yield return null;

        videoDisplay_D1.texture = videoPlayer_D1.texture;
        videoDisplay_D2.texture = videoPlayer_D2.texture;

        videoPlayer_D1.Play();
        videoPlayer_D2.Play();

        yield return new WaitForSeconds(0.5f);
        while (videoPlayer_D1.isPlaying) yield return null;

        SceneManager.LoadScene(nextSceneName);
    }

    // ── UTILS ─────────────────────────────────────────────────────────

    void DetectDevice(out InputDevice foundDevice, InputDevice excluded)
    {
        foundDevice = null;
        foreach (var device in InputSystem.devices)
        {
            if (device == excluded) continue;
            if ((device is Gamepad || device is Keyboard || device is Mouse) && CheckAnyButton(device))
            { foundDevice = device; return; }
        }
    }

    bool CheckAnyButton(InputDevice device)
    {
        if (device is Gamepad pad) return pad.allControls[0].IsPressed() || pad.buttonSouth.wasPressedThisFrame;
        if (device is Keyboard kb) return kb.anyKey.wasPressedThisFrame;
        if (device is Mouse m)     return m.leftButton.wasPressedThisFrame;
        return false;
    }

    void UpdatePlayerUI(int num, string name)
    {
        string msg = $"<color=green>J{num} : {name} CONNECTÉ</color>";
        if (num == 1) { if (statusTextJ1_D1) statusTextJ1_D1.text = msg; if (statusTextJ1_D2) statusTextJ1_D2.text = msg; }
        else          { if (statusTextJ2_D1) statusTextJ2_D1.text = msg; if (statusTextJ2_D2) statusTextJ2_D2.text = msg; }
    }

    IEnumerator FlashText(TextMeshProUGUI text)
    {
        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * vitesseClignotement) + 1f) / 2f;
            if (text != null) text.alpha = Mathf.Clamp(alpha, 0.2f, 1f);
            yield return null;
        }
    }

    // ── UI HELPERS ────────────────────────────────────────────────────

    Image CreateImg(Transform parent, string goName, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string goName, string text, float fontSize,
                              Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }
}
