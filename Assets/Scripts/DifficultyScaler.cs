using UnityEngine;

/// <summary>
/// Optional: Add this to enemy prefabs to apply difficulty multipliers
/// Automatically scales health and damage based on selected difficulty
/// </summary>
public class DifficultyScaler : MonoBehaviour
{
    [Header("Apply Multipliers")]
    public bool scaleHealth = true;
    public bool scaleDamage = true;
    
    private HealthSystem healthSystem;
    private bool scaled = false;
    
    void Start()
    {
        ApplyDifficultyScaling();
    }
    
    /// <summary>
    /// Apply difficulty multipliers to this enemy
    /// </summary>
    void ApplyDifficultyScaling()
    {
        if (scaled) return; // Only scale once
        
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name}: GameSettings not found. Using default stats.");
            return;
        }
        
        WaveConfig config = GameSettings.Instance.GetWaveConfig();
        
        // Scale Health
        if (scaleHealth)
        {
            healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                float originalHealth = healthSystem.maxHealth;
                healthSystem.maxHealth *= config.enemyHealthMultiplier;
                healthSystem.SetHealth(healthSystem.maxHealth);
                
                Debug.Log($"{gameObject.name}: Health scaled from {originalHealth} to {healthSystem.maxHealth} " +
                         $"({config.enemyHealthMultiplier}x)");
            }
        }
        
        // Scale Damage - Note: This requires your enemy attack script to expose damage
        // You'll need to modify this based on your specific enemy attack implementation
        if (scaleDamage)
        {
            ScaleDamage(config.enemyDamageMultiplier);
        }
        
        scaled = true;
    }
    
    /// <summary>
    /// Scale damage based on your specific enemy implementation
    /// Modify this method based on how your enemies deal damage
    /// </summary>
    void ScaleDamage(float multiplier)
    {
        // Example 1: If using EnemyAttack component
        // EnemyAttack attack = GetComponent<EnemyAttack>();
        // if (attack != null)
        // {
        //     // Assuming EnemyAttack has a public damage field
        //     // attack.damage *= multiplier;
        //     Debug.Log($"{gameObject.name}: Damage scaled by {multiplier}x");
        // }
        
        // Example 2: If damage is in a different component
        // YourEnemyDamageScript damageScript = GetComponent<YourEnemyDamageScript>();
        // if (damageScript != null)
        // {
        //     damageScript.attackDamage *= multiplier;
        // }
        
        // Example 3: If using a scriptable object for stats
        // EnemyStats stats = GetComponent<Enemy>().stats;
        // stats.damage *= multiplier;
    }
    
    /// <summary>
    /// Get current difficulty level for debugging
    /// </summary>
    public string GetCurrentDifficulty()
    {
        if (GameSettings.Instance != null)
        {
            return GameSettings.Instance.currentDifficulty.ToString();
        }
        return "Unknown";
    }
}
