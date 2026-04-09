using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SceneMusicPlayer : MonoBehaviour
{
    [Header("Musique")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeInDuration = 1f;

    [Header("Voix off (optionnelle)")]
    public AudioClip voiceOverClip;
    [Range(0f, 1f)] public float voiceOverVolume = 0.9f;
    public float voiceOverDelay = 0.5f;

    private AudioSource musicSource;
    private AudioSource voiceOverSource;

    void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();

        musicSource.clip        = musicClip;
        musicSource.loop        = true;
        musicSource.playOnAwake = false;
        musicSource.volume      = 0f;

        if (voiceOverClip != null)
        {
            voiceOverSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            voiceOverSource.loop        = false;
            voiceOverSource.playOnAwake = false;
            voiceOverSource.volume      = voiceOverVolume;
        }
    }

    void Start()
    {
        if (musicClip != null)
        {
            musicSource.Play();
            StartCoroutine(FadeIn());
        }

        if (voiceOverClip != null)
            StartCoroutine(PlayVoiceOverDelayed());
    }

    public void FadeOut(float duration = -1f)
    {
        if (duration < 0) duration = fadeInDuration;
        StartCoroutine(FadeOutCoroutine(duration));
    }

    IEnumerator FadeIn()
    {
        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, volume, t / fadeInDuration);
            yield return null;
        }
        musicSource.volume = volume;
    }

    IEnumerator FadeOutCoroutine(float duration)
    {
        float start = musicSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
    }

    IEnumerator PlayVoiceOverDelayed()
    {
        yield return new WaitForSeconds(voiceOverDelay);
        voiceOverSource.clip = voiceOverClip;
        voiceOverSource.Play();
    }
}
