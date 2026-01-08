using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Shared.Models;
using PixelForge.Engine.Core;
using PixelForge.Engine.Graphics;
using PixelForge.Engine.Events;
using PixelForge.Engine.RPG;
using System.Text.Json;

namespace PixelForge.Engine.Map;

/// <summary>
/// Manages map loading, updating, and rendering.
/// </summary>
public class MapManager
{
    private readonly IGameContext _gameContext;
    private readonly ResourceManager _resourceManager;
    private readonly TileRenderer _tileRenderer;
    private readonly GameState _gameState;
    private readonly GameDatabase _database;
    private readonly EventProcessor _eventProcessor;
    private readonly Dictionary<string, MapData> _loadedMaps = new();
    private readonly Queue<MapEvent> _eventQueue = new();
    private readonly HashSet<string> _queuedEventIds = new(StringComparer.Ordinal);
    private string? _activeEventId;

    public MapData? CurrentMap { get; private set; }
    public Vector2 CameraPosition { get; set; }

    public MapManager(IGameContext gameContext)
    {
        _gameContext = gameContext;
        _resourceManager = gameContext.GetResourceManager();
        _tileRenderer = new TileRenderer(_resourceManager);
        _eventProcessor = new EventProcessor(gameContext);
    public MapManager(ResourceManager resourceManager, GameState gameState, GameDatabase database, EventProcessor eventProcessor)
    {
        _resourceManager = resourceManager;
        _tileRenderer = new TileRenderer(resourceManager);
        _gameState = gameState;
        _database = database;
        _eventProcessor = eventProcessor;
    }

    /// <summary>
    /// Refresh tilesets from the game database.
    /// </summary>
    public void RefreshTilesets()
    {
        _tileRenderer.ClearTilesets();
        _tileRenderer.RegisterTilesets(_database.Tilesets.Values);
    }

    /// <summary>
    /// Load a map from file.
    /// </summary>
    public void LoadMap(string mapId)
    {
        RefreshTilesets();
        // Check cache first
        if (_loadedMaps.TryGetValue(mapId, out var cachedMap))
        {
            CurrentMap = cachedMap;
            return;
        }

        // Load from file
        string mapPath = Path.Combine("Maps", $"{mapId}.json");

        if (!File.Exists(mapPath))
        {
            // Create a default map if file doesn't exist
            CurrentMap = MapData.CreateDefault();
            return;
        }

        try
        {
            string json = File.ReadAllText(mapPath);
            var map = JsonSerializer.Deserialize<MapData>(json);

            if (map != null)
            {
                _loadedMaps[mapId] = map;
                CurrentMap = map;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading map {mapId}: {ex.Message}");
            CurrentMap = MapData.CreateDefault();
        }
    }

    /// <summary>
    /// Save a map to file.
    /// </summary>
    public void SaveMap(MapData map, string mapId)
    {
        string mapsDir = "Maps";
        Directory.CreateDirectory(mapsDir);

        string mapPath = Path.Combine(mapsDir, $"{mapId}.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(map, options);
        File.WriteAllText(mapPath, json);

        _loadedMaps[mapId] = map;
    }

    /// <summary>
    /// Update the current map.
    /// </summary>
    public void Update(GameTime gameTime, InputManager inputManager)
    {
        if (CurrentMap == null)
            return;

        _eventProcessor.Update(gameTime);

        if (!_eventProcessor.IsBusy && _activeEventId != null)
        {
            _queuedEventIds.Remove(_activeEventId);
            _activeEventId = null;
        }

        EvaluateEventTriggers(inputManager);

        if (!_eventProcessor.IsBusy && _eventQueue.Count > 0)
        {
            var nextEvent = _eventQueue.Dequeue();
            _activeEventId = nextEvent.Id;
            _eventProcessor.ExecuteEvent(nextEvent);
        // Update events, animations, etc.
        if (!_eventProcessor.IsBusy)
        {
            var autorunEvent = CurrentMap.Events
                .Select(evt => (Event: evt, Page: GetActivePage(evt)))
                .FirstOrDefault(entry => entry.Page != null &&
                    (entry.Page.Trigger == EventTrigger.Autorun || entry.Page.Trigger == EventTrigger.Parallel));

            if (autorunEvent.Page != null)
            {
                _eventProcessor.ExecuteEvent(autorunEvent.Event);
            }
        }
    }

    /// <summary>
    /// Draw the current map.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (CurrentMap == null)
            return;

        // Sort layers by Z-index
        var sortedLayers = CurrentMap.Layers.OrderBy(l => l.ZIndex).ToList();

        foreach (var layer in sortedLayers)
        {
            if (layer.Type == LayerType.Tile)
            {
                _tileRenderer.DrawLayer(
                    spriteBatch,
                    layer,
                    CurrentMap.TileWidth,
                    CurrentMap.TileHeight,
                    CameraPosition
                );
            }
        }

        // Draw events
        DrawEvents(spriteBatch);
    }

    /// <summary>
    /// Draw map events.
    /// </summary>
    private void DrawEvents(SpriteBatch spriteBatch)
    {
        if (CurrentMap == null)
            return;

        foreach (var evt in CurrentMap.Events)
        {
            // Get the active page
            var activePage = GetActivePage(evt);
            if (activePage == null)
                continue;

            // Draw event sprite
            DrawEventSprite(spriteBatch, evt, activePage);
        }
    }

    /// <summary>
    /// Get the active page for an event based on conditions.
    /// </summary>
    private EventPage? GetActivePage(MapEvent evt)
    {
        if (evt.Pages.Count == 0)
            return null;

        for (int i = evt.Pages.Count - 1; i >= 0; i--)
        {
            var page = evt.Pages[i];
            if (CheckConditions(evt, page.Conditions))
                return page;
        }

        return null;
    }

    /// <summary>
    /// Check if page conditions are met against the current game state.
    /// </summary>
    private bool CheckConditions(MapEvent evt, EventConditions conditions)
    {
        var gameState = _gameContext.GetGameState();

        if (conditions.Switch1.HasValue && !gameState.GetSwitch(conditions.Switch1.Value))
            return false;

        if (conditions.Switch2.HasValue && !gameState.GetSwitch(conditions.Switch2.Value))
            return false;

        if (conditions.Variable.HasValue && conditions.VariableValue.HasValue)
        {
            if (gameState.GetVariable(conditions.Variable.Value) < conditions.VariableValue.Value)
                return false;
        }

        if (!string.IsNullOrEmpty(conditions.SelfSwitch))
        {
            var mapId = gameState.CurrentMapId ?? CurrentMap?.Id ?? string.Empty;
            if (!gameState.GetSelfSwitch(mapId, evt.Id, conditions.SelfSwitch))
                return false;
        }

        if (!string.IsNullOrEmpty(conditions.Item) && !gameState.HasItem(conditions.Item))
            return false;

        if (!string.IsNullOrEmpty(conditions.Actor) && !gameState.PartyMembers.Contains(conditions.Actor))
            return false;

        return true;
    }

    /// <summary>
    /// Draw an event's sprite.
    /// </summary>
    private void DrawEventSprite(SpriteBatch spriteBatch, MapEvent evt, EventPage page)
    {
        if (CurrentMap == null || string.IsNullOrEmpty(page.Graphic.CharacterName))
            return;

        var texture = _resourceManager.LoadTexture(page.Graphic.CharacterName);
        if (texture == null)
            return;

        var sourceRect = GetCharacterSourceRect(texture, page.Graphic);
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            return;

        // Calculate screen position
        Vector2 position = new Vector2(
            evt.Position.X * CurrentMap.TileWidth - CameraPosition.X,
            evt.Position.Y * CurrentMap.TileHeight - CameraPosition.Y
        );

        int destX = (int)position.X + (CurrentMap.TileWidth - sourceRect.Width) / 2;
        int destY = (int)position.Y + (CurrentMap.TileHeight - sourceRect.Height);
        var destRect = new Microsoft.Xna.Framework.Rectangle(destX, destY, sourceRect.Width, sourceRect.Height);

        spriteBatch.Draw(texture, destRect, sourceRect, Color.White);
    }

    private void EvaluateEventTriggers(InputManager inputManager)
    {
        if (CurrentMap == null)
            return;

        var gameState = _gameContext.GetGameState();
        bool actionPressed = inputManager.IsKeyPressed(Keys.Space)
            || inputManager.IsKeyPressed(Keys.Enter)
            || inputManager.IsButtonPressed(Buttons.A);

        foreach (var evt in CurrentMap.Events)
        {
            var activePage = GetActivePage(evt);
            if (activePage == null)
                continue;

            bool playerOnEvent = gameState.PlayerX == evt.Position.X && gameState.PlayerY == evt.Position.Y;

            switch (activePage.Trigger)
            {
                case EventTrigger.ActionButton:
                    if (actionPressed && playerOnEvent)
                        EnqueueEvent(evt);
                    break;
                case EventTrigger.PlayerTouch:
                case EventTrigger.EventTouch:
                    if (playerOnEvent)
                        EnqueueEvent(evt);
                    break;
                case EventTrigger.Autorun:
                case EventTrigger.Parallel:
                    EnqueueEvent(evt);
                    break;
            }
        }
    }

    private void EnqueueEvent(MapEvent evt)
    {
        if (!_queuedEventIds.Add(evt.Id))
            return;

        _eventQueue.Enqueue(evt);
    }

    private static Microsoft.Xna.Framework.Rectangle GetCharacterSourceRect(Texture2D texture, EventGraphic graphic)
    {
        int characterColumns = 1;
        int characterRows = 1;
        if (texture.Width % 12 == 0 && texture.Height % 8 == 0)
        {
            characterColumns = 4;
            characterRows = 2;
        }

        int frameWidth = texture.Width / (characterColumns * 3);
        int frameHeight = texture.Height / (characterRows * 4);

        if (frameWidth <= 0 || frameHeight <= 0)
            return Microsoft.Xna.Framework.Rectangle.Empty;

        int maxIndex = characterColumns * characterRows - 1;
        int characterIndex = Math.Clamp(graphic.CharacterIndex, 0, maxIndex);
        int characterColumn = characterIndex % characterColumns;
        int characterRow = characterIndex / characterColumns;

        int pattern = Math.Clamp(graphic.Pattern, 0, 2);
        int directionRow = graphic.Direction switch
        {
            2 => 0,
            4 => 1,
            6 => 2,
            8 => 3,
            _ => 0
        };

        int sourceX = (characterColumn * 3 + pattern) * frameWidth;
        int sourceY = (characterRow * 4 + directionRow) * frameHeight;

        return new Microsoft.Xna.Framework.Rectangle(sourceX, sourceY, frameWidth, frameHeight);
    }
}
