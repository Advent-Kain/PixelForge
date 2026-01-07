using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Base class for all items.
/// </summary>
public abstract class ItemBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Item";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconIndex")]
    public int IconIndex { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Consumable or key item.
/// </summary>
public class Item : ItemBase
{
    [JsonPropertyName("itemType")]
    public ItemType ItemType { get; set; } = ItemType.Regular;

    [JsonPropertyName("consumable")]
    public bool Consumable { get; set; } = true;

    [JsonPropertyName("scope")]
    public SkillScope Scope { get; set; } = SkillScope.None;

    [JsonPropertyName("occasion")]
    public ItemOccasion Occasion { get; set; } = ItemOccasion.Always;

    [JsonPropertyName("effects")]
    public List<Effect> Effects { get; set; } = new();

    [JsonPropertyName("animationId")]
    public string? AnimationId { get; set; }
}

/// <summary>
/// Equipment item (weapon or armor).
/// </summary>
public class Equipment : ItemBase
{
    [JsonPropertyName("equipType")]
    public EquipType EquipType { get; set; }

    [JsonPropertyName("stats")]
    public ActorStats Stats { get; set; } = new();

    [JsonPropertyName("traits")]
    public List<Trait> Traits { get; set; } = new();

    [JsonPropertyName("equipRequirements")]
    public EquipRequirements? Requirements { get; set; }
}

/// <summary>
/// Weapon-specific data.
/// </summary>
public class Weapon : Equipment
{
    [JsonPropertyName("weaponType")]
    public WeaponType WeaponType { get; set; }

    [JsonPropertyName("animationId")]
    public string? AnimationId { get; set; }
}

/// <summary>
/// Armor-specific data.
/// </summary>
public class Armor : Equipment
{
    [JsonPropertyName("armorType")]
    public ArmorType ArmorType { get; set; }
}

/// <summary>
/// Equipment requirements.
/// </summary>
public class EquipRequirements
{
    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("classIds")]
    public List<string>? ClassIds { get; set; }

    [JsonPropertyName("actorIds")]
    public List<string>? ActorIds { get; set; }
}

/// <summary>
/// Effect applied by items or skills.
/// </summary>
public class Effect
{
    [JsonPropertyName("code")]
    public EffectCode Code { get; set; }

    [JsonPropertyName("dataId")]
    public string? DataId { get; set; }

    [JsonPropertyName("value1")]
    public float Value1 { get; set; }

    [JsonPropertyName("value2")]
    public float Value2 { get; set; }
}

/// <summary>
/// Item types.
/// </summary>
public enum ItemType
{
    Regular = 1,
    Key = 2,
    HiddenA = 3,
    HiddenB = 4
}

/// <summary>
/// Item usage occasion.
/// </summary>
public enum ItemOccasion
{
    Always = 0,
    BattleOnly = 1,
    MenuOnly = 2,
    Never = 3
}

/// <summary>
/// Equipment types.
/// </summary>
public enum EquipType
{
    Weapon,
    Shield,
    Head,
    Body,
    Accessory
}

/// <summary>
/// Weapon types.
/// </summary>
public enum WeaponType
{
    Dagger = 0,
    Sword = 1,
    Axe = 2,
    Spear = 3,
    Staff = 4,
    Bow = 5,
    Gun = 6,
    Claw = 7,
    Whip = 8,
    Wand = 9
}

/// <summary>
/// Armor types.
/// </summary>
public enum ArmorType
{
    Shield = 0,
    Helmet = 1,
    Armor = 2,
    Accessory = 3
}

/// <summary>
/// Effect codes.
/// </summary>
public enum EffectCode
{
    RecoverHp = 11,         // Recover HP
    RecoverMp = 12,         // Recover MP
    GainTp = 13,            // Gain TP
    AddState = 21,          // Add state
    RemoveState = 22,       // Remove state
    AddBuff = 31,           // Add buff
    AddDebuff = 32,         // Add debuff
    RemoveBuff = 33,        // Remove buff
    RemoveDebuff = 34,      // Remove debuff
    SpecialEffect = 41,     // Special effect (escape, etc.)
    Grow = 42,              // Permanent stat increase
    LearnSkill = 43,        // Learn skill
    CommonEvent = 44        // Trigger common event
}
