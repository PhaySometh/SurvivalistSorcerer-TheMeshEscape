# Game Settings System - Implementation Guide

## 📋 Overview
I've created a complete difficulty system for your game with 3 difficulty levels:
- **Easy**: 3 waves, 12 minutes, weaker enemies
- **Medium**: 5 waves, 10 minutes, balanced
- **Hard**: 7 waves, 8 minutes, stronger enemies

## 📁 New Files Created

### 1. GameSettings.cs
- Manages difficulty levels and configurations
- Persists settings using PlayerPrefs
- Singleton pattern for easy access
- Contains WaveConfig data structure

### 2. SettingsManager.cs
- Handles the settings UI panel
- Allows players to select difficulty
- Shows detailed information about each difficulty
- Applies and saves settings

## 🔧 Setup Instructions

### Step 1: Create GameSettings GameObject
1. In your Menu Scene (MenuScence), create an empty GameObject
2. Name it "GameSettings"
3. Add the `GameSettings.cs` component to it
4. This will persist across scenes (DontDestroyOnLoad)

### Step 2: Create Settings Panel UI
1. In your Menu Scene Canvas, create a new Panel called "SettingsPanel"
2. Inside SettingsPanel, create:
   - **Title**: TextMeshPro - "GAME SETTINGS"
   - **Difficulty Buttons**:
     - Button: "EASY"
     - Button: "MEDIUM"  
     - Button: "HARD"
   - **Info Display**: TextMeshPro - Shows difficulty details
   - **Current Difficulty**: TextMeshPro - Shows selected difficulty
   - **Control Buttons**:
     - Button: "APPLY"
     - Button: "CLOSE"

### Step 3: Setup SettingsManager
1. Create an empty GameObject called "SettingsManager"
2. Add the `SettingsManager.cs` component
3. In the Inspector, drag and assign:
   - Settings Panel → SettingsPanel
   - Easy Button → Easy button
   - Medium Button → Medium button
   - Hard Button → Hard button
   - Difficulty Info Text → Info display TextMeshPro
   - Current Difficulty Text → Current difficulty TextMeshPro
   - Close Button → Close button
   - Apply Button → Apply button

### Step 4: Connect to MenuScreen
1. Select your MenuScreen GameObject
2. In the MenuScreen component, assign:
   - Settings Manager → Your SettingsManager GameObject
3. Connect your Settings button:
   - On Click → MenuScreen.OpenSettings()

### Step 5: Update Your Game Scene
The WaveManager and GameManager will automatically load the selected difficulty when the game starts!

## 🎮 How It Works

### Player Flow:
1. Player opens main menu
2. Clicks "Settings" button
3. Sees three difficulty options with detailed info
4. Selects a difficulty (button highlights)
5. Clicks "Apply" to save
6. Settings are persisted to PlayerPrefs
7. When player starts game, difficulty is loaded automatically

### Technical Flow:
```
MenuScreen → SettingsManager → GameSettings (saves)
                                     ↓
                              PlayerPrefs
                                     ↓
Game Start → WaveManager.LoadDifficultySettings()
                  ↓
            Applies wave config
                  ↓
        GameManager.levelTimeLimit updated
```

## 📊 Difficulty Configurations

### Easy Mode
- Waves: 3
- Total Time: 12 minutes (720s)
- Wave Duration: 2.5 minutes (150s)
- Buffer Time: 20 seconds
- Enemy Health: 70%
- Enemy Damage: 70%

### Medium Mode (Default)
- Waves: 5
- Total Time: 10 minutes (600s)
- Wave Duration: 2 minutes (120s)
- Buffer Time: 15 seconds
- Enemy Health: 100%
- Enemy Damage: 100%

### Hard Mode
- Waves: 7
- Total Time: 8 minutes (480s)
- Wave Duration: 1.5 minutes (90s)
- Buffer Time: 10 seconds
- Enemy Health: 130%
- Enemy Damage: 150%

## 🔮 Future Enhancements (Optional)

### Apply Enemy Multipliers
Currently the multipliers are stored but not applied. To apply them to enemies:

In your Enemy's HealthSystem initialization:
```csharp
void Start()
{
    if (GameSettings.Instance != null)
    {
        WaveConfig config = GameSettings.Instance.GetWaveConfig();
        maxHealth *= config.enemyHealthMultiplier;
        currentHealth = maxHealth;
    }
}
```

In your Enemy's damage dealing:
```csharp
float finalDamage = baseDamage;
if (GameSettings.Instance != null)
{
    WaveConfig config = GameSettings.Instance.GetWaveConfig();
    finalDamage *= config.enemyDamageMultiplier;
}
player.TakeDamage(finalDamage);
```

### Add More Settings
You can easily extend GameSettings.cs to include:
- Volume settings
- Graphics quality
- Control sensitivity
- Custom wave configurations

### Add Visual Feedback
- Animation when selecting difficulty
- Sound effects for button clicks
- Color-coded difficulty indicators

## ✅ Testing Checklist

- [ ] GameSettings persists across scenes
- [ ] Settings panel opens/closes correctly
- [ ] Difficulty buttons highlight when selected
- [ ] Apply button saves settings
- [ ] Settings load correctly when starting game
- [ ] Wave counts match selected difficulty
- [ ] Time limits match selected difficulty
- [ ] Settings persist after closing the game

## 🐛 Troubleshooting

**Settings not loading in game?**
- Make sure GameSettings GameObject exists in menu scene
- Check that WaveManager calls LoadDifficultySettings() in Start()

**Buttons not working?**
- Verify all UI references are assigned in SettingsManager Inspector
- Check that buttons have the Button component

**Settings not persisting?**
- PlayerPrefs.Save() is called after SetDifficulty()
- Check PlayerPrefs in Unity Editor → Edit → Clear All PlayerPrefs (to reset)

## 📝 Files Modified
- MenuScreen.cs - Added settings integration
- WaveManager.cs - Added difficulty loading

## 📝 Files Created
- GameSettings.cs - Core settings system
- SettingsManager.cs - UI management
- SETTINGS_README.md - This documentation

Enjoy your new difficulty system! 🎮✨
