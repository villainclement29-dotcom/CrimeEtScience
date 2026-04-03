using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoTransitionHandler : MonoBehaviour
{
    [Header("Configuration Écran 1 (Display 1)")]
    public GameObject videoContainer_D1;
    public RawImage videoDisplay_D1;
    private VideoPlayer videoPlayer_D1;

    [Header("Configuration Écran 2 (Display 2)")]
    public GameObject videoContainer_D2;
    public RawImage videoDisplay_D2;
    private VideoPlayer videoPlayer_D2;

    private bool isTransitioning = false;

    void Awake()
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
    }

    public void StartTransition(string sceneName)
    {
        if (isTransitioning) return;

        // Vérification de sécurité : si un composant manque sur l'un des deux côtés
        if (videoPlayer_D1 == null || videoPlayer_D2 == null || videoDisplay_D1 == null || videoDisplay_D2 == null)
        {
            Debug.LogWarning($"[Transition] Composants manquants pour le double écran sur {gameObject.name}. Chargement direct.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(PlayVideosThenLoad(sceneName));
    }

    private IEnumerator PlayVideosThenLoad(string sceneName)
    {
        isTransitioning = true;

        // 1. Activer les deux arborescences
        videoContainer_D1.SetActive(true);
        videoContainer_D2.SetActive(true);
        videoPlayer_D1.gameObject.SetActive(true);
        videoPlayer_D2.gameObject.SetActive(true);

        // 2. Préparer les deux moteurs vidéo en même temps
        videoPlayer_D1.Prepare();
        videoPlayer_D2.Prepare();

        // On attend que les DEUX soient prêts pour garantir la synchro
        while (!videoPlayer_D1.isPrepared || !videoPlayer_D2.isPrepared)
        {
            yield return null;
        }

        // 3. Lier les textures aux deux RawImages
        videoDisplay_D1.texture = videoPlayer_D1.texture;
        videoDisplay_D2.texture = videoPlayer_D2.texture;

        // 4. Lancer la lecture simultanée
        videoPlayer_D1.Play();
        videoPlayer_D2.Play();

        // 5. Petite pause technique
        yield return new WaitForSeconds(0.2f);

        // 6. Attendre la fin (on se base sur le premier lecteur)
        while (videoPlayer_D1.isPlaying)
        {
            yield return null;
        }

        // 7. Changer de scène
        SceneManager.LoadScene(sceneName);
    }
}