using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Status effect or state (buff/debuff).
/// </summary>
public class State
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New State";

    [JsonPropertyName("iconIndex")]
    public int IconIndex { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 50;

    [JsonPropertyName("restriction")]
    public StateRestriction Restriction { get; set; }

    [JsonPropertyName("removeAtBattleEnd")]
    public bool RemoveAtBattleEnd { get; set; }

    [JsonPropertyName("removeByDamage")]
    public bool RemoveByDamage { get; set; }

    [JsonPropertyName("chanceByDamage")]
    public int ChanceByDamage { get; set; } = 100;

    [JsonPropertyName("autoRemovalTiming")]
    public StateRemovalTiming AutoRemovalTiming { get; set; }

    [JsonPropertyName("minTurns")]
    public int MinTurns { get; set; } = 1;

    [JsonPropertyName("maxTurns")]
    public int MaxTurns { get; set; } = 1;

    [JsonPropertyName("message1")]
    public string? Message1 { get; set; } // Apply message

    [JsonPropertyName("message2")]
    public string? Message2 { get; set; } // Continue message

    [JsonPropertyName("message3")]
    public string? Message3 { get; set; } // Remove message

    [JsonPropertyName("traits")]
    public List<Trait> Traits { get; set; } = new();

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// State restriction level.
/// </summary>
public enum StateRestriction
{
    None = 0,
    CannotMove = 1,
    CannotAttack = 2,
    CannotUseSkills = 3,
    CannotAct = 4
}

/// <summary>
/// When state is automatically removed.
/// </summary>
public enum StateRemovalTiming
{
    None = 0,
    ActionEnd = 1,
    TurnEnd = 2
}
