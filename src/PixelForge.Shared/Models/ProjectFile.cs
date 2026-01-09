using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Defines the project configuration and content locations.
/// </summary>
public class ProjectFile
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Project";

    [JsonPropertyName("mapsPath")]
    public string MapsPath { get; set; } = "Maps";

    [JsonPropertyName("databasePath")]
    public string DatabasePath { get; set; } = "Database";

    [JsonPropertyName("assetsPath")]
    public string AssetsPath { get; set; } = "Assets";

    [JsonPropertyName("defaultMap")]
    public string DefaultMap { get; set; } = "Maps/Map001.json";

    [JsonPropertyName("runtimeProjectPath")]
    public string? RuntimeProjectPath { get; set; }

    [JsonPropertyName("gameSettings")]
    public ProjectGameSettings GameSettings { get; set; } = new();
}
