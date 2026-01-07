using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Skill or ability usable in battle or menu.
/// </summary>
public class Skill
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Skill";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconIndex")]
    public int IconIndex { get; set; }

    [JsonPropertyName("skillType")]
    public SkillType SkillType { get; set; } = SkillType.Magic;

    [JsonPropertyName("scope")]
    public SkillScope Scope { get; set; } = SkillScope.OneEnemy;

    [JsonPropertyName("occasion")]
    public ItemOccasion Occasion { get; set; } = ItemOccasion.BattleOnly;

    [JsonPropertyName("mpCost")]
    public int MpCost { get; set; }

    [JsonPropertyName("tpCost")]
    public int TpCost { get; set; }

    [JsonPropertyName("apCost")]
    public int ApCost { get; set; } // For action economy system

    [JsonPropertyName("damage")]
    public DamageFormula Damage { get; set; } = new();

    [JsonPropertyName("effects")]
    public List<Effect> Effects { get; set; } = new();

    [JsonPropertyName("animationId")]
    public string? AnimationId { get; set; }

    [JsonPropertyName("hitRate")]
    public float HitRate { get; set; } = 100f;

    [JsonPropertyName("criticalRate")]
    public float CriticalRate { get; set; } = 0f;

    [JsonPropertyName("variance")]
    public int Variance { get; set; } = 20;

    [JsonPropertyName("comboProperties")]
    public ComboProperties? ComboProperties { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Damage formula configuration.
/// </summary>
public class DamageFormula
{
    [JsonPropertyName("type")]
    public DamageType Type { get; set; } = DamageType.None;

    [JsonPropertyName("element")]
    public string? Element { get; set; }

    [JsonPropertyName("formula")]
    public string Formula { get; set; } = "a.atk * 4 - b.def * 2";

    [JsonPropertyName("critical")]
    public bool Critical { get; set; } = true;

    [JsonPropertyName("variance")]
    public int Variance { get; set; } = 20;
}

/// <summary>
/// Combo system properties (Xenogears-style).
/// </summary>
public class ComboProperties
{
    [JsonPropertyName("comboLevel")]
    public int ComboLevel { get; set; } = 1;

    [JsonPropertyName("comboInput")]
    public ComboInput ComboInput { get; set; }

    [JsonPropertyName("chainable")]
    public bool Chainable { get; set; } = true;

    [JsonPropertyName("finisher")]
    public bool Finisher { get; set; }

    [JsonPropertyName("requiredCombo")]
    public int RequiredCombo { get; set; }

    [JsonPropertyName("nextSkills")]
    public List<string> NextSkills { get; set; } = new();
}

/// <summary>
/// Skill types.
/// </summary>
public enum SkillType
{
    None = 0,
    Magic = 1,
    Special = 2,
    Physical = 3,
    Item = 4
}

/// <summary>
/// Target scope for skills and items.
/// </summary>
public enum SkillScope
{
    None = 0,
    OneEnemy = 1,
    AllEnemies = 2,
    RandomEnemies = 3,
    TwoRandomEnemies = 4,
    ThreeRandomEnemies = 5,
    FourRandomEnemies = 6,
    OneAlly = 7,
    AllAllies = 8,
    OneAllyDead = 9,
    AllAlliesDead = 10,
    User = 11
}

/// <summary>
/// Damage types.
/// </summary>
public enum DamageType
{
    None = 0,
    HpDamage = 1,
    MpDamage = 2,
    HpRecover = 3,
    MpRecover = 4,
    HpDrain = 5,
    MpDrain = 6
}

/// <summary>
/// Combo input types.
/// </summary>
public enum ComboInput
{
    Weak = 0,      // Triangle
    Strong = 1,    // Square
    Special = 2,   // X
    Deathblow = 3  // Circle (finisher)
}
