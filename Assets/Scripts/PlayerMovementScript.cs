using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject characterModel; // Assign MaleCharacterPBR here
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float jumpPower = 5f;
    public float gravity = 30f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    [Header("Movement Settings")]
    public bool useCameraRelativeMovement = true; // Move relative to camera forward/right
    public float rotateToMovementSpeed = 10f; // How fast the player rotates to movement direction

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;
    private PlayerStamina playerStamina; // NEW: Reference to stamina system

    // Public sprint state for other systems (cameras, VFX)
    public bool IsSprinting { get; private set; } = false;

    void Start()
    {
        // Prefer a CharacterController on the same GameObject, otherwise fall back to the assigned model
        characterController = GetComponent<CharacterController>();
        if (characterController == null && characterModel != null)
        {
            characterController = characterModel.GetComponent<CharacterController>();
        }
        if (characterController == null)
        {
            Debug.LogError("PlayerMovementScript requires a CharacterController on the player or the character model.");
        }
        
        // NEW: Get stamina component
        playerStamina = GetComponent<PlayerStamina>();
        if (playerStamina == null)
        {
            Debug.LogWarning("PlayerStamina not found! Stamina system disabled.");
        }
        
        // Default camera reference
        if (playerCamera == null)
            playerCamera = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // NEW: Check stamina before allowing sprint
        bool wantToRun = Input.GetKey(KeyCode.LeftShift);
        bool isRunning = wantToRun && (playerStamina == null || playerStamina.CanSprint());
        float speed = (isRunning ? runSpeed : walkSpeed);

        Vector3 input = new Vector3(h, 0f, v);
        Vector3 desiredMove = Vector3.zero;

        if (useCameraRelativeMovement && playerCamera != null)
        {
            // Move relative to camera direction on the XZ plane
            Vector3 camForward = Vector3.Scale(playerCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = playerCamera.transform.right;
            desiredMove = (camForward * v + camRight * h);
        }
        else
        {
            // Move relative to player forward/right
            desiredMove = transform.TransformDirection(new Vector3(h, 0, v));
        }

        float movementDirectionY = moveDirection.y;
        Vector3 horizontalMove = desiredMove.normalized * speed * input.magnitude;
        moveDirection = new Vector3(horizontalMove.x, moveDirection.y, horizontalMove.z);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.C) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            characterController.height = defaultHeight;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // Rotate player to movement direction when moving
        Vector3 flatMove = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (flatMove.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(flatMove.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateToMovementSpeed * Time.deltaTime);
        }
        else if (canMove)
        {
            // Optionally allow mouse to rotate view when not moving
            float mx = Input.GetAxis("Mouse X") * lookSpeed;
            transform.rotation *= Quaternion.Euler(0, mx, 0);
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
    }
}