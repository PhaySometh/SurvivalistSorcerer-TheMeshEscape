using UnityEngine;

/// <summary>
/// Checks victory conditions without modifying wave logic
/// Victory = Survive final wave for X seconds + collect Y coins
/// Attach this to WaveManager or GameManager
/// </summary>
public class VictoryConditionChecker : MonoBehaviour
{
    [Header("Victory Conditions")]
    [Tooltip("Time to survive in the final wave (in seconds)")]
    public float surviveTime = 120f; // 2 minutes
    
    [Tooltip("Number of coins needed to win")]
    public int requiredCoins = 50;
    
    [Header("References (Auto-found if not assigned)")]
    public WaveManager waveManager;
    public PlayerStats playerStats;
    
    [Header("Debug")]
    [SerializeField] private bool isFinalWave = false;
    [SerializeField] private float finalWaveTimer = 0f;
    [SerializeField] private bool victoryTriggered = false;

    void Start()
    {
        // Auto-find references
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();
            
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerStats = player.GetComponent<PlayerStats>();
        }
        
        if (waveManager == null)
        {
            Debug.LogError("VictoryConditionChecker: WaveManager not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (victoryTriggered) return;
        
        // Check if we're in the final wave
        CheckIfFinalWave();
        
        // If in final wave, count survival time
        if (isFinalWave)
        {
            finalWaveTimer += Time.deltaTime;
            
            // Check victory conditions
            CheckVictoryConditions();
        }
    }

    void CheckIfFinalWave()
    {
        // Check if current wave is the last wave
        if (waveManager.currentWave >= waveManager.totalWaves)
        {
            if (!isFinalWave)
            {
                isFinalWave = true;
                finalWaveTimer = 0f;
                Debug.Log($"🎯 Final Wave Started! Survive {surviveTime}s and collect {requiredCoins} coins to win!");
            }
        }
    }

    void CheckVictoryConditions()
    {
        // Check both conditions
        bool survivedLongEnough = finalWaveTimer >= surviveTime;
        bool hasEnoughCoins = playerStats != null && playerStats.TotalCoins >= requiredCoins;
        
        if (survivedLongEnough && hasEnoughCoins)
        {
            TriggerVictory();
        }
        else
        {
            // Optional: Show progress (you can remove this if too spammy)
            if (Mathf.FloorToInt(finalWaveTimer) % 10 == 0 && finalWaveTimer > 0.1f)
            {
                int timeLeft = Mathf.CeilToInt(surviveTime - finalWaveTimer);
                int coinsLeft = Mathf.Max(0, requiredCoins - (playerStats != null ? playerStats.TotalCoins : 0));
                
                if (timeLeft > 0 || coinsLeft > 0)
                {
                    Debug.Log($"Victory Progress - Time: {Mathf.FloorToInt(finalWaveTimer)}/{surviveTime}s | Coins: {(playerStats != null ? playerStats.TotalCoins : 0)}/{requiredCoins}");
                }
            }
        }
    }

    void TriggerVictory()
    {
        if (victoryTriggered) return;
        
        victoryTriggered = true;
        Debug.Log("🎉 VICTORY CONDITIONS MET!");
        
        // Show victory message
        if (waveManager != null)
        {
            waveManager.OnStateChange?.Invoke("🎉 YOU WIN! 🎉");
        }
        
        // Trigger game victory
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LevelComplete();
        }
    }

    // Public method to check current progress (can be called from UI)
    public string GetVictoryProgress()
    {
        if (!isFinalWave)
            return "Reach final wave to win!";
            
        int timeLeft = Mathf.CeilToInt(surviveTime - finalWaveTimer);
        int coinsLeft = Mathf.Max(0, requiredCoins - (playerStats != null ? playerStats.TotalCoins : 0));
        
        return $"Survive: {Mathf.Max(0, timeLeft)}s | Coins: {coinsLeft} more";
    }
}
