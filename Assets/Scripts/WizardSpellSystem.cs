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
    public float lightSpellDamage = 25f;
    
    [Tooltip("Base damage for heavy spell (right click)")]
    public float heavySpellDamage = 50f;
    
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
    public float projectileSpeed = 30f;
    
    [Tooltip("Delay before projectile spawns (for animation sync)")]
    public float castDelay = 0.2f;

    [Header("Auto-Targeting")]
    [Tooltip("Enable auto-targeting nearest enemy")]
    public bool autoTargetEnabled = true;
    
    [Tooltip("Maximum range to auto-target enemies")]
    public float autoTargetRange = 25f;
    
    [Tooltip("Layers that count as enemies")]
    public LayerMask enemyLayers;
    
    [Tooltip("Enable homing on projectiles")]
    public bool enableHoming = true;
    
    [Tooltip("Homing strength (0-10)")]
    [Range(0f, 10f)]
    public float homingStrength = 3f;

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
    private bool isCasting = false;

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
        isCasting = true;

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
        
        if (prefab != null)
        {
            // Spawn projectile at hand position/rotation
            GameObject projectile = Instantiate(prefab, spawnPos, Quaternion.LookRotation(targetDir));
            
            // CRITICAL: Ensure it is NOT a child of the hand so it can fly away
            projectile.transform.SetParent(null);

            MagicProjectile magicProj = projectile.GetComponent<MagicProjectile>();
            if (magicProj != null)
            {
                float homing = enableHoming ? homingStrength : 0f;
                // If we have a target, tell the projectile to home in on it
                magicProj.Initialize(targetDir, damage, currentTarget, homing);
                magicProj.speed = projectileSpeed;
            }
            else
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb == null) rb = projectile.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.velocity = targetDir * projectileSpeed;
                Destroy(projectile, 5f);
            }
        }

        isCasting = false;
    }

    private Vector3 GetTargetDirection()
    {
        // 1. If we have an auto-target, aim directy at it
        if (currentTarget != null)
        {
            Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
            return (targetPos - spellSpawnPoint.position).normalized;
        }

        // 2. Otherwise aim where the player is looking (center of screen)
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return (hit.point - spellSpawnPoint.position).normalized;
            }
            return ray.direction;
        }

        // 3. Fallback: Shoot where the Wizard's body is facing
        return transform.forward;
    }

    private void UpdateAutoTarget()
    {
        currentTarget = null;
        float closestDist = autoTargetRange;

        Collider[] enemies = Physics.OverlapSphere(transform.position, autoTargetRange, enemyLayers);

        foreach (Collider enemy in enemies)
        {
            HealthSystem health = enemy.GetComponent<HealthSystem>();
            if (health == null) health = enemy.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) continue;
            
            if (enemy.transform.root == transform.root) continue;

            // Check if enemy is in a 120-degree cone in front of wizard
            Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);
            if (angle > 60f) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                currentTarget = enemy.transform;
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
        lightSpellPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightSpellPrefab.name = "DefaultMagicProjectile";
        lightSpellPrefab.transform.localScale = Vector3.one * 0.3f;
        SphereCollider col = lightSpellPrefab.GetComponent<SphereCollider>();
        col.isTrigger = true;
        Rigidbody rb = lightSpellPrefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        MagicProjectile mp = lightSpellPrefab.AddComponent<MagicProjectile>();
        Renderer renderer = lightSpellPrefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.5f, 1f);
        mat.SetColor("_EmissionColor", new Color(0.5f, 0.7f, 1f) * 2f);
        mat.EnableKeyword("_EMISSION");
        renderer.material = mat;
        lightSpellPrefab.SetActive(false);
        heavySpellPrefab = lightSpellPrefab;
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
