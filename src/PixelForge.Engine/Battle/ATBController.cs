using Microsoft.Xna.Framework;
using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Active Time Battle (Final Fantasy style) controller.
/// </summary>
public class ATBController : IBattleController
{
    private readonly GameEngine _game;
    private readonly BattleState _state;
    private readonly RPG.GameDatabase _database;
    private readonly float _atbSpeed = 1.0f;
    private readonly List<Battler> _readyQueue = new();

    // Default attack formula
    private const string DefaultAttackFormula = "a.atk * 4 - b.def * 2";

    // Base agility for ATB calculation (characters with this agility fill gauge in ~3 seconds)
    private const float BaseAgility = 10f;

    public ATBController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
        _database = game.GetDatabase();
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
                _readyQueue.Add(battler);
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

            // Check hit rate
            if (!DamageFormulaEvaluator.CheckHit(95f, action.User.Stats.Agility, target.Stats.Agility))
                continue; // Miss

            int damage = CalculateDamage(action.User, target);

            // Check for critical hit
            if (DamageFormulaEvaluator.IsCriticalHit(4f, action.User.Stats.Luck, target.Stats.Luck))
            {
                damage = DamageFormulaEvaluator.ApplyCritical(damage);
            }

            target.ApplyDamage(damage);
        }
    }

    private void ExecuteSkill(BattleAction action)
    {
        if (action.Skill == null) return;
        action.User.CurrentMp -= action.Skill.MpCost;
        action.User.CurrentTp -= action.Skill.TpCost;

        foreach (var target in action.Targets)
        {
            // Allow targeting dead allies for revival skills
            if (!target.IsAlive && action.Skill.Scope != SkillScope.OneAllyDead && action.Skill.Scope != SkillScope.AllAlliesDead)
                continue;

            if (action.Skill.Damage.Type != DamageType.None)
            {
                int damage = CalculateSkillDamage(action.User, target, action.Skill);

                switch (action.Skill.Damage.Type)
                {
                    case DamageType.HpDamage:
                        target.ApplyDamage(damage);
                        break;
                    case DamageType.MpDamage:
                        target.CurrentMp = Math.Max(0, target.CurrentMp - damage);
                        break;
                    case DamageType.HpRecover:
                        target.ApplyHealing(damage);
                        break;
                    case DamageType.MpRecover:
                        target.CurrentMp = Math.Min(target.MaxMp, target.CurrentMp + damage);
                        break;
                    case DamageType.HpDrain:
                        int drained = target.ApplyDamage(damage);
                        action.User.ApplyHealing(drained);
                        break;
                    case DamageType.MpDrain:
                        int mpDrained = Math.Min(damage, target.CurrentMp);
                        target.CurrentMp -= mpDrained;
                        action.User.CurrentMp = Math.Min(action.User.MaxMp, action.User.CurrentMp + mpDrained);
                        break;
                }
            }

            // Apply skill effects
            ApplySkillEffects(target, action.Skill);
        }
    }

    private void ExecuteGuard(BattleAction action)
    {
        action.User.IsGuarding = true;
    }

    private void ExecuteItem(BattleAction action)
    {
        if (action.Item == null) return;

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive && action.Item.Scope != SkillScope.OneAllyDead && action.Item.Scope != SkillScope.AllAlliesDead)
                continue;

            foreach (var effect in action.Item.Effects)
            {
                switch (effect.Code)
                {
                    case EffectCode.RecoverHp:
                        int hpRecovery = (int)(target.MaxHp * effect.Value1 / 100) + (int)effect.Value2;
                        target.ApplyHealing(hpRecovery);
                        break;
                    case EffectCode.RecoverMp:
                        int mpRecovery = (int)(target.MaxMp * effect.Value1 / 100) + (int)effect.Value2;
                        target.CurrentMp = Math.Min(target.MaxMp, target.CurrentMp + mpRecovery);
                        break;
                    case EffectCode.AddState:
                        if (effect.DataId != null)
                        {
                            var state = _database.GetState(effect.DataId);
                            if (state != null && !target.States.Any(s => s.Id == state.Id))
                                target.States.Add(state);
                        }
                        break;
                    case EffectCode.RemoveState:
                        if (effect.DataId != null)
                            target.States.RemoveAll(s => s.Id == effect.DataId);
                        break;
                }
            }
        }
    }

    private int CalculateDamage(Battler attacker, Battler defender)
    {
        int baseDamage = DamageFormulaEvaluator.Evaluate(DefaultAttackFormula, attacker.Stats, defender.Stats);
        return Math.Max(1, DamageFormulaEvaluator.ApplyVariance(baseDamage, 20));
    }

    private int CalculateSkillDamage(Battler user, Battler target, Skill skill)
    {
        var result = DamageFormulaEvaluator.CalculateFullDamage(
            skill.Damage,
            user.Stats,
            target.Stats,
            skill.HitRate,
            skill.CriticalRate
        );

        return result.IsMiss ? 0 : result.Damage;
    }

    private void ApplySkillEffects(Battler target, Skill skill)
    {
        foreach (var effect in skill.Effects)
        {
            switch (effect.Code)
            {
                case EffectCode.AddState:
                    if (effect.DataId != null && new Random().NextDouble() * 100 < effect.Value1)
                    {
                        var state = _database.GetState(effect.DataId);
                        if (state != null && !target.States.Any(s => s.Id == state.Id))
                            target.States.Add(state);
                    }
                    break;
                case EffectCode.RemoveState:
                    if (effect.DataId != null)
                        target.States.RemoveAll(s => s.Id == effect.DataId);
                    break;
                case EffectCode.RecoverHp:
                    target.ApplyHealing((int)(target.MaxHp * effect.Value1 / 100) + (int)effect.Value2);
                    break;
                case EffectCode.RecoverMp:
                    target.CurrentMp = Math.Min(target.MaxMp, target.CurrentMp + (int)(target.MaxMp * effect.Value1 / 100) + (int)effect.Value2);
                    break;
            }
        }
    }

    private float GetAgilityMultiplier(Battler battler)
    {
        // Higher agility = faster ATB gauge fill
        // Base agility (10) fills in ~3 seconds at _atbSpeed = 1.0
        // ATB gauge goes from 0 to 1, so we need fill rate per second
        float agility = battler.Stats.Agility;
        return (agility / BaseAgility) * 0.33f; // 0.33 = fills in ~3 seconds at base agility
    }

    private IEnumerable<Battler> GetAllBattlers()
    {
        return _state.Party.Concat(_state.Enemies);
    }
}
