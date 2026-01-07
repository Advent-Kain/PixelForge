using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Shared.Models.Database;
using System.Collections.ObjectModel;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the database editor.
/// </summary>
public partial class DatabaseEditorViewModel : ViewModelBase
{
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

    public DatabaseEditorViewModel()
    {
        LoadDefaultData();
    }

    /// <summary>
    /// Load default/sample data.
    /// </summary>
    private void LoadDefaultData()
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
    private void Save()
    {
        // TODO: Serialize to JSON files
    }

    [RelayCommand]
    private void Load()
    {
        // TODO: Load from JSON files
    }
}
