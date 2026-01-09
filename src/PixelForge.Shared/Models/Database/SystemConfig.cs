using System.Text.Json.Serialization;
using PixelForge.Shared.Models;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// System configuration and default game settings.
/// </summary>
public class SystemConfig
{
    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = "PixelForge Game";

    [JsonPropertyName("startingMapId")]
    public string StartingMapId { get; set; } = "Map001";

    [JsonPropertyName("startingPosition")]
    public Vector2Int StartingPosition { get; set; } = new(0, 0);

    [JsonPropertyName("startingParty")]
    public List<string> StartingParty { get; set; } = new();

    [JsonPropertyName("startingGold")]
    public int StartingGold { get; set; }

    [JsonPropertyName("windowWidth")]
    public int WindowWidth { get; set; } = 1280;

    [JsonPropertyName("windowHeight")]
    public int WindowHeight { get; set; } = 720;

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
