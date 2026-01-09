using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Game settings stored in the project configuration.
/// </summary>
public class ProjectGameSettings
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "PixelForge Game";

    [JsonPropertyName("windowWidth")]
    public int WindowWidth { get; set; } = 1280;

    [JsonPropertyName("windowHeight")]
    public int WindowHeight { get; set; } = 720;

    [JsonPropertyName("battleMode")]
    public string BattleMode { get; set; } = "TurnBased";
}
