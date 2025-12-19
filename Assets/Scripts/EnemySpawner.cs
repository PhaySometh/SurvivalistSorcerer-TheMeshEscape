using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] enemyPrefabs; // [0] Weak, [1] Medium, [2] Strong
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

    public void SpawnEnemies(int count, int difficultyIndex = 0)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || playerTransform == null)
        {
            Debug.LogError("EnemySpawner: Prefabs or Player missing!");
            return;
        }

        // Clamp index to array bounds
        int index = Mathf.Clamp(difficultyIndex, 0, enemyPrefabs.Length - 1);
        GameObject prefabToSpawn = enemyPrefabs[index];

        StartCoroutine(SpawnRoutine(count, prefabToSpawn));
    }

    /// <summary>
    /// Spawns a total number of enemies distributed by difficulty weights over a duration.
    /// Example weights: [0.7, 0.2, 0.1] = 70% Weak, 20% Medium, 10% Strong
    /// </summary>
    public void SpawnMixedEnemies(int totalCount, float[] weights, float duration = 0f)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || playerTransform == null) return;

        StartCoroutine(SpawnMixedRoutine(totalCount, weights, duration));
    }

    IEnumerator SpawnMixedRoutine(int totalCount, float[] weights, float duration)
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
            selectedIndex = Mathf.Clamp(selectedIndex, 0, enemyPrefabs.Length - 1);
            SpawnOneEnemy(enemyPrefabs[selectedIndex]);

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

        // Random point between minSpawnDistance and spawnRadius
        float distance = Random.Range(minSpawnDistance, spawnRadius);
        Vector2 randomCircle = Random.insideUnitCircle.normalized * distance;
        Vector3 randomPoint = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        NavMeshHit hit;
        // Find nearest point on NavMesh within 10 units (more range for modular terrain)
        if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }
}
