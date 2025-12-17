using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoBackground : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip backgroundVideo;
    public RawImage targetImage; // Drag your RawImage here
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float videoVolume = 1.0f;
    public bool muteVideo = false;
    
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;

    void Start()
    {
        // Set up video player
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip = backgroundVideo;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = true;
        
        // Create high-quality render texture with proper color space
        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 1;
        renderTexture.Create();
        
        // Set video to render to texture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        
        // Enable audio - Try AudioSource mode for better compatibility
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        
        // Create AudioSource for video audio
        if (videoPlayer.audioTrackCount > 0)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = videoVolume;
            audioSource.mute = muteVideo;
            
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.EnableAudioTrack(0, true);
            
            Debug.Log($"VideoBackground: Audio enabled on track 0, volume: {videoVolume}");
        }
        else
        {
            Debug.LogWarning("VideoBackground: Video has no audio tracks!");
        }
        
        // Apply to UI Raw Image
        if (targetImage != null)
        {
            targetImage.texture = renderTexture;
        }
        
        videoPlayer.Play();
        
        Debug.Log("VideoBackground: Video started with audio enabled");
    }

    void OnDestroy()
    {
        // Clean up
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
