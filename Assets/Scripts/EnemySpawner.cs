using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] weakPrefabs;   // Difficulty 0
    public GameObject[] mediumPrefabs; // Difficulty 1
    public GameObject[] strongPrefabs; // Difficulty 2

    [Header("Settings")]
    public Transform playerTransform;
    public float spawnRadius = 20f;
    public float minSpawnDistance = 10f;

    [Header("Tracking")]
    public List<GameObject> activeEnemies = new List<GameObject>();
    public int ActiveEnemyCount => activeEnemies.Count;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    public void SpawnEnemies(int count, int difficultyIndex = 0, int subIndex = -1)
    {
        if (playerTransform == null)
        {
            Debug.LogError("EnemySpawner: Player missing!");
            return;
        }

        GameObject prefabToSpawn = GetPrefabByDifficulty(difficultyIndex, subIndex);
        if (prefabToSpawn == null) return;

        StartCoroutine(SpawnRoutine(count, prefabToSpawn));
    }

    /// <summary>
    /// Spawns mixed enemies. subIndexCaps allows limiting which prefabs in the arrays are used (e.g. only use first 2 weak ones).
    /// </summary>
    public void SpawnMixedEnemies(int totalCount, float[] weights, float duration = 0f, int[] subIndexCaps = null)
    {
        if (playerTransform == null) return;
        
        bool hasAnyPrefabs = (weakPrefabs != null && weakPrefabs.Length > 0) || 
                             (mediumPrefabs != null && mediumPrefabs.Length > 0) || 
                             (strongPrefabs != null && strongPrefabs.Length > 0);
                             
        if (!hasAnyPrefabs) 
        {
            Debug.LogError("EnemySpawner: No prefabs assigned in any category!");
            return;
        }

        StartCoroutine(SpawnMixedRoutine(totalCount, weights, duration, subIndexCaps));
    }

    IEnumerator SpawnMixedRoutine(int totalCount, float[] weights, float duration, int[] subIndexCaps)
    {
        float interval = duration > 0 ? duration / totalCount : 0.1f;

        for (int i = 0; i < totalCount; i++)
        {
            float randomValue = Random.value;
            float cumulative = 0f;
            int selectedIndex = 0;

            for (int j = 0; j < weights.Length; j++)
            {
                cumulative += weights[j];
                if (randomValue <= cumulative)
                {
                    selectedIndex = j;
                    break;
                }
            }
            
            // Safety check for index
            selectedIndex = Mathf.Clamp(selectedIndex, 0, 2);
            
            int cap = -1;
            if (subIndexCaps != null && selectedIndex < subIndexCaps.Length) cap = subIndexCaps[selectedIndex];

            GameObject prefab = GetPrefabByDifficulty(selectedIndex, cap);
            
            if (prefab != null)
            {
                SpawnOneEnemy(prefab);
            }

            if (duration > 0)
            {
                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private GameObject GetPrefabByDifficulty(int index, int forcedSubIndex = -1)
    {
        GameObject[] targetArray;
        
        switch (index)
        {
            case 0: targetArray = weakPrefabs; break;
            case 1: targetArray = mediumPrefabs; break;
            case 2: targetArray = strongPrefabs; break;
            default: targetArray = weakPrefabs; break;
        }

        if (targetArray == null || targetArray.Length == 0)
        {
            // Fallback: try other arrays if one is empty
            if (weakPrefabs != null && weakPrefabs.Length > 0) targetArray = weakPrefabs;
            else if (mediumPrefabs != null && mediumPrefabs.Length > 0) targetArray = mediumPrefabs;
            else if (strongPrefabs != null && strongPrefabs.Length > 0) targetArray = strongPrefabs;
            else
            {
                Debug.LogError("EnemySpawner: All prefab arrays are empty!");
                return null;
            }
        }

        // If a specific subIndex is requested, use it (clamped to array size)
        if (forcedSubIndex >= 0)
        {
            int subIndex = Mathf.Clamp(forcedSubIndex, 0, targetArray.Length - 1);
            return targetArray[subIndex];
        }

        return targetArray[Random.Range(0, targetArray.Length)];
    }

    IEnumerator SpawnRoutine(int count, GameObject prefab)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOneEnemy(prefab);
            yield return new WaitForSeconds(0.2f); // Stagger spawns slightly
        }
    }

    void SpawnOneEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GetRandomNavMeshPosition();
        if (spawnPos != Vector3.zero)
        {
            GameObject newEnemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Track enemy death to update count
            HealthSystem health = newEnemy.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.OnDeath.AddListener(() => RemoveEnemy(newEnemy));
            }
            
            activeEnemies.Add(newEnemy);
        }
    }

    void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    Vector3 GetRandomNavMeshPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Random point between minSpawnDistance and spawnRadius
            float distance = Random.Range(minSpawnDistance, spawnRadius);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * distance;
            Vector3 randomPoint = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                // ADDITION: Check if hit point is inside an obstacle or building
                // We assume buildings have MeshColliders or BoxColliders
                // Check if there are any colliders nearby that are NOT the floor
                Collider[] colliders = Physics.OverlapSphere(hit.position, 1.5f);
                bool occupied = false;
                foreach (var col in colliders)
                {
                    // If it's not the enemy itself (if we had some) and not the ground (assuming ground is tagged or named)
                    // Safety check: only avoid spawning if the name contains common obstacle keywords
                    if (col.gameObject.name.Contains("Building") || 
                        col.gameObject.name.Contains("Wall") || 
                        col.gameObject.name.Contains("House") ||
                        col.gameObject.name.Contains("Tree"))
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied) return hit.position;
            }
        }
        
        return Vector3.zero;
    }
}
