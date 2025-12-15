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
        // We only sprint if moving forward and holding Shift
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);
        IsSprinting = isShiftHeld && v > 0; 

        // 3. Determine Speed based on state
        float currentSpeed = walkSpeed;
        if (IsSprinting) currentSpeed = runSpeed;
        
        // Check Crouch State
        bool isCrouching = Input.GetKey(KeyCode.C);
        if (isCrouching) 
        {
            currentSpeed = crouchSpeed;
            characterController.height = crouchHeight; // Physically shrink collider
        }
        else
        {
            characterController.height = defaultHeight; // Reset collider
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
        
        // Apply calculated speed
        Vector3 horizontalMove = desiredMove.normalized * currentSpeed * (inputDir.magnitude > 1 ? 1 : inputDir.magnitude);
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

        // 6. Apply Gravity
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // 7. Move the Controller
        characterController.Move(moveDirection * Time.deltaTime);

        // 8. Rotate Character to face movement
        Vector3 flatMove = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (flatMove.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(flatMove.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateToMovementSpeed * Time.deltaTime);
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

        // --- ATTACKS ---
        // Left Click = Attack 01
        if (Input.GetMouseButtonDown(0))
        {
            animController.TriggerAttack(1); 
        }
        // Right Click = Attack 02 (or Heavy Attack)
        if (Input.GetMouseButtonDown(1))
        {
            animController.TriggerAttack(2); 
        }

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
        
        // H for Debug: Simulate getting Hit
        if (Input.GetKeyDown(KeyCode.H))
        {
            animController.TriggerGetHit();
        }

        // K for Debug: Simulate Death
        if (Input.GetKeyDown(KeyCode.K))
        {
            animController.TriggerDeath();
        }
    }
}