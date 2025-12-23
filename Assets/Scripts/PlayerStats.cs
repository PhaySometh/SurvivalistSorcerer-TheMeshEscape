using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Player Stats System - Handles health, experience, leveling, and stat upgrades
/// Integrates with existing HealthSystem and GameManager
/// </summary>
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(PlayerAnimatorController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Starting maximum health")]
    public float baseMaxHealth = 150f;
    
    [Tooltip("Starting attack damage")]
    public float baseAttackDamage = 25f;
    
    [Tooltip("Starting movement speed bonus (added to base speed)")]
    public float baseSpeedBonus = 0f;

    [Header("Level Up Settings")]
    [Tooltip("Experience required for level 2 (scales exponentially)")]
    public int baseExpToLevel = 100;
    
    [Tooltip("Multiplier for each level's exp requirement")]
    public float expScalePerLevel = 1.5f;
    
    [Tooltip("Maximum level")]
    public int maxLevel = 20;

    [Header("Stat Growth Per Level")]
    [Tooltip("Max health increase per level")]
    public float healthPerLevel = 15f;
    
    [Tooltip("Attack damage increase per level")]
    public float damagePerLevel = 5f;
    
    [Tooltip("Speed bonus increase per level")]
    public float speedPerLevel = 0.2f;

    [Header("Current State")]
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _expToNextLevel;
    [SerializeField] private int _totalCoins = 0;

    [Header("Calculated Stats (Read Only)")]
    [SerializeField] private float _currentMaxHealth;
    [SerializeField] private float _currentAttackDamage;
    [SerializeField] private float _currentSpeedBonus;

    [Header("Events")]
    public UnityEvent<int> OnLevelUp;           // Passes new level
    public UnityEvent<int, int> OnExpChanged;    // Passes current exp, exp to next level
    public UnityEvent<int> OnCoinsChanged;       // Passes total coins
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerRespawn;

    // Components
    private HealthSystem healthSystem;
    private PlayerAnimatorController animController;
    private PlayerCombatSystem combatSystem;
    private PlayerMovementScript movementScript;

    // Public accessors
    public int CurrentLevel => _currentLevel;
    public int CurrentExp => _currentExp;
    public int ExpToNextLevel => _expToNextLevel;
    public int TotalCoins => _totalCoins;
    public float CurrentMaxHealth => _currentMaxHealth;
    public float CurrentAttackDamage => _currentAttackDamage;
    public float CurrentSpeedBonus => _currentSpeedBonus;
    public bool IsDead => healthSystem != null && healthSystem.IsDead;

    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        animController = GetComponent<PlayerAnimatorController>();
        combatSystem = GetComponent<PlayerCombatSystem>();
        movementScript = GetComponent<PlayerMovementScript>();
    }

    void Start()
    {
        // Initialize stats
        RecalculateStats();
        
        // Subscribe to health events
        if (healthSystem != null)
        {
            healthSystem.OnTakeDamage.AddListener(OnTakeDamage);
            healthSystem.OnDeath.AddListener(OnDeath);
            
            // Set initial health
            healthSystem.maxHealth = _currentMaxHealth;
        }
        
        // Connect to GameManager for exp/coins
        ConnectToGameManager();
    }

    /// <summary>
    /// Connect to GameManager events
    /// </summary>
    private void ConnectToGameManager()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnExperienceChanged.AddListener(OnGameManagerExpChanged);
            gm.OnScoreChanged.AddListener(OnGameManagerScoreChanged);
        }
    }

    /// <summary>
    /// Called when GameManager's experience changes
    /// </summary>
    private void OnGameManagerExpChanged(int totalExp)
    {
        // Calculate how much exp was added
        int expGained = totalExp - _currentExp;
        if (expGained > 0)
        {
            AddExperience(expGained);
        }
    }

    /// <summary>
    /// Called when GameManager's score (coins) changes
    /// </summary>
    private void OnGameManagerScoreChanged(int totalScore)
    {
        _totalCoins = totalScore;
        OnCoinsChanged?.Invoke(_totalCoins);
    }

    /// <summary>
    /// Add experience and check for level up
    /// </summary>
    public void AddExperience(int amount)
    {
        if (_currentLevel >= maxLevel) return;

        _currentExp += amount;
        
        // Check for level up (can level up multiple times)
        while (_currentExp >= _expToNextLevel && _currentLevel < maxLevel)
        {
            _currentExp -= _expToNextLevel;
            LevelUp();
        }
        
        OnExpChanged?.Invoke(_currentExp, _expToNextLevel);
    }

    /// <summary>
    /// Level up the player
    /// </summary>
    private void LevelUp()
    {
        _currentLevel++;
        
        // Recalculate all stats
        RecalculateStats();
        
        // Heal player to full on level up
        if (healthSystem != null)
        {
            healthSystem.maxHealth = _currentMaxHealth;
            healthSystem.Heal(_currentMaxHealth); // Full heal
        }
        
        // Update combat damage
        if (combatSystem != null)
        {
            combatSystem.baseDamage = _currentAttackDamage;
        }
        
        // Calculate new exp requirement
        _expToNextLevel = Mathf.RoundToInt(baseExpToLevel * Mathf.Pow(expScalePerLevel, _currentLevel - 1));
        
        Debug.Log($"🎉 LEVEL UP! Now Level {_currentLevel}");
        Debug.Log($"   Health: {_currentMaxHealth}, Attack: {_currentAttackDamage}, Speed: +{_currentSpeedBonus}");
        
        OnLevelUp?.Invoke(_currentLevel);
        
        // Play victory animation briefly or a level up effect
        // animController?.SetVictory(true);
    }

    /// <summary>
    /// Recalculate all stats based on level
    /// </summary>
    public void RecalculateStats()
    {
        int levelsGained = _currentLevel - 1;
        
        _currentMaxHealth = baseMaxHealth + (healthPerLevel * levelsGained);
        _currentAttackDamage = baseAttackDamage + (damagePerLevel * levelsGained);
        _currentSpeedBonus = baseSpeedBonus + (speedPerLevel * levelsGained);
        
        _expToNextLevel = Mathf.RoundToInt(baseExpToLevel * Mathf.Pow(expScalePerLevel, _currentLevel - 1));
        
        // Apply to health system
        if (healthSystem != null)
        {
            healthSystem.maxHealth = _currentMaxHealth;
        }
        
        // Apply to combat system
        if (combatSystem != null)
        {
            combatSystem.baseDamage = _currentAttackDamage;
        }
    }

    /// <summary>
    /// Called when player takes damage
    /// </summary>
    private void OnTakeDamage()
    {
        // Play hit animation
        if (animController != null)
        {
            animController.TriggerGetHit();
        }
        
        Debug.Log($"💔 Player took damage! Health remaining: {healthSystem.maxHealth}");
    }

    /// <summary>
    /// Called when player dies
    /// </summary>
    private void OnDeath()
    {
        Debug.Log("☠️ Player has died!");
        
        // Play death animation
        if (animController != null)
        {
            animController.TriggerDeath();
        }
        
        // Disable movement
        if (movementScript != null)
        {
            movementScript.canMove = false;
        }
        
        OnPlayerDeath?.Invoke();
        
        // Notify GameManager
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.GameOver();
        }
    }

    /// <summary>
    /// Respawn the player (call this to reset after death)
    /// </summary>
    public void Respawn()
    {
        if (healthSystem == null) return;
        
        // Reset health
        healthSystem.Heal(_currentMaxHealth);
        
        // Play respawn animation
        if (animController != null)
        {
            animController.TriggerRespawn();
            animController.ResetAllTriggers();
        }
        
        // Re-enable movement
        if (movementScript != null)
        {
            movementScript.canMove = true;
        }
        
        Debug.Log("🔄 Player respawned!");
        OnPlayerRespawn?.Invoke();
    }

    /// <summary>
    /// Spend coins (for future shop system)
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (_totalCoins >= amount)
        {
            _totalCoins -= amount;
            OnCoinsChanged?.Invoke(_totalCoins);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        if (healthSystem == null) return 1f;
        // Access private field via reflection or add a getter to HealthSystem
        // For now, we'll estimate based on IsDead
        return healthSystem.IsDead ? 0f : 1f;
    }

    /// <summary>
    /// Get current experience percentage to next level (0-1)
    /// </summary>
    public float GetExpPercentage()
    {
        if (_expToNextLevel <= 0) return 1f;
        return (float)_currentExp / _expToNextLevel;
    }
}
