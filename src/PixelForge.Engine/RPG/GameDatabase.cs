using System.Text.Json;
using System.IO;
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
    public Dictionary<string, CharacterClass> Classes { get; } = new();
    public Dictionary<string, Enemy> Enemies { get; } = new();
    public Dictionary<string, Troop> Troops { get; } = new();
    public Dictionary<string, Equipment> Equipment { get; } = new();
    public Dictionary<string, State> States { get; } = new();
    public Dictionary<string, Animation> Animations { get; } = new();
    public Dictionary<string, CommonEvent> CommonEvents { get; } = new();
    public Dictionary<int, Tileset> Tilesets { get; } = new();

    /// <summary>
    /// Get an actor definition by ID.
    /// </summary>
    public Actor? GetActor(string actorId)
    {
        return Actors.TryGetValue(actorId, out var actor) ? actor : null;
    }

    /// <summary>
    /// Get a class definition by ID.
    /// </summary>
    public CharacterClass? GetClass(string classId)
    {
        return Classes.TryGetValue(classId, out var classData) ? classData : null;
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
    /// Get a skill definition by ID.
    /// </summary>
    public Skill? GetSkill(string skillId)
    {
        return Skills.TryGetValue(skillId, out var skill) ? skill : null;
    }

    /// <summary>
    /// Get a consumable item by ID.
    /// </summary>
    public Item? GetItem(string itemId)
    {
        return Items.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>
    /// Get equipment by ID.
    /// </summary>
    public Equipment? GetEquipment(string equipmentId)
    {
        return Equipment.TryGetValue(equipmentId, out var equip) ? equip : null;
    }

    /// <summary>
    /// Get weapon by ID.
    /// </summary>
    public Weapon? GetWeapon(string weaponId)
    {
        return Weapons.TryGetValue(weaponId, out var weapon) ? weapon : null;
    }

    /// <summary>
    /// Get armor by ID.
    /// </summary>
    public Armor? GetArmor(string armorId)
    {
        return Armors.TryGetValue(armorId, out var armor) ? armor : null;
    }

    /// <summary>
    /// Get a state definition by ID.
    /// </summary>
    public State? GetState(string stateId)
    {
        return States.TryGetValue(stateId, out var state) ? state : null;
    }

    /// <summary>
    /// Get an animation definition by ID.
    /// </summary>
    public Animation? GetAnimation(string animationId)
    {
        return Animations.TryGetValue(animationId, out var animation) ? animation : null;
    }

    /// <summary>
    /// Get a common event definition by ID.
    /// </summary>
    public CommonEvent? GetCommonEvent(string commonEventId)
    {
        return CommonEvents.TryGetValue(commonEventId, out var commonEvent) ? commonEvent : null;
    }

    /// <summary>
    /// Get a tileset definition by ID.
    /// </summary>
    public Tileset? GetTileset(int tilesetId)
    {
        return Tilesets.TryGetValue(tilesetId, out var tileset) ? tileset : null;
    }
}
