using UnityEngine;

/// <summary>
/// Stores game settings and difficulty configurations
/// Persists settings using PlayerPrefs
/// </summary>
public class GameSettings : MonoBehaviour
{
    public enum Difficulty { Easy, Medium, Hard, Default }
    
    [Header("Current Settings")]
    public Difficulty currentDifficulty = Difficulty.Medium;
    
    // Singleton
    public static GameSettings Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSettings();
    }
    
    /// <summary>
    /// Get wave configuration based on difficulty
    /// </summary>
    public WaveConfig GetWaveConfig()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                return new WaveConfig
                {
                    startWave = 1,
                    endWave = 1,
                    totalWaves = 1,
                    waveDuration = 120f,
                    bufferDuration = 15f,
                    timeLimit = 120f, // 3 minutes
                    enemyHealthMultiplier = 0.8f,
                    enemyDamageMultiplier = 0.8f,
                    startWithSuddenDeath = false,
                    coinsRequired = 0 // No coin requirement
                };
                
            case Difficulty.Medium:
                return new WaveConfig
                {
                    startWave = 1,
                    endWave = 3,
                    totalWaves = 3,
                    waveDuration = 120f,
                    bufferDuration = 15f,
                    timeLimit = 480f, // 8 minutes
                    enemyHealthMultiplier = 1.0f,
                    enemyDamageMultiplier = 1.0f,
                    startWithSuddenDeath = false,
                    coinsRequired = 0 // No coin requirement
                };
                
            case Difficulty.Hard:
                return new WaveConfig
                {
                    startWave = 5,
                    endWave = 5,
                    totalWaves = 1,
                    waveDuration = 60f, // 1 minute before sudden death
                    bufferDuration = 0f,
                    timeLimit = 60f, // 1 minute total
                    enemyHealthMultiplier = 1.2f,
                    enemyDamageMultiplier = 1.2f,
                    startWithSuddenDeath = true, // Sudden death immediately after wave
                    coinsRequired = 50 // Need to collect 50 coins
                };
                
            case Difficulty.Default:
                return new WaveConfig
                {
                    startWave = 1,
                    endWave = 5,
                    totalWaves = 5,
                    waveDuration = 120f, // 2 minutes per wave
                    bufferDuration = 15f, // 15 seconds rest
                    timeLimit = 600f, // 10 minutes total
                    enemyHealthMultiplier = 1.0f,
                    enemyDamageMultiplier = 1.0f,
                    startWithSuddenDeath = false,
                    coinsRequired = 0 // No coin requirement
                };
                
            default:
                return GetWaveConfig(); // Fallback to current
        }
    }
    
    /// <summary>
    /// Set difficulty and save
    /// </summary>
    public void SetDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;
        SaveSettings();
        Debug.Log($"Difficulty set to: {difficulty}");
    }
    
    /// <summary>
    /// Save settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetInt("GameDifficulty", (int)currentDifficulty);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Load settings from PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("GameDifficulty"))
        {
            currentDifficulty = (Difficulty)PlayerPrefs.GetInt("GameDifficulty");
        }
        else
        {
            // Default to Medium
            currentDifficulty = Difficulty.Medium;
            SaveSettings();
        }
        
        Debug.Log($"Loaded difficulty: {currentDifficulty}");
    }
    
    /// <summary>
    /// Get difficulty description for UI
    /// </summary>
    public string GetDifficultyDescription()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                return "EASY\nWave 1 Only | 3 Min | Tutorial";
            case Difficulty.Medium:
                return "MEDIUM\nWaves 1-3 | 8 Min | No Boss";
            case Difficulty.Hard:
                return "HARD\nWave 5 + Boss | 1 Min | Sudden Death";
            case Difficulty.Default:
                return "DEFAULT\nAll 5 Waves | 10 Min | Full Game";
            default:
                return "MEDIUM";
        }
    }
}

/// <summary>
/// Wave configuration data structure
/// </summary>
[System.Serializable]
public class WaveConfig
{
    public int startWave;              // Which wave to start from (1-5)
    public int endWave;                // Which wave to end at (1-5)
    public int totalWaves;             // Total number of waves to play
    public float waveDuration;
    public float bufferDuration;
    public float timeLimit;
    public float enemyHealthMultiplier;
    public float enemyDamageMultiplier;
    public bool startWithSuddenDeath;  // Start sudden death immediately
    public int coinsRequired;          // Coins needed to win (0 = disabled)
}
