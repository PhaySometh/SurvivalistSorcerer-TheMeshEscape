using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingRunner : MonoBehaviour
{
    // ... existing configuration fields ...
    [Header("1. Scene and UI")]
    public string sceneToLoadName = "GameScene"; 
    public Slider progressBar;

    [Header("2. Animation")]
    public Animator characterAnimator; 
    
    [Header("3. Speed Control")]
    public float minimumLoadTime = 2f; // Reduced minimum time
    public float pauseChance = 0.3f; // 30% chance to pause at a checkpoint
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;

    private const string SPEED_PARAM = "Speed"; 
    private const float RUNNING_SPEED_VALUE = 3.0f;
    
    private float targetProgress = 0f;
    private bool sceneIsReady = false;

    private void Start()
    {
        // Get the scene to load from PlayerPrefs (set by menu)
        string targetScene = PlayerPrefs.GetString("SceneToLoad", "VilageMapScene");
        if (!string.IsNullOrEmpty(targetScene))
        {
            sceneToLoadName = targetScene;
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(SPEED_PARAM, RUNNING_SPEED_VALUE); 
        }

        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // Start the background loading operation
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoadName);
        operation.allowSceneActivation = false; // Keep the scene from switching

        // Start a secondary coroutine to listen for the actual load completion
        StartCoroutine(CheckSceneReady(operation));
        
        // Loop 1: Control the visual progress bar and introduce delays
        while (progressBar.value < 0.9f)
        {
            // Set the visual target progress (e.g., 0.1, 0.2, 0.3...)
            targetProgress = Mathf.Min(0.9f, targetProgress + 0.1f);

            // Smoothly move the bar to the target
            while (progressBar.value < targetProgress)
            {
                progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, Time.deltaTime / minimumLoadTime);
                yield return null;
            }

            // Random Pause Check (Simulates asset retrieval)
            if (Random.value < pauseChance)
            {
                float pauseTime = Random.Range(minPauseDuration, maxPauseDuration);
                // Pause the progress bar movement while the wizard keeps running!
                yield return new WaitForSeconds(pauseTime); 
            }
        }
        
        // Loop 2: Wait for the actual scene loading to finish
        // This is where the bar waits at ~90% until the CheckSceneReady coroutine finishes.
        while (!sceneIsReady)
        {
            // Add a small idle animation if you have one, or keep running
            yield return null;
        }

        // --- Final Steps: Full Bar, Stop Running, Switch Scene ---
        
        // 1. Stop the Wizard from running
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(SPEED_PARAM, 0f); 
        }

        // 2. Quickly fill the bar from 90% to 100%
        while (progressBar.value < 1.0f)
        {
            progressBar.value = Mathf.MoveTowards(progressBar.value, 1.0f, Time.deltaTime * 5f);
            yield return null;
        }

        // 3. Wait a moment (The stop animation completes and the player sees 100%)
        yield return new WaitForSeconds(0.75f); 
        
        // 4. Final Scene Switch
        operation.allowSceneActivation = true;
    }

    // Secondary coroutine to listen for the actual Unity load completion
    IEnumerator CheckSceneReady(AsyncOperation operation)
    {
        // Wait until the real load is done
        while (operation.progress < 0.9f)
        {
            yield return null;
        }
        // Set the flag for the main coroutine
        sceneIsReady = true; 
    }
}