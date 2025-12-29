# 📊 PlantUML Architecture Diagrams

This folder contains comprehensive PlantUML diagrams for the **Survivalist Sorcerer - The Mesh Escape** project.

## 📁 Available Diagrams

### 1. **SystemArchitecture.puml** - System Architecture Diagram
- **Purpose**: High-level overview of all game systems and their relationships
- **Shows**:
  - Core Systems (GameManager, WaveManager, GameSettings)
  - Player Systems (Movement, Animation, Combat, Spells)
  - AI Systems (EnemyAI, Spawner, NavMesh)
  - UI Systems (Menus, HUD, Panels)
  - Camera System
  - Support Systems (Save, Audio, Collectibles)
  - Unity Built-in systems

### 2. **GameFlow.puml** - Game Flow Diagram
- **Purpose**: Complete game flow from start to finish
- **Shows**:
  - Menu navigation
  - Difficulty selection
  - Wave progression
  - Boss fight sequence
  - Victory/Game Over conditions
  - Sudden Death mechanics

### 3. **ClassDiagram.puml** - Detailed Class Diagram
- **Purpose**: Object-oriented design of core classes
- **Shows**:
  - Class attributes and methods
  - Relationships (composition, aggregation, inheritance)
  - Singleton patterns
  - Enumerations
  - Data structures
  - Design patterns used

### 4. **SequenceDiagram.puml** - Combat Sequence Diagram
- **Purpose**: Detailed interaction flow during combat
- **Shows**:
  - Player spell casting sequence
  - Projectile lifecycle
  - Enemy AI behavior
  - Damage calculation
  - Death and scoring events
  - Wave completion logic

### 5. **DeploymentDiagram.puml** - Deployment Architecture
- **Purpose**: Platform deployment structure
- **Shows**:
  - Windows, Linux, macOS builds
  - Unity Engine components
  - Runtime dependencies
  - Storage locations
  - System requirements

## 🚀 How to Use These Diagrams

### Online Rendering
1. Visit [PlantUML Online Server](http://www.plantuml.com/plantuml/uml/)
2. Copy the content of any `.puml` file
3. Paste into the text area
4. View the generated diagram

### VS Code (Recommended)
1. Install the **PlantUML** extension by jebbs
2. Install **Graphviz** (required for rendering):
   - **Windows**: `choco install graphviz` or download from [graphviz.org](https://graphviz.org/download/)
   - **Linux**: `sudo apt install graphviz`
   - **macOS**: `brew install graphviz`
3. Open any `.puml` file in VS Code
4. Press `Alt+D` to preview
5. Right-click → Export to PNG/SVG

### Command Line
```bash
# Install PlantUML
# On macOS: brew install plantuml
# On Linux: sudo apt install plantuml
# On Windows: choco install plantuml

# Generate PNG
plantuml SystemArchitecture.puml

# Generate SVG
plantuml -tsvg SystemArchitecture.puml

# Generate all diagrams
plantuml *.puml
```

### IntelliJ IDEA / WebStorm
1. Install **PlantUML Integration** plugin
2. Right-click on `.puml` file
3. Select "Show PlantUML Diagram"

## 📸 Export Options

The diagrams can be exported in multiple formats:
- **PNG** - For documentation and presentations
- **SVG** - For scalable vector graphics
- **PDF** - For print-ready documents
- **LaTeX** - For academic papers

## 🎨 Diagram Features

All diagrams include:
- ✅ Color-coded components
- ✅ Clear relationships and arrows
- ✅ Descriptive notes and legends
- ✅ Professional theming
- ✅ Comprehensive labels

## 📚 PlantUML Syntax Reference

- [Official PlantUML Documentation](https://plantuml.com/)
- [Component Diagram](https://plantuml.com/component-diagram)
- [Class Diagram](https://plantuml.com/class-diagram)
- [Sequence Diagram](https://plantuml.com/sequence-diagram)
- [Activity Diagram](https://plantuml.com/activity-diagram-beta)
- [Deployment Diagram](https://plantuml.com/deployment-diagram)

## 🔄 Updating Diagrams

When updating the game architecture:
1. Modify the corresponding `.puml` file
2. Regenerate the diagram
3. Update documentation if needed
4. Commit both `.puml` and exported images

## 📝 Diagram Conventions

- **Color Coding**:
  - Pink: Core Systems
  - Light Blue: Player Systems
  - Orange: AI Systems
  - Green: UI Systems
  - Purple: Support Systems
  - Yellow: Camera System
  - Gray: Unity Built-in

- **Arrow Types**:
  - `-->` : Dependency/Usage
  - `--` : Association
  - `*--` : Composition
  - `o--` : Aggregation
  - `<|--` : Inheritance

## 🎯 Use Cases

These diagrams are useful for:
- 📖 **Documentation**: Understanding system architecture
- 🎓 **Education**: Teaching game development concepts
- 👥 **Team Communication**: Onboarding new developers
- 🔍 **Code Review**: Visualizing dependencies
- 📊 **Presentations**: Academic or professional talks
- 🔧 **Debugging**: Understanding data flow

## 🤝 Contributing

When adding new systems to the game:
1. Update the relevant PlantUML diagrams
2. Add new diagrams if introducing major features
3. Keep diagrams in sync with code changes
4. Export updated images for documentation

---

**Made with 📊 PlantUML | Part of Survivalist Sorcerer - The Mesh Escape**
