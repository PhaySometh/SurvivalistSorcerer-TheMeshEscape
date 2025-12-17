using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class SimpleVideoPlayer : MonoBehaviour
{
    public VideoClip videoClip;
    public RawImage displayImage;
    [Range(0f, 1f)]
    public float volume = 1f;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;

    void Start()
    {
        // Setup Video Player
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create render texture
        RenderTexture rt = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = rt;
        if (displayImage != null)
        {
            displayImage.texture = rt;
        }

        // Setup Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        // Link audio to video
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        
        // Prepare and play
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("Video prepared! Playing with audio...");
        Debug.Log($"Audio tracks: {videoPlayer.audioTrackCount}");
        videoPlayer.Play();
        
        if (videoPlayer.audioTrackCount == 0)
        {
            Debug.LogWarning("WARNING: Video has no audio tracks!");
        }
    }
}
