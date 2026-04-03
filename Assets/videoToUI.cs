using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoToUI : MonoBehaviour
{
    private RawImage rawImage;
    private VideoPlayer videoPlayer;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null && rawImage != null)
        {
            // On prépare la vidéo pour récupérer sa texture
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += Prepared;
        }
    }

    void Prepared(VideoPlayer source)
    {
        // On donne la texture de la vidéo à la Raw Image
        rawImage.texture = source.texture;
        videoPlayer.Play();
    }
}