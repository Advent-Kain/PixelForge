using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Enemy battler.
/// </summary>
public class Enemy
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Enemy";

    [JsonPropertyName("battlerImage")]
    public string? BattlerImage { get; set; }

    [JsonPropertyName("stats")]
    public ActorStats Stats { get; set; } = new();

    [JsonPropertyName("exp")]
    public int Exp { get; set; } = 0;

    [JsonPropertyName("gold")]
    public int Gold { get; set; } = 0;

    [JsonPropertyName("dropItems")]
    public List<DropItem> DropItems { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<EnemyAction> Actions { get; set; } = new();

    [JsonPropertyName("traits")]
    public List<Trait> Traits { get; set; } = new();

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Item dropped by enemy.
/// </summary>
public class DropItem
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public DropItemKind Kind { get; set; }

    [JsonPropertyName("denominator")]
    public int Denominator { get; set; } = 1; // 1/denominator chance
}

/// <summary>
/// Enemy action/behavior.
/// </summary>
public class EnemyAction
{
    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = string.Empty;

    [JsonPropertyName("conditionType")]
    public ActionCondition ConditionType { get; set; }

    [JsonPropertyName("conditionParam1")]
    public float ConditionParam1 { get; set; }

    [JsonPropertyName("conditionParam2")]
    public float ConditionParam2 { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; } = 5; // AI priority
}

/// <summary>
/// Enemy troop formation.
/// </summary>
public class Troop
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Troop";

    [JsonPropertyName("members")]
    public List<TroopMember> Members { get; set; } = new();

    [JsonPropertyName("pages")]
    public List<TroopPage> Pages { get; set; } = new();

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Member of a troop.
/// </summary>
public class TroopMember
{
    [JsonPropertyName("enemyId")]
    public string EnemyId { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
}

/// <summary>
/// Event page during battle.
/// </summary>
public class TroopPage
{
    [JsonPropertyName("conditions")]
    public BattleConditions Conditions { get; set; } = new();

    [JsonPropertyName("span")]
    public BattleSpan Span { get; set; }

    [JsonPropertyName("commands")]
    public List<EventCommand> Commands { get; set; } = new();
}

/// <summary>
/// Battle event conditions.
/// </summary>
public class BattleConditions
{
    [JsonPropertyName("turnEnding")]
    public bool TurnEnding { get; set; }

    [JsonPropertyName("turnValid")]
    public bool TurnValid { get; set; }

    [JsonPropertyName("turnA")]
    public int TurnA { get; set; }

    [JsonPropertyName("turnB")]
    public int TurnB { get; set; }

    [JsonPropertyName("enemyValid")]
    public bool EnemyValid { get; set; }

    [JsonPropertyName("enemyIndex")]
    public int EnemyIndex { get; set; }

    [JsonPropertyName("enemyHp")]
    public int EnemyHp { get; set; } = 50; // Percentage

    [JsonPropertyName("actorValid")]
    public bool ActorValid { get; set; }

    [JsonPropertyName("actorId")]
    public string? ActorId { get; set; }

    [JsonPropertyName("actorHp")]
    public int ActorHp { get; set; } = 50; // Percentage

    [JsonPropertyName("switchValid")]
    public bool SwitchValid { get; set; }

    [JsonPropertyName("switchId")]
    public int SwitchId { get; set; }
}

/// <summary>
/// Drop item kinds.
/// </summary>
public enum DropItemKind
{
    None = 0,
    Item = 1,
    Weapon = 2,
    Armor = 3
}

/// <summary>
/// Enemy action conditions.
/// </summary>
public enum ActionCondition
{
    Always = 0,
    Turn = 1,
    Hp = 2,
    Mp = 3,
    State = 4,
    PartyLevel = 5,
    Switch = 6
}

/// <summary>
/// Battle event span.
/// </summary>
public enum BattleSpan
{
    Battle = 0,
    Turn = 1,
    Moment = 2
}
