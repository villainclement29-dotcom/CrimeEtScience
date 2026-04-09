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

    // ── Devices ──────────────────────────────────────────────────────
    private InputDevice deviceJ1;
    private InputDevice deviceJ2;
    private bool loadingStarted = false;

    // ── Sélection ────────────────────────────────────────────────────
    private int  indexJ1 = 0, indexJ2 = 0;
    private bool selectedJ1 = false, selectedJ2 = false;
    private bool stickUsedJ1 = false, stickUsedJ2 = false;
    private const float stickThreshold = 0.5f;

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

        // Cache les carousels au démarrage
        if (carouselRootJ1 != null) carouselRootJ1.SetActive(false);
        if (carouselRootJ2 != null) carouselRootJ2.SetActive(false);
    }

    void Update()
    {
        if (loadingStarted) return;

        // ── Assignation par index de manette ─────────────────────────
        if (deviceJ1 == null)
        {
            if (Gamepad.all.Count > 0)       deviceJ1 = Gamepad.all[0];
            else if (Keyboard.current != null) deviceJ1 = Keyboard.current;
            if (deviceJ1 != null) ShowCarousel(1);
        }
        if (deviceJ2 == null)
        {
            if (Gamepad.all.Count > 1)                                          deviceJ2 = Gamepad.all[1];
            else if (deviceJ1 is Gamepad && Keyboard.current != null)           deviceJ2 = Keyboard.current;
            if (deviceJ2 != null) ShowCarousel(2);
        }

        HandleSelectionInput();
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
        int    myIdx    = playerNum == 1 ? indexJ1    : indexJ2;
        int    otherIdx = playerNum == 1 ? indexJ2    : indexJ1;
        bool   otherSel = playerNum == 1 ? selectedJ2 : selectedJ1;
        bool   mySel    = playerNum == 1 ? selectedJ1 : selectedJ2;
        Image  img      = playerNum == 1 ? charSpriteJ1 : charSpriteJ2;
        TextMeshProUGUI nm = playerNum == 1 ? charNameJ1   : charNameJ2;
        TextMeshProUGUI st = playerNum == 1 ? charStatusJ1 : charStatusJ2;

        if (img == null) return;

        bool taken = otherSel && otherIdx == myIdx;
        CharacterData cd = (characters != null && myIdx < characters.Length) ? characters[myIdx] : null;

        // Sprite
        if (cd != null && cd.spriteCarousel != null)
        {
            img.sprite = cd.spriteCarousel;
            img.color  = taken && !mySel ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
        }

        // Nom
        if (nm != null)
        {
            nm.text  = (cd != null && !string.IsNullOrEmpty(cd.characterName)) ? cd.characterName : $"Personnage {myIdx + 1}";
            nm.color = taken && !mySel ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }

        // Statut
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

    // ── INPUT ─────────────────────────────────────────────────────────

    void HandleSelectionInput()
    {
        if (deviceJ1 != null && !selectedJ1 && carouselRootJ1 != null && carouselRootJ1.activeSelf)
        {
            int nav = GetNav(deviceJ1, ref stickUsedJ1);
            if (nav != 0 && characters != null)
            {
                indexJ1 = (indexJ1 + nav + characters.Length) % characters.Length;
                RefreshCarousel(1);
                if (carouselRootJ2 != null && carouselRootJ2.activeSelf) RefreshCarousel(2);
            }
            if (IsConfirm(deviceJ1) && !(selectedJ2 && indexJ2 == indexJ1))
                ConfirmSelection(1);
        }

        if (deviceJ2 != null && !selectedJ2 && carouselRootJ2 != null && carouselRootJ2.activeSelf)
        {
            int nav = GetNav(deviceJ2, ref stickUsedJ2);
            if (nav != 0 && characters != null)
            {
                indexJ2 = (indexJ2 + nav + characters.Length) % characters.Length;
                RefreshCarousel(2);
                if (carouselRootJ1 != null && carouselRootJ1.activeSelf) RefreshCarousel(1);
            }
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

        RefreshCarousel(playerNum);
        RefreshCarousel(playerNum == 1 ? 2 : 1);

        if (selectedJ1 && selectedJ2 && !loadingStarted)
        {
            loadingStarted = true;
            if (carouselRootJ1 != null) carouselRootJ1.SetActive(false);
            if (carouselRootJ2 != null) carouselRootJ2.SetActive(false);
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

        SceneMusicPlayer smp = Object.FindFirstObjectByType<SceneMusicPlayer>();
        if (smp != null) smp.FadeOut(0.5f);
        videoPlayer_D1.Play();
        videoPlayer_D2.Play();

        yield return new WaitForSeconds(0.5f);
        while (videoPlayer_D1.isPlaying) yield return null;

        SceneManager.LoadScene(nextSceneName);
    }
}
