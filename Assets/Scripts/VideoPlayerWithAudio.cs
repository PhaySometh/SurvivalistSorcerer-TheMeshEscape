using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoPlayerWithAudio : MonoBehaviour
{
    public RawImage displayImage;
    public string videoFileName = "Game_Loading_Screen_Video_Prompt";
    
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;

    void Start()
    {
        SetupVideoPlayer();
    }

    void SetupVideoPlayer()
    {
        // Get or add components
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Load video from Resources or path
        videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName + ".mp4");
        
        // OR if using Resources folder:
        // videoPlayer.clip = Resources.Load<VideoClip>(videoFileName);
        
        // Setup video player
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create render texture
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = renderTexture;
        
        if (displayImage != null)
        {
            displayImage.texture = renderTexture;
        }

        // Setup audio
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        
        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;
        
        // Prepare and play
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        
        Debug.Log("VideoPlayerWithAudio: Setup initiated");
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"Video prepared! Audio tracks: {vp.audioTrackCount}");
        
        if (vp.audioTrackCount == 0)
        {
            Debug.LogError("ERROR: Video has NO audio tracks detected by Unity!");
        }
        else
        {
            Debug.Log("Audio tracks detected. Playing video with sound...");
        }
        
        vp.Play();
    }
}
