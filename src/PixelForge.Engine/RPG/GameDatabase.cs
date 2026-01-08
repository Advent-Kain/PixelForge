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
    public Dictionary<int, Tileset> Tilesets { get; } = new();
    public Dictionary<string, Weapon> Weapons { get; } = new();
    public Dictionary<string, Armor> Armors { get; } = new();
    public Dictionary<string, Animation> Animations { get; } = new();
    public Dictionary<string, CommonEvent> CommonEvents { get; } = new();
    public SystemConfig SystemConfig { get; private set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string ActorsFileName = "actors.json";
    private const string ClassesFileName = "classes.json";
    private const string SkillsFileName = "skills.json";
    private const string ItemsFileName = "items.json";
    private const string WeaponsFileName = "weapons.json";
    private const string ArmorsFileName = "armors.json";
    private const string EnemiesFileName = "enemies.json";
    private const string TroopsFileName = "troops.json";
    private const string StatesFileName = "states.json";
    private const string TilesetsFileName = "tilesets.json";
    private const string AnimationsFileName = "animations.json";
    private const string SystemConfigFileName = "system.json";
    private const string CommonEventsFileName = "commonEvents.json";

    public Dictionary<string, State> States { get; } = new();

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
    /// Get a tileset definition by ID.
    /// </summary>
    public Tileset? GetTileset(int tilesetId)
    {
        return Tilesets.TryGetValue(tilesetId, out var tileset) ? tileset : null;
    }

    /// <summary>
    /// Get a weapon definition by ID.
    /// </summary>
    public Tileset? GetTileset(int tilesetId)
    {
        return Tilesets.TryGetValue(tilesetId, out var tileset) ? tileset : null;
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
    /// Load database data from disk.
    /// </summary>
    public bool LoadFromDirectory(string databaseDirectory)
    {
        if (!Directory.Exists(databaseDirectory))
            return false;

        Skills.Clear();
        Items.Clear();
        Weapons.Clear();
        Armors.Clear();
        Actors.Clear();
        Classes.Clear();
        Enemies.Clear();
        Troops.Clear();
        States.Clear();
        Tilesets.Clear();
        Animations.Clear();
        CommonEvents.Clear();

        LoadDictionary(ActorsFileName, databaseDirectory, Actors, actor => actor.Id);
        LoadDictionary(ClassesFileName, databaseDirectory, Classes, classData => classData.Id);
        LoadDictionary(SkillsFileName, databaseDirectory, Skills, skill => skill.Id);
        LoadDictionary(ItemsFileName, databaseDirectory, Items, item => item.Id);
        LoadDictionary(WeaponsFileName, databaseDirectory, Weapons, weapon => weapon.Id);
        LoadDictionary(ArmorsFileName, databaseDirectory, Armors, armor => armor.Id);
        LoadDictionary(EnemiesFileName, databaseDirectory, Enemies, enemy => enemy.Id);
        LoadDictionary(TroopsFileName, databaseDirectory, Troops, troop => troop.Id);
        LoadDictionary(StatesFileName, databaseDirectory, States, state => state.Id);
        LoadDictionary(TilesetsFileName, databaseDirectory, Tilesets, tileset => tileset.Id);
        LoadDictionary(AnimationsFileName, databaseDirectory, Animations, animation => animation.Id);
        LoadDictionary(CommonEventsFileName, databaseDirectory, CommonEvents, commonEvent => commonEvent.Id);

        SystemConfig = LoadSingle(SystemConfigFileName, databaseDirectory) ?? new SystemConfig();
        return true;
    }

    private void LoadDictionary<T, TKey>(string fileName, string databaseDirectory, Dictionary<TKey, T> target, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var filePath = Path.Combine(databaseDirectory, fileName);
        if (!File.Exists(filePath))
            return;

        var json = File.ReadAllText(filePath);
        var items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        foreach (var item in items)
        {
            target[keySelector(item)] = item;
        }
    }

    private T? LoadSingle<T>(string fileName, string databaseDirectory)
    {
        var filePath = Path.Combine(databaseDirectory, fileName);
        if (!File.Exists(filePath))
            return default;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
