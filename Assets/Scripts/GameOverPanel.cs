using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverPanel : MonoBehaviour
{
    [Header("Optional - Auto-find buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;
    
    [Header("Optional - Text elements")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI scoreText;
    
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        
        // Auto-connect buttons if assigned
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
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClick);
        }
    }

    void OnEnable()
    {
        // Update score when panel shows
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = $"Final Score: {GameManager.Instance.currentScore}";
        }
        
        // Set message
        if (messageText != null)
        {
            messageText.text = "Game Over!";
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
            Debug.LogWarning("UIManager not found! Using direct restart.");
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneWithLoading(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    public void OnMainMenuClick()
    {
        if (uiManager != null)
        {
            uiManager.BackToMainMenu();
        }
        else
        {
            Debug.LogWarning("UIManager not found! Using direct load.");
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneDirect("MenuScence");
        }
    }

    public void OnQuitClick()
    {
        if (uiManager != null)
        {
            uiManager.QuitGame();
        }
        else
        {
            SceneTransitionManager.Instance.QuitGame();
        }
    }
}
