using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float levelTimeLimit = 600f; // 10 minutes (600 seconds)
    public int targetScore = 100; // For future win condition reference

    [Header("Current State")]
    public int currentScore = 0;
    public int currentExperience = 0;
    public float timeRemaining;
    public bool isGameActive = false;
    public bool isInSuddenDeath = false;

    // Events
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnExperienceChanged;
    public UnityEvent<float> OnTimeChanged;
    public UnityEvent OnGameOver;
    public UnityEvent OnLevelComplete;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Keeping the Instance alive, its inspector values will be preserved.
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Removed auto-start here. WaveManager handles the start sequence now.
    }

    public void StartGame()
    {
        currentScore = 0;
        timeRemaining = levelTimeLimit;
        isGameActive = true;
        OnScoreChanged?.Invoke(currentScore);
    }

    void Update()
    {
        if (!isGameActive) return;

        // Timer Logic
        timeRemaining -= Time.deltaTime;
        OnTimeChanged?.Invoke(timeRemaining);

        if (timeRemaining <= 0)
        {
            if (!isInSuddenDeath)
            {
                TriggerSuddenDeath();
            }
            else
            {
                GameOver();
            }
        }
    }

    private void TriggerSuddenDeath()
    {
        isInSuddenDeath = true;
        timeRemaining = 120f; // 2 minutes overtime
        
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.TriggerSuddenDeath();
        }

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.SetTimerSuddenDeath(true);
        }
    }

    public void AddScore(int amount)
    {
        if (!isGameActive) return;

        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    public void AddExperience(int amount)
    {
        if (!isGameActive) return;

        currentExperience += amount;
        OnExperienceChanged?.Invoke(currentExperience);
        Debug.Log("Experience Added: " + amount + ". Total: " + currentExperience);
    }

    public void GameOver()
    {
        // Check if WaveManager handled this as "Sudden Death"
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null && waveManager.currentState == WaveManager.WaveState.Victory) return;
    
        Debug.Log("Game Over!");
        isGameActive = false;
        OnGameOver?.Invoke();
        
        // Logic to show results screen or reload would go here
        // For MVP, maybe just reload scene after delay?
    }

    public void LevelComplete()
    {
        Debug.Log("Level Complete!");
        isGameActive = false;
        OnLevelComplete?.Invoke();
    }
}
