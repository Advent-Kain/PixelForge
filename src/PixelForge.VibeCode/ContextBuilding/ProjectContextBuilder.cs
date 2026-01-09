using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PixelForge.VibeCode.ContextBuilding;

/// <summary>
/// Builds context about the current project for AI code generation.
/// </summary>
public class ProjectContextBuilder
{
    private readonly string _projectPath;

    public ProjectContextBuilder(string projectPath)
    {
        _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
    }

    /// <summary>
    /// Build a comprehensive context string for the AI.
    /// </summary>
    public async Task<string> BuildContextAsync(bool includeProjectStructure = true)
    {
        var sb = new StringBuilder();

        // API Documentation
        sb.AppendLine("# PixelForge Engine API");
        sb.AppendLine();
        sb.AppendLine(GetApiDocumentation());
        sb.AppendLine();

        // Project structure
        if (includeProjectStructure && Directory.Exists(_projectPath))
        {
            sb.AppendLine("# Current Project Structure");
            sb.AppendLine();
            sb.AppendLine(await GetProjectStructureAsync());
            sb.AppendLine();
        }

        // Common patterns
        sb.AppendLine("# Common Patterns");
        sb.AppendLine();
        sb.AppendLine(GetCommonPatterns());

        return sb.ToString();
    }

    /// <summary>
    /// Get API documentation for common engine classes.
    /// </summary>
    private string GetApiDocumentation()
    {
        return @"## Core Classes

### GameEngine
Main game loop and engine management.
- LoadMap(string mapId): Load a map
- GetGameState(): Access game state (switches, variables, etc.)
- GetResourceManager(): Access resource manager
- GetMapManager(): Access map manager

### GameState
Global game state management.
- GetSwitch(int id) / SetSwitch(int id, bool value): Switch operations
- GetVariable(int id) / SetVariable(int id, int value): Variable operations
- GetSelfSwitch(mapId, eventId, key) / SetSelfSwitch(...): Self-switch operations
- AddItem(itemId, count) / RemoveItem(itemId, count) / HasItem(itemId, count): Inventory

### MapManager
Map loading and rendering.
- LoadMap(string mapId): Load a map
- SaveMap(MapData map, string mapId): Save a map
- CurrentMap: Access current map data

### MapData
Represents a complete map.
- Properties: Id, Name, Width, Height, TileWidth, TileHeight
- Layers: List<MapLayer>
- Events: List<MapEvent>
- CreateDefault(width, height): Create new map

### MapLayer
A single layer in a map.
- Properties: Name, Type, Visible, Opacity, ZIndex
- GetTile(x, y) / SetTile(x, y, tile): Tile operations
- Initialize(width, height): Initialize layer

### EventCommand Codes
- 101: Show Message
- 102: Show Choices
- 103: Input Number
- 111: Conditional Branch
- 112: Loop
- 113: Break Loop
- 115: Exit Event Processing
- 118: Label
- 119: Jump to Label
- 121: Control Switches
- 122: Control Variables
- 123: Control Self Switch
- 125: Change Gold
- 126: Change Items
- 129: Change Party Member
- 201: Transfer Player
- 204: Scroll Map
- 205: Set Movement Route
- 214: Erase Event
- 221: Fadeout Screen
- 222: Fadein Screen
- 223: Tint Screen
- 224: Flash Screen
- 225: Shake Screen
- 230: Wait
- 231: Show Picture
- 232: Move Picture
- 233: Rotate Picture
- 234: Tint Picture
- 235: Erase Picture
- 241: Play BGM
- 242: Fadeout BGM
- 245: Play BGS
- 246: Fadeout BGS
- 249: Play ME
- 250: Play SE
- 251: Stop SE
- 301: Battle Processing
- 302: Shop Processing
- 355: Script (C# code)
- 413: Repeat Above

## Custom Scripts

Create custom game scripts by extending GameScript:

```csharp
public class MyCustomScript : GameScript
{
    public override void OnGameStart()
    {
        // Called when game starts
    }

    public override void OnMapLoad(Map map)
    {
        // Called when a map loads
    }
}
```";
    }

    /// <summary>
    /// Get the project structure as a tree.
    /// </summary>
    private async Task<string> GetProjectStructureAsync()
    {
        var sb = new StringBuilder();

        try
        {
            var mapsDir = Path.Combine(_projectPath, "Maps");
            if (Directory.Exists(mapsDir))
            {
                sb.AppendLine("Maps/");
                foreach (var file in Directory.GetFiles(mapsDir, "*.json"))
                {
                    sb.AppendLine($"  - {Path.GetFileName(file)}");
                }
            }

            var scriptsDir = Path.Combine(_projectPath, "Scripts");
            if (Directory.Exists(scriptsDir))
            {
                sb.AppendLine("Scripts/");
                foreach (var file in Directory.GetFiles(scriptsDir, "*.cs"))
                {
                    sb.AppendLine($"  - {Path.GetFileName(file)}");
                }
            }
        }
        catch
        {
            // Ignore errors reading project structure
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get common code patterns and examples.
    /// </summary>
    private string GetCommonPatterns()
    {
        return @"## Example: Custom Damage Formula

```csharp
public class CustomDamageFormula : IDamageFormula
{
    public int Calculate(Actor attacker, Actor defender, Skill skill)
    {
        int baseDamage = attacker.Attack * 4 - defender.Defense * 2;
        baseDamage += skill.BaseDamage;

        // Add randomness
        int variance = (int)(baseDamage * 0.2f);
        baseDamage += Random.Next(-variance, variance + 1);

        return Math.Max(0, baseDamage);
    }
}
```

## Example: Custom Event Command

```csharp
public class ShowNotificationCommand : CustomEventCommand
{
    public override string CommandName => ""Show Notification"";

    public override void Execute(EventContext context)
    {
        string message = context.Parameters[0].ToString();
        int duration = (int)context.Parameters[1];

        // Show notification UI
        context.Game.ShowNotification(message, duration);
    }

    public override List<EventParameter> GetParameters()
    {
        return new List<EventParameter>
        {
            new EventParameter { Name = ""Message"", Type = ParameterType.String },
            new EventParameter { Name = ""Duration"", Type = ParameterType.Integer }
        };
    }
}
```

## Example: Quest System

```csharp
public class QuestManager
{
    private Dictionary<string, Quest> _quests = new();

    public void StartQuest(string questId)
    {
        if (_quests.ContainsKey(questId))
            return;

        var quest = LoadQuest(questId);
        quest.Status = QuestStatus.Active;
        _quests[questId] = quest;
    }

    public void CompleteQuest(string questId)
    {
        if (_quests.TryGetValue(questId, out var quest))
        {
            quest.Status = QuestStatus.Completed;
            GiveRewards(quest);
        }
    }
}
```";
    }
}
