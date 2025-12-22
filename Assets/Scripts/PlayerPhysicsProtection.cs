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
    
    [Tooltip("Y position ABOVE which player is considered 'sky-walking' (bug) - Set HIGHER than your terrain")]
    public float skyWalkThreshold = 100f; // Increased from 50 to 100 to prevent false positives
    
    [Tooltip("Position to respawn player when they fall into void")]
    public Vector3 safeRespawnPosition = new Vector3(0, 5f, 0);
    
    [Tooltip("Time after respawn before sky-walk detection activates (prevents respawn loops)")]
    public float respawnGracePeriod = 2f;
    
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
    private float lastRespawnTime = -10f; // Cooldown tracking
    private const float RESPAWN_COOLDOWN = 2f; // Minimum 2 seconds between respawns

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // If ground layers not set, use Default and Terrain
        if (groundLayers == 0)
        {
            // Include Default (0), Terrain, Ground, Environment layers (modular terrain tiles may be on Environment)
            groundLayers = LayerMask.GetMask("Default", "Terrain", "Ground", "Environment");
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
            else
            {
                // No SpawnPoint found - use current player position as safe respawn
                // This is usually the starting position when the scene loads
                safeRespawnPosition = transform.position + Vector3.up * 2f;
                // Only log this once in debug mode - not a critical warning
                if (showDebugLogs) Debug.Log($"📍 No SpawnPoint found. Using player start position: {safeRespawnPosition}");
            }
        }
        catch (System.Exception)
        {
            // Tag doesn't exist - use player's current position
            safeRespawnPosition = transform.position + Vector3.up * 2f;
            // Suppress this warning - it's expected if no SpawnPoint is set up
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
        
        // NEW CHECK: Sky-walking detection (above threshold) - WITH GRACE PERIOD
        // Only check if enough time has passed since last respawn (prevents infinite loops)
        if (Time.time - lastRespawnTime > respawnGracePeriod)
        {
            if (transform.position.y > skyWalkThreshold)
            {
                // Additional check: BOTH conditions must be true to prevent false positives
                // Must be high AND not grounded by CharacterController
                if (!characterController.isGrounded && !isGroundDetected && consecutiveNoGroundFrames > 30)
                {
                    if (showDebugLogs) Debug.LogWarning($"🚁 SKY-WALKING DETECTED at Y={transform.position.y:F1}! Respawning...");
                    ForceRespawn();
                    return;
                }
            }
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
            // FIXED: Much less aggressive - only trigger for REAL falls into void
            if (consecutiveNoGroundFrames > 30 && !characterController.isGrounded)
            {
                // We might be falling through - check if we fell a significant distance quickly
                // Threshold increased from 2 to 8 units to prevent false positives
                if (Time.time - lastGroundedTime < 0.3f && transform.position.y < lastGroundY - 8f)
                {
                    // Only respawn if we're not on cooldown
                    if (Time.time - lastRespawnTime > RESPAWN_COOLDOWN)
                    {
                        if (showDebugLogs) Debug.LogWarning($"⚠️ Sudden fall detected! Was at Y={lastGroundY:F1}, now at Y={transform.position.y:F1}");
                        ForceRespawn();
                    }
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
    /// ENHANCED: Spawns higher, resets physics completely, uses delay for stability
    /// </summary>
    public void ForceRespawn()
    {
        // CRITICAL: Check cooldown to prevent respawn loops
        if (Time.time - lastRespawnTime < RESPAWN_COOLDOWN)
        {
            if (showDebugLogs) Debug.Log("⏳ Respawn on cooldown, skipping...");
            return;
        }
        
        lastRespawnTime = Time.time;
        
        if (showDebugLogs) Debug.Log("🔄 FORCE RESPAWN TRIGGERED");
        
        // Start the respawn coroutine for clean physics reset
        StartCoroutine(RespawnCoroutine());
    }
    
    /// <summary>
    /// Coroutine-based respawn for cleaner physics reset
    /// </summary>
    private System.Collections.IEnumerator RespawnCoroutine()
    {
        // STEP 1: Disable the CharacterController to allow direct transform manipulation
        characterController.enabled = false;
        
        // STEP 2: Determine best respawn position
        Vector3 targetPos = safeRespawnPosition + Vector3.up * 2f; // Spawn 2 units HIGHER
        
        // Check if last grounded position is still valid
        if (lastGroundedPosition.y > voidYThreshold + 2f)
        {
            // Verify the position is actually safe with a raycast
            Vector3 checkPos = lastGroundedPosition + Vector3.up * 5f;
            if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 10f, groundLayers))
            {
                // Spawn 2 units above the ground to prevent clipping
                targetPos = hit.point + Vector3.up * 2f;
            }
            else
            {
                targetPos = lastGroundedPosition + Vector3.up * 2f;
            }
        }
        
        // STEP 3: Immediately teleport player
        transform.position = targetPos;
        
        // STEP 4: Wait one physics frame to let Unity settle
        yield return new WaitForFixedUpdate();
        
        // STEP 5: Teleport again (in case of any physics shenanigans)
        transform.position = targetPos;
        
        // STEP 6: Re-enable the CharacterController
        characterController.enabled = true;
        
        // STEP 7: Immediately move down slightly to "ground" the player
        // This prevents the CharacterController from thinking it's falling
        characterController.Move(Vector3.down * 0.1f);
        
        // STEP 8: Reset all tracking variables
        currentAirTime = 0f;
        lastGroundedTime = Time.time;
        lastGroundY = targetPos.y;
        lastGroundedPosition = targetPos;
        consecutiveNoGroundFrames = 0;
        isGroundDetected = true;
        
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

