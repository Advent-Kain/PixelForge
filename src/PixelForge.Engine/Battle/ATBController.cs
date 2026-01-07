using Microsoft.Xna.Framework;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Active Time Battle (Final Fantasy style) controller.
/// </summary>
public class ATBController : IBattleController
{
    private readonly GameEngine _game;
    private readonly BattleState _state;
    private readonly float _atbSpeed = 1.0f;
    private readonly Queue<Battler> _readyQueue = new();

    public ATBController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
    }

    public void Initialize()
    {
        _state.Phase = BattlePhase.Start;

        // Initialize ATB gauges
        foreach (var battler in GetAllBattlers())
        {
            battler.ATBGauge = 0f;
        }

        _state.Phase = BattlePhase.Input;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state.Phase)
        {
            case BattlePhase.Input:
                UpdateATBGauges(deltaTime);
                CheckReadyBattlers();
                HandleInput();
                break;

            case BattlePhase.Execution:
                ExecuteActions();
                break;
        }
    }

    public void Cleanup()
    {
        _readyQueue.Clear();
    }

    /// <summary>
    /// Update ATB gauges for all battlers.
    /// </summary>
    private void UpdateATBGauges(float deltaTime)
    {
        foreach (var battler in GetAllBattlers())
        {
            if (!battler.CanAct)
                continue;

            if (battler.ATBGauge < 1.0f)
            {
                // Calculate fill speed based on agility
                float fillSpeed = _atbSpeed * GetAgilityMultiplier(battler);
                battler.ATBGauge += fillSpeed * deltaTime;

                if (battler.ATBGauge >= 1.0f)
                {
                    battler.ATBGauge = 1.0f;
                    OnBattlerReady(battler);
                }
            }
        }
    }

    /// <summary>
    /// Check for battlers who are ready to act.
    /// </summary>
    private void CheckReadyBattlers()
    {
        foreach (var battler in GetAllBattlers())
        {
            if (battler.ATBGauge >= 1.0f && !_readyQueue.Contains(battler))
            {
                _readyQueue.Enqueue(battler);
            }
        }
    }

    /// <summary>
    /// Called when a battler's ATB gauge is full.
    /// </summary>
    private void OnBattlerReady(Battler battler)
    {
        if (!battler.IsActor)
        {
            // Enemy decides action immediately
            DecideEnemyAction(battler);
        }
    }

    /// <summary>
    /// Handle player input for ready actors.
    /// </summary>
    private void HandleInput()
    {
        var readyActor = _readyQueue.FirstOrDefault(b => b.IsActor);
        if (readyActor != null)
        {
            // TODO: Show battle menu for this actor
            // For now, auto-create an attack action
            CreateAutoAction(readyActor);
        }

        if (_state.ActionQueue.Count > 0)
        {
            _state.Phase = BattlePhase.Execution;
        }
    }

    /// <summary>
    /// Execute queued actions.
    /// </summary>
    private void ExecuteActions()
    {
        if (_state.ActionQueue.Count == 0)
        {
            _state.Phase = BattlePhase.Input;
            return;
        }

        var action = _state.ActionQueue.Dequeue();
        ExecuteAction(action);

        // Reset ATB gauge
        action.User.ATBGauge = 0f;
        _readyQueue.Remove(action.User);
    }

    /// <summary>
    /// Execute a battle action.
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
    /// Enemy AI decision making.
    /// </summary>
    private void DecideEnemyAction(Battler enemy)
    {
        var random = new Random();
        var target = _state.Party.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();

        if (target != null)
        {
            var action = new BattleAction
            {
                User = enemy,
                Type = ActionType.Attack,
                Targets = new List<Battler> { target }
            };
            _state.ActionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Create auto-action for testing.
    /// </summary>
    private void CreateAutoAction(Battler battler)
    {
        var random = new Random();
        var target = _state.Enemies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();

        if (target != null)
        {
            var action = new BattleAction
            {
                User = battler,
                Type = ActionType.Attack,
                Targets = new List<Battler> { target }
            };
            _state.ActionQueue.Enqueue(action);
        }
    }

    private void ExecuteAttack(BattleAction action)
    {
        foreach (var target in action.Targets)
        {
            if (!target.IsAlive) continue;
            int damage = CalculateDamage(action.User, target);
            ApplyDamage(target, damage);
        }
    }

    private void ExecuteSkill(BattleAction action)
    {
        if (action.Skill == null) return;
        action.User.CurrentMp -= action.Skill.MpCost;
        action.User.CurrentTp -= action.Skill.TpCost;

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive) continue;
            int damage = CalculateSkillDamage(action.User, target, action.Skill);
            if (damage > 0)
                ApplyDamage(target, damage);
            else if (damage < 0)
                ApplyHealing(target, -damage);
        }
    }

    private void ExecuteGuard(BattleAction action) { }
    private void ExecuteItem(BattleAction action) { }

    private int CalculateDamage(Battler attacker, Battler defender)
    {
        int baseDamage = 20;
        int variance = new Random().Next(-5, 6);
        return Math.Max(0, baseDamage + variance);
    }

    private int CalculateSkillDamage(Battler user, Battler target, Shared.Models.Database.Skill skill)
    {
        return 30;
    }

    private void ApplyDamage(Battler target, int damage)
    {
        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
    }

    private void ApplyHealing(Battler target, int healing)
    {
        int maxHp = 100;
        target.CurrentHp = Math.Min(maxHp, target.CurrentHp + healing);
    }

    private float GetAgilityMultiplier(Battler battler)
    {
        // TODO: Get actual agility from stats
        return 1.0f;
    }

    private IEnumerable<Battler> GetAllBattlers()
    {
        return _state.Party.Concat(_state.Enemies);
    }
}
