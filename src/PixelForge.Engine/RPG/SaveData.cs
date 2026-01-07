using System.Text.Json.Serialization;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Complete save data for a game.
/// </summary>
public class SaveData
{
    [JsonPropertyName("saveSlot")]
    public int SaveSlot { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("playTime")]
    public TimeSpan PlayTime { get; set; }

    [JsonPropertyName("mapId")]
    public string? CurrentMapId { get; set; }

    [JsonPropertyName("playerX")]
    public int PlayerX { get; set; }

    [JsonPropertyName("playerY")]
    public int PlayerY { get; set; }

    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    [JsonPropertyName("party")]
    public List<SavedActor> Party { get; set; } = new();

    [JsonPropertyName("inventory")]
    public Dictionary<string, int> Inventory { get; set; } = new();

    [JsonPropertyName("weapons")]
    public Dictionary<string, int> Weapons { get; set; } = new();

    [JsonPropertyName("armors")]
    public Dictionary<string, int> Armors { get; set; } = new();

    [JsonPropertyName("switches")]
    public Dictionary<int, bool> Switches { get; set; } = new();

    [JsonPropertyName("variables")]
    public Dictionary<int, int> Variables { get; set; } = new();

    [JsonPropertyName("selfSwitches")]
    public Dictionary<string, bool> SelfSwitches { get; set; } = new();

    /// <summary>
    /// Get display string for save slot.
    /// </summary>
    public string GetDisplayString()
    {
        if (Party.Count == 0)
            return "Empty";

        int hours = (int)PlayTime.TotalHours;
        int minutes = PlayTime.Minutes;

        var leader = Party[0];
        return $"Lv.{leader.Level} {leader.Name} - {hours:D2}:{minutes:D2}";
    }
}

/// <summary>
/// Saved actor data.
/// </summary>
public class SavedActor
{
    [JsonPropertyName("actorId")]
    public string ActorId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("experience")]
    public int Experience { get; set; }

    [JsonPropertyName("currentHp")]
    public int CurrentHp { get; set; }

    [JsonPropertyName("currentMp")]
    public int CurrentMp { get; set; }

    [JsonPropertyName("currentTp")]
    public int CurrentTp { get; set; }

    [JsonPropertyName("equipment")]
    public Dictionary<string, string?> Equipment { get; set; } = new();

    [JsonPropertyName("learnedSkills")]
    public List<string> LearnedSkills { get; set; } = new();

    [JsonPropertyName("states")]
    public List<SavedState> States { get; set; } = new();
}

/// <summary>
/// Saved state data.
/// </summary>
public class SavedState
{
    [JsonPropertyName("stateId")]
    public string StateId { get; set; } = string.Empty;

    [JsonPropertyName("turnsRemaining")]
    public int TurnsRemaining { get; set; }
}
