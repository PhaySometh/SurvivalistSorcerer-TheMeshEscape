# Survivalist Sorcerer: The Mesh Escape
## Fundamental Game Development - Year 3 Term 1 Project Presentation

---

# SLIDE 1: Introduction

## 🎮 Game Overview

- **Title:** Survivalist Sorcerer: The Mesh Escape
- **Genre:** 3D Action Survival / Wave Defense
- **Concept:** A wizard trapped in a cursed village with 10 minutes to survive 5 waves, defeat the boss, and escape

### What Makes It Unique?
- Dynamic wave system: Next wave spawns even if you're slow
- Time-pressured survival mechanic
- Personality-driven feedback messages
- Sudden Death mode when time runs out

---

# SLIDE 2: Gameplay & Core Mechanics

## 🎯 Core Systems

### Game Flow
- **10-minute timer** with 5 waves (2 min each, soft limit)
- **Wave 1-4:** Progressive difficulty (Slimes → Skeletons → Golems)
- **Wave 5:** Boss fight (Bull King)
- **Sudden Death:** Infinite enemies if timer expires

### Key Mechanics
- **Dynamic Pressure:** Slow to kill enemies? Next wave spawns anyway
- **Combat:** WASD + Mouse, Light/Heavy attacks, attack while moving
- **Personality Messages:** Mocking when slow, praising on victory
- **Sudden Death Mode:** Timer hits 0? Infinite spawning begins

---

# SLIDE 3: Design & Art Direction

## 🎨 Visual Style & UI

### Art Style
- **Low-Poly 3D / Stylized Fantasy**
- Cambodian temple-inspired environment (Angkor Wat)
- Clean, colorful, and readable during intense combat

### Characters & Enemies
| Type | Role |
|------|------|
| Wizard | Player character with staff |
| Slimes, Turtles | Weak enemies (Wave 1) |
| Skeletons | Medium enemies (Waves 2-3) |
| Golems | Heavy units (Wave 4) |
| Bull King | Boss (Wave 5) |

### UI & Animation
- **HUD:** Health bar, timer (turns red in Sudden Death), score
- **Fantasy Wooden GUI** theme
- Smooth animations, fade-in/fade-out notifications
- Particle effects for magic projectiles and coins

---

# SLIDE 4: Technical Implementation

## 🔧 Key Scripts & Systems

### 1. **WaveManager.cs** - Wave Progression
- State machine: WaitingToStart → WaveInProgress → BossFight → SuddenDeath → Victory
- Manages 5 waves + boss spawn
- Event-driven notifications

### 2. **GameManager.cs** - Global State (Singleton)
- 10-minute countdown timer
- Triggers Sudden Death when timer expires
- Tracks score and game state

### 3. **EnemySpawner.cs** - Enemy Management
- Spawns enemies by difficulty (Weak/Medium/Strong)
- Uses NavMesh to avoid obstacles
- Tracks active enemies for wave completion

### 4. **EnemyAI.cs** - NavMesh-Based AI
- Chases player using pathfinding
- Attacks when in range
- State-based animations (idle/run)

### 5. **PlayerCombatSystem.cs** - Combat
- Light and heavy attacks
- Attack while moving
- Hitbox-based damage detection

### 6. **UIManager.cs** - HUD & Notifications
- Real-time health, timer, score updates
- Personality messages (mocking/praising)
- Victory/Game Over panels

### 7. **HealthSystem.cs** - Damage System
- Universal health component for player and enemies
- Damage events trigger animations and UI updates

---

# SLIDE 5: Conclusion & Future Improvements

## 🚀 What We Achieved

### ✅ Completed Features
- Fully functional 10-minute wave-based survival game
- 5 enemy types + boss with unique behaviors
- Dynamic difficulty scaling
- Polished UI with personality feedback
- Complete game loop (Menu → Gameplay → Victory/Defeat)
- Smooth combat system

### 🎯 Future Improvements (If More Time)
1. **Multiple Maps** - Expand beyond village
2. **Upgrade System** - Spend coins to enhance abilities
3. **More Enemy Types** - Flying enemies, ranged attackers
4. **Multiplayer Co-op** - 2-4 player survival mode
5. **Leaderboards & Achievements** - Track high scores
6. **Difficulty Settings** - Easy, Normal, Hard, Nightmare modes

---

# SLIDE 6: Demo Playthrough

## 🎬 Live Gameplay Demonstration (3-5 minutes)

### Demo Flow:
1. **Main Menu** → Click "Start Game"
2. **Loading Screen** → Wait for load (5 sec)
3. **Cinematic Intro** → Show intro messages
4. **Wave 1 Gameplay** (30 sec)
   - Show combat mechanics (light/heavy attacks)
   - Demonstrate enemy AI chasing
   - Clear wave quickly → "Round Clear!" message
5. **Waves 2-5 & Boss** (60 sec)
   - Show difficulty increase (more enemies)
   - Showcase boss fight with Bull King
   - Defeat boss → Victory panel
6. **Victory Screen** → Show stats

### Key Features to Highlight:
- ✨ Dynamic wave system (enemies overlapping)
- 💬 Personality messages ("You're so weak!" vs "Damn, boi!")
- ⚔️ Fluid combat while moving
- 👾 Variety of enemy AI behaviors
- ⏰ Timer mechanics and Sudden Death (if time permits)
