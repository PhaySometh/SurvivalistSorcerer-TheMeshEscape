using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles pause menu when player presses ESC
/// Attach this to a PauseManager GameObject or your Canvas
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanel;
    
    [Header("Optional - Auto-connect buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;
    
    private bool isPaused = false;
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        
        // Make sure pause panel is hidden at start
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // Auto-connect buttons if assigned
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClick);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(BackToMainMenu);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null)
        {
            Debug.LogError("PauseManager: Pause panel is not assigned!");
            return;
        }
        
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause camera if exists
        ThirdPersonCameraController cameraControl = FindObjectOfType<ThirdPersonCameraController>();
        if (cameraControl != null)
        {
            cameraControl.PauseCamera();
        }
        
        Debug.Log("⏸️ Game Paused");
    }

    public void ResumeGame()
    {
        if (pausePanel == null) return;
        
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f; // Resume the game
        
        // Lock cursor back (if you want)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Resume camera if exists
        ThirdPersonCameraController cameraControl = FindObjectOfType<ThirdPersonCameraController>();
        if (cameraControl != null)
        {
            cameraControl.ResumeCamera();
        }
        
        Debug.Log("▶️ Game Resumed");
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

    public void BackToMainMenu()
    {
        // Unpause before loading scene
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

    public void QuitGame()
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
    
    // Public property to check pause state
    public bool IsPaused => isPaused;
}
