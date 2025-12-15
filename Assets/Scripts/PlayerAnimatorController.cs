using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterController _controller;

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
    private int _isDefendingHash; // Bool for holding shield
    private int _defendHitTriggerHash; // Blocked an attack
    private int _getHitTriggerHash; // Took damage
    
    // States
    private int _isDizzyHash;
    private int _victoryBoolHash;
    private int _dieTriggerHash;
    private int _respawnTriggerHash;

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
        _isDefendingHash = Animator.StringToHash("IsDefending");
        _defendHitTriggerHash = Animator.StringToHash("DefendHit");
        _getHitTriggerHash = Animator.StringToHash("GetHit");

        _isDizzyHash = Animator.StringToHash("IsDizzy");
        _victoryBoolHash = Animator.StringToHash("Victory");
        _dieTriggerHash = Animator.StringToHash("Die");
        _respawnTriggerHash = Animator.StringToHash("Respawn");
    }

    private void Update()
    {
        // Handle continuous physical parameters automatically
        UpdateMovementParameters();
    }

    private void UpdateMovementParameters()
    {
        if (_controller == null) return;

        // Calculate horizontal speed (ignoring jumping/falling Y)
        Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Send Speed to Animator
        _animator.SetFloat(_speedHash, currentSpeed);
        _animator.SetBool(_isGroundedHash, _controller.isGrounded);
    }

    // =========================================================
    // PUBLIC API - Call these from your PlayerMovement or Input Script
    // =========================================================

    /// <summary>
    /// Updates values for Blend Trees (Strafing)
    /// </summary>
    public void SetLocomotionInput(float x, float y, bool isSprinting)
    {
        _animator.SetFloat(_inputXHash, x);
        _animator.SetFloat(_inputYHash, y);
        _animator.SetBool(_isSprintingHash, isSprinting);
    }

    public void SetCrouch(bool isCrouching)
    {
        _animator.SetBool(_crouchBoolHash, isCrouching);
    }

    public void TriggerJump()
    {
        // Only trigger jump if we aren't already jumping to prevent spam
        if(_controller.isGrounded)
            _animator.SetTrigger(_jumpTriggerHash);
    }

    /// <summary>
    /// Triggers an attack. 
    /// Index 1 = Attack01, Index 2 = Attack02, etc.
    /// </summary>
    public void TriggerAttack(int attackIndex)
    {
        _animator.SetInteger(_attackIndexHash, attackIndex);
        _animator.SetTrigger(_attackTriggerHash);
    }

    public void SetDefending(bool isDefending)
    {
        _animator.SetBool(_isDefendingHash, isDefending);
    }

    public void TriggerDefendHit()
    {
        _animator.SetTrigger(_defendHitTriggerHash);
    }

    public void TriggerGetHit()
    {
        _animator.SetTrigger(_getHitTriggerHash);
    }

    public void SetDizzy(bool state)
    {
        _animator.SetBool(_isDizzyHash, state);
    }

    public void TriggerInteraction() => _animator.SetTrigger(_interactTriggerHash);
    public void TriggerPickUp() => _animator.SetTrigger(_pickupTriggerHash);
    public void TriggerPotion() => _animator.SetTrigger(_potionTriggerHash);

    public void SetVictory(bool state) => _animator.SetBool(_victoryBoolHash, state);

    public void TriggerDeath() => _animator.SetTrigger(_dieTriggerHash);
    public void TriggerRespawn() => _animator.SetTrigger(_respawnTriggerHash); // For DieRecovery
}