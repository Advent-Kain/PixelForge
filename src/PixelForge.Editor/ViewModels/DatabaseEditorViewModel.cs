using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Engine.Quest;
using PixelForge.Editor.Services;
using PixelForge.Shared.Models;
using PixelForge.Shared.Models.Database;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the database editor.
/// </summary>
public partial class DatabaseEditorViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const string ActorsFileName = "actors.json";
    private const string ClassesFileName = "classes.json";
    private const string SkillsFileName = "skills.json";
    private const string ItemsFileName = "items.json";
    private const string WeaponsFileName = "weapons.json";
    private const string ArmorsFileName = "armors.json";
    private const string EnemiesFileName = "enemies.json";
    private const string TroopsFileName = "troops.json";
    private const string StatesFileName = "states.json";
    private const string QuestsFileName = "quests.json";
    private const string TilesetsFileName = "tilesets.json";
    private const string AnimationsFileName = "animations.json";
    private const string SystemConfigFileName = "system.json";
    private const string CommonEventsFileName = "commonEvents.json";

    [ObservableProperty]
    private int _selectedTab;

    // Actors
    [ObservableProperty]
    private Actor? _selectedActor;

    public ObservableCollection<Actor> Actors { get; } = new();
    public ObservableCollection<CharacterClass> Classes { get; } = new();

    // Items
    [ObservableProperty]
    private Item? _selectedItem;

    public ObservableCollection<Item> Items { get; } = new();
    public ObservableCollection<Weapon> Weapons { get; } = new();
    public ObservableCollection<Armor> Armors { get; } = new();

    // Skills
    [ObservableProperty]
    private Skill? _selectedSkill;

    public ObservableCollection<Skill> Skills { get; } = new();

    // Enemies
    [ObservableProperty]
    private Enemy? _selectedEnemy;

    public ObservableCollection<Enemy> Enemies { get; } = new();
    public ObservableCollection<Troop> Troops { get; } = new();

    // States
    public ObservableCollection<State> States { get; } = new();

    // Quests
    [ObservableProperty]
    private Quest? _selectedQuest;

    [ObservableProperty]
    private QuestObjective? _selectedQuestObjective;

    [ObservableProperty]
    private QuestFlagRequirement? _selectedQuestFlag;

    [ObservableProperty]
    private string? _newPrerequisiteId;

    [ObservableProperty]
    private string? _selectedPrerequisite;

    public ObservableCollection<Quest> Quests { get; } = new();
    // Tilesets
    [ObservableProperty]
    private Tileset? _selectedTileset;

    public ObservableCollection<Tileset> Tilesets { get; } = new();

    // Animations
    [ObservableProperty]
    private Animation? _selectedAnimation;

    public ObservableCollection<Animation> Animations { get; } = new();

    // Common Events
    [ObservableProperty]
    private CommonEvent? _selectedCommonEvent;

    public ObservableCollection<CommonEvent> CommonEvents { get; } = new();

    // System Config
    [ObservableProperty]
    private SystemConfig _systemConfig = new();

    public DatabaseEditorViewModel()
    {
        if (!LoadFromDisk())
        {
            LoadSampleData();
        }
    }

    /// <summary>
    /// Load default/sample data.
    /// </summary>
    private void LoadSampleData()
    {
        // Add sample actor
        var actor = new Actor
        {
            Name = "Hero",
            InitialLevel = 1,
            MaxLevel = 99,
            BaseStats = new ActorStats
            {
                MaxHp = 100,
                MaxMp = 50,
                Attack = 15,
                Defense = 10,
                MagicAttack = 12,
                MagicDefense = 8,
                Agility = 14,
                Luck = 10
            }
        };
        Actors.Add(actor);

        // Add sample skill
        var skill = new Skill
        {
            Name = "Fireball",
            SkillType = SkillType.Magic,
            MpCost = 10,
            Scope = SkillScope.OneEnemy,
            Damage = new DamageFormula
            {
                Type = DamageType.HpDamage,
                Element = "Fire",
                Formula = "a.mat * 4 - b.mdf * 2"
            }
        };
        Skills.Add(skill);

        // Add sample enemy
        var enemy = new Enemy
        {
            Name = "Goblin",
            Stats = new ActorStats
            {
                MaxHp = 50,
                MaxMp = 10,
                Attack = 12,
                Defense = 8
            },
            Exp = 20,
            Gold = 15
        };
        Enemies.Add(enemy);

        // Add sample item
        var item = new Item
        {
            Name = "Potion",
            ItemType = ItemType.Regular,
            Price = 50,
            Consumable = true,
            Scope = SkillScope.OneAlly,
            Effects = new List<Effect>
            {
                new Effect
                {
                    Code = EffectCode.RecoverHp,
                    Value1 = 50
                }
            }
        };
        Items.Add(item);

        var quest = new Quest
        {
            Title = "Rat Extermination",
            Description = "Clear the cellar of rats.",
            Requirements = new QuestRequirements
            {
                MinLevel = 1
            },
            Rewards = new QuestRewards
            {
                Experience = 100,
                Gold = 50
            }
        };
        quest.Objectives.Add(new QuestObjective
        {
            Description = "Defeat 5 rats",
            Type = ObjectiveType.Kill,
            TargetId = "rat",
            TargetCount = 5
        });
        Quests.Add(quest);
        // Add sample tileset
        var tileset = new Tileset
        {
            Id = 1,
            Name = "Default Tileset",
            ImagePath = "Assets/Graphics/Tilesets/Default.png",
            TileWidth = 48,
            TileHeight = 48,
            Columns = 8,
            Rows = 8
        };
        Tilesets.Add(tileset);

        var animation = new Animation
        {
            Name = "Sparkle",
            ImagePath = "Graphics/Animations/Sparkle.png",
            FrameWidth = 192,
            FrameHeight = 192,
            FrameCount = 8,
            FrameDuration = 0.08f
        };
        Animations.Add(animation);

        var commonEvent = new CommonEvent
        {
            Name = "Show Greeting",
            Commands = new List<EventCommand>
            {
                new EventCommand
                {
                    Code = 101,
                    Parameters = new List<object> { "Hello from a common event!" }
                }
            }
        };
        CommonEvents.Add(commonEvent);

        SystemConfig = new SystemConfig
        {
            GameTitle = "PixelForge Game",
            StartingMapId = "Map001",
            StartingPosition = new Vector2Int(0, 0),
            StartingParty = Actors.Select(a => a.Id).ToList(),
            StartingGold = 100
        };
    }

    [RelayCommand]
    private void NewActor()
    {
        var actor = new Actor { Name = $"Actor{Actors.Count + 1:D3}" };
        Actors.Add(actor);
        SelectedActor = actor;
    }

    [RelayCommand]
    private void DeleteActor()
    {
        if (SelectedActor != null)
        {
            Actors.Remove(SelectedActor);
            SelectedActor = Actors.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewSkill()
    {
        var skill = new Skill { Name = $"Skill{Skills.Count + 1:D3}" };
        Skills.Add(skill);
        SelectedSkill = skill;
    }

    [RelayCommand]
    private void DeleteSkill()
    {
        if (SelectedSkill != null)
        {
            Skills.Remove(SelectedSkill);
            SelectedSkill = Skills.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewItem()
    {
        var item = new Item { Name = $"Item{Items.Count + 1:D3}" };
        Items.Add(item);
        SelectedItem = item;
    }

    [RelayCommand]
    private void DeleteItem()
    {
        if (SelectedItem != null)
        {
            Items.Remove(SelectedItem);
            SelectedItem = Items.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewEnemy()
    {
        var enemy = new Enemy { Name = $"Enemy{Enemies.Count + 1:D3}" };
        Enemies.Add(enemy);
        SelectedEnemy = enemy;
    }

    [RelayCommand]
    private void DeleteEnemy()
    {
        if (SelectedEnemy != null)
        {
            Enemies.Remove(SelectedEnemy);
            SelectedEnemy = Enemies.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewQuest()
    {
        var quest = new Quest
        {
            Title = $"Quest{Quests.Count + 1:D3}",
            Requirements = new QuestRequirements(),
            Rewards = new QuestRewards()
        };
        quest.Objectives.Add(new QuestObjective { Description = "New objective" });
        Quests.Add(quest);
        SelectedQuest = quest;
    }

    [RelayCommand]
    private void DeleteQuest()
    {
        if (SelectedQuest != null)
        {
            Quests.Remove(SelectedQuest);
            SelectedQuest = Quests.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewTileset()
    {
        var tileset = new Tileset
        {
            Id = Tilesets.Count > 0 ? Tilesets.Max(t => t.Id) + 1 : 1,
            Name = $"Tileset{Tilesets.Count + 1:D3}"
        };
        Tilesets.Add(tileset);
        SelectedTileset = tileset;
    }

    [RelayCommand]
    private void DeleteTileset()
    {
        if (SelectedTileset != null)
        {
            Tilesets.Remove(SelectedTileset);
            SelectedTileset = Tilesets.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewQuestObjective()
    {
        if (SelectedQuest == null)
            return;

        var objective = new QuestObjective { Description = $"Objective{SelectedQuest.Objectives.Count + 1:D3}" };
        SelectedQuest.Objectives.Add(objective);
        SelectedQuestObjective = objective;
    }

    [RelayCommand]
    private void DeleteQuestObjective()
    {
        if (SelectedQuest == null || SelectedQuestObjective == null)
            return;

        SelectedQuest.Objectives.Remove(SelectedQuestObjective);
        SelectedQuestObjective = SelectedQuest.Objectives.FirstOrDefault();
    }

    [RelayCommand]
    private void AddQuestPrerequisite()
    {
        if (SelectedQuest?.Requirements == null || string.IsNullOrWhiteSpace(NewPrerequisiteId))
            return;

        SelectedQuest.Requirements.PreviousQuests.Add(NewPrerequisiteId.Trim());
        SelectedPrerequisite = NewPrerequisiteId.Trim();
        NewPrerequisiteId = string.Empty;
    }

    [RelayCommand]
    private void RemoveQuestPrerequisite()
    {
        if (SelectedQuest?.Requirements == null || SelectedPrerequisite == null)
            return;

        SelectedQuest.Requirements.PreviousQuests.Remove(SelectedPrerequisite);
        SelectedPrerequisite = SelectedQuest.Requirements.PreviousQuests.FirstOrDefault();
    }

    [RelayCommand]
    private void NewQuestFlag()
    {
        if (SelectedQuest?.Requirements == null)
            return;

        var flag = new QuestFlagRequirement { SwitchId = 1, Value = true };
        SelectedQuest.Requirements.Flags.Add(flag);
        SelectedQuestFlag = flag;
    }

    [RelayCommand]
    private void DeleteQuestFlag()
    {
        if (SelectedQuest?.Requirements == null || SelectedQuestFlag == null)
            return;

        SelectedQuest.Requirements.Flags.Remove(SelectedQuestFlag);
        SelectedQuestFlag = SelectedQuest.Requirements.Flags.FirstOrDefault();
    }

    [RelayCommand]
    private void NewAnimation()
    {
        var animation = new Animation { Name = $"Animation{Animations.Count + 1:D3}" };
        Animations.Add(animation);
        SelectedAnimation = animation;
    }

    [RelayCommand]
    private void DeleteAnimation()
    {
        if (SelectedAnimation != null)
        {
            Animations.Remove(SelectedAnimation);
            SelectedAnimation = Animations.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void NewCommonEvent()
    {
        var commonEvent = new CommonEvent { Name = $"CommonEvent{CommonEvents.Count + 1:D3}" };
        CommonEvents.Add(commonEvent);
        SelectedCommonEvent = commonEvent;
    }

    [RelayCommand]
    private void DeleteCommonEvent()
    {
        if (SelectedCommonEvent != null)
        {
            CommonEvents.Remove(SelectedCommonEvent);
            SelectedCommonEvent = CommonEvents.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void Save()
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null)
            return;

        Directory.CreateDirectory(databaseDirectory);

        SaveCollection(ActorsFileName, Actors);
        SaveCollection(ClassesFileName, Classes);
        SaveCollection(SkillsFileName, Skills);
        SaveCollection(ItemsFileName, Items);
        SaveCollection(WeaponsFileName, Weapons);
        SaveCollection(ArmorsFileName, Armors);
        SaveCollection(EnemiesFileName, Enemies);
        SaveCollection(TroopsFileName, Troops);
        SaveCollection(StatesFileName, States);
        SaveCollection(QuestsFileName, Quests);
        SaveCollection(TilesetsFileName, Tilesets);
        SaveCollection(AnimationsFileName, Animations);
        SaveCollection(CommonEventsFileName, CommonEvents);
        SaveSingle(SystemConfigFileName, SystemConfig);
    }

    [RelayCommand]
    private void Load()
    {
        if (!LoadFromDisk())
        {
            LoadSampleData();
        }
    }

    private bool LoadFromDisk()
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null || !Directory.Exists(databaseDirectory))
            return false;

        var files = new[]
        {
            ActorsFileName,
            ClassesFileName,
            SkillsFileName,
            ItemsFileName,
            WeaponsFileName,
            ArmorsFileName,
            EnemiesFileName,
            TroopsFileName,
            StatesFileName,
            QuestsFileName,
            TilesetsFileName,
            AnimationsFileName,
            SystemConfigFileName,
            CommonEventsFileName
        };

        var hasAnyFile = files.Any(fileName => File.Exists(Path.Combine(databaseDirectory, fileName)));
        if (!hasAnyFile)
            return false;

        LoadCollection(ActorsFileName, Actors);
        LoadCollection(ClassesFileName, Classes);
        LoadCollection(SkillsFileName, Skills);
        LoadCollection(ItemsFileName, Items);
        LoadCollection(WeaponsFileName, Weapons);
        LoadCollection(ArmorsFileName, Armors);
        LoadCollection(EnemiesFileName, Enemies);
        LoadCollection(TroopsFileName, Troops);
        LoadCollection(StatesFileName, States);
        LoadCollection(QuestsFileName, Quests);
        LoadCollection(TilesetsFileName, Tilesets);
        LoadCollection(AnimationsFileName, Animations);
        LoadCollection(CommonEventsFileName, CommonEvents);
        SystemConfig = LoadSingle<SystemConfig>(SystemConfigFileName) ?? new SystemConfig();

        SelectedActor = Actors.FirstOrDefault();
        SelectedSkill = Skills.FirstOrDefault();
        SelectedItem = Items.FirstOrDefault();
        SelectedEnemy = Enemies.FirstOrDefault();
        SelectedQuest = Quests.FirstOrDefault();
        SelectedTileset = Tilesets.FirstOrDefault();
        SelectedAnimation = Animations.FirstOrDefault();
        SelectedCommonEvent = CommonEvents.FirstOrDefault();

        return true;
    }

    private void SaveCollection<T>(string fileName, IEnumerable<T> collection)
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null)
            return;

        var filePath = Path.Combine(databaseDirectory, fileName);
        var json = JsonSerializer.Serialize(collection, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private void LoadCollection<T>(string fileName, ObservableCollection<T> target)
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null)
            return;

        var filePath = Path.Combine(databaseDirectory, fileName);
        target.Clear();

        if (!File.Exists(filePath))
            return;

        var json = File.ReadAllText(filePath);
        var items = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    partial void OnSelectedQuestChanged(Quest? value)
    {
        if (value == null)
            return;

        value.Requirements ??= new QuestRequirements();
        value.Rewards ??= new QuestRewards();

        SelectedQuestObjective = value.Objectives.FirstOrDefault();
        SelectedQuestFlag = value.Requirements.Flags.FirstOrDefault();
        SelectedPrerequisite = value.Requirements.PreviousQuests.FirstOrDefault();
    }

    private void SaveSingle<T>(string fileName, T item)
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null)
            return;

        var filePath = Path.Combine(databaseDirectory, fileName);
        var json = JsonSerializer.Serialize(item, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private T? LoadSingle<T>(string fileName)
    {
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory == null)
            return default;

        var filePath = Path.Combine(databaseDirectory, fileName);
        if (!File.Exists(filePath))
            return default;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
