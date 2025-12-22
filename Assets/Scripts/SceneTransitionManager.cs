using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple scene transition manager that can be called from anywhere
/// Handles transitions through loading screen
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SceneTransitionManager");
                instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Load a scene through the loading screen
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        // Save the target scene
        PlayerPrefs.SetString("SceneToLoad", sceneName);
        PlayerPrefs.Save();
        
        // Load the loading scene
        SceneManager.LoadScene("LoadingScene");
    }

    /// <summary>
    /// Load a scene directly without loading screen
    /// </summary>
    public void LoadSceneDirect(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reload the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
