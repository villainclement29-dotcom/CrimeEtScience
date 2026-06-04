using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    [Header("Carousel J1 (Display 1)")]
    public GameObject carouselRootJ1;
    public Image      charSpriteJ1;
    public TextMeshProUGUI charNameJ1;
    public TextMeshProUGUI charStatusJ1;

    [Header("Carousel J2 (Display 2)")]
    public GameObject carouselRootJ2;
    public Image      charSpriteJ2;
    public TextMeshProUGUI charNameJ2;
    public TextMeshProUGUI charStatusJ2;

    [Header("Personnages jouables")]
    public CharacterData[] characters;

    [Header("Vidéos de Transition")]
    public GameObject videoContainer_D1;
    public RawImage   videoDisplay_D1;
    public GameObject videoContainer_D2;
    public RawImage   videoDisplay_D2;
    private VideoPlayer videoPlayer_D1;
    private VideoPlayer videoPlayer_D2;

    [Header("Navigation")]
    public string nextSceneName = "MainScene";

    [Header("Carousel Animation")]
    [Tooltip("Décalage horizontal (px) des personnages prev/next par rapport au centre")]
    public float sideOffsetX = 420f;
    [Tooltip("Échelle des personnages latéraux (1 = taille du centre)")]
    public float sideScale = 0.55f;
    public float centerScale = 1f;
    [Tooltip("Couleur appliquée aux personnages latéraux (grisé / transparence)")]
    public Color sideColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
    [Tooltip("Durée de la transition entre personnages (s)")]
    public float transitionDuration = 0.22f;

    [Header("Flèches Navigation")]
    [Tooltip("Décalage horizontal des flèches par rapport au personnage central (px)")]
    public float arrowOffsetX = 320f;
    [Tooltip("Taille de la zone des flèches (px)")]
    public Vector2 arrowSize = new Vector2(140f, 140f);
    [Tooltip("Amplitude du rebond horizontal (px)")]
    public float arrowPulseAmplitude = 14f;
    [Tooltip("Vitesse du rebond")]
    public float arrowPulseSpeed = 3f;
    [Tooltip("Couleur des flèches")]
    public Color arrowColor = new Color(1f, 1f, 1f, 0.85f);

    // ── Devices ──────────────────────────────────────────────────────
    private InputDevice deviceJ1;
    private InputDevice deviceJ2;
    private bool loadingStarted = false;

    // ── Sélection ────────────────────────────────────────────────────
    private int  indexJ1 = 0, indexJ2 = 0;
    private bool selectedJ1 = false, selectedJ2 = false;
    private bool stickUsedJ1 = false, stickUsedJ2 = false;
    private const float stickThreshold = 0.5f;

    // ── Slots latéraux (auto-générés au Start) ───────────────────────
    private Image charSpritePrevJ1, charSpriteNextJ1;
    private Image charSpritePrevJ2, charSpriteNextJ2;
    private RectTransform rtCenterJ1, rtPrevJ1, rtNextJ1;
    private RectTransform rtCenterJ2, rtPrevJ2, rtNextJ2;
    private Coroutine animJ1, animJ2;

    // ── Flèches ──────────────────────────────────────────────────────
    private RectTransform arrowLeftJ1, arrowRightJ1;
    private RectTransform arrowLeftJ2, arrowRightJ2;

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

        CharacterSelectionData.Reset();
        indexJ1 = 0;
        indexJ2 = (characters != null && characters.Length > 1) ? 1 : 0;

        EnsureSlots(1);
        EnsureSlots(2);
        EnsureArrows(1);
        EnsureArrows(2);

        // Cache les carousels au démarrage
        if (carouselRootJ1 != null) carouselRootJ1.SetActive(false);
        if (carouselRootJ2 != null) carouselRootJ2.SetActive(false);
    }

    void EnsureArrows(int playerNum)
    {
        GameObject root = playerNum == 1 ? carouselRootJ1 : carouselRootJ2;
        if (root == null) return;

        Transform al = root.transform.Find("ArrowLeft");
        Transform ar = root.transform.Find("ArrowRight");

        RectTransform alRT = al != null ? al.GetComponent<RectTransform>() : null;
        RectTransform arRT = ar != null ? ar.GetComponent<RectTransform>() : null;

        if (alRT != null)
        {
            alRT.anchorMin = alRT.anchorMax = new Vector2(0.5f, 0.5f);
            alRT.pivot     = new Vector2(0.5f, 0.5f);
            alRT.sizeDelta = arrowSize;
            alRT.anchoredPosition = new Vector2(-arrowOffsetX, 0);
            TextMeshProUGUI t = alRT.GetComponent<TextMeshProUGUI>();
            if (t != null) { t.color = arrowColor; t.raycastTarget = false; }
            // Devant les sprites latéraux mais derrière le centre
            alRT.SetAsLastSibling();
        }
        if (arRT != null)
        {
            arRT.anchorMin = arRT.anchorMax = new Vector2(0.5f, 0.5f);
            arRT.pivot     = new Vector2(0.5f, 0.5f);
            arRT.sizeDelta = arrowSize;
            arRT.anchoredPosition = new Vector2(arrowOffsetX, 0);
            TextMeshProUGUI t = arRT.GetComponent<TextMeshProUGUI>();
            if (t != null) { t.color = arrowColor; t.raycastTarget = false; }
            arRT.SetAsLastSibling();
        }

        if (playerNum == 1) { arrowLeftJ1 = alRT; arrowRightJ1 = arRT; }
        else                { arrowLeftJ2 = alRT; arrowRightJ2 = arRT; }
    }

    void EnsureSlots(int playerNum)
    {
        Image main = playerNum == 1 ? charSpriteJ1 : charSpriteJ2;
        if (main == null) return;

        Transform parent = main.transform.parent;
        GameObject prevGo = Instantiate(main.gameObject, parent);
        prevGo.name = main.name + "_Prev";
        GameObject nextGo = Instantiate(main.gameObject, parent);
        nextGo.name = main.name + "_Next";

        Image prevImg = prevGo.GetComponent<Image>();
        Image nextImg = nextGo.GetComponent<Image>();
        prevImg.raycastTarget = false;
        nextImg.raycastTarget = false;

        RectTransform centerRT = main.rectTransform;
        RectTransform prevRT   = prevImg.rectTransform;
        RectTransform nextRT   = nextImg.rectTransform;

        prevRT.anchoredPosition = new Vector2(-sideOffsetX, 0);
        nextRT.anchoredPosition = new Vector2( sideOffsetX, 0);
        prevRT.localScale = Vector3.one * sideScale;
        nextRT.localScale = Vector3.one * sideScale;
        prevImg.color = sideColor;
        nextImg.color = sideColor;
        centerRT.anchoredPosition = Vector2.zero;
        centerRT.localScale = Vector3.one * centerScale;

        // Side slots derrière le centre dans la hiérarchie
        prevGo.transform.SetSiblingIndex(main.transform.GetSiblingIndex());
        nextGo.transform.SetSiblingIndex(main.transform.GetSiblingIndex());
        main.transform.SetAsLastSibling();

        if (playerNum == 1)
        {
            charSpritePrevJ1 = prevImg;
            charSpriteNextJ1 = nextImg;
            rtCenterJ1 = centerRT; rtPrevJ1 = prevRT; rtNextJ1 = nextRT;
        }
        else
        {
            charSpritePrevJ2 = prevImg;
            charSpriteNextJ2 = nextImg;
            rtCenterJ2 = centerRT; rtPrevJ2 = prevRT; rtNextJ2 = nextRT;
        }
    }

    void Update()
    {
        if (loadingStarted) return;

        // ── Assignation par index de manette ─────────────────────────
        if (deviceJ1 == null)
        {
            InputDevice soloDevice = StartMenuController.IsSoloMode ? StartMenuController.SoloPlayerDevice : null;
            if (soloDevice != null)               deviceJ1 = soloDevice;
            else if (Gamepad.all.Count > 0)       deviceJ1 = Gamepad.all[0];
            else if (Keyboard.current != null)     deviceJ1 = Keyboard.current;
            if (deviceJ1 != null) ShowCarousel(1);
        }
        if (deviceJ2 == null && !StartMenuController.IsSoloMode)
        {
            if (Gamepad.all.Count > 1)                                          deviceJ2 = Gamepad.all[1];
            else if (deviceJ1 is Gamepad && Keyboard.current != null)           deviceJ2 = Keyboard.current;
            if (deviceJ2 != null) ShowCarousel(2);
        }

        HandleSelectionInput();
        PulseArrows();
    }

    void PulseArrows()
    {
        float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * arrowPulseSpeed)) * arrowPulseAmplitude;
        UpdateArrowPair(1, pulse);
        UpdateArrowPair(2, pulse);
    }

    void UpdateArrowPair(int playerNum, float pulse)
    {
        RectTransform al = playerNum == 1 ? arrowLeftJ1  : arrowLeftJ2;
        RectTransform ar = playerNum == 1 ? arrowRightJ1 : arrowRightJ2;
        bool selected = playerNum == 1 ? selectedJ1 : selectedJ2;
        InputDevice dev = playerNum == 1 ? deviceJ1 : deviceJ2;
        GameObject root = playerNum == 1 ? carouselRootJ1 : carouselRootJ2;

        bool show = dev != null && !selected
                    && root != null && root.activeSelf
                    && characters != null && characters.Length > 1;

        if (al != null)
        {
            if (al.gameObject.activeSelf != show) al.gameObject.SetActive(show);
            if (show) al.anchoredPosition = new Vector2(-arrowOffsetX - pulse, 0);
        }
        if (ar != null)
        {
            if (ar.gameObject.activeSelf != show) ar.gameObject.SetActive(show);
            if (show) ar.anchoredPosition = new Vector2( arrowOffsetX + pulse, 0);
        }
    }

    // ── CAROUSEL ─────────────────────────────────────────────────────

    void ShowCarousel(int playerNum)
    {
        if (playerNum == 1 && carouselRootJ1 != null) carouselRootJ1.SetActive(true);
        if (playerNum == 2 && carouselRootJ2 != null) carouselRootJ2.SetActive(true);
        RefreshCarousel(playerNum);
    }

    void RefreshCarousel(int playerNum)
    {
        Image  imgC = playerNum == 1 ? charSpriteJ1     : charSpriteJ2;
        Image  imgP = playerNum == 1 ? charSpritePrevJ1 : charSpritePrevJ2;
        Image  imgN = playerNum == 1 ? charSpriteNextJ1 : charSpriteNextJ2;
        RectTransform rtC = playerNum == 1 ? rtCenterJ1 : rtCenterJ2;
        RectTransform rtP = playerNum == 1 ? rtPrevJ1   : rtPrevJ2;
        RectTransform rtN = playerNum == 1 ? rtNextJ1   : rtNextJ2;

        if (imgC == null || characters == null || characters.Length == 0) return;

        int myIdx = playerNum == 1 ? indexJ1 : indexJ2;
        int len   = characters.Length;
        int prevI = (myIdx - 1 + len) % len;
        int nextI = (myIdx + 1) % len;

        bool taken = IsTaken(playerNum, myIdx);
        bool mySel = playerNum == 1 ? selectedJ1 : selectedJ2;
        CharacterData cd = characters[myIdx];

        // Centre
        if (cd != null && cd.spriteCarousel != null)
            imgC.sprite = cd.spriteCarousel;
        imgC.color = (taken && !mySel) ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
        if (rtC != null) { rtC.anchoredPosition = Vector2.zero; rtC.localScale = Vector3.one * centerScale; }

        // Prev
        if (imgP != null && rtP != null)
        {
            CharacterData cdP = characters[prevI];
            imgP.sprite  = cdP != null ? cdP.spriteCarousel : null;
            imgP.enabled = imgP.sprite != null && len > 1;
            imgP.color   = sideColor;
            rtP.anchoredPosition = new Vector2(-sideOffsetX, 0);
            rtP.localScale       = Vector3.one * sideScale;
        }
        // Next
        if (imgN != null && rtN != null)
        {
            CharacterData cdN = characters[nextI];
            imgN.sprite  = cdN != null ? cdN.spriteCarousel : null;
            imgN.enabled = imgN.sprite != null && len > 1;
            imgN.color   = sideColor;
            rtN.anchoredPosition = new Vector2(sideOffsetX, 0);
            rtN.localScale       = Vector3.one * sideScale;
        }

        UpdateLabels(playerNum);
    }

    void UpdateLabels(int playerNum)
    {
        TextMeshProUGUI nm = playerNum == 1 ? charNameJ1   : charNameJ2;
        TextMeshProUGUI st = playerNum == 1 ? charStatusJ1 : charStatusJ2;
        int  myIdx = playerNum == 1 ? indexJ1 : indexJ2;
        bool mySel = playerNum == 1 ? selectedJ1 : selectedJ2;
        if (characters == null || characters.Length == 0 || myIdx >= characters.Length) return;
        bool taken = IsTaken(playerNum, myIdx);
        CharacterData cd = characters[myIdx];

        if (nm != null)
        {
            nm.text  = (cd != null && !string.IsNullOrEmpty(cd.characterName)) ? cd.characterName : $"Personnage {myIdx + 1}";
            nm.color = (taken && !mySel) ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }
        if (st != null)
        {
            if (mySel)
            { st.text = "✓  PERSONNAGE SÉLECTIONNÉ"; st.color = new Color(0.2f, 0.95f, 0.3f); }
            else if (taken)
            { st.text = "✗  Déjà choisi par l'autre joueur"; st.color = new Color(1f, 0.3f, 0.3f); }
            else
            { st.text = "Appuyez sur  A  pour sélectionner"; st.color = new Color(0.85f, 0.85f, 0.85f); }
        }
    }

    bool IsTaken(int playerNum, int idx)
    {
        bool otherSel = playerNum == 1 ? selectedJ2 : selectedJ1;
        int  otherIdx = playerNum == 1 ? indexJ2    : indexJ1;
        return otherSel && otherIdx == idx;
    }

    void NavigateCarousel(int playerNum, int dir)
    {
        if (characters == null || characters.Length == 0) return;
        int len = characters.Length;
        if (len < 2) return;

        if (playerNum == 1) indexJ1 = (indexJ1 + dir + len) % len;
        else                indexJ2 = (indexJ2 + dir + len) % len;

        Image c = playerNum == 1 ? charSpriteJ1     : charSpriteJ2;
        Image p = playerNum == 1 ? charSpritePrevJ1 : charSpritePrevJ2;
        Image n = playerNum == 1 ? charSpriteNextJ1 : charSpriteNextJ2;
        RectTransform rtC = playerNum == 1 ? rtCenterJ1 : rtCenterJ2;
        RectTransform rtP = playerNum == 1 ? rtPrevJ1   : rtPrevJ2;
        RectTransform rtN = playerNum == 1 ? rtNextJ1   : rtNextJ2;

        int idx = playerNum == 1 ? indexJ1 : indexJ2;
        int newPrevIdx = (idx - 1 + len) % len;
        int newNextIdx = (idx + 1) % len;

        // Stoppe une éventuelle animation en cours (on repart de l'état visuel courant)
        Coroutine running = playerNum == 1 ? animJ1 : animJ2;
        if (running != null) StopCoroutine(running);

        Image newC, newP, newN;
        RectTransform rtNewC, rtNewP, rtNewN;

        if (dir > 0)
        {
            // Le slot "next" devient le centre, le centre devient prev,
            // le slot prev est recyclé : on lui assigne le nouveau "next" hors écran à droite.
            newC = n;  rtNewC = rtN;
            newP = c;  rtNewP = rtC;
            newN = p;  rtNewN = rtP;

            CharacterData cdN = characters[newNextIdx];
            newN.sprite  = cdN != null ? cdN.spriteCarousel : null;
            newN.enabled = newN.sprite != null;
            rtNewN.anchoredPosition = new Vector2(sideOffsetX * 1.6f, 0);
            rtNewN.localScale       = Vector3.one * sideScale * 0.7f;
            newN.color = new Color(sideColor.r, sideColor.g, sideColor.b, 0f);
        }
        else
        {
            // Direction gauche : le slot prev devient centre, le centre devient next,
            // le slot next est recyclé en nouveau "prev" hors écran à gauche.
            newC = p;  rtNewC = rtP;
            newN = c;  rtNewN = rtC;
            newP = n;  rtNewP = rtN;

            CharacterData cdP = characters[newPrevIdx];
            newP.sprite  = cdP != null ? cdP.spriteCarousel : null;
            newP.enabled = newP.sprite != null;
            rtNewP.anchoredPosition = new Vector2(-sideOffsetX * 1.6f, 0);
            rtNewP.localScale       = Vector3.one * sideScale * 0.7f;
            newP.color = new Color(sideColor.r, sideColor.g, sideColor.b, 0f);
        }

        // Boucle infinie : on garde tous les slots visibles si plus d'un personnage.
        if (newC.sprite != null) newC.enabled = true;
        if (newP.sprite != null) newP.enabled = true;
        if (newN.sprite != null) newN.enabled = true;

        // Met à jour les références de rôle
        if (playerNum == 1)
        {
            charSpriteJ1     = newC; rtCenterJ1 = rtNewC;
            charSpritePrevJ1 = newP; rtPrevJ1   = rtNewP;
            charSpriteNextJ1 = newN; rtNextJ1   = rtNewN;
        }
        else
        {
            charSpriteJ2     = newC; rtCenterJ2 = rtNewC;
            charSpritePrevJ2 = newP; rtPrevJ2   = rtNewP;
            charSpriteNextJ2 = newN; rtNextJ2   = rtNewN;
        }
        rtNewC.SetAsLastSibling();

        UpdateLabels(playerNum);

        Coroutine cor = StartCoroutine(AnimateSlots(playerNum));
        if (playerNum == 1) animJ1 = cor; else animJ2 = cor;
    }

    IEnumerator AnimateSlots(int playerNum)
    {
        Image  c = playerNum == 1 ? charSpriteJ1     : charSpriteJ2;
        Image  p = playerNum == 1 ? charSpritePrevJ1 : charSpritePrevJ2;
        Image  n = playerNum == 1 ? charSpriteNextJ1 : charSpriteNextJ2;
        RectTransform rtC = playerNum == 1 ? rtCenterJ1 : rtCenterJ2;
        RectTransform rtP = playerNum == 1 ? rtPrevJ1   : rtPrevJ2;
        RectTransform rtN = playerNum == 1 ? rtNextJ1   : rtNextJ2;
        int  myIdx = playerNum == 1 ? indexJ1 : indexJ2;
        bool mySel = playerNum == 1 ? selectedJ1 : selectedJ2;
        bool taken = IsTaken(playerNum, myIdx);

        Vector2 sPC = rtC.anchoredPosition, sPP = rtP.anchoredPosition, sPN = rtN.anchoredPosition;
        Vector3 sSC = rtC.localScale,        sSP = rtP.localScale,        sSN = rtN.localScale;
        Color   sCC = c.color,                sCP = p.color,              sCN = n.color;

        Vector2 tPC = Vector2.zero;
        Vector2 tPP = new Vector2(-sideOffsetX, 0);
        Vector2 tPN = new Vector2( sideOffsetX, 0);
        Vector3 tSC = Vector3.one * centerScale;
        Vector3 tSP = Vector3.one * sideScale;
        Vector3 tSN = Vector3.one * sideScale;
        Color   tCC = (taken && !mySel) ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
        Color   tCP = sideColor;
        Color   tCN = sideColor;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);
            float e = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
            rtC.anchoredPosition = Vector2.LerpUnclamped(sPC, tPC, e);
            rtP.anchoredPosition = Vector2.LerpUnclamped(sPP, tPP, e);
            rtN.anchoredPosition = Vector2.LerpUnclamped(sPN, tPN, e);
            rtC.localScale = Vector3.LerpUnclamped(sSC, tSC, e);
            rtP.localScale = Vector3.LerpUnclamped(sSP, tSP, e);
            rtN.localScale = Vector3.LerpUnclamped(sSN, tSN, e);
            c.color = Color.LerpUnclamped(sCC, tCC, e);
            p.color = Color.LerpUnclamped(sCP, tCP, e);
            n.color = Color.LerpUnclamped(sCN, tCN, e);
            yield return null;
        }
        rtC.anchoredPosition = tPC; rtP.anchoredPosition = tPP; rtN.anchoredPosition = tPN;
        rtC.localScale = tSC; rtP.localScale = tSP; rtN.localScale = tSN;
        c.color = tCC; p.color = tCP; n.color = tCN;

        if (playerNum == 1) animJ1 = null; else animJ2 = null;
    }

    // ── INPUT ─────────────────────────────────────────────────────────

    void HandleSelectionInput()
    {
        if (deviceJ1 != null && !selectedJ1 && carouselRootJ1 != null && carouselRootJ1.activeSelf)
        {
            int nav = GetNav(deviceJ1, ref stickUsedJ1);
            if (nav != 0 && characters != null)
                NavigateCarousel(1, nav);
            if (IsConfirm(deviceJ1) && !(selectedJ2 && indexJ2 == indexJ1))
                ConfirmSelection(1);
        }

        if (deviceJ2 != null && !selectedJ2 && carouselRootJ2 != null && carouselRootJ2.activeSelf)
        {
            int nav = GetNav(deviceJ2, ref stickUsedJ2);
            if (nav != 0 && characters != null)
                NavigateCarousel(2, nav);
            if (IsConfirm(deviceJ2) && !(selectedJ1 && indexJ1 == indexJ2))
                ConfirmSelection(2);
        }
    }

    void ConfirmSelection(int playerNum)
    {
        if (playerNum == 1)
        {
            selectedJ1 = true;
            CharacterSelectionData.J1CharacterIndex = indexJ1;
            CharacterSelectionData.J1Data = (characters != null && indexJ1 < characters.Length) ? characters[indexJ1] : null;
        }
        else
        {
            selectedJ2 = true;
            CharacterSelectionData.J2CharacterIndex = indexJ2;
            CharacterSelectionData.J2Data = (characters != null && indexJ2 < characters.Length) ? characters[indexJ2] : null;
        }

        UpdateLabels(playerNum);
        // Met à jour le centre + labels de l'autre joueur (statut "déjà pris")
        int other = playerNum == 1 ? 2 : 1;
        Image otherC = other == 1 ? charSpriteJ1 : charSpriteJ2;
        if (otherC != null && characters != null)
        {
            int otherIdx = other == 1 ? indexJ1 : indexJ2;
            bool otherSel = other == 1 ? selectedJ1 : selectedJ2;
            bool otherTaken = IsTaken(other, otherIdx);
            otherC.color = (otherTaken && !otherSel) ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
        }
        UpdateLabels(other);

        bool bothReady = StartMenuController.IsSoloMode ? selectedJ1 : (selectedJ1 && selectedJ2);
        if (bothReady && !loadingStarted)
        {
            loadingStarted = true;
            if (carouselRootJ1 != null) carouselRootJ1.SetActive(false);
            if (carouselRootJ2 != null) carouselRootJ2.SetActive(false);
            if (GlobalPlayerManager.Instance != null)
            {
                GlobalPlayerManager.Instance.AssignPlayer(0, deviceJ1);
                if (!StartMenuController.IsSoloMode)
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
            if (s.magnitude > stickThreshold && !stickUsed) { stickUsed = true; return s.x >= 0 ? 1 : -1; }
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

    // ── VIDÉO + CHARGEMENT ────────────────────────────────────────────

    IEnumerator PlayVideosAndLoad()
    {
        bool solo = StartMenuController.IsSoloMode;

        if (videoPlayer_D1 == null || (!solo && videoPlayer_D2 == null))
        {
            SceneTransitionManager.LoadScene(nextSceneName);
            yield break;
        }

        videoContainer_D1.SetActive(true);
        videoPlayer_D1.gameObject.SetActive(true);
        videoPlayer_D1.Prepare();

        if (!solo && videoContainer_D2 != null)
        {
            videoContainer_D2.SetActive(true);
            videoPlayer_D2.gameObject.SetActive(true);
            videoPlayer_D2.Prepare();
        }

        if (solo) { while (!videoPlayer_D1.isPrepared) yield return null; }
        else      { while (!videoPlayer_D1.isPrepared || !videoPlayer_D2.isPrepared) yield return null; }

        videoDisplay_D1.texture = videoPlayer_D1.texture;
        if (!solo && videoDisplay_D2 != null) videoDisplay_D2.texture = videoPlayer_D2.texture;

        SceneMusicPlayer smp = Object.FindFirstObjectByType<SceneMusicPlayer>();
        if (smp != null) smp.FadeOut(0.5f);
        videoPlayer_D1.Play();
        if (!solo) videoPlayer_D2.Play();

        yield return new WaitForSeconds(0.5f);
        while (videoPlayer_D1.isPlaying) yield return null;

        SceneTransitionManager.LoadScene(nextSceneName);
    }
}
