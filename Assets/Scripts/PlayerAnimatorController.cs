using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterController _controller;

    [Header("Animation Settings")]
    [Tooltip("Transition time for smooth animation blending")]
    [SerializeField] private float _transitionDuration = 0.1f;
    
    [Tooltip("Allow interrupting attack animations with new attacks")]
    [SerializeField] private bool _allowAttackInterrupt = true;

    // --- Optimization: Hash IDs for performance ---
    // Locomotion
    private int _speedHash;
    private int _inputXHash; // For strafing
    private int _inputYHash; // For forward/back
    private int _isGroundedHash;
    private int _isSprintingHash;
    
    // Actions
    private int _jumpTriggerHash;
    private int _crouchBoolHash;
    private int _interactTriggerHash;
    private int _pickupTriggerHash;
    private int _potionTriggerHash;

    // Combat
    private int _attackTriggerHash;
    private int _attackIndexHash; // To select Attack 01, 02, 03...
    private int _airAttackTriggerHash; // For air attacks
    private int _isDefendingHash; // Bool for holding shield
    private int _defendHitTriggerHash; // Blocked an attack
    private int _getHitTriggerHash; // Took damage
    private int _isAttackingHash; // Bool for attack state
    
    // States
    private int _isDizzyHash;
    private int _victoryBoolHash;
    private int _dieTriggerHash;
    private int _respawnTriggerHash;
    
    // State tracking
    private bool _isAttacking = false;
    private float _attackEndTime = 0f;
    private bool _wasInAir = false; // Track if we were airborne
    
    // Public state
    public bool IsAttacking => _isAttacking && Time.time < _attackEndTime;
    public Animator Animator => _animator;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_controller == null) _controller = GetComponent<CharacterController>();

        // Initialize Hashes (Matches Parameter names in Animator)
        _speedHash = Animator.StringToHash("Speed");
        _inputXHash = Animator.StringToHash("InputX");
        _inputYHash = Animator.StringToHash("InputY");
        _isGroundedHash = Animator.StringToHash("IsGrounded");
        _isSprintingHash = Animator.StringToHash("IsSprinting");
        
        _jumpTriggerHash = Animator.StringToHash("Jump");
        _crouchBoolHash = Animator.StringToHash("IsCrouching");
        _interactTriggerHash = Animator.StringToHash("Interact");
        _pickupTriggerHash = Animator.StringToHash("PickUp");
        _potionTriggerHash = Animator.StringToHash("PotionDrink");

        _attackTriggerHash = Animator.StringToHash("Attack");
        _attackIndexHash = Animator.StringToHash("AttackIndex");
        _airAttackTriggerHash = Animator.StringToHash("AirAttack");
        _isDefendingHash = Animator.StringToHash("IsDefending");
        _defendHitTriggerHash = Animator.StringToHash("DefendHit");
        _getHitTriggerHash = Animator.StringToHash("GetHit");
        _isAttackingHash = Animator.StringToHash("IsAttacking");

        _isDizzyHash = Animator.StringToHash("IsDizzy");
        _victoryBoolHash = Animator.StringToHash("Victory");
        _dieTriggerHash = Animator.StringToHash("Die");
        _respawnTriggerHash = Animator.StringToHash("Respawn");
    }

    // Helper method to safely set bool parameters
    private void SafeSetBool(int hash, bool value)
    {
        if (_animator == null) return;
        try
        {
            _animator.SetBool(hash, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist in animator controller - silently ignore
        }
    }

    // Helper method to safely set float parameters
    private void SafeSetFloat(int hash, float value)
    {
        if (_animator == null) return;
        try
        {
            _animator.SetFloat(hash, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist in animator controller - silently ignore
        }
    }

    // Helper method to safely set trigger parameters
    private void SafeSetTrigger(int hash)
    {
        if (_animator == null) return;
        try
        {
            _animator.SetTrigger(hash);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist in animator controller - silently ignore
        }
    }

    // Helper method to safely set integer parameters
    private void SafeSetInteger(int hash, int value)
    {
        if (_animator == null) return;
        try
        {
            _animator.SetInteger(hash, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist in animator controller - silently ignore
        }
    }

    private void Update()
    {
        // Handle continuous physical parameters automatically
        UpdateMovementParameters();
        
        // CRITICAL FIX: Detect landing and reset attack state
        bool isCurrentlyGrounded = _controller.isGrounded;
        if (_wasInAir && isCurrentlyGrounded)
        {
            // Just landed! Force reset attack state to prevent stuck animation
            if (_isAttacking)
            {
                _isAttacking = false;
                SafeSetBool(_isAttackingHash, false);
                // Force transition to grounded state
                _animator.ResetTrigger(_airAttackTriggerHash);
                SafeSetBool(_isGroundedHash, true);
                Debug.Log("🔧 Landing detected - resetting attack state");
            }
        }
        _wasInAir = !isCurrentlyGrounded;
        
        // Update attack state
        if (_isAttacking && Time.time >= _attackEndTime)
        {
            _isAttacking = false;
            SafeSetBool(_isAttackingHash, false);
        }
    }


    private void UpdateMovementParameters()
    {
        if (_controller == null) return;

        // Calculate horizontal speed (ignoring jumping/falling Y)
        Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Send Speed to Animator
        SafeSetFloat(_speedHash, currentSpeed);
        SafeSetBool(_isGroundedHash, _controller.isGrounded);
    }

    // =========================================================
    // PUBLIC API - Call these from your PlayerMovement or Combat Script
    // =========================================================

    /// <summary>
    /// Updates values for Blend Trees (Strafing)
    /// </summary>
    public void SetLocomotionInput(float x, float y, bool isSprinting)
    {
        SafeSetFloat(_inputXHash, x);
        SafeSetFloat(_inputYHash, y);
        SafeSetBool(_isSprintingHash, isSprinting);
    }

    public void SetCrouch(bool isCrouching)
    {
        SafeSetBool(_crouchBoolHash, isCrouching);
    }

    public void TriggerJump()
    {
        // Only trigger jump if we aren't already jumping to prevent spam
        if(_controller.isGrounded)
            SafeSetTrigger(_jumpTriggerHash);
    }

    /// <summary>
    /// Triggers a ground attack. 
    /// Index 1 = Attack01, Index 2 = Attack02, etc.
    /// Uses crossfade for smooth transition.
    /// </summary>
    public void TriggerAttack(int attackIndex)
    {
        // Check if we can interrupt current attack
        if (_isAttacking && !_allowAttackInterrupt) return;
        
        SafeSetInteger(_attackIndexHash, attackIndex);
        SafeSetTrigger(_attackTriggerHash);
        SafeSetBool(_isAttackingHash, true);
        
        // Mark as attacking with timeout
        _isAttacking = true;
        _attackEndTime = Time.time + 0.6f; // Approximate attack duration
    }
    
    /// <summary>
    /// Triggers an air attack (JumpAirAttack or JumpUpAttack)
    /// </summary>
    public void TriggerAirAttack()
    {
        // prevent spamming air attacks to float
        if (_isAttacking) return;

        // Use crossfade for smoother air attack transition
        _animator.CrossFade("JumpAirAttack", _transitionDuration);
        SafeSetBool(_isAttackingHash, true);
        
        _isAttacking = true;
        _attackEndTime = Time.time + 0.5f;
    }

    /// <summary>
    /// Force play an animation by name with crossfade
    /// </summary>
    public void PlayAnimation(string animationName, float transitionTime = -1f)
    {
        if (transitionTime < 0) transitionTime = _transitionDuration;
        _animator.CrossFade(animationName, transitionTime);
    }

    public void SetDefending(bool isDefending)
    {
        SafeSetBool(_isDefendingHash, isDefending);
    }

    public void TriggerDefendHit()
    {
        SafeSetTrigger(_defendHitTriggerHash);
    }

    public void TriggerGetHit()
    {
        SafeSetTrigger(_getHitTriggerHash);
    }

    public void SetDizzy(bool state)
    {
        SafeSetBool(_isDizzyHash, state);
    }

    public void TriggerInteraction() => SafeSetTrigger(_interactTriggerHash);
    public void TriggerPickUp() => SafeSetTrigger(_pickupTriggerHash);
    public void TriggerPotion() => SafeSetTrigger(_potionTriggerHash);

    public void SetVictory(bool state) => SafeSetBool(_victoryBoolHash, state);

    public void TriggerDeath() => SafeSetTrigger(_dieTriggerHash);
    public void TriggerRespawn() => SafeSetTrigger(_respawnTriggerHash); // For DieRecovery
    
    /// <summary>
    /// Reset all triggers (useful when respawning)
    /// </summary>
    public void ResetAllTriggers()
    {
        _animator.ResetTrigger(_jumpTriggerHash);
        _animator.ResetTrigger(_attackTriggerHash);
        _animator.ResetTrigger(_airAttackTriggerHash);
        _animator.ResetTrigger(_getHitTriggerHash);
        _animator.ResetTrigger(_dieTriggerHash);
        _animator.ResetTrigger(_respawnTriggerHash);
        
        _isAttacking = false;
        SafeSetBool(_isAttackingHash, false);
    }
}