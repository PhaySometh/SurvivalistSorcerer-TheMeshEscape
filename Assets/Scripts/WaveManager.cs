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
    public UnityEvent<int> OnWaveChange;
    public UnityEvent<string> OnStateChange; // e.g. "Wave 1", "Resting...", "BOSS!"

    private int suddenDeathEnemyCount = 0;

    void Start()
    {
        // Optional: Auto start or wait for player input
        StartCoroutine(StartGameLoop());
    }

    IEnumerator StartGameLoop()
    {
        currentState = WaveState.WaitingToStart;
        
        // Fix: Wait slightly for UIManager to finish Start() and subscribe
        yield return new WaitForSeconds(0.5f);

        // A. Game Start Sequence
        OnStateChange?.Invoke("You have 10 minutes to escape...");
        yield return new WaitForSeconds(2.5f);
        OnStateChange?.Invoke("Defeat the boss as fast as you can!");
        yield return new WaitForSeconds(2.5f);
        OnStateChange?.Invoke("Are you ready?");
        yield return new WaitForSeconds(1.5f);
        OnStateChange?.Invoke("THE GAME BEGINS!");
        yield return new WaitForSeconds(1f);

        // Start the actual game timer in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.levelTimeLimit = 600f; // Force 10 minutes
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
                // Spawn continuously and faster!
                if (Time.frameCount % 20 == 0) // Every 20 frames (~3 per sec)
                {
                    enemySpawner.SpawnMixedEnemies(2, new float[] { 0.2f, 0.4f, 0.4f });
                }
                break;
        }
    }

    void HandleWaveLogic()
    {
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
        yield return new WaitForSeconds(bufferDuration - 5f);
        OnStateChange?.Invoke($"Wave {currentWave + 1} is coming!");
    }

    void StartNextWave()
    {
        currentWave++;

        if (currentWave > totalWaves)
        {
            // This is actually handled by Wave 5 check now
            return;
        }

        currentState = WaveState.WaveInProgress;
        stateTimer = waveDuration; // Reset soft timer (2 mins)
        
        OnWaveChange?.Invoke(currentWave);
        OnStateChange?.Invoke($"WAVE {currentWave}");

        // Difficulty Scaling with Weights [Weak, Medium, Strong]
        float[] weights;
        int enemyCount = 5 + (currentWave * 3);
        float spawnDuration = 50f; // Spread spawning over the first 50 seconds

        switch (currentWave)
        {
            case 1: weights = new float[] { 0.9f, 0.1f, 0.0f }; break;
            case 2: weights = new float[] { 0.7f, 0.3f, 0.0f }; break;
            case 3: weights = new float[] { 0.5f, 0.4f, 0.1f }; break;
            case 4: weights = new float[] { 0.3f, 0.4f, 0.3f }; break;
            case 5:
                weights = new float[] { 0.1f, 0.3f, 0.6f };
                StartBossFight(); // Spawn Boss right at the start of Wave 5
                break;
            default: weights = new float[] { 0.1f, 0.4f, 0.5f }; break;
        }

        enemySpawner.SpawnMixedEnemies(enemyCount, weights, spawnDuration);
    }

    void StartBossFight()
    {
        currentState = WaveState.BossFight;
        OnStateChange?.Invoke("BOSS FIGHT!");
        
        // Spawn Boss logic...
        if (bossPrefab != null && bossSpawnPoint != null)
        {
            GameObject boss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            HealthSystem bossHealth = boss.GetComponent<HealthSystem>();
            if (bossHealth != null)
            {
                bossHealth.OnDeath.AddListener(OnBossDefeated);
            }
        }
    }

    public void TriggerSuddenDeath()
    {
        if (currentState != WaveState.Victory)
        {
            currentState = WaveState.SuddenDeath;
            string[] timeUpMocks = { "Hehe, still haven't defeated the boss?", "Broooo, go defeat the boss!", "Time is UP. Survive this!" };
            OnStateChange?.Invoke(timeUpMocks[Random.Range(0, timeUpMocks.Length)]);
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
