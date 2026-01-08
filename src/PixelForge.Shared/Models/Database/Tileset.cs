using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Tileset definition for map rendering.
/// </summary>
public class Tileset
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Tileset";

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("tileWidth")]
    public int TileWidth { get; set; } = 48;

    [JsonPropertyName("tileHeight")]
    public int TileHeight { get; set; } = 48;

    [JsonPropertyName("spacing")]
    public int Spacing { get; set; }

    [JsonPropertyName("margin")]
    public int Margin { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
