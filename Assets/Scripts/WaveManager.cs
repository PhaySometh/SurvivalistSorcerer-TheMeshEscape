using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    public enum WaveState { WaitingToStart, WaveInProgress, PreparationBuffer, BossFight, SuddenDeath, Victory }

    [Header("Wave Configuration")]
    public int totalWaves = 5;
    public float waveDuration = 120f; // 2 minutes soft limit
    public float bufferDuration = 15f; // 15 seconds break

    [Header("Current Status")]
    public int currentWave = 0; // 0 means not started, 1-5 are waves
    public WaveState currentState = WaveState.WaitingToStart;
    public float stateTimer = 0f;

    [Header("References")]
    public EnemySpawner enemySpawner;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;

    // Events for UI
    public UnityEvent<int> OnWaveChange = new UnityEvent<int>();
    public UnityEvent<string> OnStateChange = new UnityEvent<string>(); // e.g. "Wave 1", "Resting...", "BOSS!"

    private int suddenDeathEnemyCount = 0;

    void Start()
    {
        // Optional: Auto start or wait for player input
        StartCoroutine(StartGameLoop());
    }

    IEnumerator StartGameLoop()
    {
        currentState = WaveState.WaitingToStart;
        
        // Minor delay to ensure UI is initialized, but feels immediate
        yield return new WaitForSeconds(0.1f); 

        // A. Game Start Sequence - First message starts instantly
        OnStateChange?.Invoke("You have 10 minutes to escape...");
        yield return new WaitForSeconds(5.0f);
        
        OnStateChange?.Invoke("Defeat the boss as fast as you can!");
        yield return new WaitForSeconds(5.0f);
        
        OnStateChange?.Invoke("Are you ready?");
        yield return new WaitForSeconds(4.0f);
        
        OnStateChange?.Invoke("THE GAME BEGINS!");
        yield return new WaitForSeconds(2.0f);

        // Start the actual game timer in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }

        currentWave = 0;
        StartNextWave();
    }

    void Update()
    {
        if (currentState == WaveState.Victory || currentState == WaveState.WaitingToStart) return;

        // Handle Timers
        if (stateTimer > 0)
        {
            stateTimer -= Time.deltaTime;
        }

        // State Machine
        switch (currentState)
        {
            case WaveState.WaveInProgress:
                HandleWaveLogic();
                break;

            case WaveState.PreparationBuffer:
                if (stateTimer <= 0)
                {
                    StartNextWave();
                }
                break;

            case WaveState.BossFight:
                // Check if Boss is dead (This would be triggered by Boss's HealthSystem OnDeath)
                break;
                
            case WaveState.SuddenDeath:
                // Relaxed spawning (once per second) so it's challenging but not impossible
                if (Time.frameCount % 60 == 0) 
                {
                    // Mix 60% Weak (Slime/Turtle) and 40% Medium (Skeleton/Golem)
                    enemySpawner.SpawnMixedEnemies(1, new float[] { 0.6f, 0.4f, 0.0f });
                }
                break;
        }
    }

    void HandleWaveLogic()
    {
        // DON'T trigger next wave logic if we are already in Wave 5 (Boss Fight)
        if (currentWave >= totalWaves) return;

        // Scenario A: All enemies dead -> Early buffer
        if (enemySpawner.ActiveEnemyCount == 0 && stateTimer > 0)
        {
            Debug.Log("Wave Cleared Early!");
            OnStateChange?.Invoke("Round Clear!");
            StartBuffer();
            return;
        }

        // Scenario B: Time up -> Force buffer (enemies stay)
        if (stateTimer <= 0)
        {
            Debug.Log("Time's Up! Enemies remain.");
            OnStateChange?.Invoke("Haven't cleared the wave yet?");
            
            // Random mocking text
            string[] mocks = { "You're so weak...", "Too slow!", "My grandma moves faster than you.", "Is that all?" };
            string randomMock = mocks[Random.Range(0, mocks.Length)];
            
            StartCoroutine(ShowSequenceDelayed(randomMock, 2f, "Next wave spawning anyway!"));
            StartBuffer();
        }
    }

    IEnumerator ShowSequenceDelayed(string first, float delay, string second)
    {
        OnStateChange?.Invoke(first);
        yield return new WaitForSeconds(delay);
        OnStateChange?.Invoke(second);
    }

    void StartBuffer()
    {
        currentState = WaveState.PreparationBuffer;
        stateTimer = bufferDuration;
        
        StartCoroutine(BufferSequence());
    }

    IEnumerator BufferSequence()
    {
        yield return new WaitForSeconds(2f);
        OnStateChange?.Invoke("Prepare yourself...");
        
        // Only say "Wave X is coming" if there IS a next wave
        if (currentWave < totalWaves)
        {
            yield return new WaitForSeconds(bufferDuration - 5f);
            OnStateChange?.Invoke($"Wave {currentWave + 1} is coming!");
        }
    }

    void StartNextWave()
    {
        // FIX: Only increment and start if we haven't finished all waves
        if (currentWave >= totalWaves) 
        {
            Debug.Log("All waves complete. Waiting for boss death or sudden death.");
            return;
        }

        currentWave++;

        currentState = WaveState.WaveInProgress;
        stateTimer = waveDuration; 
        
        OnWaveChange?.Invoke(currentWave);
        OnStateChange?.Invoke($"WAVE {currentWave}");

        // Start the specialized sequence for this wave
        StartCoroutine(SpawnWaveSequence(currentWave));
    }

    IEnumerator SpawnWaveSequence(int wave)
    {
        // INDEX REFERENCE (Based on your setup):
        // Weak Category [0]:  [0] Slime, [1] Turtle
        // Medium Category [1]: [0] Skeleton, [1] Golem
        // Strong Category [2]: (Empty or other monsters)
        // Boss Prefab: Bull King (Assigned to bossPrefab slot, NOT in spawner lists)

        switch (wave)
        {
            case 1:
                yield return StartCoroutine(SpawnSequenceStep(5, 0, 0)); // 5 Slimes (Weak 0)
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(3, 0, 1)); // 3 Turtles (Weak 1)
                break;

            case 2:
                yield return StartCoroutine(SpawnSequenceStep(4, 0, 0)); // 4 Slimes
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(1, 1, 0)); // 1 Skeleton (Medium 0)
                break;

            case 3:
                yield return StartCoroutine(SpawnSequenceStep(4, 0, 0)); // 4 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 0)); // 2 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(1, 1, 1)); // 1 Golem (Medium 1)
                break;

            case 4:
                yield return StartCoroutine(SpawnSequenceStep(3, 0, 0)); // 3 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(3, 1, 0)); // 3 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 1)); // 2 Golems
                break;

            case 5:
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 0)); // 2 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(1, 0, 1)); // 1 Turtle
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(3, 1, 0)); // 3 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 1)); // 2 Golems
                yield return new WaitForSeconds(3f);
                StartBossFight(); // The Bull King (Unique Boss Slot)
                break;
        }
    }

    IEnumerator SpawnSequenceStep(int count, int difficulty, int subIndex)
    {
        enemySpawner.SpawnEnemies(count, difficulty, subIndex);
        // Wait just a bit before spawning the next tier to avoid total overlap
        yield return new WaitForSeconds(1.0f);
    }

    void StartBossFight()
    {
        currentState = WaveState.BossFight;
        OnStateChange?.Invoke("BOSS FIGHT!");
        
        // Spawn Boss logic...
        if (bossPrefab != null && bossSpawnPoint != null)
        {
            Vector3 spawnPos = bossSpawnPoint.position;
            
            // ENSURE BOSS IS GROUNDED ON NAVMESH
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            
            // Force NavMeshAgent to snap to position
            UnityEngine.AI.NavMeshAgent agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(spawnPos);
                agent.enabled = true;
            }

            // TRIGGER BOSS TAUNT (DEFY)
            Animator anim = boss.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("defy");
            }

            HealthSystem bossHealth = boss.GetComponent<HealthSystem>();
            if (bossHealth != null)
            {
                bossHealth.OnDeath.AddListener(OnBossDefeated);
            }
        }
    }

    public void TriggerSuddenDeath()
    {
        if (currentState != WaveState.Victory && currentState != WaveState.SuddenDeath)
        {
            currentState = WaveState.SuddenDeath;
            string[] timeUpMocks = { "Hehe, still haven't defeated the boss?", "Broooo, go defeat the boss!", "Time is UP. Survive this!" };
            OnStateChange?.Invoke(timeUpMocks[Random.Range(0, timeUpMocks.Length)]);
            
            // UI Notification is already called from GameManager for the timer color
        }
    }

    void OnBossDefeated()
    {
        currentState = WaveState.Victory;
        OnStateChange?.Invoke("Damn, boi. You got this!");
        
        StartCoroutine(VictorySequence());
    }

    IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(3f);
        OnStateChange?.Invoke("ESCAPE PORTAL OPEN!");
        GameManager.Instance.LevelComplete();
    }
}
