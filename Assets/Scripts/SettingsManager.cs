using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the settings panel UI
/// Shows difficulty options and allows player to customize game settings
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;
    
    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button defaultButton;
    
    [Header("Map Buttons")]
    public Button villageMapButton;
    public Button level2MapButton;
    
    [Header("Info Display")]
    public TextMeshProUGUI difficultyInfoText;
    public TextMeshProUGUI currentDifficultyText;
    public TextMeshProUGUI currentMapText;
    
    [Header("Control Buttons")]
    public Button closeButton;
    public Button applyButton;
    
    private GameSettings.Difficulty selectedDifficulty;
    private GameSettings.GameMap selectedMap;
    
    void Start()
    {
        InitializeSettings();
    }
    
    void OnEnable()
    {
        // Re-initialize when enabled (e.g., returning to menu scene)
        InitializeSettings();
    }
    
    void InitializeSettings()
    {
        // Initialize
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // Load current difficulty and map
        if (GameSettings.Instance != null)
        {
            selectedDifficulty = GameSettings.Instance.currentDifficulty;
            selectedMap = GameSettings.Instance.currentMap;
        }
        else
        {
            selectedDifficulty = GameSettings.Difficulty.Medium;
            selectedMap = GameSettings.GameMap.VillageMap;
        }
        
        // Setup button listeners
        if (easyButton != null)
        {
            easyButton.onClick.RemoveAllListeners();
            easyButton.onClick.AddListener(() => SelectDifficulty(GameSettings.Difficulty.Easy));
        }
        
        if (mediumButton != null)
        {
            mediumButton.onClick.RemoveAllListeners();
            mediumButton.onClick.AddListener(() => SelectDifficulty(GameSettings.Difficulty.Medium));
        }
        
        if (hardButton != null)
        {
            hardButton.onClick.RemoveAllListeners();
            hardButton.onClick.AddListener(() => SelectDifficulty(GameSettings.Difficulty.Hard));
        }
        
        if (defaultButton != null)
        {
            defaultButton.onClick.RemoveAllListeners();
            defaultButton.onClick.AddListener(() => SelectDifficulty(GameSettings.Difficulty.Default));
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }
        
        if (applyButton != null)
        {
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(ApplySettings);
        }
        
        // Setup map button listeners
        if (villageMapButton != null)
        {
            villageMapButton.onClick.RemoveAllListeners();
            villageMapButton.onClick.AddListener(() => SelectMap(GameSettings.GameMap.VillageMap));
        }
        
        if (level2MapButton != null)
        {
            level2MapButton.onClick.RemoveAllListeners();
            level2MapButton.onClick.AddListener(() => SelectMap(GameSettings.GameMap.Level2Map));
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Open the settings panel
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            
            // Load current settings
            if (GameSettings.Instance != null)
            {
                selectedDifficulty = GameSettings.Instance.currentDifficulty;
                selectedMap = GameSettings.Instance.currentMap;
            }
            
            UpdateUI();
            
            Debug.Log("Settings panel opened");
        }
    }
    
    /// <summary>
    /// Close the settings panel
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("Settings panel closed");
        }
    }
    
    /// <summary>
    /// Select a difficulty level
    /// </summary>
    void SelectDifficulty(GameSettings.Difficulty difficulty)
    {
        selectedDifficulty = difficulty;
        UpdateUI();
        Debug.Log($"Selected difficulty: {difficulty}");
    }
    
    /// <summary>
    /// Select a map
    /// </summary>
    void SelectMap(GameSettings.GameMap map)
    {
        selectedMap = map;
        UpdateUI();
        Debug.Log($"Selected map: {map}");
    }
    
    /// <summary>
    /// Apply and save the selected settings
    /// </summary>
    void ApplySettings()
    {
        // Ensure GameSettings exists
        if (GameSettings.Instance == null)
        {
            // Create it if it doesn't exist
            GameObject settingsObj = new GameObject("GameSettings");
            settingsObj.AddComponent<GameSettings>();
        }
        
        // Apply the difficulty and map
        GameSettings.Instance.SetDifficulty(selectedDifficulty);
        GameSettings.Instance.SetMap(selectedMap);
        
        Debug.Log($"Settings applied! Difficulty: {selectedDifficulty}, Map: {selectedMap}");
        
        // Show feedback with coroutine
        StartCoroutine(ShowSaveConfirmation());
    }
    
    /// <summary>
    /// Show save confirmation feedback
    /// </summary>
    System.Collections.IEnumerator ShowSaveConfirmation()
    {
        // Show "SAVED!" message
        if (currentDifficultyText != null)
        {
            string originalText = currentDifficultyText.text;
            currentDifficultyText.text = $"SAVED! Difficulty: {selectedDifficulty}";
            
            // Change color to green if possible
            Color originalColor = currentDifficultyText.color;
            currentDifficultyText.color = new Color(0.2f, 1f, 0.2f); // Green
            
            // Wait 2 seconds
            yield return new WaitForSeconds(2f);
            
            // Restore original
            currentDifficultyText.text = $"Current: {selectedDifficulty}";
            currentDifficultyText.color = originalColor;
        }
        
        if (currentMapText != null)
        {
            currentMapText.text = $"Current Map: {GameSettings.Instance.GetMapDisplayName()}";
        }
        
        // Optional: Close panel after showing confirmation
        // yield return new WaitForSeconds(0.5f);
        // CloseSettings();
    }
    
    /// <summary>
    /// Update the UI based on current selection
    /// </summary>
    void UpdateUI()
    {
        // Update difficulty info
        if (difficultyInfoText != null)
        {
            difficultyInfoText.text = GetDifficultyDescription(selectedDifficulty);
        }
        
        // Update current difficulty display
        if (currentDifficultyText != null)
        {
            currentDifficultyText.text = $"Current: {selectedDifficulty}";
        }
        
        // Update current map display
        if (currentMapText != null)
        {
            string mapName = selectedMap == GameSettings.GameMap.VillageMap ? "Village Map" : "Level 2";
            currentMapText.text = $"Current Map: {mapName}";
        }
        
        // Highlight selected buttons
        HighlightButton(selectedDifficulty);
        HighlightMapButton(selectedMap);
    }
    
    /// <summary>
    /// Highlight the selected difficulty button
    /// </summary>
    void HighlightButton(GameSettings.Difficulty difficulty)
    {
        // Reset all buttons
        ResetButtonColor(easyButton);
        ResetButtonColor(mediumButton);
        ResetButtonColor(hardButton);
        ResetButtonColor(defaultButton);
        
        // Highlight selected
        switch (difficulty)
        {
            case GameSettings.Difficulty.Easy:
                SetButtonHighlight(easyButton);
                break;
            case GameSettings.Difficulty.Medium:
                SetButtonHighlight(mediumButton);
                break;
            case GameSettings.Difficulty.Hard:
                SetButtonHighlight(hardButton);
                break;
            case GameSettings.Difficulty.Default:
                SetButtonHighlight(defaultButton);
                break;
        }
    }
    
    void SetButtonHighlight(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f); // Green highlight
            button.colors = colors;
        }
    }
    
    void ResetButtonColor(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// Highlight the selected map button
    /// </summary>
    void HighlightMapButton(GameSettings.GameMap map)
    {
        // Reset all map buttons
        ResetButtonColor(villageMapButton);
        ResetButtonColor(level2MapButton);
        
        // Highlight selected
        switch (map)
        {
            case GameSettings.GameMap.VillageMap:
                SetButtonHighlight(villageMapButton);
                break;
            case GameSettings.GameMap.Level2Map:
                SetButtonHighlight(level2MapButton);
                break;
        }
    }
    
    /// <summary>
    /// Get detailed description for each difficulty
    /// </summary>
    string GetDifficultyDescription(GameSettings.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case GameSettings.Difficulty.Easy:
                return "EASY MODE \n\n" +
                       "• Only Wave 1\n" +
                       "• Time Limit: 3 Minutes\n" +
                       "• Enemy Strength: 80%\n" +
                       "• Enemy Damage: 80%\n" +
                       "• No Boss Fight\n\n" +
                       "Perfect for learning the game!";
                       
            case GameSettings.Difficulty.Medium:
                return "MEDIUM MODE \n\n\n" +
                       "• Waves 1, 2, and 3\n" +
                       "• Time Limit: 8 Minutes\n" +
                       "• Wave Duration: 2 Min\n" +
                       "• Rest Time: 15 Seconds\n" +
                       "• No Boss Fight\n\n" +
                       "Balanced gameplay experience!";
                       
            case GameSettings.Difficulty.Hard:
                return "HARD MODE \n\n" +
                       "• Wave 5 ONLY (All Enemies + Boss)\n" +
                       "• Time Limit: 1 Minute\n" +
                       "• SUDDEN DEATH from start\n" +
                       "• Collect 50 Coins to Win\n" +
                       "• Enemy Strength: 120%\n" +
                       "• Enemy Damage: 120%\n\n" +
                       "Extreme challenge for experts!";
                                   case GameSettings.Difficulty.Default:
                return "DEFAULT MODE \n\n" +
                       "• All 5 Waves\n" +
                       "• Time Limit: 10 Minutes\n" +
                       "• Wave Duration: 2 Min\n" +
                       "• Rest Time: 15 Seconds\n" +
                       "• Includes Boss Fight (Wave 5)\n" +
                       "• Enemy Strength: 100%\n" +
                       "• Enemy Damage: 100%\n\n" +
                       "Original intended experience!";
                                   default:
                return "Select a difficulty level";
        }
    }
}
