using UnityEngine;
using UnityEngine.Video;

public class VidPlayer : MonoBehaviour
{
    private VideoPlayer video;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        video = GetComponent<VideoPlayer>();
        video.url = Application.streamingAssetsPath + "/Main Menu LOOP.mp4";
        video.Play();
    }
}
