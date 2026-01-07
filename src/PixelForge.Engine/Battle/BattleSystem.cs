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

    /// <summary>
    /// Start a battle.
    /// </summary>
    public void StartBattle(Troop troop, BattleMode mode = BattleMode.TurnBased)
    {
        _mode = mode;
        _state = new BattleState();

        // Initialize party
        foreach (var memberId in _game.GetGameState().PartyMembers)
        {
            var battler = new Battler
            {
                ActorId = memberId,
                IsActor = true,
                CurrentHp = 100, // TODO: Load from actor data
                CurrentMp = 50,
                CurrentTp = 0
            };
            _state.Party.Add(battler);
        }

        // Initialize enemies
        // TODO: Load from troop data
        _state.Enemies.Add(new Battler
        {
            EnemyId = "enemy1",
            IsActor = false,
            CurrentHp = 150,
            CurrentMp = 30
        });

        // Create appropriate controller
        _controller = mode switch
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

    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public int CurrentTp { get; set; }
    public int CurrentAp { get; set; } // Action Points for combo system

    public float ATBGauge { get; set; }
    public List<State> States { get; set; } = new();

    public bool IsAlive => CurrentHp > 0;
    public bool CanAct => IsAlive && !HasRestriction();

    private bool HasRestriction()
    {
        return States.Any(s => s.Restriction != StateRestriction.None);
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
