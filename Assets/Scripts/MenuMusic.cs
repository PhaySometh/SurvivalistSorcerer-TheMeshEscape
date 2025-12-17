using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip menuMusic;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool playOnStart = true;
    
    [Header("Persistence")]
    public bool dontDestroyOnLoad = true; // Keep music between scenes
    
    private AudioSource audioSource;
    private static MenuMusic instance;

    void Awake()
    {
        // Singleton pattern - only one music manager
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        SetupAudioSource();
        
        if (playOnStart && menuMusic != null)
        {
            PlayMusic();
        }
        else
        {
            Debug.LogWarning("MenuMusic: No audio clip assigned or playOnStart is false!");
        }
    }

    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.clip = menuMusic;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        
        Debug.Log($"MenuMusic: AudioSource setup complete. Clip: {(menuMusic != null ? menuMusic.name : "None")}");
    }

    public void PlayMusic()
    {
        if (audioSource != null && menuMusic != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"MenuMusic: Playing {menuMusic.name}");
            }
        }
        else
        {
            Debug.LogError("MenuMusic: Cannot play - AudioSource or AudioClip is null!");
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("MenuMusic: Stopped");
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log("MenuMusic: Paused");
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
            Debug.Log("MenuMusic: Resumed");
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
            Debug.Log($"MenuMusic: Volume set to {volume}");
        }
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.volume = volume; // Reset for next time
    }
}
