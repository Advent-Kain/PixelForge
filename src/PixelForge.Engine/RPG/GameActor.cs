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

    // Current state
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public int CurrentTp { get; set; }

    // Equipment
    public Dictionary<string, string?> EquippedItems { get; set; } = new()
    {
        { "weapon", null },
        { "shield", null },
        { "head", null },
        { "body", null },
        { "accessory1", null },
        { "accessory2", null }
    };

    // Learned skills
    public List<string> LearnedSkills { get; set; } = new();

    // Status effects
    public List<ActiveState> States { get; set; } = new();

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
        foreach (var itemId in EquippedItems.Values.Where(id => !string.IsNullOrEmpty(id)))
        {
            // TODO: Load equipment and apply stats
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

        return stats;
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
    /// Equip an item.
    /// </summary>
    public bool Equip(string slot, string? itemId)
    {
        if (!EquippedItems.ContainsKey(slot))
            return false;

        EquippedItems[slot] = itemId;
        return true;
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
    /// Check if actor can use a skill.
    /// </summary>
    public bool CanUseSkill(Skill skill)
    {
        if (!LearnedSkills.Contains(skill.Id))
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
