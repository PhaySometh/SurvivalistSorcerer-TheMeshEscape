using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 150f;
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

        // Flash red when hit (for enemies)
        if (!gameObject.CompareTag("Player"))
        {
            StartCoroutine(FlashRed());
        }

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
        
        // VISUAL DEATH EFFECT - Create explosion
        CreateDeathExplosion();
        
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
        
        // Make enemy semi-transparent when dead (visual indicator)
        if (!gameObject.CompareTag("Player"))
        {
            MakeTransparent();
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
    
    /// <summary>
    /// Create explosion effect when enemy dies
    /// </summary>
    private void CreateDeathExplosion()
    {
        if (gameObject.CompareTag("Player")) return;
        
        // Create a sphere that explodes
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.transform.position = transform.position + Vector3.up;
        explosion.transform.localScale = Vector3.one * 0.5f;
        
        // Make it red and glowing
        Renderer rend = explosion.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.red;
        mat.SetColor("_EmissionColor", Color.red * 3f);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;
        
        // Remove collider
        Destroy(explosion.GetComponent<Collider>());
        
        // Animate expansion
        StartCoroutine(ExpandAndFade(explosion));
    }
    
    private System.Collections.IEnumerator ExpandAndFade(GameObject obj)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = obj.transform.localScale;
        Vector3 endScale = startScale * 5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Fade out
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Color col = rend.material.color;
                col.a = 1f - t;
                rend.material.color = col;
            }
            
            yield return null;
        }
        
        Destroy(obj);
    }
    
    /// <summary>
    /// Flash red when taking damage
    /// </summary>
    private System.Collections.IEnumerator FlashRed()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Material[] originalMaterials = new Material[renderers.Length];
        
        // Store original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
        
        // Flash red
        Material redMat = new Material(Shader.Find("Standard"));
        redMat.color = Color.red;
        redMat.SetColor("_EmissionColor", Color.red * 2f);
        redMat.EnableKeyword("_EMISSION");
        
        foreach (Renderer rend in renderers)
        {
            rend.material = redMat;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }
    
    /// <summary>
    /// Make enemy transparent when dead (visual indicator)
    /// </summary>
    private void MakeTransparent()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material mat = rend.material;
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            Color col = mat.color;
            col.a = 0.3f; // Semi-transparent
            mat.color = col;
        }
    }
}
