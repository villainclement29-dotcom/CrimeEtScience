using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    [Header("Paramètres de Test")]
    public bool testMode = false;

    [Header("UI Status")]
    public TextMeshProUGUI statusTextJ1_D1;
    public TextMeshProUGUI statusTextJ2_D1;
    public TextMeshProUGUI statusTextJ1_D2;
    public TextMeshProUGUI statusTextJ2_D2;

    [Header("Vidéos de Transition (Double Écran)")]
    public GameObject videoContainer_D1;
    public RawImage videoDisplay_D1;
    private VideoPlayer videoPlayer_D1;

    [Space]
    public GameObject videoContainer_D2;
    public RawImage videoDisplay_D2;
    private VideoPlayer videoPlayer_D2;

    [Header("Navigation")]
    public string nextSceneName = "MainScene";
    public float vitesseClignotement = 3f;

    private InputDevice deviceJ1;
    private InputDevice deviceJ2;
    private bool loadingStarted = false;

    void Start()
    {
        // Initialisation Écran 1
        if (videoContainer_D1 != null)
        {
            videoPlayer_D1 = videoContainer_D1.GetComponentInChildren<VideoPlayer>(true);
            videoContainer_D1.SetActive(false);
        }

        // Initialisation Écran 2
        if (videoContainer_D2 != null)
        {
            videoPlayer_D2 = videoContainer_D2.GetComponentInChildren<VideoPlayer>(true);
            videoContainer_D2.SetActive(false);
        }

        // Initialisation Textes
        string attenteMsg = "<color=white>APPUYEZ SUR UNE TOUCHE...</color>";
        if (statusTextJ1_D1) { statusTextJ1_D1.text = "J1 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ1_D1)); }
        if (statusTextJ1_D2) { statusTextJ1_D2.text = "J1 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ1_D2)); }
        if (statusTextJ2_D1) { statusTextJ2_D1.text = "J2 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ2_D1)); }
        if (statusTextJ2_D2) { statusTextJ2_D2.text = "J2 : " + attenteMsg; StartCoroutine(FlashText(statusTextJ2_D2)); }
    }

    void Update()
    {
        if (loadingStarted) return;

        if (deviceJ1 == null)
        {
            DetectDevice(out deviceJ1, null);
            if (deviceJ1 != null) UpdatePlayerUI(1, deviceJ1.displayName);
        }
        else if (deviceJ2 == null)
        {
            DetectDevice(out deviceJ2, deviceJ1);
            if (deviceJ2 != null) UpdatePlayerUI(2, deviceJ2.displayName);
        }

        if (deviceJ1 != null && deviceJ2 != null && !loadingStarted)
        {
            loadingStarted = true;
            SaveAndPrepare();
        }
    }

    void SaveAndPrepare()
    {
        if (GlobalPlayerManager.Instance != null)
        {
            GlobalPlayerManager.Instance.AssignPlayer(0, deviceJ1);
            GlobalPlayerManager.Instance.AssignPlayer(1, deviceJ2);
        }
        StartCoroutine(PlayVideosAndLoad());
    }

    IEnumerator PlayVideosAndLoad()
    {
        // Sécurité si les composants sont manquants
        if (videoPlayer_D1 == null || videoPlayer_D2 == null)
        {
            Debug.LogWarning("Composants vidéo manquants sur l'un des écrans. Chargement direct.");
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        // 1. Activer les deux containers
        videoContainer_D1.SetActive(true);
        videoContainer_D2.SetActive(true);
        videoPlayer_D1.gameObject.SetActive(true);
        videoPlayer_D2.gameObject.SetActive(true);

        // 2. Préparer les deux vidéos simultanément
        videoPlayer_D1.Prepare();
        videoPlayer_D2.Prepare();

        // On attend que les DEUX soient prêtes
        while (!videoPlayer_D1.isPrepared || !videoPlayer_D2.isPrepared)
        {
            yield return null;
        }

        // 3. Assigner les textures aux RawImages respectives
        videoDisplay_D1.texture = videoPlayer_D1.texture;
        videoDisplay_D2.texture = videoPlayer_D2.texture;

        // 4. Lancer les deux vidéos
        videoPlayer_D1.Play();
        videoPlayer_D2.Play();

        yield return new WaitForSeconds(0.5f);

        // 5. Attendre la fin (on se base sur la vidéo 1 pour le timing)
        while (videoPlayer_D1.isPlaying)
        {
            yield return null;
        }

        // 6. Chargement final
        SceneManager.LoadScene(nextSceneName);
    }

    // --- UTILS ---
    void DetectDevice(out InputDevice foundDevice, InputDevice excluded)
    {
        foundDevice = null;
        foreach (var device in InputSystem.devices)
        {
            if (device == excluded) continue;
            bool isValid = device is Gamepad || device is Keyboard || device is Mouse;
            if (isValid && CheckAnyButton(device)) { foundDevice = device; return; }
        }
    }

    bool CheckAnyButton(InputDevice device)
    {
        if (device is Gamepad pad) return pad.allControls[0].IsPressed() || pad.buttonSouth.wasPressedThisFrame;
        if (device is Keyboard kb) return kb.anyKey.wasPressedThisFrame;
        if (device is Mouse m) return m.leftButton.wasPressedThisFrame;
        return false;
    }

    void UpdatePlayerUI(int num, string name)
    {
        string msg = $"<color=green>J{num} : {name} CONNECTÉ</color>";
        if (num == 1) { if (statusTextJ1_D1) statusTextJ1_D1.text = msg; if (statusTextJ1_D2) statusTextJ1_D2.text = msg; }
        else { if (statusTextJ2_D1) statusTextJ2_D1.text = msg; if (statusTextJ2_D2) statusTextJ2_D2.text = msg; }
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
}