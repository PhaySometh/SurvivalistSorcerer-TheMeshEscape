using UnityEngine;

/// <summary>
/// Magic Projectile - The spell that flies toward enemies
/// Spawned by WizardSpellSystem
/// </summary>
public class MagicProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 15f;
    public float damage = 25f;
    public float lifetime = 5f;
    public float homingStrength = 0f; // 0 = no homing, higher = more homing
    
    [Header("Visual Effects")]
    public GameObject impactEffectPrefab;
    public TrailRenderer trail;
    
    [Header("Audio")]
    public AudioClip impactSound;
    
    // Internal
    private Transform target;
    private Vector3 direction;
    private bool hasHit = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
        
        // If no rigidbody, move via transform
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void FixedUpdate()
    {
        if (hasHit) return;
        
        Vector3 moveDirection = direction;
        
        // Homing behavior (if target exists and homing is enabled)
        if (target != null && homingStrength > 0f)
        {
            Vector3 toTarget = (target.position + Vector3.up * 1f - transform.position).normalized;
            moveDirection = Vector3.Lerp(direction, toTarget, homingStrength * Time.fixedDeltaTime);
            direction = moveDirection.normalized;
            
            // Rotate projectile to face direction
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }
        
        // Move projectile
        rb.velocity = moveDirection * speed;
    }

    /// <summary>
    /// Initialize the projectile with direction and optional target
    /// </summary>
    public void Initialize(Vector3 shootDirection, float projectileDamage, Transform homingTarget = null, float homing = 0f)
    {
        direction = shootDirection.normalized;
        damage = projectileDamage;
        target = homingTarget;
        homingStrength = homing;
        
        // Face the direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Don't hit the player
        if (other.CompareTag("Player")) return;
        if (other.transform.root.CompareTag("Player")) return;
        
        // Try to damage the target
        HealthSystem targetHealth = other.GetComponent<HealthSystem>();
        if (targetHealth == null)
        {
            targetHealth = other.GetComponentInParent<HealthSystem>();
        }
        
        if (targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(damage);
            Debug.Log($"🔥 Magic projectile hit {other.name} for {damage} damage!");
            OnHit(other.ClosestPoint(transform.position));
        }
        else if (!other.isTrigger)
        {
            // Hit a wall or ground
            OnHit(transform.position);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        // Don't hit the player
        if (collision.gameObject.CompareTag("Player")) return;
        
        // Try to damage
        HealthSystem targetHealth = collision.gameObject.GetComponent<HealthSystem>();
        if (targetHealth == null)
        {
            targetHealth = collision.gameObject.GetComponentInParent<HealthSystem>();
        }
        
        if (targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(damage);
            Debug.Log($"🔥 Magic projectile hit {collision.gameObject.name} for {damage} damage!");
        }
        
        OnHit(collision.contacts[0].point);
    }

    /// <summary>
    /// Called when projectile hits something
    /// </summary>
    private void OnHit(Vector3 hitPoint)
    {
        hasHit = true;
        
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(impact, 2f);
        }
        
        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, hitPoint, 0.8f);
        }
        
        // Disable visuals
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
        
        if (trail != null) trail.enabled = false;
        
        // Stop movement
        if (rb != null) rb.velocity = Vector3.zero;
        
        // Destroy after short delay (for effects to play)
        Destroy(gameObject, 0.5f);
    }
}
