using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;

    [Header("Debug/Status")]
    [SerializeField] private float currentHealth;

    // Events
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;
    public UnityEvent<float, float> OnHealthChanged; // currentHealth, maxHealth

    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}");

        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Set health to a specific value (used when leveling up)
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Get health as percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    private void Die()
    {
        currentHealth = 0;
        Debug.Log($"{gameObject.name} died.");
        OnDeath?.Invoke();

        // Optional: Destroy gameObject after a delay or immediately, managed by this flag
        // or by a separate Death handler script.
        if (destroyOnDeath)
        {
            // If it's the player, we usually don't want to destroy the GameObject immediately 
            // because we need to show Game Over UI, etc.
            if (!gameObject.CompareTag("Player"))
            {
                Destroy(gameObject, 3f); // Delay for death animation
            }
        }
    }
    
    /// <summary>
    /// Reset health to full (used for respawn)
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}

