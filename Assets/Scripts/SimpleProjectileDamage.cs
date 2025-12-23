using UnityEngine;

/// <summary>
/// Simple damage script for projectiles without MagicProjectile component
/// </summary>
public class SimpleProjectileDamage : MonoBehaviour
{
    public float damage = 500f;
    private bool hasHit = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Don't hit the player
        if (other.CompareTag("Player")) return;
        if (other.transform.root.CompareTag("Player")) return;
        
        // Check if it's an enemy
        bool isEnemy = other.CompareTag("Enemy") || 
                      other.GetComponent<EnemyAI>() != null || 
                      other.GetComponentInParent<EnemyAI>() != null;
        
        if (isEnemy)
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health == null) health = other.GetComponentInParent<HealthSystem>();
            if (health == null) health = other.transform.root.GetComponent<HealthSystem>();
            
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(damage);
                Debug.Log($"💥 SimpleProjectile hit {other.transform.root.name} for {damage} damage!");
                hasHit = true;
                Destroy(gameObject);
                return;
            }
        }
        
        // Hit something else
        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        // Don't hit the player
        if (collision.gameObject.CompareTag("Player")) return;
        
        // Try to damage
        HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
        if (health == null) health = collision.gameObject.GetComponentInParent<HealthSystem>();
        
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            Debug.Log($"💥 SimpleProjectile hit {collision.gameObject.name} for {damage} damage!");
        }
        
        hasHit = true;
        Destroy(gameObject);
    }
}
