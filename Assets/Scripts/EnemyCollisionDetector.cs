using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy collision detector - Deals damage to player on collision
/// ENHANCED: Works with trigger colliders, regular colliders, or proximity detection
/// </summary>
public class EnemyCollisionDetector : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 25f; // Damage dealt to player per hit
    public float damageCooldown = 1f; // Cooldown between damage hits
    
    [Header("Detection Settings")]
    [Tooltip("Enable proximity-based damage detection (backup if colliders fail)")]
    public bool useProximityDetection = true;
    
    [Tooltip("Range for proximity detection")]
    public float proximityRange = 1.5f;
    
    [Tooltip("How often to check proximity (seconds)")]
    public float proximityCheckInterval = 0.2f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private float lastDamageTime = 0f;
    private Transform playerTransform;
    private HealthSystem playerHealth;
    private float nextProximityCheck = 0f;
    private HealthSystem myHealth; // Enemy's own health
    private bool isEnemyDead = false;

    /* 
    SETUP GUIDE FOR PHAY:
    -----------------------------
    OPTION 1 (Best): Put this script on a CHILD object with a Trigger Collider
    - Main enemy has Rigidbody + Collider (not trigger) for physics
    - Child has Collider (IS TRIGGER = true) + this script
    
    OPTION 2 (Also works): Put this script on the main enemy object
    - The script will use proximity detection as a backup
    
    OPTION 3: Attach to any parent/child - it will find the right setup
    */

    private void Start()
    {
        // Find player at start
        FindPlayer();
        
        // Get enemy's health system
        myHealth = GetComponent<HealthSystem>();
        if (myHealth == null) myHealth = GetComponentInParent<HealthSystem>();
        if (myHealth != null)
        {
            myHealth.OnDeath.AddListener(OnEnemyDeath);
        }
        
        // Check if we have a collider set as trigger
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && !myCollider.isTrigger)
        {
            if (showDebugLogs) Debug.LogWarning($"⚠️ {gameObject.name}: EnemyCollisionDetector works best with a Trigger collider! Enabling proximity detection as backup.");
            useProximityDetection = true;
        }
    }
    
    private void OnEnemyDeath()
    {
        isEnemyDead = true;
        // Disable this script when enemy dies
        enabled = false;
    }
    
    private void FindPlayer()
    {
        // Try to find player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<HealthSystem>();
            return;
        }
        
        // Fallback: find by name
        playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<HealthSystem>();
            return;
        }
        
        // Fallback: find by CharacterController
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            playerTransform = cc.transform;
            playerHealth = cc.GetComponent<HealthSystem>();
        }
    }
    
    private void Update()
    {
        // Proximity-based damage detection as backup
        if (useProximityDetection && Time.time >= nextProximityCheck)
        {
            nextProximityCheck = Time.time + proximityCheckInterval;
            CheckProximityDamage();
        }
    }
    
    /// <summary>
    /// Backup: Check if player is within range and deal damage
    /// </summary>
    private void CheckProximityDamage()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // Get the root enemy position (in case we're on a child)
        Vector3 enemyPos = transform.root.position;
        float distance = Vector3.Distance(enemyPos, playerTransform.position);
        
        if (distance <= proximityRange)
        {
            // Within range - try to deal damage
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                DealDamageToPlayer(playerTransform.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        // Check if it's the player
        if (IsPlayer(collision.gameObject))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        // Deal continuous damage while in contact
        if (IsPlayer(collision.gameObject))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                DealDamageToPlayer(collision.gameObject);
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Also handle regular collisions (not just triggers)
        if (IsPlayer(collision.gameObject))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        // Continuous damage on regular collision
        if (IsPlayer(collision.gameObject))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                DealDamageToPlayer(collision.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Check if the given object is the player
    /// </summary>
    private bool IsPlayer(GameObject obj)
    {
        // Check tag
        if (obj.CompareTag("Player")) return true;
        
        // Check name
        if (obj.name.Contains("Player")) return true;
        
        // Check for CharacterController
        if (obj.GetComponent<CharacterController>() != null) return true;
        
        // Check parent
        if (obj.transform.root.CompareTag("Player")) return true;
        
        return false;
    }

    /// <summary>
    /// Deal damage to the player
    /// </summary>
    private void DealDamageToPlayer(GameObject playerObject)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Find HealthSystem on the player
        HealthSystem health = playerObject.GetComponent<HealthSystem>();
        if (health == null) health = playerObject.GetComponentInParent<HealthSystem>();
        if (health == null) health = playerObject.GetComponentInChildren<HealthSystem>();
        if (health == null) health = playerHealth; // Use cached reference
        
        if (health != null && !health.IsDead)
        {
            lastDamageTime = Time.time;
            health.TakeDamage(damageAmount);
            
            if (showDebugLogs)
            {
                string enemyName = transform.root.name;
                Debug.Log($"🎯 {enemyName} dealt {damageAmount} damage to player! Player health: {health.CurrentHealth}");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (useProximityDetection)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.root.position, proximityRange);
        }
    }
}

