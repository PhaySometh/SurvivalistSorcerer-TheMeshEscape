using UnityEngine;

/// <summary>
/// Magic Projectile - The spell that flies toward enemies
/// Spawned by WizardSpellSystem
/// </summary>
public class MagicProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 80f;
    public float damage = 500f;
    public float lifetime = 30f;
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
        
        Debug.Log($"🚀 MagicProjectile spawned! Damage: {damage}, Speed: {speed}, Homing: {homingStrength}");
        
        // Add glowing trail effect
        AddTrailEffect();
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
        
        // If no rigidbody, move via transform
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        // Ensure we have a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2f; // Very large radius for guaranteed hits
        }
        else if (col is SphereCollider sphereCol)
        {
            sphereCol.radius = Mathf.Max(sphereCol.radius, 2f); // Ensure at least 2 unit radius
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
    
    /// <summary>
    /// Add a glowing trail effect to make projectile more visible
    /// </summary>
    private void AddTrailEffect()
    {
        // Check if trail doesn't exist
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.5f;
            trail.endWidth = 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(0.3f, 0.7f, 1f, 1f); // Bright blue
            trail.endColor = new Color(0.3f, 0.7f, 1f, 0f); // Fade to transparent
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        Debug.Log($"🎯 Projectile hit: {other.name} (Tag: {other.tag})");
        
        // Don't hit the player
        if (other.CompareTag("Player")) return;
        if (other.transform.root.CompareTag("Player")) return;
        
        // Check if it's an enemy using multiple methods
        bool isEnemy = CheckIfEnemy(other.gameObject);
        
        Debug.Log($"   Is enemy? {isEnemy}");
        
        // Try to find and damage HealthSystem
        if (isEnemy)
        {
            HealthSystem targetHealth = other.GetComponent<HealthSystem>();
            if (targetHealth == null) targetHealth = other.GetComponentInParent<HealthSystem>();
            if (targetHealth == null) targetHealth = other.transform.root.GetComponent<HealthSystem>();
            
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(damage);
                Debug.Log($"🔥 Magic projectile hit {other.transform.root.name} for {damage} damage! Their HP: {targetHealth.CurrentHealth}/{targetHealth.maxHealth}");
                OnHit(other.ClosestPoint(transform.position));
                return;
            }
            else
            {
                Debug.LogWarning($"⚠️ Enemy {other.name} has no HealthSystem or is already dead!");
            }
        }
        
        // Hit environment (wall, ground, etc.)
        if (!other.isTrigger)
        {
            OnHit(transform.position);
        }
    }
    
    /// <summary>
    /// Check if object is an enemy using multiple methods
    /// </summary>
    private bool CheckIfEnemy(GameObject obj)
    {
        // Check tag
        if (obj.CompareTag("Enemy")) return true;
        if (obj.transform.root.CompareTag("Enemy")) return true;
        
        // Check layer
        if (obj.layer == LayerMask.NameToLayer("Enemy")) return true;
        
        // Check for enemy components
        if (obj.GetComponent<EnemyAI>() != null) return true;
        if (obj.GetComponentInParent<EnemyAI>() != null) return true;
        if (obj.GetComponent<EnemyCollisionDetector>() != null) return true;
        if (obj.GetComponentInParent<EnemyCollisionDetector>() != null) return true;
        
        // Check name for common enemy names
        string name = obj.name.ToLower();
        string rootName = obj.transform.root.name.ToLower();
        if (name.Contains("enemy") || name.Contains("slime") || name.Contains("turtle") ||
            name.Contains("skeleton") || name.Contains("golem") || name.Contains("boss"))
            return true;
        if (rootName.Contains("enemy") || rootName.Contains("slime") || rootName.Contains("turtle") ||
            rootName.Contains("skeleton") || rootName.Contains("golem") || rootName.Contains("boss"))
            return true;
        
        // If it has a HealthSystem but isn't the player, it might be an enemy
        HealthSystem health = obj.GetComponentInParent<HealthSystem>();
        if (health != null && !obj.transform.root.CompareTag("Player"))
            return true;
            
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        Debug.Log($"💥 Projectile collision with: {collision.gameObject.name}");
        
        // Don't hit the player
        if (collision.gameObject.CompareTag("Player")) return;
        
        // Try to damage
        HealthSystem targetHealth = collision.gameObject.GetComponent<HealthSystem>();
        if (targetHealth == null)
        {
            targetHealth = collision.gameObject.GetComponentInParent<HealthSystem>();
        }
        if (targetHealth == null)
        {
            targetHealth = collision.gameObject.transform.root.GetComponent<HealthSystem>();
        }
        
        if (targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(damage);
            Debug.Log($"🔥💥 Collision damage: {damage} to {collision.gameObject.name}! HP: {targetHealth.CurrentHealth}/{targetHealth.maxHealth}");
            OnHit(collision.contacts[0].point);
            return;
        }
        
        // Hit environment
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
