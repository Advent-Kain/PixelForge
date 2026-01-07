using Microsoft.Xna.Framework;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Traditional turn-based battle controller.
/// </summary>
public class TurnBasedController : IBattleController
{
    private readonly GameEngine _game;
    private readonly BattleState _state;
    private List<Battler> _turnOrder = new();
    private int _currentBattlerIndex;
    private Battler? _currentBattler;

    public TurnBasedController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
    }

    public void Initialize()
    {
        _state.Phase = BattlePhase.Start;
        CalculateTurnOrder();
        StartNewTurn();
    }

    public void Update(GameTime gameTime)
    {
        switch (_state.Phase)
        {
            case BattlePhase.Input:
                HandleInput();
                break;

            case BattlePhase.Execution:
                ExecuteActions();
                break;

            case BattlePhase.TurnEnd:
                EndTurn();
                break;
        }
    }

    public void Cleanup()
    {
        _turnOrder.Clear();
    }

    /// <summary>
    /// Calculate turn order based on agility.
    /// </summary>
    private void CalculateTurnOrder()
    {
        _turnOrder.Clear();
        _turnOrder.AddRange(_state.Party.Where(b => b.IsAlive));
        _turnOrder.AddRange(_state.Enemies.Where(b => b.IsAlive));

        // Sort by agility (for now, random)
        var random = new Random();
        _turnOrder = _turnOrder.OrderByDescending(_ => random.Next()).ToList();
    }

    /// <summary>
    /// Start a new turn.
    /// </summary>
    private void StartNewTurn()
    {
        if (_currentBattlerIndex >= _turnOrder.Count)
        {
            // All battlers have acted, start new round
            _state.TurnCount++;
            _currentBattlerIndex = 0;
            CalculateTurnOrder();
        }

        _currentBattler = _turnOrder[_currentBattlerIndex];

        if (!_currentBattler.CanAct)
        {
            // Skip this battler
            _currentBattlerIndex++;
            StartNewTurn();
            return;
        }

        _state.Phase = BattlePhase.Input;

        if (!_currentBattler.IsActor)
        {
            // Enemy AI decides action
            DecideEnemyAction();
        }
    }

    /// <summary>
    /// Handle player input.
    /// </summary>
    private void HandleInput()
    {
        if (_currentBattler == null || !_currentBattler.IsActor)
            return;

        // TODO: Show battle menu and handle input
        // For now, auto-advance
        _state.Phase = BattlePhase.Execution;
    }

    /// <summary>
    /// Execute queued actions.
    /// </summary>
    private void ExecuteActions()
    {
        if (_state.ActionQueue.Count == 0)
        {
            _state.Phase = BattlePhase.TurnEnd;
            return;
        }

        var action = _state.ActionQueue.Dequeue();
        ExecuteAction(action);
    }

    /// <summary>
    /// Execute a single action.
    /// </summary>
    private void ExecuteAction(BattleAction action)
    {
        switch (action.Type)
        {
            case ActionType.Attack:
                ExecuteAttack(action);
                break;

            case ActionType.Skill:
                ExecuteSkill(action);
                break;

            case ActionType.Guard:
                ExecuteGuard(action);
                break;

            case ActionType.Item:
                ExecuteItem(action);
                break;
        }
    }

    /// <summary>
    /// Execute physical attack.
    /// </summary>
    private void ExecuteAttack(BattleAction action)
    {
        foreach (var target in action.Targets)
        {
            if (!target.IsAlive)
                continue;

            // Simple damage calculation
            int damage = CalculateDamage(action.User, target);
            ApplyDamage(target, damage);
        }
    }

    /// <summary>
    /// Execute skill.
    /// </summary>
    private void ExecuteSkill(BattleAction action)
    {
        if (action.Skill == null)
            return;

        // Cost
        action.User.CurrentMp -= action.Skill.MpCost;
        action.User.CurrentTp -= action.Skill.TpCost;

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive)
                continue;

            // Apply skill effects
            int damage = CalculateSkillDamage(action.User, target, action.Skill);
            if (damage > 0)
            {
                ApplyDamage(target, damage);
            }
            else if (damage < 0)
            {
                ApplyHealing(target, -damage);
            }
        }
    }

    /// <summary>
    /// Execute guard.
    /// </summary>
    private void ExecuteGuard(BattleAction action)
    {
        // Add guard state or set flag
    }

    /// <summary>
    /// Execute item use.
    /// </summary>
    private void ExecuteItem(BattleAction action)
    {
        // TODO: Apply item effects
    }

    /// <summary>
    /// End current turn.
    /// </summary>
    private void EndTurn()
    {
        _currentBattlerIndex++;
        StartNewTurn();
    }

    /// <summary>
    /// Enemy AI decision making.
    /// </summary>
    private void DecideEnemyAction()
    {
        if (_currentBattler == null)
            return;

        // Simple AI: Random attack on random target
        var random = new Random();
        var target = _state.Party.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();

        if (target != null)
        {
            var action = new BattleAction
            {
                User = _currentBattler,
                Type = ActionType.Attack,
                Targets = new List<Battler> { target }
            };
            _state.ActionQueue.Enqueue(action);
        }

        _state.Phase = BattlePhase.Execution;
    }

    /// <summary>
    /// Calculate physical damage.
    /// </summary>
    private int CalculateDamage(Battler attacker, Battler defender)
    {
        // Simple formula: ATK * 2 - DEF
        int baseDamage = 20; // Placeholder
        int variance = new Random().Next(-5, 6);
        return Math.Max(0, baseDamage + variance);
    }

    /// <summary>
    /// Calculate skill damage.
    /// </summary>
    private int CalculateSkillDamage(Battler user, Battler target, Shared.Models.Database.Skill skill)
    {
        // TODO: Parse and evaluate formula
        return 30; // Placeholder
    }

    /// <summary>
    /// Apply damage to battler.
    /// </summary>
    private void ApplyDamage(Battler target, int damage)
    {
        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
    }

    /// <summary>
    /// Apply healing to battler.
    /// </summary>
    private void ApplyHealing(Battler target, int healing)
    {
        // TODO: Get max HP from stats
        int maxHp = 100;
        target.CurrentHp = Math.Min(maxHp, target.CurrentHp + healing);
    }
}
