# Getting Started with PixelForge

This guide will help you create your first game with PixelForge.

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- A code editor (Visual Studio 2022, VS Code, or Rider)
- (Optional) Anthropic API key for AI features

### Build from Source

1. Clone the repository:
```bash
git clone https://github.com/yourusername/PixelForge.git
cd PixelForge
```

2. Restore and build:
```bash
dotnet restore
dotnet build
```

3. Run the editor:
```bash
dotnet run --project src/PixelForge.Editor/PixelForge.Editor.csproj
```

## Creating Your First Project

### 1. New Project

1. Launch PixelForge Editor
2. Click **File → New Project**
3. Choose a project name and location
4. Click **Create**

### 2. Create a Map

1. In the Project panel, right-click **Maps**
2. Select **New Map**
3. Set map dimensions (e.g., 20x15 tiles)
4. Click **OK**

### 3. Paint Tiles

1. Select a layer in the Layers panel
2. Choose a tile from the Tileset panel
3. Click on the map to paint
4. Use right-click to erase

### 4. Add an Event

1. Right-click on the map where you want an event
2. Select **Create Event**
3. Set the event graphic and properties
4. Add commands in the Event Commands panel

### 5. Test Your Game

Click **Game → Test Play** or press **F5** to test your game.

## Basic Concepts

### Maps

Maps are composed of multiple layers:
- **Ground Layer**: Base terrain
- **Decoration Layer**: Objects and decorations
- **Collision Layer**: Defines walkable areas
- **Event Layer**: Contains events

### Events

Events are interactive objects on maps:
- **NPCs**: Characters you can talk to
- **Items**: Pickups and treasures
- **Triggers**: Automated actions
- **Doors**: Map transitions

### Database

The database stores game data:
- **Actors**: Player characters
- **Items**: Consumables and equipment
- **Skills**: Abilities and magic
- **Enemies**: Battle opponents
- **Troops**: Enemy formations

### Scripting

Extend your game with C# scripts:

```csharp
public class MyScript : GameScript
{
    public override void OnGameStart()
    {
        Game.ShowMessage("Welcome to my game!");
    }
}
```

## Next Steps

- [Map Editor Guide](MAP_EDITOR.md)
- [Event System Guide](EVENT_SYSTEM.md)
- [Scripting Guide](SCRIPTING_GUIDE.md)
- [Vibe Coding Tutorial](VIBE_CODING.md)

## Troubleshooting

### Editor won't start
- Ensure .NET 8.0 SDK is installed
- Check the console for error messages

### Maps won't load
- Verify map file exists in Maps/ folder
- Check JSON syntax is valid

### Scripts won't compile
- Check for syntax errors
- Ensure using correct namespaces
- Verify API references are correct

## Getting Help

- [Documentation](../README.md)
- [GitHub Issues](https://github.com/yourusername/PixelForge/issues)
- [Community Discussions](https://github.com/yourusername/PixelForge/discussions)
