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
        }

        // Subscribe to WaveManager for personality text
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnStateChange.AddListener(ShowNotification);
        }
        
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        if (centerNotificationText == null) return;

        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(FadeNotification(message));
    }

    IEnumerator FadeNotification(string message)
    {
        centerNotificationText.text = message;
        
        // Fade In
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            centerTextGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        centerTextGroup.alpha = 1;

        yield return new WaitForSeconds(2f); // Hold

        // Fade Out
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
}
