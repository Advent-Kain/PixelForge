using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models.Database;

/// <summary>
/// Represents a playable character actor.
/// </summary>
public class Actor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Actor";

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("classId")]
    public string? ClassId { get; set; }

    [JsonPropertyName("initialLevel")]
    public int InitialLevel { get; set; } = 1;

    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; set; } = 99;

    [JsonPropertyName("characterImage")]
    public string? CharacterImage { get; set; }

    [JsonPropertyName("faceImage")]
    public string? FaceImage { get; set; }

    [JsonPropertyName("battlerImage")]
    public string? BattlerImage { get; set; }

    [JsonPropertyName("baseStats")]
    public ActorStats BaseStats { get; set; } = new();

    [JsonPropertyName("growthCurve")]
    public GrowthCurve GrowthCurve { get; set; } = new();

    [JsonPropertyName("equips")]
    public EquipSlots Equips { get; set; } = new();

    [JsonPropertyName("traits")]
    public List<Trait> Traits { get; set; } = new();

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Base stats for an actor.
/// </summary>
public class ActorStats
{
    [JsonPropertyName("maxHp")]
    public int MaxHp { get; set; } = 100;

    [JsonPropertyName("maxMp")]
    public int MaxMp { get; set; } = 50;

    [JsonPropertyName("attack")]
    public int Attack { get; set; } = 10;

    [JsonPropertyName("defense")]
    public int Defense { get; set; } = 10;

    [JsonPropertyName("magicAttack")]
    public int MagicAttack { get; set; } = 10;

    [JsonPropertyName("magicDefense")]
    public int MagicDefense { get; set; } = 10;

    [JsonPropertyName("agility")]
    public int Agility { get; set; } = 10;

    [JsonPropertyName("luck")]
    public int Luck { get; set; } = 10;
}

/// <summary>
/// Growth curve for stat progression.
/// </summary>
public class GrowthCurve
{
    [JsonPropertyName("hpGrowth")]
    public float HpGrowth { get; set; } = 1.2f;

    [JsonPropertyName("mpGrowth")]
    public float MpGrowth { get; set; } = 1.1f;

    [JsonPropertyName("attackGrowth")]
    public float AttackGrowth { get; set; } = 1.15f;

    [JsonPropertyName("defenseGrowth")]
    public float DefenseGrowth { get; set; } = 1.15f;

    [JsonPropertyName("magicAttackGrowth")]
    public float MagicAttackGrowth { get; set; } = 1.15f;

    [JsonPropertyName("magicDefenseGrowth")]
    public float MagicDefenseGrowth { get; set; } = 1.15f;

    [JsonPropertyName("agilityGrowth")]
    public float AgilityGrowth { get; set; } = 1.1f;

    [JsonPropertyName("luckGrowth")]
    public float LuckGrowth { get; set; } = 1.05f;
}

/// <summary>
/// Equipment slot configuration.
/// </summary>
public class EquipSlots
{
    [JsonPropertyName("weapon")]
    public bool Weapon { get; set; } = true;

    [JsonPropertyName("shield")]
    public bool Shield { get; set; } = true;

    [JsonPropertyName("head")]
    public bool Head { get; set; } = true;

    [JsonPropertyName("body")]
    public bool Body { get; set; } = true;

    [JsonPropertyName("accessory1")]
    public bool Accessory1 { get; set; } = true;

    [JsonPropertyName("accessory2")]
    public bool Accessory2 { get; set; }
}

/// <summary>
/// Character class definition.
/// </summary>
public class CharacterClass
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Class";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("expCurve")]
    public List<int> ExpCurve { get; set; } = new();

    [JsonPropertyName("learnedSkills")]
    public List<LearnedSkill> LearnedSkills { get; set; } = new();

    [JsonPropertyName("traits")]
    public List<Trait> Traits { get; set; } = new();

    [JsonPropertyName("statBonuses")]
    public ActorStats StatBonuses { get; set; } = new();
}

/// <summary>
/// Skill learned at a specific level.
/// </summary>
public class LearnedSkill
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = string.Empty;
}

/// <summary>
/// Trait that modifies character properties.
/// </summary>
public class Trait
{
    [JsonPropertyName("code")]
    public TraitCode Code { get; set; }

    [JsonPropertyName("dataId")]
    public string? DataId { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }
}

/// <summary>
/// Trait type codes.
/// </summary>
public enum TraitCode
{
    ElementRate = 11,       // Element damage rate
    DebuffRate = 12,        // Debuff resistance
    StateRate = 13,         // State resistance
    StateResist = 14,       // Immune to state
    Parameter = 21,         // Parameter bonus
    ExParameter = 22,       // Ex-parameter (hit, evasion, etc.)
    SpParameter = 23,       // Sp-parameter (target rate, etc.)
    AttackElement = 31,     // Attack element
    AttackState = 32,       // Attack inflicts state
    AttackSpeed = 33,       // Attack speed modifier
    AttackTimes = 34,       // Extra attacks
    AddSkillType = 41,      // Add skill type access
    SealSkillType = 42,     // Seal skill type
    AddSkill = 43,          // Add skill
    SealSkill = 44,         // Seal skill
    EquipWeapon = 51,       // Equip weapon type
    EquipArmor = 52,        // Equip armor type
    LockEquip = 53,         // Lock equipment slot
    SealEquip = 54,         // Seal equipment slot
    SlotType = 55,          // Dual wield
    ActionTimes = 61,       // Action times+ bonus
    SpecialFlag = 62,       // Special flag (auto-battle, etc.)
    CollapseEffect = 63,    // Collapse effect
    PartyAbility = 64       // Party ability
}
