using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Runtime instance of a playable character.
/// </summary>
public class GameActor
{
    // Base data
    public string ActorId { get; set; } = string.Empty;
    public Actor ActorData { get; set; } = null!;
    public CharacterClass? ClassData { get; set; }

    // Reference to game database for equipment lookups
    private GameDatabase? _database;

    // Current state
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public int CurrentTp { get; set; }

    // Equipment slots - weapon, head, body, legs, accessory1, accessory2
    public Dictionary<EquipSlot, string?> EquippedItems { get; set; } = new()
    {
        { EquipSlot.Weapon, null },
        { EquipSlot.Head, null },
        { EquipSlot.Body, null },
        { EquipSlot.Legs, null },
        { EquipSlot.Accessory1, null },
        { EquipSlot.Accessory2, null }
    };

    // Learned skills (from leveling/class)
    public List<string> LearnedSkills { get; set; } = new();

    // Status effects
    public List<ActiveState> States { get; set; } = new();

    /// <summary>
    /// Set the database reference for equipment lookups.
    /// </summary>
    public void SetDatabase(GameDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Get current stats with equipment and buffs applied.
    /// </summary>
    public ActorStats GetCurrentStats()
    {
        var stats = new ActorStats
        {
            MaxHp = CalculateStat(ActorData.BaseStats.MaxHp, ActorData.GrowthCurve.HpGrowth),
            MaxMp = CalculateStat(ActorData.BaseStats.MaxMp, ActorData.GrowthCurve.MpGrowth),
            Attack = CalculateStat(ActorData.BaseStats.Attack, ActorData.GrowthCurve.AttackGrowth),
            Defense = CalculateStat(ActorData.BaseStats.Defense, ActorData.GrowthCurve.DefenseGrowth),
            MagicAttack = CalculateStat(ActorData.BaseStats.MagicAttack, ActorData.GrowthCurve.MagicAttackGrowth),
            MagicDefense = CalculateStat(ActorData.BaseStats.MagicDefense, ActorData.GrowthCurve.MagicDefenseGrowth),
            Agility = CalculateStat(ActorData.BaseStats.Agility, ActorData.GrowthCurve.AgilityGrowth),
            Luck = CalculateStat(ActorData.BaseStats.Luck, ActorData.GrowthCurve.LuckGrowth)
        };

        // Apply equipment bonuses
        if (_database != null)
        {
            foreach (var equipment in GetEquippedEquipment())
            {
                // Apply stat bonuses
                stats.MaxHp += equipment.Stats.MaxHp;
                stats.MaxMp += equipment.Stats.MaxMp;
                stats.Attack += equipment.Stats.Attack;
                stats.Defense += equipment.Stats.Defense;
                stats.MagicAttack += equipment.Stats.MagicAttack;
                stats.MagicDefense += equipment.Stats.MagicDefense;
                stats.Agility += equipment.Stats.Agility;
                stats.Luck += equipment.Stats.Luck;

                // Apply trait modifiers (Parameter traits)
                foreach (var trait in equipment.Traits)
                {
                    if (trait.Code == TraitCode.Parameter)
                    {
                        ApplyParameterTrait(stats, trait);
                    }
                }
            }
        }

        // Apply class bonuses
        if (ClassData != null)
        {
            stats.MaxHp += ClassData.StatBonuses.MaxHp;
            stats.MaxMp += ClassData.StatBonuses.MaxMp;
            stats.Attack += ClassData.StatBonuses.Attack;
            stats.Defense += ClassData.StatBonuses.Defense;
            stats.MagicAttack += ClassData.StatBonuses.MagicAttack;
            stats.MagicDefense += ClassData.StatBonuses.MagicDefense;
            stats.Agility += ClassData.StatBonuses.Agility;
            stats.Luck += ClassData.StatBonuses.Luck;
        }

        // Apply state modifiers (buffs/debuffs)
        foreach (var activeState in States)
        {
            ApplyStateModifiers(stats, activeState.StateData);
        }

        return stats;
    }

    /// <summary>
    /// Apply state modifiers to stats.
    /// </summary>
    private void ApplyStateModifiers(ActorStats stats, State state)
    {
        foreach (var trait in state.Traits)
        {
            if (trait.Code == TraitCode.Parameter)
            {
                // Trait value is a multiplier (e.g., 1.5 = +50%, 0.5 = -50%)
                float multiplier = trait.Value;
                switch (trait.DataId)
                {
                    case "0": stats.MaxHp = (int)(stats.MaxHp * multiplier); break;
                    case "1": stats.MaxMp = (int)(stats.MaxMp * multiplier); break;
                    case "2": stats.Attack = (int)(stats.Attack * multiplier); break;
                    case "3": stats.Defense = (int)(stats.Defense * multiplier); break;
                    case "4": stats.MagicAttack = (int)(stats.MagicAttack * multiplier); break;
                    case "5": stats.MagicDefense = (int)(stats.MagicDefense * multiplier); break;
                    case "6": stats.Agility = (int)(stats.Agility * multiplier); break;
                    case "7": stats.Luck = (int)(stats.Luck * multiplier); break;
                }
            }
        }
    }

    /// <summary>
    /// Calculate stat value at current level.
    /// </summary>
    private int CalculateStat(int baseStat, float growth)
    {
        return (int)(baseStat * Math.Pow(growth, Level - 1));
    }

    /// <summary>
    /// Get experience needed for next level.
    /// </summary>
    public int GetExpForNextLevel()
    {
        if (ClassData?.ExpCurve != null && Level < ClassData.ExpCurve.Count)
        {
            return ClassData.ExpCurve[Level];
        }

        // Default curve
        return (int)(100 * Math.Pow(1.2, Level - 1));
    }

    /// <summary>
    /// Gain experience and check for level up.
    /// </summary>
    public bool GainExperience(int exp)
    {
        Experience += exp;
        bool leveledUp = false;

        while (Experience >= GetExpForNextLevel() && Level < ActorData.MaxLevel)
        {
            Experience -= GetExpForNextLevel();
            Level++;
            leveledUp = true;

            // Learn new skills
            if (ClassData != null)
            {
                var newSkills = ClassData.LearnedSkills
                    .Where(ls => ls.Level == Level && !LearnedSkills.Contains(ls.SkillId))
                    .Select(ls => ls.SkillId);
                LearnedSkills.AddRange(newSkills);
            }

            // Heal to full on level up
            var stats = GetCurrentStats();
            CurrentHp = stats.MaxHp;
            CurrentMp = stats.MaxMp;
        }

        return leveledUp;
    }

    /// <summary>
    /// Equip an item to a slot.
    /// </summary>
    public bool Equip(EquipSlot slot, string? itemId)
    {
        if (!EquippedItems.ContainsKey(slot))
            return false;

        // Validate equipment type matches slot
        if (itemId != null && _database != null)
        {
            var equipment = _database.GetEquipment(itemId);
            if (equipment != null && !IsValidSlotForEquipType(slot, equipment.EquipType))
                return false;
        }

        EquippedItems[slot] = itemId;
        return true;
    }

    /// <summary>
    /// Get the equipment ID in a specific slot.
    /// </summary>
    public string? GetEquippedId(EquipSlot slot)
    {
        return EquippedItems.TryGetValue(slot, out var id) ? id : null;
    }

    /// <summary>
    /// Unequip an item from a slot.
    /// </summary>
    public string? Unequip(EquipSlot slot)
    {
        if (!EquippedItems.TryGetValue(slot, out var itemId))
            return null;

        EquippedItems[slot] = null;
        return itemId;
    }

    /// <summary>
    /// Get all equipped equipment objects.
    /// </summary>
    public IEnumerable<Equipment> GetEquippedEquipment()
    {
        if (_database == null)
            yield break;

        foreach (var itemId in EquippedItems.Values)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                var equipment = _database.GetEquipment(itemId);
                if (equipment != null)
                    yield return equipment;
            }
        }
    }

    /// <summary>
    /// Get all skills granted by equipped items (via AddSkill trait).
    /// </summary>
    public IEnumerable<string> GetEquipmentSkills()
    {
        foreach (var equipment in GetEquippedEquipment())
        {
            foreach (var trait in equipment.Traits)
            {
                if (trait.Code == TraitCode.AddSkill && !string.IsNullOrEmpty(trait.DataId))
                {
                    yield return trait.DataId;
                }
            }
        }
    }

    /// <summary>
    /// Get all available skills (learned + equipment-granted).
    /// </summary>
    public IEnumerable<string> GetAllAvailableSkills()
    {
        return LearnedSkills.Concat(GetEquipmentSkills()).Distinct();
    }

    /// <summary>
    /// Check if equipment type is valid for a slot.
    /// </summary>
    private static bool IsValidSlotForEquipType(EquipSlot slot, EquipType equipType)
    {
        return slot switch
        {
            EquipSlot.Weapon => equipType == EquipType.Weapon,
            EquipSlot.Head => equipType == EquipType.Head,
            EquipSlot.Body => equipType == EquipType.Body,
            EquipSlot.Legs => equipType == EquipType.Legs,
            EquipSlot.Accessory1 or EquipSlot.Accessory2 => equipType == EquipType.Accessory,
            _ => false
        };
    }

    /// <summary>
    /// Apply a parameter trait modifier to stats.
    /// </summary>
    private static void ApplyParameterTrait(ActorStats stats, Trait trait)
    {
        // Trait value is a multiplier (e.g., 1.5 = +50%, 0.5 = -50%)
        float multiplier = trait.Value;
        switch (trait.DataId)
        {
            case "0": stats.MaxHp = (int)(stats.MaxHp * multiplier); break;
            case "1": stats.MaxMp = (int)(stats.MaxMp * multiplier); break;
            case "2": stats.Attack = (int)(stats.Attack * multiplier); break;
            case "3": stats.Defense = (int)(stats.Defense * multiplier); break;
            case "4": stats.MagicAttack = (int)(stats.MagicAttack * multiplier); break;
            case "5": stats.MagicDefense = (int)(stats.MagicDefense * multiplier); break;
            case "6": stats.Agility = (int)(stats.Agility * multiplier); break;
            case "7": stats.Luck = (int)(stats.Luck * multiplier); break;
        }
    }

    /// <summary>
    /// Check if actor can equip item.
    /// </summary>
    public bool CanEquip(Equipment equipment)
    {
        // Check class requirements
        if (equipment.Requirements?.ClassIds != null &&
            !equipment.Requirements.ClassIds.Contains(ActorData.ClassId ?? string.Empty))
        {
            return false;
        }

        // Check actor requirements
        if (equipment.Requirements?.ActorIds != null &&
            !equipment.Requirements.ActorIds.Contains(ActorId))
        {
            return false;
        }

        // Check level requirement
        if (equipment.Requirements?.Level != null && Level < equipment.Requirements.Level)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Heal HP.
    /// </summary>
    public void HealHp(int amount)
    {
        var stats = GetCurrentStats();
        CurrentHp = Math.Min(stats.MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// Heal MP.
    /// </summary>
    public void HealMp(int amount)
    {
        var stats = GetCurrentStats();
        CurrentMp = Math.Min(stats.MaxMp, CurrentMp + amount);
    }

    /// <summary>
    /// Take damage.
    /// </summary>
    public void TakeDamage(int damage)
    {
        CurrentHp = Math.Max(0, CurrentHp - damage);
    }

    /// <summary>
    /// Check if actor is alive.
    /// </summary>
    public bool IsAlive => CurrentHp > 0;

    /// <summary>
    /// Check if actor can use a skill (considers equipment-granted skills).
    /// </summary>
    public bool CanUseSkill(Skill skill)
    {
        // Check if skill is available (learned or from equipment)
        if (!GetAllAvailableSkills().Contains(skill.Id))
            return false;

        if (CurrentMp < skill.MpCost)
            return false;

        if (CurrentTp < skill.TpCost)
            return false;

        return true;
    }
}

/// <summary>
/// Active state on an actor.
/// </summary>
public class ActiveState
{
    public State StateData { get; set; } = null!;
    public int TurnsRemaining { get; set; }
}
