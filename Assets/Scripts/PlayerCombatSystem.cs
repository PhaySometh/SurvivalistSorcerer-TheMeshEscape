using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Enhanced Player Combat System
/// - Responsive animation system with attack combos
/// - Can attack while moving/sprinting/jumping
/// - Deals damage to enemies via weapon hitbox
/// </summary>
[RequireComponent(typeof(PlayerAnimatorController))]
public class PlayerCombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Base damage per attack")]
    public float baseDamage = 25f;
    
    [Tooltip("Damage multiplier for heavy attack (right click)")]
    public float heavyAttackMultiplier = 1.5f;
    
    [Tooltip("Damage multiplier for air attacks")]
    public float airAttackMultiplier = 1.2f;
    
    [Tooltip("Time window to chain combo attacks")]
    public float comboWindowTime = 0.8f;
    
    [Tooltip("Minimum time between attacks (prevents spam)")]
    public float attackCooldown = 0.3f;

    [Header("Weapon Hitbox")]
    [Tooltip("Transform representing the weapon/hand position")]
    public Transform weaponHitboxCenter;
    
    [Tooltip("Size of the weapon hitbox for detecting enemies")]
    public float hitboxRadius = 1.2f;
    
    [Tooltip("Layers that can be damaged")]
    public LayerMask damageableLayers;
    
    [Tooltip("Duration the hitbox is active during attack")]
    public float hitboxActiveTime = 0.3f;
    
    [Tooltip("Delay before hitbox activates (animation wind-up)")]
    public float hitboxActivationDelay = 0.15f;

    [Header("Animation Settings")]
    [Tooltip("Allow moving while attacking")]
    public bool canMoveWhileAttacking = true;
    
    [Tooltip("Speed reduction while attacking (1 = normal, 0.5 = half speed)")]
    [Range(0.1f, 1f)]
    public float attackMoveSpeedMultiplier = 0.6f;

    [Header("Audio")]
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    
    [Header("Events")]
    public UnityEvent<float> OnDealDamage;  // Passes damage dealt
    public UnityEvent OnAttackStart;
    public UnityEvent OnComboComplete;

    // Components
    private PlayerAnimatorController animController;
    private CharacterController characterController;
    private AudioSource audioSource;
    
    // State
    private int currentComboCount = 0;
    private float lastAttackTime = -999f;
    private float comboResetTime = 0f;
    private bool isAttacking = false;
    private bool isHitboxActive = false;
    private Coroutine hitboxCoroutine;
    
    // For tracking which enemies were hit this attack (prevent multi-hit)
    private System.Collections.Generic.HashSet<GameObject> hitEnemiesThisAttack = 
        new System.Collections.Generic.HashSet<GameObject>();

    // Public properties for other systems
    public bool IsAttacking => isAttacking;
    public bool CanMove => canMoveWhileAttacking || !isAttacking;
    public float MoveSpeedModifier => isAttacking ? attackMoveSpeedMultiplier : 1f;
    public int CurrentComboCount => currentComboCount;

    void Start()
    {
        animController = GetComponent<PlayerAnimatorController>();
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }

        // Default hitbox center to player transform if not set
        if (weaponHitboxCenter == null)
        {
            weaponHitboxCenter = transform;
        }
        
        // Set default damageable layers if not set
        if (damageableLayers == 0)
        {
            damageableLayers = LayerMask.GetMask("Enemy", "Default");
        }
    }

    void Update()
    {
        HandleCombatInput();
        UpdateComboState();
    }

    /// <summary>
    /// Handle combat input (attacks, blocking)
    /// </summary>
    private void HandleCombatInput()
    {
        // Check if we can attack (cooldown)
        if (Time.time < lastAttackTime + attackCooldown) return;

        // Left Click - Light Attack
        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack(AttackType.Light);
        }
        // Right Click - Heavy Attack
        else if (Input.GetMouseButtonDown(1))
        {
            PerformAttack(AttackType.Heavy);
        }
    }

    /// <summary>
    /// Update combo state - reset if window expires
    /// </summary>
    private void UpdateComboState()
    {
        if (currentComboCount > 0 && Time.time > comboResetTime)
        {
            // Combo window expired
            ResetCombo();
        }
    }

    /// <summary>
    /// Perform an attack
    /// </summary>
    public void PerformAttack(AttackType attackType)
    {
        bool isInAir = !characterController.isGrounded;
        
        // Determine attack index for animation
        int attackIndex;
        float damageMultiplier = 1f;

        if (isInAir)
        {
            // Air attack - uses special animation
            attackIndex = 5; // JumpAirAttack or JumpUpAttack
            damageMultiplier = airAttackMultiplier;
            animController.TriggerAirAttack();
        }
        else
        {
            // Ground combo attack
            currentComboCount++;
            
            if (attackType == AttackType.Heavy)
            {
                // Heavy attacks use Attack03 or Attack04
                attackIndex = currentComboCount <= 1 ? 3 : 4;
                damageMultiplier = heavyAttackMultiplier;
            }
            else
            {
                // Light attacks cycle through Attack01, Attack02
                attackIndex = ((currentComboCount - 1) % 2) + 1;
            }
            
            // Cap combo at 4 attacks
            if (currentComboCount >= 4)
            {
                OnComboComplete?.Invoke();
                currentComboCount = 0;
            }
            
            animController.TriggerAttack(attackIndex);
        }

        // Update state
        lastAttackTime = Time.time;
        comboResetTime = Time.time + comboWindowTime;
        isAttacking = true;
        
        OnAttackStart?.Invoke();
        
        // Play attack sound
        PlayAttackSound();
        
        // Calculate damage for this attack
        float attackDamage = baseDamage * damageMultiplier;
        
        // Start hitbox detection coroutine
        if (hitboxCoroutine != null)
        {
            StopCoroutine(hitboxCoroutine);
        }
        hitboxCoroutine = StartCoroutine(ActivateHitbox(attackDamage));
    }

    /// <summary>
    /// Coroutine to activate hitbox for a short duration
    /// </summary>
    private IEnumerator ActivateHitbox(float damage)
    {
        hitEnemiesThisAttack.Clear();
        
        // Wait for animation wind-up
        yield return new WaitForSeconds(hitboxActivationDelay);
        
        isHitboxActive = true;
        float hitboxEndTime = Time.time + hitboxActiveTime;
        
        // Continuously check for hits during active window
        while (Time.time < hitboxEndTime)
        {
            CheckForHits(damage);
            yield return null; // Check every frame
        }
        
        isHitboxActive = false;
        isAttacking = false;
        hitEnemiesThisAttack.Clear();
    }

    /// <summary>
    /// Check for enemies in hitbox and deal damage
    /// </summary>
    private void CheckForHits(float damage)
    {
        // Get hitbox position (in front of player + offset up)
        Vector3 hitboxPos = weaponHitboxCenter.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        
        // Find all colliders in range
        Collider[] hits = Physics.OverlapSphere(hitboxPos, hitboxRadius, damageableLayers);
        
        foreach (Collider hit in hits)
        {
            GameObject target = hit.gameObject;
            
            // Skip if already hit this attack
            if (hitEnemiesThisAttack.Contains(target)) continue;
            
            // Skip self
            if (target == gameObject) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            
            // Try to find HealthSystem on target or parent
            HealthSystem targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth == null)
            {
                targetHealth = target.GetComponentInParent<HealthSystem>();
            }
            
            if (targetHealth != null && !targetHealth.IsDead)
            {
                // Deal damage!
                targetHealth.TakeDamage(damage);
                hitEnemiesThisAttack.Add(target);
                
                // Play hit effects
                PlayHitSound();
                SpawnHitEffect(hit.ClosestPoint(hitboxPos));
                
                OnDealDamage?.Invoke(damage);
                
                Debug.Log($"⚔️ Player dealt {damage} damage to {target.name}!");
            }
        }
    }

    /// <summary>
    /// Reset combo state
    /// </summary>
    private void ResetCombo()
    {
        currentComboCount = 0;
    }

    /// <summary>
    /// Play a random attack sound
    /// </summary>
    private void PlayAttackSound()
    {
        if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    /// <summary>
    /// Play a random hit sound
    /// </summary>
    private void PlayHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(clip, 0.8f);
        }
    }

    /// <summary>
    /// Spawn hit effect at position (can be expanded with particle systems)
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        // TODO: Spawn particle effect at hit position
        // For now, just log
    }

    /// <summary>
    /// Draw hitbox gizmo for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Transform center = weaponHitboxCenter != null ? weaponHitboxCenter : transform;
        Vector3 hitboxPos = center.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        
        Gizmos.color = isHitboxActive ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(hitboxPos, hitboxRadius);
    }
}

/// <summary>
/// Attack type enum
/// </summary>
public enum AttackType
{
    Light,
    Heavy
}
