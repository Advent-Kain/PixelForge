using Microsoft.Xna.Framework;
using PixelForge.Shared.Models.Database;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Main battle system manager.
/// </summary>
public class BattleSystem
{
    private readonly GameEngine _game;
    private BattleMode _mode;
    private IBattleController? _controller;
    private BattleState? _state;

    public BattleSystem(GameEngine game)
    {
        _game = game;
        _mode = BattleMode.TurnBased;
    }

    public void SetDefaultMode(BattleMode mode)
    {
        _mode = mode;
    }

    /// <summary>
    /// Start a battle.
    /// </summary>
    public void StartBattle(Troop troop, BattleMode? mode = null)
    {
        var selectedMode = mode ?? _mode;
        _state = new BattleState();
        var gameState = _game.GetGameState();
        var partyManager = _game.GetPartyManager();
        var database = _game.GetDatabase();

        // Initialize party
        foreach (var memberId in gameState.PartyMembers)
        {
            var partyActor = partyManager.GetActor(memberId);
            var actorData = partyActor?.ActorData ?? database.GetActor(memberId);
            var actorStats = partyActor?.GetCurrentStats() ?? actorData?.BaseStats ?? new ActorStats();

            var battler = new Battler
            {
                ActorId = memberId,
                IsActor = true,
                Stats = actorStats,
                CurrentHp = partyActor != null ? Math.Clamp(partyActor.CurrentHp, 0, actorStats.MaxHp) : actorStats.MaxHp,
                CurrentMp = partyActor != null ? Math.Clamp(partyActor.CurrentMp, 0, actorStats.MaxMp) : actorStats.MaxMp,
                CurrentTp = partyActor?.CurrentTp ?? 0,
                // Include both learned skills AND equipment-granted skills
                LearnedSkills = partyActor?.GetAllAvailableSkills().ToList() ?? new List<string>()
            };
            _state.Party.Add(battler);
        }

        // Initialize enemies
        for (int i = 0; i < troop.Members.Count; i++)
        {
            var member = troop.Members[i];
            var enemyData = database.GetEnemy(member.EnemyId);
            var enemyStats = enemyData?.Stats ?? new ActorStats();

            _state.Enemies.Add(new Battler
            {
                EnemyId = member.EnemyId,
                IsActor = false,
                Stats = enemyStats,
                CurrentHp = enemyStats.MaxHp,
                CurrentMp = enemyStats.MaxMp,
                CurrentTp = 0,
                TroopMemberIndex = i,
                TroopX = member.X,
                TroopY = member.Y,
                IsHidden = member.Hidden
            });
        }

        // Create appropriate controller
        _controller = selectedMode switch
        {
            BattleMode.TurnBased => new TurnBasedController(_game, _state),
            BattleMode.ATB => new ATBController(_game, _state),
            BattleMode.ActionEconomy => new ActionEconomyController(_game, _state),
            _ => new TurnBasedController(_game, _state)
        };

        _controller.Initialize();
    }

    /// <summary>
    /// Update battle.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _controller?.Update(gameTime);

        // Check victory/defeat
        if (_state != null)
        {
            if (_state.Enemies.All(e => e.CurrentHp <= 0))
            {
                EndBattle(BattleResult.Victory);
            }
            else if (_state.Party.All(p => p.CurrentHp <= 0))
            {
                EndBattle(BattleResult.Defeat);
            }
        }
    }

    /// <summary>
    /// End the battle.
    /// </summary>
    private void EndBattle(BattleResult result)
    {
        _controller?.Cleanup();
        _controller = null;
        _state = null;

        // TODO: Process rewards, experience, etc.
    }

    public BattleState? CurrentBattle => _state;
    public bool IsActive => _state != null;
}

/// <summary>
/// Battle state data.
/// </summary>
public class BattleState
{
    public List<Battler> Party { get; set; } = new();
    public List<Battler> Enemies { get; set; } = new();
    public int TurnCount { get; set; }
    public Queue<BattleAction> ActionQueue { get; set; } = new();
    public BattlePhase Phase { get; set; } = BattlePhase.Input;
}

/// <summary>
/// Individual battler (actor or enemy).
/// </summary>
public class Battler
{
    public string? ActorId { get; set; }
    public string? EnemyId { get; set; }
    public bool IsActor { get; set; }
    public int? TroopMemberIndex { get; set; }
    public int TroopX { get; set; }
    public int TroopY { get; set; }
    public bool IsHidden { get; set; }

    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public int CurrentTp { get; set; }
    public int CurrentAp { get; set; } // Action Points for combo system

    // Stats for damage calculations
    public ActorStats Stats { get; set; } = new();
    public int MaxHp => Stats.MaxHp;
    public int MaxMp => Stats.MaxMp;

    // Guard state reduces incoming damage
    public bool IsGuarding { get; set; }
    public float GuardMultiplier { get; set; } = 0.5f; // 50% damage when guarding

    public float ATBGauge { get; set; }
    public List<State> States { get; set; } = new();

    // Learned skills for actors
    public List<string> LearnedSkills { get; set; } = new();

    public bool IsAlive => CurrentHp > 0;
    public bool CanAct => IsAlive && !HasRestriction();

    private bool HasRestriction()
    {
        return States.Any(s => s.Restriction != StateRestriction.None);
    }

    /// <summary>
    /// Apply damage to this battler, accounting for guard state.
    /// </summary>
    public int ApplyDamage(int rawDamage)
    {
        int actualDamage = IsGuarding ? (int)(rawDamage * GuardMultiplier) : rawDamage;
        CurrentHp = Math.Max(0, CurrentHp - actualDamage);
        return actualDamage;
    }

    /// <summary>
    /// Apply healing to this battler.
    /// </summary>
    public int ApplyHealing(int amount)
    {
        int actualHealing = Math.Min(amount, MaxHp - CurrentHp);
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        return actualHealing;
    }
}

/// <summary>
/// Battle action.
/// </summary>
public class BattleAction
{
    public required Battler User { get; set; }
    public Skill? Skill { get; set; }
    public Item? Item { get; set; }
    public string? ItemId { get; set; }
    public List<Battler> Targets { get; set; } = new();
    public ActionType Type { get; set; }
}

/// <summary>
/// Battle controller interface.
/// </summary>
public interface IBattleController
{
    void Initialize();
    void Update(GameTime gameTime);
    void Cleanup();
}

/// <summary>
/// Battle modes.
/// </summary>
public enum BattleMode
{
    TurnBased,
    ATB,
    ActionEconomy
}

/// <summary>
/// Battle phases.
/// </summary>
public enum BattlePhase
{
    Start,
    Input,
    Execution,
    TurnEnd,
    Victory,
    Defeat,
    Escape
}

/// <summary>
/// Action types.
/// </summary>
public enum ActionType
{
    Attack,
    Skill,
    Guard,
    Item,
    Escape
}

/// <summary>
/// Battle result.
/// </summary>
public enum BattleResult
{
    Victory,
    Defeat,
    Escape
}
