using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScreen : MonoBehaviour
{
    [Header("Settings Reference")]
    public SettingsManager settingsManager;
    
    [Header("Menu Buttons (Optional - Auto-finds if not assigned)")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    
    void Start()
    {
        Debug.Log("=== MenuScreen: Start() called ===");
        StartCoroutine(DelayedInitialize());
    }
    
    void OnEnable()
    {
        Debug.Log("=== MenuScreen: OnEnable() called ===");
        // Re-initialize when scene becomes active (e.g., returning from game)
        StartCoroutine(DelayedInitialize());
    }
    
    IEnumerator DelayedInitialize()
    {
        // Wait a frame to ensure everything is loaded
        yield return null;
        InitializeSettings();
    }
    
    void InitializeSettings()
    {
        Debug.Log("=== MenuScreen: InitializeSettings() called ===");
        
        // Show cursor and unlock (like PauseManager does)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Reset time scale in case it was paused
        Time.timeScale = 1f;
        
        // Ensure GameSettings exists
        if (GameSettings.Instance == null)
        {
            GameObject settingsObj = new GameObject("GameSettings");
            settingsObj.AddComponent<GameSettings>();
            Debug.Log("Created new GameSettings instance");
        }
        
        // ALWAYS try to find SettingsManager (it might have been destroyed)
        settingsManager = FindObjectOfType<SettingsManager>();
        
        if (settingsManager == null)
        {
            Debug.LogError("✗✗✗ SettingsManager NOT FOUND in scene! Settings button won't work!");
        }
        else
        {
            Debug.Log("✓✓✓ SettingsManager found: " + settingsManager.name);
        }
        
        // Setup button listeners
        SetupButtons();
    }
    
    void SetupButtons()
    {
        Debug.Log("=== MenuScreen: SetupButtons() called ===");
        
        // Setup Play button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
            playButton.interactable = true;
            Debug.Log("✓✓✓ Play button listener added and enabled!");
        }
        else
        {
            Debug.LogError("✗✗✗ Play button is NULL! Assign it in Inspector!");
        }
        
        // Setup Settings button
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
            settingsButton.interactable = true;
            Debug.Log("✓✓✓ Settings button listener added and enabled!");
        }
        else
        {
            Debug.LogError("✗✗✗ Settings button is NULL! Assign it in Inspector!");
        }
        
        // Setup Quit button
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            quitButton.interactable = true;
            Debug.Log("✓✓✓ Quit button listener added and enabled!");
        }
        else
        {
            Debug.LogError("✗✗✗ Quit button is NULL! Assign it in Inspector!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method to load the loading scene when play button is clicked
    public void PlayGame()
    {
        Debug.Log("Play button clicked!");
        
        // Get the selected map from GameSettings
        string sceneToLoad = "VilageMapScene"; // Default
        if (GameSettings.Instance != null)
        {
            sceneToLoad = GameSettings.Instance.GetMapSceneName();
            Debug.Log($"Loading selected map: {sceneToLoad}");
        }
        
        // Set the target scene for the loading screen
        PlayerPrefs.SetString("SceneToLoad", sceneToLoad);
        PlayerPrefs.Save();
        
        // Load the loading scene directly
        try
        {
            SceneManager.LoadScene("LoadingScene");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load LoadingScene: {e.Message}");
            // Fallback: Load game scene directly
            SceneManager.LoadScene(sceneToLoad);
        }
    }
    
    // Quit game - same approach as PauseManager
    public void QuitGame()
    {
        Debug.Log("Quit button clicked!");
        
        // Check if SceneTransitionManager exists
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.QuitGame();
        }
        else
        {
            // Fallback: Quit directly
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    // Open settings panel
    public void OpenSettings()
    {
        Debug.Log(">>> OpenSettings() called!");
        
        // ALWAYS try to find SettingsManager fresh
        if (settingsManager == null)
        {
            Debug.Log("SettingsManager was null, searching...");
            settingsManager = FindObjectOfType<SettingsManager>();
        }
        
        if (settingsManager != null)
        {
            Debug.Log("Found SettingsManager: " + settingsManager.name + ", calling OpenSettings()");
            settingsManager.OpenSettings();
        }
        else
        {
            Debug.LogError("✗✗✗ CRITICAL: SettingsManager NOT FOUND! Make sure SettingsManager GameObject exists in the menu scene!");
            
            // List all objects in scene for debugging
            SettingsManager[] allManagers = FindObjectsOfType<SettingsManager>(true);
            Debug.Log("Found " + allManagers.Length + " SettingsManager(s) in scene (including inactive)");
            foreach (var mgr in allManagers)
            {
                Debug.Log("  - " + mgr.name + " (active: " + mgr.gameObject.activeInHierarchy + ")");
            }
        }
    }
}
