using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Realistic Third-Person Camera Controller
/// Features: Over-the-shoulder view, collision detection, smooth follow, FOV adjustments
/// </summary>
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera playerCamera;
    public Transform playerCharacter; // The character model's transform
    public Transform cameraLookPoint; // A transform at character's head for look target
    
    [Header("Distance Settings")]
    public float defaultDistance = 3f; // Distance behind player
    public float minDistance = 1f; // Minimum distance (for collision)
    public float maxDistance = 5f; // Maximum distance allowed
    public float defaultHeight = 0.6f; // Height offset (shoulder level)
    
    [Header("Rotation Settings")]
    public float mouseSensitivity = 2f;
    public float verticalRotationLimit = 45f; // Max angle up/down
    public float rotationSmoothTime = 0.1f; // Smoothing for rotation
    
    [Header("Follow Settings")]
    public float followSmoothTime = 0.15f; // How smooth the camera follows
    public bool invertMouseY = false; // Option to invert Y
    public float zoomSpeed = 2f; // Speed for scroll-wheel zoom
    
    [Header("Collision Settings")]
    public LayerMask collisionLayerMask = ~0; // Layers to check collision against (default: everything)
    public float collisionSmoothness = 0.2f; // How quickly camera moves when hitting obstacles
    public bool avoidClipping = true; // Prevent clipping into objects
    
    [Header("FOV Settings")]
    public float defaultFOV = 60f;
    public float sprintFOV = 70f; // Slightly wider FOV when sprinting
    public float fovSmoothTime = 0.3f;
    
    // Private variables
    private float rotationX = 0f; // Vertical rotation (up/down)
    private float rotationY = 0f; // Horizontal rotation (left/right)
    private float targetRotationX = 0f;
    private float targetRotationY = 0f;
    
    private float velocityRotationX = 0f;
    private float velocityRotationY = 0f;
    
    private Vector3 cameraVelocity = Vector3.zero;
    private float targetDistance = 0f;
    private float currentDistance = 0f;
    private float distanceVelocity = 0f;
    
    private float targetFOV = 0f;
    private float fovVelocity = 0f;
    
    // Camera position calculation
    private Vector3 desiredCameraPosition = Vector3.zero;
    private Vector3 adjustedCameraPosition = Vector3.zero;
    
    // Pause state
    private bool isCameraActive = true;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();
        
        if (cameraLookPoint == null)
            cameraLookPoint = playerCharacter; // Fallback to character if no look point specified
        
        targetDistance = defaultDistance;
        currentDistance = defaultDistance;
        targetFOV = defaultFOV;
        
        // Lock cursor for camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // If camera is paused, unlock mouse and don't process input
        if (!isCameraActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertMouseY)
            mouseY = -mouseY;
        
        // Update target rotations
        targetRotationY += mouseX;
        targetRotationX -= mouseY;
        
        // Clamp vertical rotation to prevent over-rotation
        targetRotationX = Mathf.Clamp(targetRotationX, -verticalRotationLimit, verticalRotationLimit);
        
        // Smoothly interpolate to target rotations
        rotationX = Mathf.SmoothDamp(rotationX, targetRotationX, ref velocityRotationX, rotationSmoothTime);
        rotationY = Mathf.SmoothDamp(rotationY, targetRotationY, ref velocityRotationY, rotationSmoothTime);
        
        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Re-lock cursor on click (only if camera is still active)
        if (Input.GetMouseButtonDown(0) && Cursor.visible && isCameraActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Update FOV based on sprint state (check if PlayerMovementScript is running)
        UpdateFOV();

        // Zoom using scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (playerCharacter == null || playerCamera == null || !isCameraActive)
            return;
        
        // Calculate desired camera position (behind and above player)
        CalculateCameraPosition();
        
        // Handle collision avoidance
        if (avoidClipping)
        {
            AdjustCameraForCollisions();
        }
        else
        {
            adjustedCameraPosition = desiredCameraPosition;
        }
        
        // Smoothly move camera to desired position
        playerCamera.transform.position = Vector3.SmoothDamp(
            playerCamera.transform.position,
            adjustedCameraPosition,
            ref cameraVelocity,
            followSmoothTime
        );
        
        
        // Safety: Ensure we don't look at a NaN position
        Vector3 lookTarget = cameraLookPoint.position + Vector3.up * defaultHeight;
        if (float.IsNaN(lookTarget.x) || float.IsNaN(lookTarget.y) || float.IsNaN(lookTarget.z)) return;

        // Look at character's head/look point
        playerCamera.transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Calculate the desired camera position behind and above the player
    /// </summary>
    void CalculateCameraPosition()
    {
        // Create rotation from target angles
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        
        // Calculate backward direction (behind player)
        Vector3 backwardDirection = rotation * Vector3.back;
        
        // Calculate camera position relative to character
        desiredCameraPosition = cameraLookPoint.position 
            + Vector3.up * defaultHeight 
            + backwardDirection * currentDistance;
    }

    /// <summary>
    /// Check for collisions and adjust camera distance/position
    /// Prevents camera from clipping through objects
    /// </summary>
    void AdjustCameraForCollisions()
    {
        adjustedCameraPosition = desiredCameraPosition;
        
        // Raycast from player to desired camera position
        Vector3 direction = (desiredCameraPosition - cameraLookPoint.position).normalized;
        float distance = Vector3.Distance(desiredCameraPosition, cameraLookPoint.position);
        
        RaycastHit hit;
        
        // Cast ray and check for collisions
        if (Physics.Raycast(cameraLookPoint.position, direction, out hit, distance, collisionLayerMask))
        {
            // Move camera closer to avoid collision
            float collisionDistance = Vector3.Distance(cameraLookPoint.position, hit.point) - 0.2f; // Small buffer
            collisionDistance = Mathf.Max(collisionDistance, minDistance);
            
            // Smoothly transition to collision distance
            currentDistance = Mathf.SmoothDamp(
                currentDistance,
                collisionDistance,
                ref distanceVelocity,
                collisionSmoothness
            );
            
            // Recalculate position with new distance
            CalculateCameraPosition();
            adjustedCameraPosition = desiredCameraPosition;
        }
        else
        {
            // No collision, smoothly return to default distance
            currentDistance = Mathf.SmoothDamp(
                currentDistance,
                targetDistance,
                ref distanceVelocity,
                collisionSmoothness
            );
        }
    }

    /// <summary>
    /// Update FOV based on character movement state
    /// </summary>
    void UpdateFOV()
    {
        // Prefer a PlayerMovementScript's sprint state, otherwise fall back to Shift key
        bool sprinting = false;
        if (playerCharacter != null)
        {
            var pm = playerCharacter.GetComponent<PlayerMovementScript>();
            if (pm != null)
                sprinting = pm.IsSprinting;
            else
                sprinting = Input.GetKey(KeyCode.LeftShift);
        }
        else
        {
            sprinting = Input.GetKey(KeyCode.LeftShift);
        }

        targetFOV = sprinting ? sprintFOV : defaultFOV;
        
        playerCamera.fieldOfView = Mathf.SmoothDamp(
            playerCamera.fieldOfView,
            targetFOV,
            ref fovVelocity,
            fovSmoothTime
        );
    }

    /// <summary>
    /// Public method to set camera distance (useful for cutscenes or transitions)
    /// </summary>
    public void SetCameraDistance(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    /// <summary>
    /// Public method to toggle camera lock (for menus, etc)
    /// </summary>
    public void LockCamera(bool locked)
    {
        enabled = !locked;
    }

    /// <summary>
    /// Pause camera input and unlock mouse (for Game Over, menus, etc)
    /// </summary>
    public void PauseCamera()
    {
        isCameraActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Resume camera input and lock mouse
    /// </summary>
    public void ResumeCamera()
    {
        isCameraActive = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Debug visualization of camera raycast
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (playerCharacter == null || !avoidClipping)
            return;
        
        // Draw desired camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(desiredCameraPosition, 0.2f);
        
        // Draw line from character to camera
        Gizmos.color = Color.green;
        Gizmos.DrawLine(cameraLookPoint.position, desiredCameraPosition);
    }
}
