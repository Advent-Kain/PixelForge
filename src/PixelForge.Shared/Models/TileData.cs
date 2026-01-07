using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Represents a single tile in a map layer.
/// </summary>
public record TileData
{
    /// <summary>
    /// The tileset ID this tile belongs to.
    /// </summary>
    [JsonPropertyName("tilesetId")]
    public int TilesetId { get; init; }

    /// <summary>
    /// The tile ID within the tileset (0-based).
    /// </summary>
    [JsonPropertyName("tileId")]
    public int TileId { get; init; }

    /// <summary>
    /// Horizontal flip flag.
    /// </summary>
    [JsonPropertyName("flipH")]
    public bool FlipH { get; init; }

    /// <summary>
    /// Vertical flip flag.
    /// </summary>
    [JsonPropertyName("flipV")]
    public bool FlipV { get; init; }

    /// <summary>
    /// Rotation in 90-degree increments (0, 1, 2, 3).
    /// </summary>
    [JsonPropertyName("rotation")]
    public int Rotation { get; init; }

    /// <summary>
    /// Empty tile (no tile rendered).
    /// </summary>
    public static readonly TileData Empty = new()
    {
        TilesetId = -1,
        TileId = -1
    };

    public bool IsEmpty => TilesetId < 0 || TileId < 0;
}
