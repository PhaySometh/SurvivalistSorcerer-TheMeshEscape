using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TextMeshPro is used, if not we'll use legacy Text
using System.Collections; // Required for IEnumerator

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI centerNotificationText; // For personality messages
    
    [Header("Player Stats UI")]
    public Slider healthBar;
    public Slider experienceBar;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText; // Optional: "100/100"
    
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("HUD Containers")]
    public GameObject scoreContainer;
    public GameObject timerContainer;
    public GameObject playerStatsContainer; // Container for health/exp bars

    private CanvasGroup centerTextGroup;
    private Coroutine currentFadeRoutine;
    
    // Player references
    private HealthSystem playerHealth;
    private PlayerStats playerStats;

    void Awake()
    {
        if (centerNotificationText != null)
        {
            centerTextGroup = centerNotificationText.GetComponent<CanvasGroup>();
            if (centerTextGroup == null) centerTextGroup = centerNotificationText.gameObject.AddComponent<CanvasGroup>();
            centerTextGroup.alpha = 0;
        }

        // Hide HUD containers OR the text objects directly if containers aren't assigned
        Debug.Log("UIManager: Attempting to hide HUD at Awake...");
        
        if (scoreContainer) { scoreContainer.SetActive(false); Debug.Log("UIManager: scoreContainer hidden."); }
        else if (scoreText) { scoreText.gameObject.SetActive(false); Debug.Log("UIManager: scoreText object hidden directly."); }

        if (timerContainer) { timerContainer.SetActive(false); Debug.Log("UIManager: timerContainer hidden."); }
        else if (timerText) { timerText.gameObject.SetActive(false); Debug.Log("UIManager: timerText object hidden directly."); }
    }

    void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnTimeChanged.AddListener(UpdateTimeUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
            GameManager.Instance.OnLevelComplete.AddListener(ShowVictory);
            
            // Force initial update
            UpdateScoreUI(GameManager.Instance.currentScore);
            if (timerText != null) timerText.color = Color.white;
        }

        // Subscribe to WaveManager for personality text
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            Debug.Log("UIManager: Found WaveManager. Subscribing to OnStateChange.");
            waveManager.OnStateChange.AddListener(ShowNotification);
            
            // Subscribe to wave changes to show HUD
            if (waveManager.OnWaveChange != null)
                waveManager.OnWaveChange.AddListener(OnWaveStarted);
                
            // If game already started (on restart), show HUD immediately
            if (waveManager.currentWave > 0)
            {
                ShowHUD();
            }
        }
        else
        {
            Debug.LogWarning("UIManager: WaveManager NOT found in scene!");
            // If no WaveManager, show HUD by default
            ShowHUD();
        }
        
        // Find and subscribe to Player's health and stats
        ConnectToPlayer();
        
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
    }
    
    /// <summary>
    /// Show all HUD elements
    /// </summary>
    private void ShowHUD()
    {
        Debug.Log("UIManager: Showing HUD elements");
        
        if (scoreContainer) scoreContainer.SetActive(true);
        else if (scoreText) scoreText.gameObject.SetActive(true);

        if (timerContainer) timerContainer.SetActive(true);
        else if (timerText) timerText.gameObject.SetActive(true);
        
        if (playerStatsContainer) playerStatsContainer.SetActive(true);
    }
    
    /// <summary>
    /// Find player and connect to their health/stats systems
    /// </summary>
    private void ConnectToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        
        if (player != null)
        {
            // Connect to HealthSystem
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
                // IMPORTANT: Force initial update
                UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.maxHealth);
                Debug.Log($"UIManager: Connected to Player HealthSystem - HP: {playerHealth.CurrentHealth}/{playerHealth.maxHealth}");
            }
            
            // Connect to PlayerStats
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnLevelUp.AddListener(OnPlayerLevelUp);
                playerStats.OnExpChanged.AddListener(UpdateExperienceUI);
                
                // Initial updates
                UpdateLevelUI(playerStats.CurrentLevel);
                UpdateExperienceUI(playerStats.CurrentExp, playerStats.ExpToNextLevel);
                Debug.Log("UIManager: Connected to PlayerStats");
            }
            
            // Show player stats container
            if (playerStatsContainer != null)
            {
                playerStatsContainer.SetActive(true);
            }
            
            // Show HUD after a short delay to ensure everything is ready
            Invoke(nameof(ShowHUD), 0.2f);
        }
        else
        {
            Debug.LogWarning("UIManager: Player not found! Retrying in 0.5s...");
            // Retry after a short delay (player might spawn later)
            Invoke(nameof(RetryConnectToPlayer), 0.5f);
        }
    }
    
    /// <summary>
    /// Retry connecting to player if not found initially
    /// </summary>
    private void RetryConnectToPlayer()
    {
        if (playerHealth == null || playerStats == null)
        {
            Debug.Log("UIManager: Retrying player connection...");
            ConnectToPlayer();
        }
    }

    private void OnWaveStarted(int waveNumber)
    {
        // Show HUD when Wave 1 starts
        if (waveNumber == 1)
        {
            Debug.Log("UIManager: Wave 1 started. Showing HUD.");
            ShowHUD();
        }
    }

    public void ShowNotification(string message)
    {
        Debug.Log($"UIManager: Received Notification - {message}");
        if (centerNotificationText == null) return;

        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(FadeNotification(message));
    }

    IEnumerator FadeNotification(string message)
    {
        centerNotificationText.text = message;
        
        // SLOWER FADE
        float duration = 1.0f; 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            centerTextGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        centerTextGroup.alpha = 1;

        yield return new WaitForSeconds(2.5f); // Hold slightly longer

        // SLOWER FADE
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            centerTextGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }
        centerTextGroup.alpha = 0;
        currentFadeRoutine = null;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreUI);
            GameManager.Instance.OnTimeChanged.RemoveListener(UpdateTimeUI);
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
            GameManager.Instance.OnLevelComplete.RemoveListener(ShowVictory);
        }
        
        // Unsubscribe from player events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
        }
        if (playerStats != null)
        {
            playerStats.OnLevelUp.RemoveListener(OnPlayerLevelUp);
            playerStats.OnExpChanged.RemoveListener(UpdateExperienceUI);
        }
    }

    void UpdateScoreUI(int score)
    {
        if (scoreText != null) 
            scoreText.text = "Score: " + score;
    }

    void UpdateTimeUI(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
        }
    }
    
    /// <summary>
    /// Update health bar UI
    /// </summary>
    public void UpdateHealthUI(float current, float max)
    {
        Debug.Log($"UIManager: UpdateHealthUI called - {current}/{max}");
        
        if (healthBar != null)
        {
            healthBar.value = current / max;
        }
        else
        {
            Debug.LogWarning("UIManager: Health bar is null!");
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
    
    /// <summary>
    /// Update experience bar UI
    /// </summary>
    public void UpdateExperienceUI(int currentExp, int expToNextLevel)
    {
        if (experienceBar != null)
        {
            experienceBar.value = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 1f;
        }
    }
    
    /// <summary>
    /// Called when player levels up
    /// </summary>
    private void OnPlayerLevelUp(int newLevel)
    {
        UpdateLevelUI(newLevel);
        
        // Show level up notification
        ShowNotification($"🎉 Level Up! You are now Level {newLevel}!");
    }
    
    /// <summary>
    /// Update level text
    /// </summary>
    private void UpdateLevelUI(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {level}";
        }
    }

    void ShowGameOver()
    {
        Debug.Log("🎮 UIManager: Showing Game Over panel!");
        
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ UIManager: gameOverPanel is not assigned!");
            // Show notification as fallback
            ShowNotification("💀 GAME OVER 💀");
        }
        
        // Unlock cursor so player can interact with UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause camera
        ThirdPersonCameraController cameraControl = FindObjectOfType<ThirdPersonCameraController>();
        if (cameraControl != null) cameraControl.PauseCamera();

        // Pause the game
        Time.timeScale = 0f;
    }

    void ShowVictory()
    {
        Debug.Log("🎮 UIManager: Showing Victory panel!");
        
        if (victoryPanel != null) 
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ UIManager: victoryPanel is not assigned!");
            // Show notification as fallback
            ShowNotification("🎉 VICTORY! 🎉");
        }
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause camera
        ThirdPersonCameraController cameraControl = FindObjectOfType<ThirdPersonCameraController>();
        if (cameraControl != null) cameraControl.PauseCamera();

        // Pause the game
        Time.timeScale = 0f;
    }

    public void SetTimerSuddenDeath(bool activate)
    {
        if (timerText != null)
        {
            timerText.color = activate ? Color.red : Color.white;
        }
    }

    // ========== GAME OVER / RESTART BUTTONS ==========
    
    /// <summary>
    /// Restart the current level (call this from Restart button)
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("🔄 Restarting game...");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load current scene through loading screen
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SceneTransitionManager.Instance.LoadSceneWithLoading(currentScene);
    }

    /// <summary>
    /// Go back to main menu (call this from Menu button)
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("🏠 Returning to main menu...");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load menu scene
        SceneTransitionManager.Instance.LoadSceneDirect("MenuScence");
    }

    /// <summary>
    /// Continue to next level (call this from Victory panel)
    /// </summary>
    public void ContinueToNextLevel()
    {
        Debug.Log("➡️ Loading next level...");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load next level (you can modify this based on your level progression)
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (currentScene == "VilageMapScene")
        {
            SceneTransitionManager.Instance.LoadSceneWithLoading("Map2_AngkorWat");
        }
        else
        {
            // Default: back to village
            SceneTransitionManager.Instance.LoadSceneWithLoading("VilageMapScene");
        }
    }

    /// <summary>
    /// Quit the game (call this from Quit button)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("👋 Quitting game...");
        SceneTransitionManager.Instance.QuitGame();
    }
}

