# PixelForge API Reference

Complete API reference for the PixelForge engine.

## Core Namespace: PixelForge.Engine.Core

### GameEngine

Main game engine class.

```csharp
public class GameEngine : Game
{
    // Methods
    void LoadMap(string mapId);
    GameState GetGameState();
    ResourceManager GetResourceManager();
    MapManager GetMapManager();
}
```

### GameState

Global game state management.

```csharp
public class GameState
{
    // Properties
    string? CurrentMapId { get; set; }
    TimeSpan PlayTime { get; }
    int PartyGold { get; set; }
    List<string> PartyMembers { get; }
    Dictionary<string, int> Inventory { get; }

    // Methods
    bool GetSwitch(int id);
    void SetSwitch(int id, bool value);
    int GetVariable(int id);
    void SetVariable(int id, int value);
    bool GetSelfSwitch(string mapId, string eventId, string switchKey);
    void SetSelfSwitch(string mapId, string eventId, string switchKey, bool value);
    void AddItem(string itemId, int count = 1);
    void RemoveItem(string itemId, int count = 1);
    bool HasItem(string itemId, int count = 1);
    void Reset();
}
```

### InputManager

Input handling for keyboard, mouse, and gamepad.

```csharp
public class InputManager
{
    // Keyboard
    bool IsKeyDown(Keys key);
    bool IsKeyUp(Keys key);
    bool IsKeyPressed(Keys key);
    bool IsKeyReleased(Keys key);

    // Mouse
    Point MousePosition { get; }
    bool IsLeftMouseButtonDown { get; }
    bool IsRightMouseButtonDown { get; }
    bool IsLeftMouseButtonPressed { get; }
    bool IsRightMouseButtonPressed { get; }

    // GamePad
    bool IsButtonDown(Buttons button);
    bool IsButtonPressed(Buttons button);
    Vector2 LeftThumbstick { get; }
    Vector2 RightThumbstick { get; }

    // Utility
    Vector2 GetDirectionInput();
}
```

### ResourceManager

Asset loading and caching.

```csharp
public class ResourceManager
{
    Texture2D GetPixelTexture();
    Texture2D? LoadTexture(string path);
    Texture2D? LoadTextureFromFile(string filePath, GraphicsDevice graphicsDevice);
    SoundEffect? LoadSoundEffect(string path);
    Song? LoadSong(string path);
    void UnloadAll();
    void UnloadTexture(string path);
}
```

## Map Namespace: PixelForge.Engine.Map

### MapManager

Map loading and rendering.

```csharp
public class MapManager
{
    // Properties
    MapData? CurrentMap { get; }
    Vector2 CameraPosition { get; set; }

    // Methods
    void LoadMap(string mapId);
    void SaveMap(MapData map, string mapId);
    void Update(GameTime gameTime, InputManager inputManager);
    void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}
```

## Graphics Namespace: PixelForge.Engine.Graphics

### TileRenderer

Tile rendering system.

```csharp
public class TileRenderer
{
    void RegisterTileset(Tileset tileset);
    void DrawTile(SpriteBatch spriteBatch, TileData tile, Vector2 position,
                  int tileWidth, int tileHeight, float opacity = 1.0f);
    void DrawLayer(SpriteBatch spriteBatch, MapLayer layer,
                   int tileWidth, int tileHeight, Vector2 cameraOffset = default);
}
```

### Tileset

Tileset definition.

```csharp
public class Tileset
{
    int Id { get; set; }
    string Name { get; set; }
    string ImagePath { get; set; }
    int TileWidth { get; set; }
    int TileHeight { get; set; }
    int Spacing { get; set; }
    int Margin { get; set; }
}
```

## Shared Models: PixelForge.Shared.Models

### MapData

Complete map representation.

```csharp
public class MapData
{
    string Id { get; set; }
    string Name { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    int TileWidth { get; set; }
    int TileHeight { get; set; }
    string? Bgm { get; set; }
    string? Bgs { get; set; }
    bool ScrollX { get; set; }
    bool ScrollY { get; set; }
    int EncounterStep { get; set; }
    List<string> Encounters { get; set; }
    List<MapLayer> Layers { get; set; }
    List<MapEvent> Events { get; set; }
    ParallaxSettings? Parallax { get; set; }

    static MapData CreateDefault(int width = 20, int height = 15);
}
```

### MapLayer

Single layer in a map.

```csharp
public class MapLayer
{
    string Id { get; set; }
    string Name { get; set; }
    LayerType Type { get; set; }
    bool Visible { get; set; }
    float Opacity { get; set; }
    int ZIndex { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    TileData[][] Tiles { get; set; }

    void Initialize(int width, int height);
    TileData? GetTile(int x, int y);
    void SetTile(int x, int y, TileData tile);
}
```

### MapEvent

Event on a map.

```csharp
public class MapEvent
{
    string Id { get; set; }
    string Name { get; set; }
    Vector2Int Position { get; set; }
    List<EventPage> Pages { get; set; }
    string? Note { get; set; }
}
```

### TileData

Single tile data.

```csharp
public record TileData
{
    int TilesetId { get; init; }
    int TileId { get; init; }
    bool FlipH { get; init; }
    bool FlipV { get; init; }
    int Rotation { get; init; }

    static readonly TileData Empty;
    bool IsEmpty { get; }
}
```

### Vector2Int

Integer 2D vector.

```csharp
public record struct Vector2Int(int X, int Y)
{
    static readonly Vector2Int Zero;
    static readonly Vector2Int One;
    static readonly Vector2Int Up;
    static readonly Vector2Int Down;
    static readonly Vector2Int Left;
    static readonly Vector2Int Right;

    static Vector2Int operator +(Vector2Int a, Vector2Int b);
    static Vector2Int operator -(Vector2Int a, Vector2Int b);
    static Vector2Int operator *(Vector2Int a, int scalar);
    static Vector2Int operator /(Vector2Int a, int scalar);

    int LengthSquared();
    float Length();
}
```

### Rectangle

Integer rectangle.

```csharp
public record struct Rectangle(int X, int Y, int Width, int Height)
{
    int Left { get; }
    int Right { get; }
    int Top { get; }
    int Bottom { get; }
    Vector2Int Position { get; }
    Vector2Int Size { get; }
    Vector2Int Center { get; }

    bool Contains(Vector2Int point);
    bool Intersects(Rectangle other);
    static Rectangle FromPoints(Vector2Int min, Vector2Int max);
}
```

## Vibe Code: PixelForge.VibeCode

### ClaudeApiClient

Claude API client.

```csharp
public class ClaudeApiClient : IDisposable
{
    Task<ClaudeResponse?> SendMessageAsync(string userMessage,
        string? systemPrompt = null,
        List<Message>? conversationHistory = null,
        int maxTokens = 4096,
        string? model = null);

    Task<string?> GenerateCodeAsync(string description,
        string context,
        string language = "C#");
}
```

### CodeGenerator

Code generation service.

```csharp
public class CodeGenerator
{
    Task<CodeGenerationResult> GenerateAsync(string prompt,
        CodeGenerationOptions? options = null);
    Task<string?> ExplainCodeAsync(string code);
    Task<string?> OptimizeCodeAsync(string code);
    Task<string?> FixCodeAsync(string code, string errorMessage);
    void ClearHistory();
}
```

## Enumerations

### LayerType

```csharp
public enum LayerType
{
    Tile,
    Collision,
    Region,
    Event
}
```

### EventTrigger

```csharp
public enum EventTrigger
{
    ActionButton = 0,
    PlayerTouch = 1,
    EventTouch = 2,
    Autorun = 3,
    Parallel = 4
}
```

### EventPriority

```csharp
public enum EventPriority
{
    Below = 0,
    Normal = 1,
    Above = 2
}
```

### MovementType

```csharp
public enum MovementType
{
    Fixed = 0,
    Random = 1,
    Approach = 2,
    Custom = 3
}
```

## Event Command Codes

Common event command codes:

- **101**: Show Message
- **102**: Show Choices
- **103**: Input Number
- **111**: Conditional Branch
- **112**: Loop
- **113**: Break Loop
- **118**: Label
- **119**: Jump to Label
- **121**: Control Switches
- **122**: Control Variables
- **123**: Control Self Switch
- **125**: Change Gold
- **126**: Change Items
- **201**: Transfer Player
- **202**: Set Vehicle Location
- **203**: Set Event Location
- **204**: Scroll Map
- **205**: Set Movement Route
- **221**: Fadeout Screen
- **222**: Fadein Screen
- **224**: Flash Screen
- **225**: Shake Screen
- **241**: Play BGM
- **301**: Battle Processing
- **302**: Shop Processing
- **355**: Script (C# code execution)
- **413**: Repeat Above

---

For more information, see the [Getting Started Guide](GETTING_STARTED.md).
