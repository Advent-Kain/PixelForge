using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Action Economy / Combo system (Xenogears-style) controller.
/// Characters build AP (Action Points) and execute combos.
/// </summary>
public class ActionEconomyController : IBattleController
{
    private readonly GameEngine _game;
    private readonly BattleState _state;
    private Battler? _activeBattler;
    private ComboSequence? _currentCombo;
    private readonly int _maxApPerTurn = 7;

    public ActionEconomyController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
    }

    public void Initialize()
    {
        _state.Phase = BattlePhase.Start;

        // Initialize AP
        foreach (var battler in GetAllBattlers())
        {
            battler.CurrentAp = _maxApPerTurn;
        }

        StartNewTurn();
    }

    public void Update(GameTime gameTime)
    {
        switch (_state.Phase)
        {
            case BattlePhase.Input:
                HandleComboInput();
                break;

            case BattlePhase.Execution:
                ExecuteCombo();
                break;

            case BattlePhase.TurnEnd:
                EndTurn();
                break;
        }
    }

    public void Cleanup()
    {
        _currentCombo = null;
    }

    /// <summary>
    /// Start a new turn.
    /// </summary>
    private void StartNewTurn()
    {
        // Refill AP for all battlers
        foreach (var battler in GetAllBattlers())
        {
            battler.CurrentAp = _maxApPerTurn;
        }

        // Get next actor to act
        _activeBattler = _state.Party.FirstOrDefault(b => b.CanAct);
        if (_activeBattler != null)
        {
            _currentCombo = new ComboSequence { User = _activeBattler };
            _state.Phase = BattlePhase.Input;
        }
        else
        {
            // All players acted, enemy turn
            ExecuteEnemyTurn();
        }
    }

    /// <summary>
    /// Handle combo input from player.
    /// </summary>
    private void HandleComboInput()
    {
        if (_activeBattler == null || _currentCombo == null)
            return;

        // TODO: Get input from battle UI
        // For now, simulate with keyboard
        var input = GetComboInput();

        if (input.HasValue)
        {
            var skill = GetSkillForInput(input.Value);
            if (skill != null && CanUseSkill(skill))
            {
                _currentCombo.Skills.Add(skill);
                _activeBattler.CurrentAp -= skill.ApCost;

                // Check if combo can continue
                if (_activeBattler.CurrentAp <= 0 || !skill.ComboProperties?.Chainable == true)
                {
                    // End combo and execute
                    _state.Phase = BattlePhase.Execution;
                }
            }
        }
    }

    /// <summary>
    /// Execute the combo sequence.
    /// </summary>
    private void ExecuteCombo()
    {
        if (_currentCombo == null || _currentCombo.Skills.Count == 0)
        {
            _state.Phase = BattlePhase.TurnEnd;
            return;
        }

        // Get target(s)
        var targets = SelectTargets(_currentCombo.Skills[0]);

        // Execute each skill in sequence
        foreach (var skill in _currentCombo.Skills)
        {
            var action = new BattleAction
            {
                User = _currentCombo.User,
                Type = ActionType.Skill,
                Skill = skill,
                Targets = targets
            };

            ExecuteAction(action);

            // Apply combo bonus
            if (_currentCombo.Skills.Count > 1)
            {
                ApplyComboBonus(action, _currentCombo.ComboLevel);
            }
        }

        // Check for finishers
        if (IsFinisherCombo(_currentCombo))
        {
            ExecuteFinisher(_currentCombo, targets);
        }

        _currentCombo = null;
        _state.Phase = BattlePhase.TurnEnd;
    }

    /// <summary>
    /// Execute a single action.
    /// </summary>
    private void ExecuteAction(BattleAction action)
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

            int damage = CalculateSkillDamage(action.User, target, action.Skill);

            if (damage > 0)
            {
                ApplyDamage(target, damage);
            }
            else if (damage < 0)
            {
                ApplyHealing(target, -damage);
            }

            // Apply effects
            ApplySkillEffects(target, action.Skill);
        }
    }

    /// <summary>
    /// Apply combo bonus damage.
    /// </summary>
    private void ApplyComboBonus(BattleAction action, int comboLevel)
    {
        // Combo multiplier: 1.1x per combo level
        float multiplier = 1.0f + (comboLevel * 0.1f);

        foreach (var target in action.Targets)
        {
            int bonus = (int)(20 * multiplier);
            ApplyDamage(target, bonus);
        }
    }

    /// <summary>
    /// Check if this is a finisher combo.
    /// </summary>
    private bool IsFinisherCombo(ComboSequence combo)
    {
        if (combo.Skills.Count < 3)
            return false;

        var lastSkill = combo.Skills.Last();
        return lastSkill.ComboProperties?.Finisher == true;
    }

    /// <summary>
    /// Execute a finisher attack.
    /// </summary>
    private void ExecuteFinisher(ComboSequence combo, List<Battler> targets)
    {
        // Finishers do massive damage
        foreach (var target in targets)
        {
            int finisherDamage = 100 + (combo.ComboLevel * 50);
            ApplyDamage(target, finisherDamage);
        }
    }

    /// <summary>
    /// Enemy turn execution.
    /// </summary>
    private void ExecuteEnemyTurn()
    {
        foreach (var enemy in _state.Enemies.Where(e => e.CanAct))
        {
            // Simple enemy AI: random attack
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

                ExecuteAttack(action);
            }
        }

        _state.Phase = BattlePhase.TurnEnd;
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

    /// <summary>
    /// End the turn.
    /// </summary>
    private void EndTurn()
    {
        _state.TurnCount++;
        StartNewTurn();
    }

    private ComboInput? GetComboInput()
    {
        // TODO: Get from battle UI
        return null;
    }

    private Skill? GetSkillForInput(ComboInput input)
    {
        // TODO: Look up skill based on input and current combo state
        return null;
    }

    private bool CanUseSkill(Skill skill)
    {
        if (_activeBattler == null)
            return false;

        return _activeBattler.CurrentAp >= skill.ApCost &&
               _activeBattler.CurrentMp >= skill.MpCost &&
               _activeBattler.CurrentTp >= skill.TpCost;
    }

    private List<Battler> SelectTargets(Skill skill)
    {
        // TODO: Smart target selection based on scope
        return _state.Enemies.Where(e => e.IsAlive).Take(1).ToList();
    }

    private void ApplySkillEffects(Battler target, Skill skill)
    {
        // TODO: Apply status effects, buffs, etc.
    }

    private int CalculateDamage(Battler attacker, Battler defender)
    {
        int baseDamage = 20;
        int variance = new Random().Next(-5, 6);
        return Math.Max(0, baseDamage + variance);
    }

    private int CalculateSkillDamage(Battler user, Battler target, Skill skill)
    {
        // TODO: Parse and evaluate formula
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

    private IEnumerable<Battler> GetAllBattlers()
    {
        return _state.Party.Concat(_state.Enemies);
    }
}

/// <summary>
/// Combo sequence being built.
/// </summary>
public class ComboSequence
{
    public required Battler User { get; set; }
    public List<Skill> Skills { get; set; } = new();
    public int ComboLevel => Skills.Count;
}
