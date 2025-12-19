using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public int totalCoins;
    public int highScore;
    public List<int> unlockedLevels;

    public PlayerData()
    {
        totalCoins = 0;
        highScore = 0;
        unlockedLevels = new List<int>() { 1 }; // Level 1 unlocked by default
    }
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private string saveFilePath;
    public PlayerData currentData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");
        LoadGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved to: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Game Loaded.");
        }
        else
        {
            currentData = new PlayerData();
            Debug.Log("No save file found. Created new data.");
        }
    }

    // Helper to add coins and save
    public void AddCoins(int amount)
    {
        currentData.totalCoins += amount;
        SaveGame();
    }

    // Helper to check high score
    public void CheckHighScore(int score)
    {
        if (score > currentData.highScore)
        {
            currentData.highScore = score;
            SaveGame();
        }
    }
}
