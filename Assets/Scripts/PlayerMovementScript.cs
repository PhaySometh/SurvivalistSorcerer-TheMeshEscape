using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerAnimatorController))]
public class PlayerMovementScript : MonoBehaviour
{
    [Header("Components")]
    public Camera playerCamera;
    public GameObject characterModel; // Optional: If model is a child object
    
    // Reference to our new Animation API
    private PlayerAnimatorController animController;
    private CharacterController characterController;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float crouchSpeed = 3f;
    public float jumpPower = 5f;
    public float gravity = 30f;
    
    [Header("Rotation Settings")]
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float rotateToMovementSpeed = 10f; // How fast character turns

    [Header("Collider Settings")]
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Input Settings")]
    public bool useCameraRelativeMovement = true;
    public bool canMove = true;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    private float lastDebugLogTime = 0f;

    // Internal State
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    public bool IsSprinting { get; private set; } = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // AUTOMATICALLY CONNECT TO THE ANIMATOR SCRIPT
        animController = GetComponent<PlayerAnimatorController>();
        if (animController == null)
            Debug.LogError("PlayerAnimatorController is missing! Please attach it to the player.");

        // Fallback for camera
        if (playerCamera == null) playerCamera = Camera.main;

        // Hide Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleActionInputs(); // New method to handle Attacks/Interactions
    }

    void HandleMovement()
    {
        // 1. Read Input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 2. Determine Sprint Status
        // We sprint if moving in ANY direction while holding Shift (omnidirectional)
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);
        Vector2 inputVector = new Vector2(h, v);
        IsSprinting = isShiftHeld && inputVector.magnitude > 0.1f;

        // 3. Determine Speed based on state
        float currentSpeed = walkSpeed;
        if (IsSprinting) currentSpeed = runSpeed;
        
        // Check Crouch State - WITH SAFETY CHECK
        bool isCrouching = Input.GetKey(KeyCode.C);
        if (isCrouching) 
        {
            // SAFETY: Only shrink collider if we're grounded to prevent falling through
            if (characterController.isGrounded)
            {
                currentSpeed = crouchSpeed;
                characterController.height = crouchHeight; // Physically shrink collider
            }
            else
            {
                // In air - don't change height, just reduce speed
                currentSpeed = crouchSpeed;
                isCrouching = false; // Don't trigger crouch animation in air
            }
        }
        else
        {
            // SAFETY: Smoothly restore height to prevent popping through ceiling
            if (characterController.height < defaultHeight)
            {
                // Check if there's room above to stand up
                Vector3 headCheck = transform.position + Vector3.up * defaultHeight;
                if (!Physics.SphereCast(transform.position, 0.3f, Vector3.up, out RaycastHit hit, defaultHeight - crouchHeight + 0.1f))
                {
                    characterController.height = defaultHeight; // Reset collider
                }
                // else stay crouched if ceiling is too low
            }
            else
            {
                characterController.height = defaultHeight;
            }
        }


        // --- ANIMATION SYNC: LOCOMOTION ---
        if (animController != null)
        {
            // Pass raw input (h, v) to the Animator Blend Tree
            animController.SetLocomotionInput(h, v, IsSprinting);
            // Pass Crouch state
            animController.SetCrouch(isCrouching);
        }

        // 4. Calculate Movement Direction
        Vector3 inputDir = new Vector3(h, 0f, v);
        Vector3 desiredMove = Vector3.zero;

        if (useCameraRelativeMovement && playerCamera != null)
        {
            // Move relative to Camera
            Vector3 camForward = Vector3.Scale(playerCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = playerCamera.transform.right;
            desiredMove = (camForward * v + camRight * h);
        }
        else
        {
            // Move relative to Player transform
            desiredMove = transform.TransformDirection(inputDir);
        }

        // Keep the existing Y velocity (Gravity/Jump)
        float movementDirectionY = moveDirection.y;
        
        // Apply calculated speed - ENSURE horizontal movement is truly horizontal
        Vector3 horizontalMove = desiredMove.normalized * currentSpeed * (inputDir.magnitude > 1 ? 1 : inputDir.magnitude);
        horizontalMove.y = 0f; // Force Y to zero for horizontal movement
        moveDirection = new Vector3(horizontalMove.x, 0f, horizontalMove.z);

        // 5. Handle Jump
        if (Input.GetButtonDown("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
            
            // --- ANIMATION SYNC: JUMP ---
            animController.TriggerJump();
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // 6. Apply Gravity - ALWAYS apply downward force
        // CRITICAL FIX: Check if we're actually on real ground, not NavMesh geometry
        bool isActuallyGrounded = characterController.isGrounded;
        
        // Additional validation: Check if there's REAL terrain below us
        if (isActuallyGrounded)
        {
            // Verify ground with raycast - only check Ground layer
            LayerMask groundCheck = LayerMask.GetMask("Ground", "Default", "Terrain");
            if (groundCheck != 0) // Only check if these layers exist
            {
                // Cast ray slightly ahead to catch edges
                Vector3 rayStart = transform.position + Vector3.up * 0.2f;
                bool hasGroundBelow = Physics.Raycast(rayStart, Vector3.down, 2.0f, groundCheck);
                
                if (!hasGroundBelow)
                {
                    // CharacterController thinks we're grounded, but there's no actual terrain below!
                    // This happens when standing on NavMesh geometry
                    isActuallyGrounded = false;
                    
                    // Apply stronger downward force to pull player down
                    moveDirection.y = -gravity * 0.5f;
                    
                    // Log only once per second to avoid spam
                    if (showDebugLogs && Time.time - lastDebugLogTime > 1f)
                    {
                        Debug.LogWarning("⚠️ False ground detected! Forcing gravity.");
                        lastDebugLogTime = Time.time;
                    }
                }
            }
        }
        
        if (!isActuallyGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
            
            // Clamp maximum fall speed to prevent physics issues
            moveDirection.y = Mathf.Max(moveDirection.y, -50f);
        }
        else
        {
            // When grounded, apply small downward force to keep player grounded
            // This prevents "floating" on slopes and uneven terrain
            if (moveDirection.y < 0)
            {
                moveDirection.y = -2f; // Small constant downward force
            }
        }

        // 7. Move the Controller - ONLY if enabled (prevents errors during respawn)
        if (characterController.enabled)
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }
        
        // 7.5 SKY-WALK PREVENTION: Check if we're rising when we shouldn't be
        // If player is moving upward but not jumping and not on a slope, force them down
        if (characterController.enabled && characterController.velocity.y > 0.5f && !Input.GetButton("Jump"))
        {
            // Check if there's ground below us
            if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 2f))
            {
                // No ground below - we're walking into the sky! Apply extra gravity
                characterController.Move(Vector3.down * gravity * Time.deltaTime);
            }
        }

        // 8. Rotate Character to face movement
        Vector3 flatMove = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (flatMove.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(flatMove.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateToMovementSpeed * Time.deltaTime);
        }

        // 9. Void Failsafe - Reset player if they fall through the map
        if (transform.position.y < -10f)
        {
            Debug.LogWarning("Player fell through map! Resetting position.");
            characterController.enabled = false;
            transform.position = new Vector3(0, 20f, 0); // Reset to safe spawn position
            characterController.enabled = true;
        }
        
        // // Optional: Camera Rotation Logic (Mouse Look)
        // if (canMove)
        // {
        //     float mx = Input.GetAxis("Mouse X") * lookSpeed;
        //     transform.Rotate(0, mx, 0); // Rotate player body horizontally
            
        //     // Rotate camera vertically
        //     rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        //     rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        //     if (playerCamera != null)
        //         playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        // }
    }

    void HandleActionInputs()
    {
        if (!canMove || animController == null) return;

        // NOTE: Attack handling is now done by PlayerCombatSystem
        // This method only handles non-combat interactions

        // --- DEFENSE ---
        // Hold Left Ctrl to Defend
        bool isBlocking = Input.GetKey(KeyCode.LeftControl);
        animController.SetDefending(isBlocking);

        // --- INTERACTIONS ---
        // E to Interact
        if (Input.GetKeyDown(KeyCode.E))
        {
            animController.TriggerInteraction();
        }

        // F to Pick Up
        if (Input.GetKeyDown(KeyCode.F))
        {
            animController.TriggerPickUp();
        }

        // Q to Drink Potion
        if (Input.GetKeyDown(KeyCode.Q))
        {
            animController.TriggerPotion();
        }
        
        // H for Debug: Simulate getting Hit (only in editor)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.H))
        {
            animController.TriggerGetHit();
        }

        // K for Debug: Simulate Death
        if (Input.GetKeyDown(KeyCode.K))
        {
            animController.TriggerDeath();
        }
        #endif
    }
    
    /// <summary>
    /// Get the current actual movement speed (used by other systems)
    /// </summary>
    public float GetCurrentSpeed()
    {
        // Check if combat system exists and is attacking (for speed reduction)
        PlayerCombatSystem combatSystem = GetComponent<PlayerCombatSystem>();
        float speedMultiplier = 1f;
        
        if (combatSystem != null && combatSystem.IsAttacking)
        {
            speedMultiplier = combatSystem.MoveSpeedModifier;
        }
        
        // Check for stats speed bonus
        PlayerStats stats = GetComponent<PlayerStats>();
        float speedBonus = 0f;
        if (stats != null)
        {
            speedBonus = stats.CurrentSpeedBonus;
        }
        
        float baseSpeed = IsSprinting ? runSpeed : walkSpeed;
        return (baseSpeed + speedBonus) * speedMultiplier;
    }
}