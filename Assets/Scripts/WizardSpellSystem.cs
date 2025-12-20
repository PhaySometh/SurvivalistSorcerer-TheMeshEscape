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
    public float projectileSpeed = 20f;
    
    [Tooltip("Delay before projectile spawns (for animation sync)")]
    public float castDelay = 0.2f;

    [Header("Auto-Targeting")]
    [Tooltip("Enable auto-targeting nearest enemy")]
    public bool autoTargetEnabled = true;
    
    [Tooltip("Maximum range to auto-target enemies")]
    public float autoTargetRange = 20f;
    
    [Tooltip("Layers that count as enemies")]
    public LayerMask enemyLayers;
    
    [Tooltip("Enable homing on projectiles")]
    public bool enableHoming = true;
    
    [Tooltip("Homing strength (0-10)")]
    [Range(0f, 10f)]
    public float homingStrength = 3f;

    [Header("Charge Settings")]
    [Tooltip("Is charging required for heavy spell?")]
    public bool useChargeMechanic = true;
    
    [Tooltip("How long to reach maximum charge")]
    public float maxChargeTime = 1.5f;
    
    [Tooltip("Minimum charge ratio to fire (0-1)")]
    public float minChargeRequired = 0.2f;

    [Header("Audio")]
    public AudioClip[] castSounds;
    public AudioClip chargeSound;

    [Header("Visual Effects")]
    public GameObject castEffectPrefab;
    public GameObject chargeEffectPrefab;

    [Header("Events")]
    public UnityEvent OnSpellCast;
    public UnityEvent<float> OnDamageDealt;

    // Components
    private PlayerAnimatorController animController;
    private CharacterController characterController;
    private AudioSource audioSource;
    private AudioSource chargeAudioSource; // Dedicated for looping charge sound
    private Camera playerCamera;

    // State
    private float lastLightSpellTime = -999f;
    private float lastHeavySpellTime = -999f;
    private Transform currentTarget;
    private bool isCasting = false;
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private GameObject activeChargeEffect;

    // Cached enemies list for targeting
    private List<Transform> nearbyEnemies = new List<Transform>();

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

        // Secondary audio source for charging sound
        chargeAudioSource = gameObject.AddComponent<AudioSource>();
        chargeAudioSource.playOnAwake = false;
        chargeAudioSource.loop = true;
        chargeAudioSource.spatialBlend = 0.3f;
        chargeAudioSource.clip = chargeSound;

        playerCamera = Camera.main;
        
        // Default enemy layers if not set
        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy", "Default");
        }

        // Create default spawn point if not assigned
        if (spellSpawnPoint == null)
        {
            // Create a spawn point in front of player
            GameObject spawnObj = new GameObject("SpellSpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = new Vector3(0.5f, 1.2f, 0.8f); // Right hand, approximately
            spellSpawnPoint = spawnObj.transform;
        }
        
        // Create default projectile if none assigned
        if (lightSpellPrefab == null)
        {
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

    /// <summary>
    /// Handle spell casting input
    /// </summary>
    private void HandleSpellInput()
    {
        bool isInAir = !characterController.isGrounded;

        // --- LIGHT SPELL (Left Click) ---
        if (Input.GetMouseButtonDown(0) && !isCharging)
        {
            if (Time.time >= lastLightSpellTime + lightSpellCooldown)
            {
                CastSpell(SpellType.Light, 1f, isInAir);
                lastLightSpellTime = Time.time;
            }
        }

        // --- HEAVY SPELL (Right Click) ---
        if (useChargeMechanic)
        {
            HandleChargingInput(isInAir);
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (Time.time >= lastHeavySpellTime + heavySpellCooldown)
                {
                    CastSpell(SpellType.Heavy, 1f, isInAir);
                    lastHeavySpellTime = Time.time;
                }
            }
        }
    }

    /// <summary>
    /// Handle the press-hold-release charging logic
    /// </summary>
    private void HandleChargingInput(bool isInAir)
    {
        // Press Down
        if (Input.GetMouseButtonDown(1) && !isCharging && Time.time >= lastHeavySpellTime + heavySpellCooldown)
        {
            StartCharging();
        }

        // Holding
        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;
            
            // Visual/Sound feedback could scale here
            if (activeChargeEffect != null)
            {
                float scale = Mathf.Lerp(0.5f, 1.5f, currentChargeTime / maxChargeTime);
                activeChargeEffect.transform.localScale = Vector3.one * scale;
            }
            
            // Auto-fire if held too long (optional)
            if (currentChargeTime >= maxChargeTime + 0.5f)
            {
                FireChargedSpell(isInAir);
            }
        }

        // Release
        if (Input.GetMouseButtonUp(1) && isCharging)
        {
            FireChargedSpell(isInAir);
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        currentChargeTime = 0f;
        
        // Visual
        if (chargeEffectPrefab != null && spellSpawnPoint != null)
        {
            activeChargeEffect = Instantiate(chargeEffectPrefab, spellSpawnPoint.position, spellSpawnPoint.rotation);
            activeChargeEffect.transform.SetParent(spellSpawnPoint);
        }
        
        // Audio
        if (chargeAudioSource != null && chargeSound != null)
        {
            chargeAudioSource.Play();
        }
    }

    private void FireChargedSpell(bool isInAir)
    {
        float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);
        
        // Stop charging logic
        isCharging = false;
        if (chargeAudioSource != null) chargeAudioSource.Stop();
        if (activeChargeEffect != null) Destroy(activeChargeEffect);

        // Only fire if charged enough
        if (chargeRatio >= minChargeRequired)
        {
            CastSpell(SpellType.Heavy, chargeRatio, isInAir);
            lastHeavySpellTime = Time.time;
        }
    }

    /// <summary>
    /// Cast a spell with a specific power ratio
    /// </summary>
    public void CastSpell(SpellType spellType, float powerRatio, bool isInAir = false)
    {
        isCasting = true;

        // Calculate damage scaled by powerRatio
        float baseDmg = spellType == SpellType.Heavy ? heavySpellDamage : lightSpellDamage;
        float damage = baseDmg * powerRatio;
        
        if (isInAir) damage *= airSpellMultiplier;

        // Get stats bonus if available
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            damage += (stats.CurrentAttackDamage - 25f) * powerRatio;
        }

        // Trigger animation
        int attackIndex = spellType == SpellType.Heavy ? 3 : 1;
        if (isInAir)
        {
            animController.TriggerAirAttack();
        }
        else
        {
            animController.TriggerAttack(attackIndex);
        }

        // Spawn projectile after delay (sync with animation)
        StartCoroutine(SpawnProjectileDelayed(spellType, damage, powerRatio));

        // Play cast sound
        PlayCastSound();
        
        // Spawn cast effect
        SpawnCastEffect();

        OnSpellCast?.Invoke();
    }

    /// <summary>
    /// Spawn projectile after animation delay
    /// </summary>
    private IEnumerator SpawnProjectileDelayed(SpellType spellType, float damage, float powerRatio)
    {
        yield return new WaitForSeconds(castDelay);

        // Get direction
        Vector3 spawnPos = spellSpawnPoint.position;
        Vector3 targetDir = GetTargetDirection();

        // Choose prefab
        GameObject prefab = spellType == SpellType.Heavy ? heavySpellPrefab : lightSpellPrefab;
        if (prefab == null) prefab = lightSpellPrefab;
        
        if (prefab == null)
        {
            Debug.LogError("No spell prefab assigned!");
            yield break;
        }

        // Spawn projectile
        GameObject projectile = Instantiate(prefab, spawnPos, Quaternion.LookRotation(targetDir));
        
        // Scale projectile size based on power
        projectile.transform.localScale *= Mathf.Lerp(0.8f, 1.5f, powerRatio);

        // Configure projectile
        MagicProjectile magicProj = projectile.GetComponent<MagicProjectile>();
        if (magicProj != null)
        {
            float homing = enableHoming ? homingStrength : 0f;
            magicProj.Initialize(targetDir, damage, currentTarget, homing);
            magicProj.speed = projectileSpeed;
        }
        else
        {
            // Fallback: Just add velocity via rigidbody
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null) rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.velocity = targetDir * projectileSpeed;
            
            Destroy(projectile, 5f);
        }

        isCasting = false;
    }

    /// <summary>
    /// Get direction to shoot (towards target or forward)
    /// </summary>
    private Vector3 GetTargetDirection()
    {
        // If we have a valid target, aim at it
        if (currentTarget != null)
        {
            Vector3 targetPos = currentTarget.position + Vector3.up * 1f; // Aim at center mass
            return (targetPos - spellSpawnPoint.position).normalized;
        }

        // Otherwise, shoot where camera is looking (crosshair direction)
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // Raycast to find what we're aiming at
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return (hit.point - spellSpawnPoint.position).normalized;
            }
            
            // If nothing hit, shoot in camera forward direction
            return ray.direction;
        }

        // Fallback: shoot forward
        return transform.forward;
    }

    /// <summary>
    /// Find and update the nearest enemy target
    /// </summary>
    private void UpdateAutoTarget()
    {
        currentTarget = null;
        float closestDist = autoTargetRange;

        // Find all enemies in range
        Collider[] enemies = Physics.OverlapSphere(transform.position, autoTargetRange, enemyLayers);

        foreach (Collider enemy in enemies)
        {
            // Skip if no health system or dead
            HealthSystem health = enemy.GetComponent<HealthSystem>();
            if (health == null) health = enemy.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) continue;
            
            // Skip self
            if (enemy.transform.root == transform.root) continue;

            // Check if in front of player (within 120 degree cone)
            Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);
            if (angle > 60f) continue;

            // Check distance
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                currentTarget = enemy.transform;
            }
        }
    }

    /// <summary>
    /// Play a random cast sound
    /// </summary>
    private void PlayCastSound()
    {
        if (castSounds != null && castSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = castSounds[Random.Range(0, castSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    /// <summary>
    /// Spawn cast visual effect
    /// </summary>
    private void SpawnCastEffect()
    {
        if (castEffectPrefab != null)
        {
            GameObject effect = Instantiate(castEffectPrefab, spellSpawnPoint.position, spellSpawnPoint.rotation);
            effect.transform.SetParent(spellSpawnPoint);
            Destroy(effect, 1f);
        }
    }

    /// <summary>
    /// Create a default simple projectile prefab at runtime (if none assigned)
    /// </summary>
    private void CreateDefaultProjectilePrefab()
    {
        // Create a simple sphere as projectile
        lightSpellPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightSpellPrefab.name = "DefaultMagicProjectile";
        lightSpellPrefab.transform.localScale = Vector3.one * 0.3f;
        
        // Make it a trigger
        SphereCollider col = lightSpellPrefab.GetComponent<SphereCollider>();
        col.isTrigger = true;
        
        // Add rigidbody
        Rigidbody rb = lightSpellPrefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Add magic projectile component
        MagicProjectile mp = lightSpellPrefab.AddComponent<MagicProjectile>();
        
        // Make it glow (simple material)
        Renderer renderer = lightSpellPrefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.5f, 1f); // Blue-ish
        mat.SetColor("_EmissionColor", new Color(0.5f, 0.7f, 1f) * 2f);
        mat.EnableKeyword("_EMISSION");
        renderer.material = mat;
        
        // Deactivate the template
        lightSpellPrefab.SetActive(false);
        
        // Also use for heavy spell
        heavySpellPrefab = lightSpellPrefab;
        
        Debug.Log("WizardSpellSystem: Created default projectile prefab. Assign custom prefabs for better visuals!");
    }

    /// <summary>
    /// Draw targeting gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw auto-target range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, autoTargetRange);

        // Draw current target line
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spellSpawnPoint != null ? spellSpawnPoint.position : transform.position, 
                           currentTarget.position + Vector3.up);
        }

        // Draw spell spawn point
        if (spellSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spellSpawnPoint.position, 0.1f);
        }
    }
}

/// <summary>
/// Spell type enum
/// </summary>
public enum SpellType
{
    Light,
    Heavy
}
