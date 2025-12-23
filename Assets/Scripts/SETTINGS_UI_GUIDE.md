# Settings Panel UI Layout Guide

## 🎨 Visual Structure

```
┌─────────────────────────────────────────────┐
│         GAME SETTINGS                       │
│                                             │
│  Current Difficulty: MEDIUM                 │
│                                             │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐    │
│  │  EASY   │  │ MEDIUM  │  │  HARD   │    │
│  └─────────┘  └─────────┘  └─────────┘    │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │  ⭐⭐ MEDIUM MODE ⭐⭐              │   │
│  │                                     │   │
│  │  • Total Waves: 5                  │   │
│  │  • Time Limit: 10 Minutes          │   │
│  │  • Wave Duration: 2 Min            │   │
│  │  • Rest Time: 15 Seconds           │   │
│  │  • Enemy Strength: 100%            │   │
│  │  • Enemy Damage: 100%              │   │
│  │                                     │   │
│  │  Balanced challenge!                │   │
│  └─────────────────────────────────────┘   │
│                                             │
│       ┌────────┐      ┌────────┐          │
│       │ APPLY  │      │ CLOSE  │          │
│       └────────┘      └────────┘          │
└─────────────────────────────────────────────┘
```

## 📐 Hierarchy in Unity

```
Canvas
└── SettingsPanel (Panel - Initially disabled)
    ├── Background (Image - Semi-transparent dark)
    ├── TitleText (TextMeshProUGUI) "GAME SETTINGS"
    ├── CurrentDifficultyText (TextMeshProUGUI) "Current: Medium"
    │
    ├── ButtonsContainer (Empty GameObject - Horizontal Layout Group)
    │   ├── EasyButton (Button)
    │   │   └── Text (TextMeshProUGUI) "EASY"
    │   ├── MediumButton (Button)
    │   │   └── Text (TextMeshProUGUI) "MEDIUM"
    │   └── HardButton (Button)
    │       └── Text (TextMeshProUGUI) "HARD"
    │
    ├── InfoPanel (Panel - Scroll view recommended)
    │   └── DifficultyInfoText (TextMeshProUGUI - Large text area)
    │
    └── ControlButtons (Empty GameObject - Horizontal Layout Group)
        ├── ApplyButton (Button)
        │   └── Text (TextMeshProUGUI) "APPLY"
        └── CloseButton (Button)
            └── Text (TextMeshProUGUI) "CLOSE"
```

## 🎨 Recommended Styling

### Colors
- **Background Panel**: RGBA(0, 0, 0, 0.8) - Semi-transparent black
- **Buttons (Normal)**: RGBA(255, 255, 255, 1) - White
- **Buttons (Highlighted)**: RGBA(51, 204, 51, 1) - Green
- **Easy Button**: Light Green accent
- **Medium Button**: Yellow accent
- **Hard Button**: Red accent

### Font Sizes
- **Title**: 48-60
- **Current Difficulty**: 24-30
- **Button Text**: 28-36
- **Info Text**: 20-24

### Layout
- **Panel Size**: 800x600 or 70% of screen
- **Button Size**: 150x60 each
- **Info Panel**: 700x300
- **Spacing**: 20-30 pixels between elements

## 🔧 Component Setup Checklist

### SettingsPanel GameObject
- [x] Add RectTransform (automatic)
- [x] Add Image component (for background)
- [x] Set anchors to center
- [x] Initially set Active = false

### SettingsManager Component
Assign these in Inspector:
- [x] Settings Panel → SettingsPanel GameObject
- [x] Easy Button → EasyButton
- [x] Medium Button → MediumButton  
- [x] Hard Button → HardButton
- [x] Difficulty Info Text → DifficultyInfoText
- [x] Current Difficulty Text → CurrentDifficultyText
- [x] Close Button → CloseButton
- [x] Apply Button → ApplyButton

### MenuScreen Component
- [x] Settings Manager → SettingsManager GameObject

### Settings Button (in your main menu)
- [x] On Click() → MenuScreen.OpenSettings()

## 🎯 Quick Setup Steps

1. **Right-click in Hierarchy** → UI → Panel (creates SettingsPanel)
2. **Rename to "SettingsPanel"**
3. **Add Image** component if not present
4. **Create child objects** following hierarchy above
5. **Add Layout Groups** for automatic spacing:
   - ButtonsContainer: Horizontal Layout Group
   - ControlButtons: Horizontal Layout Group
6. **Create SettingsManager GameObject**:
   - Add Component → SettingsManager script
   - Assign all UI references
7. **Connect to MenuScreen**:
   - Assign SettingsManager reference
   - Hook up Settings button

## 💡 Pro Tips

### For Better UX:
- Add **hover effects** to buttons (use Button component's Color Tint)
- Add **sound effects** on button clicks
- Use **smooth transitions** (CanvasGroup fade in/out)
- Add a **semi-transparent overlay** behind the panel

### For Easy Testing:
- Keep SettingsPanel visible while designing
- Test each button individually
- Verify text updates when clicking buttons
- Check that settings persist after clicking Apply

### Layout Groups Settings:
**ButtonsContainer (Horizontal Layout Group)**:
- Spacing: 20
- Child Alignment: Middle Center
- Child Force Expand: Width & Height

**ControlButtons (Horizontal Layout Group)**:
- Spacing: 30
- Child Alignment: Middle Center
- Child Force Expand: Width

## 🖼️ Alternative: Use TextMeshPro

If using TextMeshPro (recommended):
1. Import TextMeshPro essentials (if prompted)
2. Use TextMeshProUGUI instead of Text components
3. Better font quality and features
4. More styling options

## ✅ Final Check

Before testing, verify:
- [ ] All references assigned in SettingsManager
- [ ] SettingsPanel starts disabled (Active = false)
- [ ] Buttons have Button component
- [ ] Text is readable (good contrast)
- [ ] Apply button works
- [ ] Close button works
- [ ] Settings persist between game sessions

Your settings system is now ready to use! 🎮
