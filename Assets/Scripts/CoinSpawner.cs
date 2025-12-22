using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject coinPrefab;
    public int coinCount = 20;
    public float spawnHeightOffset = 1.0f;

    [Header("Continuous Spawning")]
    public float spawnInterval = 5f;
    public float minSpawnDist = 10f;
    public float maxSpawnDist = 30f;
    public Transform playerTransform;

    void Start()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("CoinSpawner: No coin prefab assigned!");
            return;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        StartCoroutine(ContinuousSpawnRoutine());
    }

    IEnumerator ContinuousSpawnRoutine()
    {
        // Initial spawn
        SpawnCoins(5);

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (transform.childCount < coinCount)
            {
                SpawnCoins(1);
            }
        }
    }

    public void SpawnCoins(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = GetRandomPositionNearPlayer();
            if (randomPos != Vector3.zero)
            {
                Instantiate(coinPrefab, randomPos + Vector3.up * spawnHeightOffset, Quaternion.identity, transform);
            }
        }
    }

    [Header("Terrain Detection")]
    public LayerMask groundLayer;
    
    [Header("Safety Settings")]
    [Tooltip("Maximum allowed Y position for coin spawning (prevents sky spawns)")]
    public float maxSpawnHeight = 50f;
    [Tooltip("Minimum allowed Y position for coin spawning")]
    public float minSpawnHeight = -5f;

    Vector3 GetRandomPositionNearPlayer()
    {
        if (playerTransform == null) return Vector3.zero;

        // Get random angle and distance
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnDist, maxSpawnDist);

        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;

        // Start raycast from much higher to ensure we are above all modular terrain pieces
        Vector3 rayStart = new Vector3(playerTransform.position.x + x, 500f, playerTransform.position.z + z);
        
        // Use the configured groundLayer (should be set to "Ground" in Inspector)
        LayerMask safeGroundLayer = groundLayer;
        if (safeGroundLayer == 0)
        {
            // Fallback: Use Ground layer by default
            safeGroundLayer = LayerMask.GetMask("Ground", "Default", "Terrain");
        }
        
        RaycastHit hit;
        // Use a longer ray (1000f) to ensure we hit the ground
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1000f, safeGroundLayer))
        {
            // SAFETY CHECK: Validate Y position is reasonable
            if (hit.point.y > maxSpawnHeight)
            {
                Debug.LogWarning($"⚠️ Coin spawn too high at Y={hit.point.y:F1}. Skipping.");
                return Vector3.zero;
            }
            
            if (hit.point.y < minSpawnHeight)
            {
                Debug.LogWarning($"⚠️ Coin spawn too low at Y={hit.point.y:F1}. Skipping.");
                return Vector3.zero;
            }
            
            return hit.point;
        }

        return Vector3.zero;
    }

    // Debug visualization of spawn area (now dynamic)
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDist);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDist);
    }
}
