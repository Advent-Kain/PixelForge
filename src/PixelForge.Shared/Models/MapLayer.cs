using System;
using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Represents a layer in a map (tiles, collision, regions, etc.).
/// </summary>
public class MapLayer
{
    private bool _visible = true;

    public event EventHandler? VisibilityChanged;
    /// <summary>
    /// Unique identifier for this layer.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the layer.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Layer";

    /// <summary>
    /// Layer type (Tile, Collision, Region, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public LayerType Type { get; set; } = LayerType.Tile;

    /// <summary>
    /// Visibility flag for editor.
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;
            _visible = value;
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Opacity (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Z-order for rendering.
    /// </summary>
    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; }

    /// <summary>
    /// Width in tiles.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// Height in tiles.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Tile data stored as a 2D array [y][x].
    /// </summary>
    [JsonPropertyName("tiles")]
    public TileData[][] Tiles { get; set; } = Array.Empty<TileData[]>();

    /// <summary>
    /// Initialize the layer with the specified dimensions.
    /// </summary>
    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new TileData[height][];
        for (int y = 0; y < height; y++)
        {
            Tiles[y] = new TileData[width];
            for (int x = 0; x < width; x++)
            {
                Tiles[y][x] = TileData.Empty;
            }
        }
    }

    /// <summary>
    /// Get a tile at the specified position.
    /// </summary>
    public TileData? GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        return Tiles[y][x];
    }

    /// <summary>
    /// Set a tile at the specified position.
    /// </summary>
    public void SetTile(int x, int y, TileData tile)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;
        Tiles[y][x] = tile;
    }
}

/// <summary>
/// Types of map layers.
/// </summary>
public enum LayerType
{
    Tile,
    Collision,
    Region,
    Event
}
