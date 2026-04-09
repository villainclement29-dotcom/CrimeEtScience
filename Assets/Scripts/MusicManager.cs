using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Musiques")]
    public AudioClip lobbyMusic;
    public AudioClip mainSceneMusic;

    [Header("Paramètres")]
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeDuration = 1f;

    [Header("Noms de scènes")]
    public string lobbySceneName = "Lobby";
    public string mainSceneName  = "MainScene";

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop   = true;
        audioSource.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip clip = null;
        if (scene.name == lobbySceneName)    clip = lobbyMusic;
        else if (scene.name == mainSceneName) clip = mainSceneMusic;

        if (clip != null) PlayMusic(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(clip));
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeTo(AudioClip clip)
    {
        // Fade out
        float start = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;

        // Swap clip
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, volume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = volume;
        fadeCoroutine = null;
    }

    System.Collections.IEnumerator FadeOut()
    {
        float start = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();
        fadeCoroutine = null;
    }
}
