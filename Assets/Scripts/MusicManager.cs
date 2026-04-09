using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip music;
    }

    [Header("Musiques par scène")]
    public SceneMusic[] sceneMusics;

    [Header("Paramètres musique")]
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeDuration = 1f;

    [Header("Voix off par scène")]
    public SceneMusic[] sceneVoiceOvers;
    [Range(0f, 1f)] public float voiceOverVolume = 0.9f;
    public float voiceOverDelay = 0.5f;

    private AudioSource musicSource;
    private AudioSource voiceOverSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource[] sources = GetComponents<AudioSource>();
        musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        voiceOverSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        musicSource.loop        = true;
        musicSource.playOnAwake = false;
        musicSource.volume      = volume;

        voiceOverSource.loop        = false;
        voiceOverSource.playOnAwake = false;
        voiceOverSource.volume      = voiceOverVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip musicClip = FindClip(sceneMusics, scene.name);
        AudioClip voClip    = FindClip(sceneVoiceOvers, scene.name);

        if (musicClip != null)
            SwitchMusic(musicClip);
        else
            StopMusic();

        voiceOverSource.Stop();
        if (voClip != null)
            StartCoroutine(PlayVoiceOverDelayed(voClip));
    }

    AudioClip FindClip(SceneMusic[] list, string sceneName)
    {
        if (list == null) return null;
        foreach (var e in list)
            if (e.sceneName == sceneName && e.music != null) return e.music;
        return null;
    }

    void SwitchMusic(AudioClip clip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // Même clip déjà en cours : reprend juste le volume
        if (musicSource.clip == clip)
        {
            if (!musicSource.isPlaying) musicSource.Play();
            fadeCoroutine = StartCoroutine(FadeIn());
            return;
        }

        // Nouveau clip : fade out puis swap
        if (musicSource.isPlaying)
            fadeCoroutine = StartCoroutine(FadeOutThenPlay(clip));
        else
            PlayDirect(clip);
    }

    void PlayDirect(AudioClip clip)
    {
        musicSource.clip   = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        float start = musicSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            if (musicSource == null) yield break;
            musicSource.volume = Mathf.Lerp(start, volume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = volume;
        fadeCoroutine = null;
    }

    IEnumerator FadeOut()
    {
        float start = musicSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            if (musicSource == null) yield break;
            musicSource.volume = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        fadeCoroutine = null;
    }

    IEnumerator FadeOutThenPlay(AudioClip clip)
    {
        yield return FadeOut();
        PlayDirect(clip);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    IEnumerator PlayVoiceOverDelayed(AudioClip clip)
    {
        yield return new WaitForSeconds(voiceOverDelay);
        voiceOverSource.clip   = clip;
        voiceOverSource.volume = voiceOverVolume;
        voiceOverSource.Play();
    }
}
