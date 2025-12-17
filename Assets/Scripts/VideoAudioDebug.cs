using UnityEngine;
using UnityEngine.Video;

public class VideoAudioDebug : MonoBehaviour
{
    void Start()
    {
        VideoPlayer vp = GetComponent<VideoPlayer>();
        
        if (vp == null)
        {
            Debug.LogError("No VideoPlayer found!");
            return;
        }

        Debug.Log("=== VIDEO AUDIO DEBUG ===");
        Debug.Log($"Video Clip: {(vp.clip != null ? vp.clip.name : "NULL")}");
        Debug.Log($"Audio Track Count: {vp.audioTrackCount}");
        Debug.Log($"Audio Output Mode: {vp.audioOutputMode}");
        
        if (vp.audioTrackCount > 0)
        {
            for (ushort i = 0; i < vp.audioTrackCount; i++)
            {
                Debug.Log($"Track {i} - Muted: {vp.GetDirectAudioMute(i)}, Volume: {vp.GetDirectAudioVolume(i)}");
            }
        }
        else
        {
            Debug.LogWarning("Video has NO audio tracks!");
        }
        
        // Check for Audio Listener
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("No Audio Listener found in scene!");
        }
        else
        {
            Debug.Log($"Audio Listener found on: {listener.gameObject.name}");
        }
    }
}
