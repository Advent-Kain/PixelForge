# PixelForge - 2D Game Engine with Vibe Coding

**PixelForge** is a powerful 2D game engine and visual development tool similar to RPG Maker MZ, built with C# and featuring native AI-powered "vibe coding" support through Claude API integration.

## Features

### Core Engine
- **MonoGame-based 2D rendering** - Cross-platform graphics with excellent performance
- **Entity-Component-System architecture** - Flexible and maintainable game object management
- **Advanced tile-based maps** - Multi-layer support with parallax backgrounds
- **Event system** - Visual event scripting with conditional logic
- **Triple battle system** - Turn-based, ATB, and Action Economy modes
- **Resource management** - Efficient loading and caching of assets
- **Shop system** - Complete buy/sell functionality
- **Dialogue system** - Branching conversations with localization
- **Quest system** - Objective tracking with rewards
- **Save/Load system** - Multiple save slots with JSON format

### Visual Editor
- **Avalonia-based UI** - Modern, cross-platform editor interface
- **Map editor** - Multi-layer tile painting with real-time preview
- **Database editor** - Manage actors, items, skills, enemies, and more
- **Event editor** - Visual event command editor
- **Script editor** - C# code editor with syntax highlighting
- **Dialogue editor** - Visual dialogue tree creation with localization
- **String editor** - Multi-language localization management
- **Asset browser** - Import and preview sprites, tilesets, and audio
- **Menu system** - Complete inventory, equipment, skills, status UIs
- **Test Play** - Launch game directly from editor
- **Export system** - Multi-platform packaging (Windows/Linux/macOS)

### Vibe Coding (AI-Powered Development)
- **Natural language to code** - Describe features in plain English, get C# code
- **Context-aware suggestions** - AI understands your project structure
- **Code explanation** - Get detailed explanations of existing code
- **Code optimization** - AI-powered refactoring and performance improvements
- **Debugging assistance** - Fix errors with AI help

### C# Scripting
- **Full C# scripting support** - Use the power of C# for game logic
- **Hot-reload** - Modify scripts without restarting the editor
- **Rich API** - Access all engine features from scripts
- **Custom event commands** - Extend the event system with C#
- **Plugin system** - Create and share reusable game systems

## Project Structure

```
PixelForge/
├── src/
│   ├── PixelForge.Engine/        # Core game engine runtime
│   │   ├── Core/                 # Game loop, input, state management
│   │   ├── Graphics/             # Rendering, sprites, animations
│   │   ├── Audio/                # Sound system
│   │   ├── Map/                  # Map rendering and management
│   │   ├── Battle/               # Battle system
│   │   ├── UI/                   # Menu and HUD systems
│   │   ├── Scripting/            # C# script hosting
│   │   └── Data/                 # Database models
│   │
│   ├── PixelForge.Editor/        # Visual editor application
│   │   ├── Views/                # Avalonia UI views
│   │   ├── ViewModels/           # MVVM view models
│   │   ├── Controls/             # Custom UI controls
│   │   └── Services/             # Editor services
│   │
│   ├── PixelForge.Shared/        # Shared models and utilities
│   │   ├── Models/               # Data models (maps, events, etc.)
│   │   └── Utilities/            # Common utilities
│   │
│   └── PixelForge.VibeCode/      # AI coding assistant
│       ├── ClaudeClient/         # Claude API integration
│       ├── CodeGeneration/       # Code generation logic
│       └── ContextBuilding/      # Project context extraction
│
├── tests/                        # Unit and integration tests
├── docs/                         # Documentation
└── samples/                      # Sample projects
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- (Optional) Anthropic API key for vibe coding features

### Building the Project

1. Clone the repository:
```bash
git clone https://github.com/yourusername/PixelForge.git
cd PixelForge
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the solution:
```bash
dotnet build
```

4. Run the editor:
```bash
dotnet run --project src/PixelForge.Editor/PixelForge.Editor.csproj
```

### Using Vibe Coding

To enable AI-powered vibe coding features:

1. Get an API key from [Anthropic](https://console.anthropic.com/)
2. Set your API key as an environment variable:
```bash
export ANTHROPIC_API_KEY=your-api-key-here
```
3. Use the "Ask Claude" feature in the editor to generate code from natural language

## Quick Start Example

### Creating Your First Map

1. Launch PixelForge Editor
2. Click "File" → "New Project"
3. In the Map Editor, select a layer
4. Use the tileset palette to paint tiles
5. Add events by right-clicking on the map
6. Test your map with "Game" → "Test Play"

### Writing Custom Scripts

Create a custom script in the Scripts folder:

```csharp
using PixelForge.Engine.Scripting;

public class DayNightCycle : GameScript
{
    private float timeOfDay = 0f;

    public override void OnMapLoad(Map map)
    {
        // Reset time when entering a new map
        timeOfDay = 12f; // Start at noon
    }

    public void Update(float deltaTime)
    {
        timeOfDay += deltaTime / 60f; // 1 hour per minute
        if (timeOfDay >= 24f)
            timeOfDay -= 24f;

        // Adjust screen tint based on time
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (timeOfDay >= 6f && timeOfDay < 18f)
        {
            // Daytime - normal lighting
            Game.SetScreenTint(255, 255, 255, 255);
        }
        else
        {
            // Nighttime - blue tint
            Game.SetScreenTint(100, 100, 150, 200);
        }
    }
}
```

### Using Vibe Coding

Simply describe what you want in the vibe coding panel:

```
User: "Create a quest system with multiple stages and completion tracking"
```

The AI will generate a complete quest system implementation tailored to your project!

## Architecture

### Engine Architecture

PixelForge uses a modern, modular architecture:

- **Game Loop**: 60 FPS fixed timestep with MonoGame
- **Resource Management**: Lazy loading with caching for optimal performance
- **Event System**: Command pattern for extensibility
- **State Management**: Centralized game state with switches and variables
- **Serialization**: JSON-based for human-readable project files

### Editor Architecture

The editor uses MVVM (Model-View-ViewModel) pattern with:

- **Avalonia UI**: Cross-platform desktop UI framework
- **ReactiveUI**: Reactive programming for UI updates
- **CommunityToolkit.Mvvm**: Modern MVVM helpers

## API Reference

### Core Classes

#### GameEngine
Main entry point for the game runtime.

```csharp
public class GameEngine
{
    void LoadMap(string mapId);
    GameState GetGameState();
    ResourceManager GetResourceManager();
    MapManager GetMapManager();
}
```

#### GameState
Manages global game state.

```csharp
public class GameState
{
    bool GetSwitch(int id);
    void SetSwitch(int id, bool value);
    int GetVariable(int id);
    void SetVariable(int id, int value);
    bool HasItem(string itemId, int count = 1);
    void AddItem(string itemId, int count = 1);
}
```

#### MapManager
Handles map loading and rendering.

```csharp
public class MapManager
{
    void LoadMap(string mapId);
    void SaveMap(MapData map, string mapId);
    MapData? CurrentMap { get; }
}
```

## Performance Targets

- 60 FPS with 100+ events on screen
- Map load time < 500ms
- Memory usage < 500MB for typical game
- Editor responsiveness < 16ms per frame

## Roadmap

### Phase 1: Foundation (Completed)
- ✅ Basic engine architecture
- ✅ Tile rendering system
- ✅ Map editor with single layer
- ✅ Asset import pipeline
- ✅ Project structure

### Phase 2: Core Systems (In Progress)
- 🔄 Event system with basic commands
- 🔄 Database structures
- 🔄 Message system
- 🔄 Switch/variable system
- 🔄 Save/load functionality

### Phase 3: RPG Features (Planned)
- ⏳ Battle system
- ⏳ Menu system
- ⏳ Skill and item usage
- ⏳ Equipment system
- ⏳ Character progression

### Phase 4: Advanced Features (Planned)
- ⏳ Complete event command set
- ⏳ Advanced map features
- ⏳ Animation system
- ⏳ Audio management
- ⏳ Screen effects

### Phase 5: Tooling (Planned)
- ⏳ C# scripting API finalization
- ⏳ Plugin system
- ⏳ Debug tools
- ⏳ Export/deployment system

### Phase 6: AI Integration (Planned)
- ⏳ Vibe coding interface
- ⏳ Context extraction
- ⏳ Code generation templates
- ⏳ Testing and refinement

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) before submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [MonoGame](https://www.monogame.net/)
- UI powered by [Avalonia](https://avaloniaui.net/)
- AI features powered by [Anthropic Claude](https://www.anthropic.com/)
- Inspired by [RPG Maker MZ](https://www.rpgmakerweb.com/)

## Support

- Documentation: [docs/](docs/)
- Issues: [GitHub Issues](https://github.com/yourusername/PixelForge/issues)
- Discussions: [GitHub Discussions](https://github.com/yourusername/PixelForge/discussions)

---

**Made with ❤️ by the PixelForge Team**
