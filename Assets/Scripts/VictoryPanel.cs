using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple helper script for Victory panel
/// Attach this to your Victory Panel
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("Optional - Auto-find buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    
    [Header("Optional - Text elements")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
    
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClick);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClick);
        }
    }

    void OnEnable()
    {
        // Show cursor and unlock (critical for UI interaction)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Update stats when panel shows
        if (GameManager.Instance != null)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Final Score: {GameManager.Instance.currentScore}";
            }
            
            if (timeText != null)
            {
                float time = GameManager.Instance.elapsedTime;
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time % 60F);
                timeText.text = $"Time: {minutes:0}:{seconds:00}";
            }
        }
        
        // Set message
        if (messageText != null)
        {
            messageText.text = "Victory!";
        }
    }

    public void OnNextLevelClick()
    {
        if (uiManager != null)
        {
            uiManager.ContinueToNextLevel();
        }
        else
        {
            Debug.LogWarning("UIManager not found! Loading default next level.");
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneWithLoading("Map2_AngkorWat");
        }
    }

    public void OnRestartClick()
    {
        if (uiManager != null)
        {
            uiManager.RestartGame();
        }
        else
        {
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneWithLoading(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    public void OnMainMenuClick()
    {
        Debug.Log("VictoryPanel: Going to main menu...");
        
        // Ensure cursor is visible and unlocked before transitioning
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        
        if (uiManager != null)
        {
            uiManager.BackToMainMenu();
        }
        else
        {
            SceneTransitionManager.Instance.LoadSceneDirect("MenuScence");
        }
    }
}
