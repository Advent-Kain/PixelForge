using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Represents a complete map with all layers, events, and metadata.
/// </summary>
public class MapData
{
    /// <summary>
    /// Unique identifier for this map.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the map.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Map";

    /// <summary>
    /// Width in tiles.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; } = 20;

    /// <summary>
    /// Height in tiles.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; } = 15;

    /// <summary>
    /// Tile width in pixels.
    /// </summary>
    [JsonPropertyName("tileWidth")]
    public int TileWidth { get; set; } = 48;

    /// <summary>
    /// Tile height in pixels.
    /// </summary>
    [JsonPropertyName("tileHeight")]
    public int TileHeight { get; set; } = 48;

    /// <summary>
    /// Background music filename.
    /// </summary>
    [JsonPropertyName("bgm")]
    public string? Bgm { get; set; }

    /// <summary>
    /// Background sound filename.
    /// </summary>
    [JsonPropertyName("bgs")]
    public string? Bgs { get; set; }

    /// <summary>
    /// Whether the map scrolls horizontally.
    /// </summary>
    [JsonPropertyName("scrollX")]
    public bool ScrollX { get; set; }

    /// <summary>
    /// Whether the map scrolls vertically.
    /// </summary>
    [JsonPropertyName("scrollY")]
    public bool ScrollY { get; set; }

    /// <summary>
    /// Encounter step count (0 = no encounters).
    /// </summary>
    [JsonPropertyName("encounterStep")]
    public int EncounterStep { get; set; }

    /// <summary>
    /// List of possible enemy troop encounters.
    /// </summary>
    [JsonPropertyName("encounters")]
    public List<string> Encounters { get; set; } = new();

    /// <summary>
    /// All layers in this map.
    /// </summary>
    [JsonPropertyName("layers")]
    public List<MapLayer> Layers { get; set; } = new();

    /// <summary>
    /// All events in this map.
    /// </summary>
    [JsonPropertyName("events")]
    public List<MapEvent> Events { get; set; } = new();

    /// <summary>
    /// Parallax background settings.
    /// </summary>
    [JsonPropertyName("parallax")]
    public ParallaxSettings? Parallax { get; set; }

    /// <summary>
    /// Create a new map with default layers.
    /// </summary>
    public static MapData CreateDefault(int width = 20, int height = 15)
    {
        var map = new MapData
        {
            Width = width,
            Height = height
        };

        // Add default layers
        var groundLayer = new MapLayer
        {
            Name = "Ground",
            Type = LayerType.Tile,
            ZIndex = 0
        };
        groundLayer.Initialize(width, height);

        var decorationLayer = new MapLayer
        {
            Name = "Decoration",
            Type = LayerType.Tile,
            ZIndex = 1
        };
        decorationLayer.Initialize(width, height);

        var collisionLayer = new MapLayer
        {
            Name = "Collision",
            Type = LayerType.Collision,
            ZIndex = 2
        };
        collisionLayer.Initialize(width, height);

        map.Layers.Add(groundLayer);
        map.Layers.Add(decorationLayer);
        map.Layers.Add(collisionLayer);

        return map;
    }
}

/// <summary>
/// Parallax background settings.
/// </summary>
public class ParallaxSettings
{
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("scrollX")]
    public float ScrollX { get; set; }

    [JsonPropertyName("scrollY")]
    public float ScrollY { get; set; }

    [JsonPropertyName("loopX")]
    public bool LoopX { get; set; } = true;

    [JsonPropertyName("loopY")]
    public bool LoopY { get; set; }
}
