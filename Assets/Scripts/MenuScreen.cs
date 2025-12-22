using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method to load the loading scene when play button is clicked
    public void PlayGame()
    {
        // Set the target scene for the loading screen
        PlayerPrefs.SetString("SceneToLoad", "VilageMapScene");
        PlayerPrefs.Save();
        
        // Load the loading scene
        SceneManager.LoadScene("LoadingScene");
    }
    
    // Optional: Quit game
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
    
    // Optional: Open settings
    public void OpenSettings()
    {
        Debug.Log("Open settings menu");
        // Add your settings menu logic here
    }
}
