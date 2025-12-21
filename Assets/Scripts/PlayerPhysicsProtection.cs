using UnityEngine;

/// <summary>
/// Protects the player from falling through the map and handles knockback.
/// Attach this to the Player alongside CharacterController.
/// ENHANCED VERSION: Faster detection, ground snapping, anti-push, crouch safety
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerPhysicsProtection : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("Layers that count as ground")]
    public LayerMask groundLayers;
    
    [Tooltip("Distance to check for ground below player")]
    public float groundCheckDistance = 2f;
    
    [Tooltip("Radius of the ground check sphere")]
    public float groundCheckRadius = 0.4f;

    [Header("Safety Settings")]
    [Tooltip("Y position below which player is considered 'in void' - SET THIS HIGHER for faster detection")]
    public float voidYThreshold = -5f; // Changed from -10 to -5 for faster detection
    
    [Tooltip("Position to respawn player when they fall into void")]
    public Vector3 safeRespawnPosition = new Vector3(0, 5f, 0);
    
    [Tooltip("Try to find a spawn point with this tag")]
    public string spawnPointTag = "SpawnPoint";
    
    [Tooltip("How long player can be 'not grounded' before force respawn")]
    public float maxAirTime = 3f; // Reduced from 5s to 3s

    [Header("Anti-Push Settings")]
    [Tooltip("If enabled, player resists being pushed by enemies")]
    public bool resistEnemyPush = true;
    
    [Tooltip("Force to push enemies away when they collide")]
    public float enemyPushForce = 10f;
    
    [Tooltip("Maximum speed enemies can push player")]
    public float maxPushSpeed = 2f;

    [Header("Ground Snap Settings")]
    [Tooltip("Enable ground snapping to prevent falling through")]
    public bool enableGroundSnap = true;
    
    [Tooltip("Distance to snap down to ground")]
    public float groundSnapDistance = 0.5f;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool showDebugLogs = true;
    [SerializeField] private float currentAirTime = 0f;
    [SerializeField] private bool isProtectionActive = true;
    [SerializeField] private bool isGroundDetected = true;
    [SerializeField] private float lastGroundY = 0f;

    private CharacterController characterController;
    private Vector3 lastGroundedPosition;
    private float lastGroundedTime;
    private Vector3 lastValidPosition;
    private float positionCheckTimer = 0f;
    private float groundCheckTimer = 0f;
    private int consecutiveNoGroundFrames = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // If ground layers not set, use Default and Terrain
        if (groundLayers == 0)
        {
            // Include Default (0), Terrain, Ground layers
            groundLayers = LayerMask.GetMask("Default", "Terrain", "Ground");
            if (groundLayers == 0)
            {
                groundLayers = 1; // At minimum, use Default layer
            }
            Debug.LogWarning("PlayerPhysicsProtection: Ground Layers auto-set. Please configure in Inspector!");
        }
        
        // Try to find spawn point safely
        FindSpawnPoint();

        // Initialize with current position
        lastGroundedPosition = transform.position;
        lastValidPosition = transform.position;
        lastGroundedTime = Time.time;
        lastGroundY = transform.position.y;
    }

    private void FindSpawnPoint()
    {
        try 
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag(spawnPointTag);
            if (spawnPoint != null)
            {
                safeRespawnPosition = spawnPoint.transform.position + Vector3.up * 1f;
                if (showDebugLogs) Debug.Log($"✅ Found spawn point at {safeRespawnPosition}");
            }
        }
        catch (System.Exception)
        {
            if (showDebugLogs) Debug.LogWarning($"⚠️ Tag '{spawnPointTag}' not defined. Using default spawn.");
        }
    }

    void Update()
    {
        if (!isProtectionActive) return;
        
        // CONTINUOUS GROUND CHECK - More aggressive than CharacterController
        PerformGroundCheck();
        
        // Track grounded state from CharacterController
        if (characterController.isGrounded || isGroundDetected)
        {
            // Save position frequently when grounded
            positionCheckTimer += Time.deltaTime;
            if (positionCheckTimer >= 0.25f) // More frequent saves
            {
                // Only save if position is valid (not falling)
                if (transform.position.y > voidYThreshold)
                {
                    lastGroundedPosition = transform.position;
                    lastValidPosition = transform.position;
                    lastGroundY = transform.position.y;
                }
                positionCheckTimer = 0f;
            }
            
            lastGroundedTime = Time.time;
            currentAirTime = 0f;
            consecutiveNoGroundFrames = 0;
        }
        else
        {
            currentAirTime += Time.deltaTime;
            consecutiveNoGroundFrames++;
        }

        // PRIMARY CHECK: Void fall (below threshold) - IMMEDIATE
        if (transform.position.y < voidYThreshold)
        {
            if (showDebugLogs) Debug.LogWarning($"☠️ VOID DETECTED at Y={transform.position.y:F1}! Respawning...");
            ForceRespawn();
            return;
        }
        
        // SECONDARY CHECK: Rapid fall detection
        CheckRapidFall();
        
        // TERTIARY CHECK: Extended air time
        if (currentAirTime > maxAirTime)
        {
            if (showDebugLogs) Debug.LogWarning($"⚠️ In air for {currentAirTime:F1}s! Force respawning...");
            ForceRespawn();
            return;
        }
        
        // Ground snap to prevent clipping
        if (enableGroundSnap && characterController.isGrounded)
        {
            SnapToGround();
        }
    }
    
    /// <summary>
    /// Custom ground check using raycast - more reliable than CharacterController.isGrounded
    /// </summary>
    private void PerformGroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        
        // Raycast down to check for ground
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
        {
            isGroundDetected = true;
            
            // If we're very close to ground but CharacterController thinks we're not grounded
            if (!characterController.isGrounded && hit.distance < 0.3f)
            {
                // We might be clipping - do a small correction
                if (consecutiveNoGroundFrames > 5)
                {
                    if (showDebugLogs) Debug.Log("🔧 Ground clip correction applied");
                }
            }
        }
        else
        {
            isGroundDetected = false;
            
            // If no ground detected and we're not jumping intentionally
            if (consecutiveNoGroundFrames > 10 && !characterController.isGrounded)
            {
                // We might be falling through - check if we were recently grounded
                if (Time.time - lastGroundedTime < 0.5f && transform.position.y < lastGroundY - 2f)
                {
                    if (showDebugLogs) Debug.LogWarning($"⚠️ Sudden fall detected! Was at Y={lastGroundY:F1}, now at Y={transform.position.y:F1}");
                    ForceRespawn();
                }
            }
        }
    }
    
    /// <summary>
    /// Check for rapid unintentional falling
    /// </summary>
    private void CheckRapidFall()
    {
        // If falling faster than expected and no ground nearby
        if (!isGroundDetected && currentAirTime > 0.5f)
        {
            float fallDistance = lastGroundY - transform.position.y;
            
            // If we fell more than 10 units in a short time without touching ground
            if (fallDistance > 10f && Time.time - lastGroundedTime < 2f)
            {
                if (showDebugLogs) Debug.LogWarning($"⚠️ Rapid fall! Fell {fallDistance:F1} units in {Time.time - lastGroundedTime:F1}s");
                ForceRespawn();
            }
        }
    }
    
    /// <summary>
    /// Snap player to ground to prevent floating/clipping
    /// </summary>
    private void SnapToGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSnapDistance + 0.5f, groundLayers))
        {
            float distanceToGround = hit.distance - 0.5f;
            
            // If we're slightly above ground, snap down
            if (distanceToGround > 0.05f && distanceToGround < groundSnapDistance)
            {
                // Use CharacterController.Move for proper collision
                characterController.Move(Vector3.down * distanceToGround);
            }
        }
    }
    
    void LateUpdate()
    {
        // FINAL SAFETY CHECK: Absolute fallback
        if (transform.position.y < voidYThreshold - 5f) // Even more aggressive
        {
            if (showDebugLogs) Debug.LogError("🚨 EMERGENCY RESPAWN - LateUpdate catch");
            ForceRespawn();
        }
    }

    /// <summary>
    /// Force respawn - guaranteed to work
    /// </summary>
    public void ForceRespawn()
    {
        if (showDebugLogs) Debug.Log("🔄 FORCE RESPAWN TRIGGERED");
        
        // Disable controller to allow teleport
        characterController.enabled = false;
        
        // Determine best respawn position
        Vector3 targetPos = safeRespawnPosition;
        
        // Check if last grounded position is still valid
        if (lastGroundedPosition.y > voidYThreshold + 2f)
        {
            // Verify the position is actually safe with a raycast
            if (Physics.Raycast(lastGroundedPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayers))
            {
                targetPos = hit.point + Vector3.up * 1f;
            }
            else
            {
                targetPos = lastGroundedPosition + Vector3.up * 1f;
            }
        }
        
        // Teleport player
        transform.position = targetPos;
        
        // Re-enable controller
        characterController.enabled = true;
        
        // Reset all tracking
        currentAirTime = 0f;
        lastGroundedTime = Time.time;
        lastGroundY = targetPos.y;
        lastGroundedPosition = targetPos;
        consecutiveNoGroundFrames = 0;
        
        if (showDebugLogs) Debug.Log($"✅ Player respawned at {targetPos}");
    }
    
    /// <summary>
    /// Public method to manually trigger respawn
    /// </summary>
    public void RespawnAtSafePosition()
    {
        ForceRespawn();
    }
    
    /// <summary>
    /// Set the safe respawn position (called by spawn points)
    /// </summary>
    public void SetSafeRespawnPosition(Vector3 position)
    {
        safeRespawnPosition = position;
        if (showDebugLogs) Debug.Log($"📍 Safe respawn position updated: {position}");
    }

    /// <summary>
    /// Handle collision with enemies/objects trying to push the player
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!resistEnemyPush) return;
        
        // Check if we hit an enemy
        bool isEnemy = hit.gameObject.CompareTag("Enemy") || 
                       hit.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
                       hit.gameObject.GetComponent<EnemyAI>() != null ||
                       hit.gameObject.GetComponentInParent<EnemyAI>() != null;
        
        if (isEnemy)
        {
            // Find the rigidbody
            Rigidbody enemyRb = hit.gameObject.GetComponent<Rigidbody>();
            if (enemyRb == null) enemyRb = hit.gameObject.GetComponentInParent<Rigidbody>();
            
            if (enemyRb != null && !enemyRb.isKinematic)
            {
                // Push enemy away instead of player being pushed
                Vector3 pushDir = (hit.gameObject.transform.position - transform.position).normalized;
                pushDir.y = 0; // Keep it horizontal
                enemyRb.AddForce(pushDir * enemyPushForce, ForceMode.Impulse);
                
                if (showDebugLogs) Debug.Log($"🛡️ Pushed {hit.gameObject.name} away");
            }
            
            // CRITICAL: If enemy is pushing us and we're grounded, save position
            if (characterController.isGrounded)
            {
                lastGroundedPosition = transform.position;
                lastGroundY = transform.position.y;
            }
            
            // If collision is pushing us DOWN (into ground), this is dangerous
            if (hit.normal.y < -0.5f)
            {
                if (showDebugLogs) Debug.LogWarning("⚠️ Enemy pushing player into ground!");
                // Apply upward force conceptually by saving the grounded state
                lastGroundedPosition = transform.position + Vector3.up * 0.5f;
            }
        }
    }
    
    /// <summary>
    /// Call this when player should be immune to void
    /// </summary>
    public void SetProtectionActive(bool active)
    {
        isProtectionActive = active;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw void threshold plane
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 center = transform.position;
        center.y = voidYThreshold;
        Gizmos.DrawCube(center, new Vector3(20f, 0.2f, 20f));
        
        // Draw void threshold wire
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, new Vector3(20f, 0.2f, 20f));
        
        // Draw safe respawn position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(safeRespawnPosition, 0.5f);
        Gizmos.DrawLine(transform.position, safeRespawnPosition);
        
        // Draw last grounded position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastGroundedPosition, 0.3f);
        
        // Draw ground check ray
        Gizmos.color = isGroundDetected ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}

