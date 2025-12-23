using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Wizard Spell Casting System
/// - Shoots magic projectiles at enemies
/// - Auto-targets nearest enemy (optional)
/// - Multiple spell types (light, heavy, air)
/// - Integrates with animation system
/// </summary>
[RequireComponent(typeof(PlayerAnimatorController))]
public class WizardSpellSystem : MonoBehaviour
{
    [Header("Spell Settings")]
    [Tooltip("Base damage for light spell (left click)")]
    public float lightSpellDamage = 500f;
    
    [Tooltip("Base damage for heavy spell (right click)")]
    public float heavySpellDamage = 800f;
    
    [Tooltip("Damage multiplier for air spells")]
    public float airSpellMultiplier = 1.2f;
    
    [Tooltip("Cooldown between light spells")]
    public float lightSpellCooldown = 0.4f;
    
    [Tooltip("Cooldown between heavy spells")]
    public float heavySpellCooldown = 1.0f;

    [Header("Projectile Settings")]
    [Tooltip("Prefab for light spell projectile")]
    public GameObject lightSpellPrefab;
    
    [Tooltip("Prefab for heavy spell projectile")]
    public GameObject heavySpellPrefab;
    
    [Tooltip("Where spells spawn from (hand/wand position)")]
    public Transform spellSpawnPoint;
    
    [Tooltip("Speed of projectiles")]
    public float projectileSpeed = 80f;
    
    [Tooltip("Delay before projectile spawns (for animation sync)")]
    public float castDelay = 0.2f;

    [Header("Auto-Targeting")]
    [Tooltip("Enable auto-targeting nearest enemy")]
    public bool autoTargetEnabled = true;
    
    [Tooltip("Maximum range to auto-target enemies")]
    public float autoTargetRange = 100f;
    
    [Tooltip("Layers that count as enemies")]
    public LayerMask enemyLayers;
    
    [Tooltip("Enable homing on projectiles")]
    public bool enableHoming = true;
    
    [Tooltip("Homing strength (0-10)")]
    [Range(0f, 10f)]
    public float homingStrength = 10f;

    [Header("Audio")]
    public AudioClip[] castSounds;
    
    [Header("Visual Effects")]
    public GameObject castEffectPrefab;

    [Header("Events")]
    public UnityEvent OnSpellCast;

    // Components
    private PlayerAnimatorController animController;
    private CharacterController characterController;
    private AudioSource audioSource;
    private Camera playerCamera;

    // State
    private float lastLightSpellTime = -999f;
    private float lastHeavySpellTime = -999f;
    private Transform currentTarget;

    void Start()
    {
        animController = GetComponent<PlayerAnimatorController>();
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.3f;
        }

        playerCamera = Camera.main;
        
        // Default enemy layers if not set
        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy", "Default");
        }

        // Create default spawn point if not assigned
        if (spellSpawnPoint == null)
        {
            GameObject spawnObj = new GameObject("SpellSpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = new Vector3(0.5f, 1.2f, 0.8f);
            spellSpawnPoint = spawnObj.transform;
        }
        
        // Create default projectile if none assigned
        if (lightSpellPrefab == null && heavySpellPrefab == null)
        {
            Debug.LogWarning("⚠️ No spell prefabs assigned! Creating default projectile...");
            CreateDefaultProjectilePrefab();
        }
    }

    void Update()
    {
        // Update target
        if (autoTargetEnabled)
        {
            UpdateAutoTarget();
        }

        HandleSpellInput();
    }

    private void HandleSpellInput()
    {
        bool isInAir = !characterController.isGrounded;

        // Left Click - Instant Light Spell
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= lastLightSpellTime + lightSpellCooldown)
            {
                CastSpell(SpellType.Light, isInAir);
                lastLightSpellTime = Time.time;
            }
        }

        // Right Click - Instant Heavy Spell
        if (Input.GetMouseButtonDown(1))
        {
            if (Time.time >= lastHeavySpellTime + heavySpellCooldown)
            {
                CastSpell(SpellType.Heavy, isInAir);
                lastHeavySpellTime = Time.time;
            }
        }
    }

    public void CastSpell(SpellType spellType, bool isInAir = false)
    {
        // Face the target before casting
        Transform targetToFace = currentTarget;
        if (targetToFace == null)
        {
            targetToFace = FindNearestEnemyInFront();
        }
        
        if (targetToFace != null)
        {
            // Instantly rotate player to face the target
            Vector3 directionToTarget = targetToFace.position - transform.position;
            directionToTarget.y = 0; // Keep rotation horizontal
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
        }
        
        // Calculate damage
        float damage = spellType == SpellType.Heavy ? heavySpellDamage : lightSpellDamage;
        if (isInAir) damage *= airSpellMultiplier;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            damage += (stats.CurrentAttackDamage - 25f);
        }

        // Trigger animation
        int attackIndex = spellType == SpellType.Heavy ? 3 : 1;
        if (isInAir)
            animController.TriggerAirAttack();
        else
            animController.TriggerAttack(attackIndex);

        StartCoroutine(SpawnProjectileDelayed(spellType, damage));
        PlayCastSound();
        SpawnCastEffect();

        OnSpellCast?.Invoke();
    }

    private IEnumerator SpawnProjectileDelayed(SpellType spellType, float damage)
    {
        yield return new WaitForSeconds(castDelay);

        Vector3 spawnPos = spellSpawnPoint.position;
        Vector3 targetDir = GetTargetDirection();

        GameObject prefab = spellType == SpellType.Heavy ? heavySpellPrefab : lightSpellPrefab;
        if (prefab == null) prefab = lightSpellPrefab;
        
        if (prefab == null)
        {
            Debug.LogError("❌ NO PROJECTILE PREFAB! Cannot spawn spell.");
            yield break;
        }
        
        // Spawn projectile at hand position/rotation
        GameObject projectile = Instantiate(prefab, spawnPos, Quaternion.LookRotation(targetDir));
        
        // CRITICAL: Ensure it is NOT a child of the hand so it can fly away
        projectile.transform.SetParent(null);
        projectile.SetActive(true); // Make sure it's active
        
        Debug.Log($"✨ Spawned projectile at {spawnPos}, shooting direction: {targetDir}");

        MagicProjectile magicProj = projectile.GetComponent<MagicProjectile>();
        if (magicProj != null)
        {
            float homing = enableHoming ? homingStrength : 0f;
            // If we have a target, tell the projectile to home in on it
            magicProj.Initialize(targetDir, damage, currentTarget, homing);
            magicProj.speed = projectileSpeed;
            Debug.Log($"🎯 Projectile initialized with damage {damage}, speed {projectileSpeed}, target: {(currentTarget != null ? currentTarget.name : "none")}");
        }
        else
        {
            // Fallback: manual movement with Rigidbody
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null) rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.velocity = targetDir * projectileSpeed;
            
            // Add a simple damage script
            SimpleProjectileDamage simpleDamage = projectile.AddComponent<SimpleProjectileDamage>();
            simpleDamage.damage = damage;
            
            Destroy(projectile, 5f);
            Debug.Log($"⚡ Fallback projectile spawned (no MagicProjectile component)");
        }
    }

    private Vector3 GetTargetDirection()
    {
        // 1. If we have an auto-target, aim directly at it
        if (currentTarget != null)
        {
            Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
            return (targetPos - spellSpawnPoint.position).normalized;
        }

        // 2. Try to find nearest enemy even if auto-target is off
        Transform nearestEnemy = FindNearestEnemyInFront();
        if (nearestEnemy != null)
        {
            Vector3 targetPos = nearestEnemy.position + Vector3.up * 1f;
            return (targetPos - spellSpawnPoint.position).normalized;
        }

        // 3. Aim horizontally forward (player's facing direction, but keep it level)
        // This prevents shooting to the sky
        Vector3 forwardDir = transform.forward;
        forwardDir.y = 0; // Keep horizontal
        if (forwardDir != Vector3.zero)
        {
            forwardDir.Normalize();
            // Slight upward angle for better hit detection
            return (forwardDir + Vector3.up * 0.1f).normalized;
        }

        // 4. Final fallback
        return transform.forward;
    }
    
    /// <summary>
    /// Find nearest enemy in front of player (backup if auto-target fails)
    /// </summary>
    private Transform FindNearestEnemyInFront()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, autoTargetRange);
        
        Transform nearest = null;
        float closestDist = autoTargetRange;
        
        foreach (Collider col in enemies)
        {
            // Check if enemy
            bool isEnemy = col.CompareTag("Enemy") || 
                          col.GetComponent<EnemyAI>() != null || 
                          col.GetComponentInParent<EnemyAI>() != null;
            
            if (!isEnemy) continue;
            
            // Check if alive
            HealthSystem health = col.GetComponent<HealthSystem>();
            if (health == null) health = col.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) continue;
            
            // Very wide cone - 150 degrees (almost behind player too)
            Vector3 toEnemy = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);
            if (angle > 150f) continue;
            
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = col.transform;
            }
        }
        
        return nearest;
    }

    private void UpdateAutoTarget()
    {
        currentTarget = null;
        float closestDist = autoTargetRange;

        // Use OverlapSphere without layer mask to catch all enemies
        Collider[] allColliders = Physics.OverlapSphere(transform.position, autoTargetRange);

        foreach (Collider col in allColliders)
        {
            // Check if this is an enemy
            bool isEnemy = col.CompareTag("Enemy") || 
                          col.transform.root.CompareTag("Enemy") ||
                          col.GetComponent<EnemyAI>() != null || 
                          col.GetComponentInParent<EnemyAI>() != null;
            
            if (!isEnemy) continue;
            
            // Check if alive
            HealthSystem health = col.GetComponent<HealthSystem>();
            if (health == null) health = col.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) continue;
            
            // Skip self
            if (col.transform.root == transform.root) continue;

            // Much wider targeting angle - basically 360 degrees
            Vector3 toEnemy = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);
            if (angle > 150f) continue; // Very wide cone, almost full circle

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                currentTarget = col.transform.root; // Use root for consistent targeting
            }
        }
        
        // If no target found in cone, just get the closest enemy anywhere
        if (currentTarget == null)
        {
            foreach (Collider col in allColliders)
            {
                bool isEnemy = col.CompareTag("Enemy") || 
                              col.transform.root.CompareTag("Enemy") ||
                              col.GetComponent<EnemyAI>() != null || 
                              col.GetComponentInParent<EnemyAI>() != null;
                
                if (!isEnemy) continue;
                
                HealthSystem health = col.GetComponent<HealthSystem>();
                if (health == null) health = col.GetComponentInParent<HealthSystem>();
                if (health == null || health.IsDead) continue;
                
                if (col.transform.root == transform.root) continue;
                
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = col.transform.root;
                }
            }
        }
    }

    private void PlayCastSound()
    {
        if (castSounds != null && castSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = castSounds[Random.Range(0, castSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    private void SpawnCastEffect()
    {
        if (castEffectPrefab != null && spellSpawnPoint != null)
        {
            // Spawn the effect at the wand tip
            GameObject effect = Instantiate(castEffectPrefab, spellSpawnPoint.position, spellSpawnPoint.rotation);
            
            // Only parent if it's NOT a prefab asset (safety check)
            if (spellSpawnPoint.gameObject.scene.name != null)
            {
                effect.transform.SetParent(spellSpawnPoint);
            }
            
            Destroy(effect, 1f);
        }
    }

    private void CreateDefaultProjectilePrefab()
    {
        Debug.Log("🔧 Creating default spell projectile prefab...");
        
        lightSpellPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightSpellPrefab.name = "DefaultMagicProjectile";
        lightSpellPrefab.transform.localScale = Vector3.one * 1.5f; // Larger for visibility and collision
        
        // Setup collider
        SphereCollider col = lightSpellPrefab.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2f; // Very large collision radius
        
        // Setup rigidbody
        Rigidbody rb = lightSpellPrefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Add MagicProjectile script
        MagicProjectile mp = lightSpellPrefab.AddComponent<MagicProjectile>();
        mp.speed = projectileSpeed;
        mp.damage = lightSpellDamage;
        mp.lifetime = 30f; // Very long lifetime for extreme range
        
        // Create glowing material
        Renderer renderer = lightSpellPrefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.5f, 1f); // Blue color
        mat.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 1f) * 3f); // Bright blue glow
        mat.EnableKeyword("_EMISSION");
        renderer.material = mat;
        
        // Add a light for effect
        Light light = lightSpellPrefab.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.3f, 0.7f, 1f);
        light.range = 5f;
        light.intensity = 2f;
        
        lightSpellPrefab.SetActive(false);
        heavySpellPrefab = lightSpellPrefab;
        
        Debug.Log("✅ Default projectile created successfully!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, autoTargetRange);
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spellSpawnPoint != null ? spellSpawnPoint.position : transform.position, currentTarget.position + Vector3.up);
        }
    }
}

public enum SpellType
{
    Light,
    Heavy
}
