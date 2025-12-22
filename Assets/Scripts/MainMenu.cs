using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private static MainMenu instance; // Singleton to keep music persistent
    private AudioSource menuMusic;

    void Awake()
    {
        // Check if there is already a MainMenu instance
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Remove duplicate
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Keep this object across scenes

        // Get or create AudioSource for menu music
        menuMusic = GetComponent<AudioSource>();
        if (menuMusic == null)
        {
            menuMusic = gameObject.AddComponent<AudioSource>();
            // Assign your music clip here in Inspector or dynamically
        }

        menuMusic.loop = true;
        menuMusic.playOnAwake = true;

        if (!menuMusic.isPlaying)
            menuMusic.Play();
    }

    // Load the game scene (Level 1 - Village)
    public void PlayGame()
    {
        // Stop menu music when starting the real game
        if (menuMusic != null && menuMusic.isPlaying)
            menuMusic.Stop();

        SceneManager.LoadScene("VilageMapScene");
    }

    // Load Level 2 - Angkor Wat
    public void LoadLevel2()
    {
        SceneManager.LoadScene("Map2_AngkorWat");
    }

    // Return to Main Menu
    public void BackToMenu()
    {
        SceneManager.LoadScene("MenuScence");
    }

    // Open settings menu
    public void OpenSettings()
    {
        Debug.Log("Settings opened");
        // Add your settings logic here
    }

    // Quit the game
    public void QuitGame()
    {
        Debug.Log("Quit game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
