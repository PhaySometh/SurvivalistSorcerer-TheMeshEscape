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
        // Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}");

        // Trigger animation if player
        PlayerAnimatorController anim = GetComponent<PlayerAnimatorController>();
        if (anim != null) anim.TriggerGetHit();

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
        Debug.Log($"💀 {gameObject.name} died!");
        
        // Trigger death animation
        PlayerAnimatorController playerAnim = GetComponent<PlayerAnimatorController>();
        if (playerAnim != null) 
        {
            playerAnim.TriggerDeath();
        }
        
        // For enemies: Check for regular Animator
        Animator basicAnim = GetComponent<Animator>();
        if (basicAnim != null && playerAnim == null)
        {
            // Try multiple possible trigger names for death animation
            basicAnim.SetTrigger("Die");
            basicAnim.SetTrigger("die");
            basicAnim.SetTrigger("Death");
            basicAnim.SetTrigger("death");
            basicAnim.SetBool("IsDead", true);
            basicAnim.SetBool("isDead", true);
        }
        
        // For enemies: Stop NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        // For enemies: Disable AI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // For enemies: Disable collision detector
        EnemyCollisionDetector collisionDetector = GetComponentInChildren<EnemyCollisionDetector>();
        if (collisionDetector != null)
        {
            collisionDetector.enabled = false;
        }
        
        // Optionally disable the main collider so player can walk through
        Collider col = GetComponent<Collider>();
        if (col != null && !gameObject.CompareTag("Player"))
        {
            col.enabled = false;
        }

        OnDeath?.Invoke();

        if (gameObject.CompareTag("Player"))
        {
            Debug.Log("🎮 PLAYER DIED - GAME OVER");
            
            // Disable movement
            var mover = GetComponent<PlayerMovementScript>();
            if (mover != null) mover.enabled = false;
            
            // Trigger Game Over in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
        else if (destroyOnDeath)
        {
            // Destroy enemy after death animation plays
            Destroy(gameObject, 3f);
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
