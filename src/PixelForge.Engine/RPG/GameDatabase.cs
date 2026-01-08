using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// In-memory game database for runtime lookups.
/// </summary>
public class GameDatabase
{
    public Dictionary<string, Actor> Actors { get; } = new();
    public Dictionary<string, Enemy> Enemies { get; } = new();
    public Dictionary<string, Troop> Troops { get; } = new();
    public Dictionary<string, Weapon> Weapons { get; } = new();
    public Dictionary<string, Armor> Armors { get; } = new();

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
}
