using UnityEngine;
using System.Collections.Generic;

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
    private int _speedHash;
    private int _inputXHash;
    private int _inputYHash;
    private int _isGroundedHash;
    private int _isSprintingHash;
    private int _jumpTriggerHash;
    private int _crouchBoolHash;
    private int _interactTriggerHash;
    private int _pickupTriggerHash;
    private int _potionTriggerHash;
    private int _attackTriggerHash;
    private int _attackIndexHash;
    private int _airAttackTriggerHash;
    private int _isDefendingHash;
    private int _defendHitTriggerHash;
    private int _getHitTriggerHash;
    private int _isAttackingHash;
    private int _isDizzyHash;
    private int _victoryBoolHash;
    private int _dieTriggerHash;
    private int _respawnTriggerHash;
    
    // SAFETY: Track which parameters actually exist
    private HashSet<int> _validBools = new HashSet<int>();
    private HashSet<int> _validFloats = new HashSet<int>();
    private HashSet<int> _validInts = new HashSet<int>();
    private HashSet<int> _validTriggers = new HashSet<int>();
    
    // State tracking
    private bool _isAttacking = false;
    private float _attackEndTime = 0f;
    private bool _wasInAir = false;
    
    // Public state
    public bool IsAttacking => _isAttacking && Time.time < _attackEndTime;
    public Animator Animator => _animator;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_controller == null) _controller = GetComponent<CharacterController>();

        // Initialize Hashes
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

    private void Start()
    {
        // SAFETY: Cache which parameters actually exist in the Animator
        ValidateAnimatorParameters();
    }
    
    /// <summary>
    /// Check which parameters actually exist in the Animator Controller
    /// This prevents console spam from missing parameters
    /// </summary>
    private void ValidateAnimatorParameters()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("PlayerAnimatorController: No Animator or RuntimeAnimatorController found!");
            return;
        }
        
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            int hash = param.nameHash;
            
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    _validBools.Add(hash);
                    break;
                case AnimatorControllerParameterType.Float:
                    _validFloats.Add(hash);
                    break;
                case AnimatorControllerParameterType.Int:
                    _validInts.Add(hash);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    _validTriggers.Add(hash);
                    break;
            }
        }
        
        Debug.Log($"PlayerAnimatorController: Validated {_validBools.Count} bools, {_validFloats.Count} floats, {_validInts.Count} ints, {_validTriggers.Count} triggers");
    }
    
    // ========== SAFE SETTER METHODS ==========
    
    private void SafeSetBool(int hash, bool value)
    {
        if (_animator != null && _validBools.Contains(hash))
        {
            _animator.SetBool(hash, value);
        }
    }
    
    private void SafeSetFloat(int hash, float value)
    {
        if (_animator != null && _validFloats.Contains(hash))
        {
            _animator.SetFloat(hash, value);
        }
    }
    
    private void SafeSetInt(int hash, int value)
    {
        if (_animator != null && _validInts.Contains(hash))
        {
            _animator.SetInteger(hash, value);
        }
    }
    
    private void SafeSetTrigger(int hash)
    {
        if (_animator != null && _validTriggers.Contains(hash))
        {
            _animator.SetTrigger(hash);
        }
    }
    
    private void SafeResetTrigger(int hash)
    {
        if (_animator != null && _validTriggers.Contains(hash))
        {
            _animator.ResetTrigger(hash);
        }
    }

    private void Update()
    {
        UpdateMovementParameters();
        
        // Landing detection - reset attack state
        bool isCurrentlyGrounded = _controller.isGrounded;
        if (_wasInAir && isCurrentlyGrounded)
        {
            if (_isAttacking)
            {
                _isAttacking = false;
                SafeSetBool(_isAttackingHash, false);
                SafeResetTrigger(_airAttackTriggerHash);
                SafeSetBool(_isGroundedHash, true);
            }
        }
        _wasInAir = !isCurrentlyGrounded;
        
        // Auto-reset attack state
        if (_isAttacking && Time.time >= _attackEndTime)
        {
            _isAttacking = false;
            SafeSetBool(_isAttackingHash, false);
        }
    }

    private void UpdateMovementParameters()
    {
        if (_controller == null) return;

        Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        SafeSetFloat(_speedHash, currentSpeed);
        SafeSetBool(_isGroundedHash, _controller.isGrounded);
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

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
        if(_controller.isGrounded)
            SafeSetTrigger(_jumpTriggerHash);
    }

    public void TriggerAttack(int attackIndex)
    {
        if (_isAttacking && !_allowAttackInterrupt) return;
        
        SafeSetInt(_attackIndexHash, attackIndex);
        SafeSetTrigger(_attackTriggerHash);
        SafeSetBool(_isAttackingHash, true);
        
        _isAttacking = true;
        _attackEndTime = Time.time + 0.6f;
    }
    
    public void TriggerAirAttack()
    {
        if (_isAttacking) return;

        // CrossFade needs a valid state - check if it exists
        if (_animator != null && _animator.HasState(0, Animator.StringToHash("JumpAirAttack")))
        {
            _animator.CrossFade("JumpAirAttack", _transitionDuration, 0);
        }
        SafeSetBool(_isAttackingHash, true);
        
        _isAttacking = true;
        _attackEndTime = Time.time + 0.5f;
    }

    public void PlayAnimation(string animationName, float transitionTime = -1f)
    {
        if (_animator == null) return;
        if (transitionTime < 0) transitionTime = _transitionDuration;
        
        int hash = Animator.StringToHash(animationName);
        if (_animator.HasState(0, hash))
        {
            _animator.CrossFade(hash, transitionTime, 0);
        }
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

    public void TriggerInteraction()
    {
        SafeSetTrigger(_interactTriggerHash);
    }
    
    public void TriggerPickUp()
    {
        SafeSetTrigger(_pickupTriggerHash);
    }
    
    public void TriggerPotion()
    {
        SafeSetTrigger(_potionTriggerHash);
    }

    public void SetVictory(bool state)
    {
        SafeSetBool(_victoryBoolHash, state);
    }

    public void TriggerDeath()
    {
        SafeSetTrigger(_dieTriggerHash);
    }
    
    public void TriggerRespawn()
    {
        SafeSetTrigger(_respawnTriggerHash);
    }
    
    public void ResetAllTriggers()
    {
        SafeResetTrigger(_jumpTriggerHash);
        SafeResetTrigger(_attackTriggerHash);
        SafeResetTrigger(_airAttackTriggerHash);
        SafeResetTrigger(_getHitTriggerHash);
        SafeResetTrigger(_dieTriggerHash);
        SafeResetTrigger(_respawnTriggerHash);
        
        _isAttacking = false;
        SafeSetBool(_isAttackingHash, false);
    }
}