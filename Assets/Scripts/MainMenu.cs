using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public bool useLoadingScreen = true;

    // Load the game scene (Level 1 - Village)
    public void PlayGame()
    {
        if (useLoadingScreen)
        {
            PlayerPrefs.SetString("SceneToLoad", "VilageMapScene");
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            SceneManager.LoadScene("VilageMapScene");
        }
    }

    // Load Level 2 - Angkor Wat
    public void LoadLevel2()
    {
        if (useLoadingScreen)
        {
            PlayerPrefs.SetString("SceneToLoad", "Map2_AngkorWat");
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            SceneManager.LoadScene("Map2_AngkorWat");
        }
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
