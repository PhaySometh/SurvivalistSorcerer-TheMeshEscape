using UnityEngine;

/// <summary>
/// Protects the player from falling through the map and handles knockback.
/// Attach this to the Player alongside CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerPhysicsProtection : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("Layers that count as ground (if empty, uses Default layer)")]
    public LayerMask groundLayers;
    
    [Tooltip("Distance to check for ground below player")]
    public float groundCheckDistance = 1.5f;
    
    [Tooltip("Radius of the ground check sphere")]
    public float groundCheckRadius = 0.3f;

    [Header("Safety Settings")]
    [Tooltip("Y position below which player is considered 'in void'")]
    public float voidYThreshold = -10f;
    
    [Tooltip("Position to respawn player when they fall into void")]
    public Vector3 safeRespawnPosition = new Vector3(0, 2f, 0);
    
    [Tooltip("Try to find a spawn point with this tag")]
    public string spawnPointTag = "SpawnPoint";
    
    [Tooltip("How long player can be 'not grounded' before force respawn")]
    public float maxAirTime = 5f;

    [Header("Anti-Push Settings")]
    [Tooltip("If enabled, player resists being pushed by enemies")]
    public bool resistEnemyPush = true;
    
    [Tooltip("How quickly to recover from being pushed")]
    public float pushRecoverySpeed = 5f;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    [SerializeField] private float currentAirTime = 0f;
    [SerializeField] private bool isProtectionActive = true;

    private CharacterController characterController;
    private Vector3 lastGroundedPosition;
    private float lastGroundedTime;
    private Vector3 lastValidPosition;
    private float positionCheckTimer = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // If ground layers not set, default to everything except IgnoreRaycast
        if (groundLayers == 0)
        {
            groundLayers = ~(1 << 2); // Everything except IgnoreRaycast layer
            Debug.LogWarning("PlayerPhysicsProtection: Ground Layers not set, using default (all layers)");
        }
        
        // Try to find spawn point safely
        try 
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag(spawnPointTag);
            if (spawnPoint != null)
            {
                safeRespawnPosition = spawnPoint.transform.position;
                Debug.Log($"PlayerPhysicsProtection: Found spawn point at {safeRespawnPosition}");
            }
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"PlayerPhysicsProtection: Tag '{spawnPointTag}' is not defined in Tag Manager. Using default spawn position.");
        }

        // Initialize with current position
        lastGroundedPosition = transform.position;
        lastValidPosition = transform.position;
        lastGroundedTime = Time.time;
    }

    void Update()
    {
        if (!isProtectionActive) return;
        
        // Track grounded state
        if (characterController.isGrounded)
        {
            // Save position every 0.5 seconds when grounded
            positionCheckTimer += Time.deltaTime;
            if (positionCheckTimer >= 0.5f)
            {
                lastGroundedPosition = transform.position;
                lastValidPosition = transform.position;
                positionCheckTimer = 0f;
            }
            
            lastGroundedTime = Time.time;
            currentAirTime = 0f;
        }
        else
        {
            currentAirTime += Time.deltaTime;
        }

        // PRIMARY CHECK: Void fall (below threshold)
        CheckVoidFall();
        
        // SECONDARY CHECK: Too long in air
        CheckExtendedAirTime();
    }
    
    void LateUpdate()
    {
        // FINAL SAFETY CHECK: Run in LateUpdate to catch any edge cases
        if (transform.position.y < voidYThreshold)
        {
            ForceRespawn();
        }
    }

    /// <summary>
    /// Check if player fell into the void
    /// </summary>
    private void CheckVoidFall()
    {
        if (transform.position.y < voidYThreshold)
        {
            Debug.LogWarning($"☠️ Player fell into void (Y={transform.position.y})! Respawning...");
            ForceRespawn();
        }
    }
    
    /// <summary>
    /// Check if player has been in the air too long (stuck or falling forever)
    /// </summary>
    private void CheckExtendedAirTime()
    {
        if (currentAirTime > maxAirTime)
        {
            Debug.LogWarning($"⚠️ Player in air for {currentAirTime}s! Force respawning...");
            ForceRespawn();
        }
    }

    /// <summary>
    /// Force respawn - guaranteed to work
    /// </summary>
    public void ForceRespawn()
    {
        Debug.Log("🔄 FORCE RESPAWN TRIGGERED");
        
        // Disable controller to allow teleport
        characterController.enabled = false;
        
        // Determine best respawn position
        Vector3 targetPos = safeRespawnPosition;
        
        // Check if last grounded position is still valid (above void)
        if (lastGroundedPosition.y > voidYThreshold + 5f)
        {
            targetPos = lastGroundedPosition + Vector3.up * 0.5f;
        }
        
        // Teleport player
        transform.position = targetPos;
        
        // Re-enable controller
        characterController.enabled = true;
        
        // Reset air time
        currentAirTime = 0f;
        lastGroundedTime = Time.time;
        
        Debug.Log($"✅ Player respawned at {targetPos}");
    }
    
    /// <summary>
    /// Public method to manually trigger respawn (can be called from UI button)
    /// </summary>
    public void RespawnAtSafePosition()
    {
        ForceRespawn();
    }

    /// <summary>
    /// Handle collision with enemies/objects trying to push the player
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!resistEnemyPush) return;
        
        // Check if we hit an enemy
        if (hit.gameObject.CompareTag("Enemy") || hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Rigidbody enemyRb = hit.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null && !enemyRb.isKinematic)
            {
                // Push enemy away instead of player being pushed
                Vector3 pushDir = (hit.gameObject.transform.position - transform.position).normalized;
                pushDir.y = 0;
                enemyRb.AddForce(pushDir * 5f, ForceMode.Impulse);
            }
            
            // If enemy is pushing us into ground, save position
            if (characterController.isGrounded)
            {
                lastGroundedPosition = transform.position;
            }
        }
    }
    
    /// <summary>
    /// Call this when player should be immune to void (during cutscenes, etc)
    /// </summary>
    public void SetProtectionActive(bool active)
    {
        isProtectionActive = active;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw void threshold line
        Gizmos.color = Color.red;
        Vector3 center = transform.position;
        center.y = voidYThreshold;
        Gizmos.DrawWireCube(center, new Vector3(10f, 0.1f, 10f));
        
        // Draw safe respawn position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(safeRespawnPosition, 0.5f);
        Gizmos.DrawLine(transform.position, safeRespawnPosition);
        
        // Draw last grounded position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastGroundedPosition, 0.3f);
    }
}

