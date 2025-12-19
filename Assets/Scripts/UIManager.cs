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
    public Slider healthBar; // Optional if we have player health
    
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("HUD Containers")]
    public GameObject scoreContainer;
    public GameObject timerContainer;

    private CanvasGroup centerTextGroup;
    private Coroutine currentFadeRoutine;

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
        }
        else
        {
            Debug.LogWarning("UIManager: WaveManager NOT found in scene!");
        }
        
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
    }

    private void OnWaveStarted(int waveNumber)
    {
        // Show Score and Timer once Wave 1 starts
        if (waveNumber == 1)
        {
            Debug.Log("UIManager: Wave 1 started. Showing HUD.");
            if (scoreContainer) scoreContainer.SetActive(true);
            else if (scoreText) scoreText.gameObject.SetActive(true);

            if (timerContainer) timerContainer.SetActive(true);
            else if (timerText) timerText.gameObject.SetActive(true);
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
    
    // Optional: Call this from Player HealthSystem
    public void UpdateHealthUI(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.value = current / max;
        }
    }

    void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowVictory()
    {
        if (victoryPanel) victoryPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetTimerSuddenDeath(bool activate)
    {
        if (timerText != null)
        {
            timerText.color = activate ? Color.red : Color.white;
        }
    }
}
