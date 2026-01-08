using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private readonly ResourceManager _resourceManager;
    private readonly TileRenderer _tileRenderer;
    private readonly GameState _gameState;
    private readonly GameDatabase _database;
    private readonly EventProcessor _eventProcessor;
    private readonly Dictionary<string, MapData> _loadedMaps = new();

    public MapData? CurrentMap { get; private set; }
    public Vector2 CameraPosition { get; set; }

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
        if (conditions.Switch1.HasValue && !_gameState.GetSwitch(conditions.Switch1.Value))
            return false;

        if (conditions.Switch2.HasValue && !_gameState.GetSwitch(conditions.Switch2.Value))
            return false;

        if (conditions.Variable.HasValue && conditions.VariableValue.HasValue)
        {
            if (_gameState.GetVariable(conditions.Variable.Value) < conditions.VariableValue.Value)
                return false;
        }

        if (!string.IsNullOrEmpty(conditions.SelfSwitch))
        {
            var mapId = _gameState.CurrentMapId ?? CurrentMap?.Id ?? string.Empty;
            if (!_gameState.GetSelfSwitch(mapId, evt.Id, conditions.SelfSwitch))
                return false;
        }

        if (!string.IsNullOrEmpty(conditions.Item) && !_gameState.HasItem(conditions.Item))
            return false;

        if (!string.IsNullOrEmpty(conditions.Actor) && !_gameState.PartyMembers.Contains(conditions.Actor))
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

        // Calculate screen position
        Vector2 position = new Vector2(
            evt.Position.X * CurrentMap.TileWidth - CameraPosition.X,
            evt.Position.Y * CurrentMap.TileHeight - CameraPosition.Y
        );

        // TODO: Load and draw character sprite
        // For now, just draw a placeholder rectangle
        var pixel = _resourceManager.GetPixelTexture();
        var rect = new Microsoft.Xna.Framework.Rectangle(
            (int)position.X,
            (int)position.Y,
            CurrentMap.TileWidth,
            CurrentMap.TileHeight
        );

        spriteBatch.Draw(pixel, rect, Color.Blue * 0.5f);
    }
}
