using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// In-memory game database for runtime lookups.
/// </summary>
public class GameDatabase
{
    public Dictionary<string, Skill> Skills { get; } = new();
    public Dictionary<string, Item> Items { get; } = new();
    public Dictionary<string, Weapon> Weapons { get; } = new();
    public Dictionary<string, Armor> Armors { get; } = new();
    public Dictionary<string, Actor> Actors { get; } = new();
    public Dictionary<string, Enemy> Enemies { get; } = new();
    public Dictionary<string, Troop> Troops { get; } = new();

    /// <summary>
    /// Get a skill definition by ID.
    /// </summary>
    public Skill? GetSkill(string skillId)
    {
        return Skills.TryGetValue(skillId, out var skill) ? skill : null;
    }

    /// <summary>
    /// Get an item definition by ID.
    /// </summary>
    public Item? GetItem(string itemId)
    {
        return Items.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>
    /// Get a weapon definition by ID.
    /// </summary>
    public Weapon? GetWeapon(string weaponId)
    {
        return Weapons.TryGetValue(weaponId, out var weapon) ? weapon : null;
    }

    /// <summary>
    /// Get an armor definition by ID.
    /// </summary>
    public Armor? GetArmor(string armorId)
    {
        return Armors.TryGetValue(armorId, out var armor) ? armor : null;
    }

    /// <summary>
    /// Get an equipment definition by ID.
    /// </summary>
    public Equipment? GetEquipment(string equipmentId)
    {
        if (Weapons.TryGetValue(equipmentId, out var weapon))
            return weapon;

        if (Armors.TryGetValue(equipmentId, out var armor))
            return armor;

        return null;
    }

    /// <summary>
    /// Get an actor definition by ID.
    /// </summary>
    public Actor? GetActor(string actorId)
    {
        return Actors.TryGetValue(actorId, out var actor) ? actor : null;
    }

    /// <summary>
    /// Get an enemy definition by ID.
    /// </summary>
    public Enemy? GetEnemy(string enemyId)
    {
        return Enemies.TryGetValue(enemyId, out var enemy) ? enemy : null;
    }

    /// <summary>
    /// Get a troop definition by ID.
    /// </summary>
    public Troop? GetTroop(string troopId)
    {
        return Troops.TryGetValue(troopId, out var troop) ? troop : null;
    }
}
