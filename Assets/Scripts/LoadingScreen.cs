using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;
    public Text loadingText; // Or TextMeshProUGUI
    public string sceneToLoad;

    void Start()
    {
        // Get scene name from PlayerPrefs (set by MainMenu)
        if (PlayerPrefs.HasKey("SceneToLoad"))
        {
            sceneToLoad = PlayerPrefs.GetString("SceneToLoad");
        }
        
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // Small delay so user can see the loading screen
        yield return new WaitForSeconds(0.5f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";
            }

            // When loading is almost done
            if (operation.progress >= 0.9f)
            {
                if (progressBar != null)
                    progressBar.value = 1f;
                    
                if (loadingText != null)
                    loadingText.text = "Press any key to continue...";

                // Wait for key press or auto-load
                yield return new WaitForSeconds(1f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
