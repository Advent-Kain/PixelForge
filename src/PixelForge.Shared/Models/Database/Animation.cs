using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Animation definition for skills, items, and events.
/// </summary>
public class Animation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Animation";

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("frameWidth")]
    public int FrameWidth { get; set; } = 192;

    [JsonPropertyName("frameHeight")]
    public int FrameHeight { get; set; } = 192;

    [JsonPropertyName("frameCount")]
    public int FrameCount { get; set; } = 1;

    [JsonPropertyName("frameDuration")]
    public float FrameDuration { get; set; } = 0.1f;

    [JsonPropertyName("loop")]
    public bool Loop { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
