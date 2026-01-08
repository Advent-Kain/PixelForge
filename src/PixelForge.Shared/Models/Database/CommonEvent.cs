using System.Text.Json.Serialization;
using PixelForge.Shared.Models;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Shared event definition for reuse across maps, skills, and items.
/// </summary>
public class CommonEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Common Event";

    [JsonPropertyName("trigger")]
    public CommonEventTrigger Trigger { get; set; } = CommonEventTrigger.None;

    [JsonPropertyName("switchId")]
    public int? SwitchId { get; set; }

    [JsonPropertyName("commands")]
    public List<EventCommand> Commands { get; set; } = new();

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Trigger type for common events.
/// </summary>
public enum CommonEventTrigger
{
    None = 0,
    Autorun = 1,
    Parallel = 2
}
