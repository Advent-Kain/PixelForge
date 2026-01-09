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
    private readonly RPG.GameDatabase _database;
    private Battler? _activeBattler;
    private ComboSequence? _currentCombo;
    private readonly int _maxApPerTurn = 7;
    private int _targetIndex;
    private KeyboardState _previousKeyboard;

    // Combo skill mappings by input type and level
    private Dictionary<string, Dictionary<ComboInput, List<string>>> _comboSkillMappings = new();

    // Default attack formula
    private const string DefaultAttackFormula = "a.atk * 4 - b.def * 2";

    // Keyboard state tracking for combo input
    private KeyboardState _previousKeyboardState;

    public ActionEconomyController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
        _database = game.GetDatabase();
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
            _targetIndex = 0;
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

        var keyboard = Keyboard.GetState();
        UpdateTargetSelection(keyboard);
        var input = GetComboInput(keyboard);

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

        if (IsPressed(keyboard, Keys.Enter) && _currentCombo.Skills.Count > 0)
        {
            _state.Phase = BattlePhase.Execution;
        }

        _previousKeyboard = keyboard;
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

        action.User.IsGuarding = false;

        // Cost
        action.User.CurrentMp -= action.Skill.MpCost;
        action.User.CurrentTp -= action.Skill.TpCost;

        foreach (var target in action.Targets)
        {
            // Allow targeting dead allies for revival
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

            // Apply effects
            ApplySkillEffects(target, action.Skill);
        }
    }

    /// <summary>
    /// Apply combo bonus damage based on combo level.
    /// </summary>
    private void ApplyComboBonus(BattleAction action, int comboLevel)
    {
        // Combo multiplier: 10% bonus per hit in combo
        // Uses attacker's stats for scaling
        float comboMultiplier = 0.1f * comboLevel;
        int baseBonus = (int)(action.User.Stats.Attack * comboMultiplier);

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive) continue;

            // Apply defense reduction to bonus
            int bonus = Math.Max(1, baseBonus - (target.Stats.Defense / 4));
            bonus = DamageFormulaEvaluator.ApplyVariance(bonus, 10);
            target.ApplyDamage(bonus);
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
    /// Execute a finisher attack (Deathblow).
    /// </summary>
    private void ExecuteFinisher(ComboSequence combo, List<Battler> targets)
    {
        // Finisher damage scales with:
        // - Base attack stat
        // - Combo level (number of hits)
        // - Target's remaining HP percentage (more damage to weakened enemies)
        var user = combo.User;

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            // Base finisher damage
            int baseDamage = (int)(user.Stats.Attack * 2.5f);

            // Combo scaling: +25% per combo hit
            float comboMultiplier = 1.0f + (combo.ComboLevel * 0.25f);
            baseDamage = (int)(baseDamage * comboMultiplier);

            // Execution bonus: more damage to low-HP targets (up to +50%)
            float hpPercent = (float)target.CurrentHp / target.MaxHp;
            float executionBonus = 1.0f + (0.5f * (1.0f - hpPercent));
            baseDamage = (int)(baseDamage * executionBonus);

            // Defense reduction
            int defense = target.Stats.Defense;
            int finalDamage = Math.Max(baseDamage / 2, baseDamage - defense);

            // Apply variance and critical chance
            finalDamage = DamageFormulaEvaluator.ApplyVariance(finalDamage, 15);

            // High critical rate for finishers
            if (DamageFormulaEvaluator.IsCriticalHit(30f, user.Stats.Luck, target.Stats.Luck))
            {
                finalDamage = DamageFormulaEvaluator.ApplyCritical(finalDamage, 2.5f);
            }

            target.ApplyDamage(finalDamage);
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
        action.User.IsGuarding = false;
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
        var currentState = Keyboard.GetState();

        // Check for new key presses (not held from previous frame)
        ComboInput? result = null;

        // Xenogears-style input mapping:
        // Triangle (Weak) = W or Up Arrow - Light attack, costs 1 AP
        // Square (Strong) = A or Left Arrow - Medium attack, costs 2 AP
        // X (Special) = S or Down Arrow - Heavy attack, costs 3 AP
        // Circle (Deathblow) = D or Right Arrow - Finisher, costs all remaining AP

        if (IsKeyPressed(currentState, Keys.W) || IsKeyPressed(currentState, Keys.Up))
        {
            result = ComboInput.Weak;
        }
        else if (IsKeyPressed(currentState, Keys.A) || IsKeyPressed(currentState, Keys.Left))
        {
            result = ComboInput.Strong;
        }
        else if (IsKeyPressed(currentState, Keys.S) || IsKeyPressed(currentState, Keys.Down))
        {
            result = ComboInput.Special;
        }
        else if (IsKeyPressed(currentState, Keys.D) || IsKeyPressed(currentState, Keys.Right))
        {
            result = ComboInput.Deathblow;
        }
        else if (IsKeyPressed(currentState, Keys.Enter) || IsKeyPressed(currentState, Keys.Space))
        {
            // Confirm current combo and execute
            if (_currentCombo != null && _currentCombo.Skills.Count > 0)
            {
                _state.Phase = BattlePhase.Execution;
            }
        }

        _previousKeyboardState = currentState;
        return result;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private Skill? GetSkillForInput(ComboInput input)
    {
        if (_activeBattler == null)
            return null;

        // Get base AP cost for this input type
        int apCost = input switch
        {
            ComboInput.Weak => 1,
            ComboInput.Strong => 2,
            ComboInput.Special => 3,
            ComboInput.Deathblow => _activeBattler.CurrentAp, // Uses all remaining AP
            _ => 1
        };

        // Check if we have enough AP
        if (_activeBattler.CurrentAp < apCost)
            return null;

        // Look for a skill that matches this input and current combo level
        var actorId = _activeBattler.ActorId ?? "";
        int comboLevel = _currentCombo?.ComboLevel ?? 0;

        // First, try to find a learned skill with matching combo properties
        foreach (var skillId in _activeBattler.LearnedSkills)
        {
            var skill = _database.GetSkill(skillId);
            if (skill?.ComboProperties != null)
            {
                if (skill.ComboProperties.ComboInput == input &&
                    skill.ComboProperties.RequiredCombo <= comboLevel &&
                    skill.ApCost <= _activeBattler.CurrentAp)
                {
                    // Check if this is a finisher and we meet requirements
                    if (skill.ComboProperties.Finisher && comboLevel < 3)
                        continue; // Need at least 3 hits for finisher

                    return skill;
                }
            }
        }

        // If no specific skill found, create a basic combo attack
        return CreateBasicComboSkill(input, apCost, comboLevel);
    }

    /// <summary>
    /// Create a basic combo skill for the given input type.
    /// </summary>
    private Skill CreateBasicComboSkill(ComboInput input, int apCost, int comboLevel)
    {
        // Base damage multiplier based on input type
        float multiplier = input switch
        {
            ComboInput.Weak => 1.0f,
            ComboInput.Strong => 1.5f,
            ComboInput.Special => 2.0f,
            ComboInput.Deathblow => 3.0f + (comboLevel * 0.5f), // Scales with combo
            _ => 1.0f
        };

        string name = input switch
        {
            ComboInput.Weak => "Light Attack",
            ComboInput.Strong => "Medium Attack",
            ComboInput.Special => "Heavy Attack",
            ComboInput.Deathblow => "Deathblow",
            _ => "Attack"
        };

        return new Skill
        {
            Id = $"combo_{input}_{comboLevel}",
            Name = name,
            SkillType = SkillType.Physical,
            Scope = SkillScope.OneEnemy,
            ApCost = apCost,
            MpCost = 0,
            TpCost = 0,
            HitRate = 95f,
            CriticalRate = input == ComboInput.Deathblow ? 25f : 5f,
            Variance = 10,
            Damage = new DamageFormula
            {
                Type = DamageType.HpDamage,
                Formula = $"(a.atk * {multiplier:F1} * 4) - (b.def * 2)",
                Critical = true,
                Variance = 10
            },
            ComboProperties = new ComboProperties
            {
                ComboInput = input,
                ComboLevel = comboLevel + 1,
                Chainable = input != ComboInput.Deathblow,
                Finisher = input == ComboInput.Deathblow,
                RequiredCombo = input == ComboInput.Deathblow ? 3 : 0
            }
        };
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
        var random = new Random();
        var targets = new List<Battler>();

        var allies = _activeBattler?.IsActor == true ? _state.Party : _state.Enemies;
        var enemies = _activeBattler?.IsActor == true ? _state.Enemies : _state.Party;

        switch (skill.Scope)
        {
            case SkillScope.OneEnemy:
                var enemy = enemies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (enemy != null) targets.Add(enemy);
                break;
            case SkillScope.AllEnemies:
                targets.AddRange(enemies.Where(b => b.IsAlive));
                break;
            case SkillScope.OneAlly:
                var ally = allies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (ally != null) targets.Add(ally);
                break;
            case SkillScope.AllAllies:
                targets.AddRange(allies.Where(b => b.IsAlive));
                break;
            case SkillScope.User:
                if (_activeBattler != null) targets.Add(_activeBattler);
                break;
            default:
                // Default to first alive enemy
                var defaultTarget = enemies.FirstOrDefault(b => b.IsAlive);
                if (defaultTarget != null) targets.Add(defaultTarget);
                break;
        }

        return targets;
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
                case EffectCode.GainTp:
                    target.CurrentTp += (int)effect.Value1;
                    break;
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

    private IEnumerable<Battler> GetAllBattlers()
    {
        return _state.Party.Concat(_state.Enemies);
    }

    private ComboInput? GetComboInput(KeyboardState keyboard)
    {
        if (IsPressed(keyboard, Keys.Z) || IsPressed(keyboard, Keys.D1))
            return ComboInput.Weak;
        if (IsPressed(keyboard, Keys.X) || IsPressed(keyboard, Keys.D2))
            return ComboInput.Strong;
        if (IsPressed(keyboard, Keys.C) || IsPressed(keyboard, Keys.D3))
            return ComboInput.Special;
        if (IsPressed(keyboard, Keys.V) || IsPressed(keyboard, Keys.D4))
            return ComboInput.Deathblow;

        return null;
    }

    private void UpdateTargetSelection(KeyboardState keyboard)
    {
        if (_activeBattler == null || _currentCombo == null)
            return;

        var scope = _currentCombo.Skills.LastOrDefault()?.Scope ?? SkillScope.OneEnemy;
        if (!BattleTargeting.RequiresTargetSelection(scope))
            return;

        var candidates = BattleTargeting.GetSelectableTargets(_state, _activeBattler, scope);
        if (candidates.Count == 0)
            return;

        if (IsPressed(keyboard, Keys.Left) || IsPressed(keyboard, Keys.Up))
        {
            _targetIndex = (_targetIndex - 1 + candidates.Count) % candidates.Count;
        }
        else if (IsPressed(keyboard, Keys.Right) || IsPressed(keyboard, Keys.Down))
        {
            _targetIndex = (_targetIndex + 1) % candidates.Count;
        }
    }

    private bool IsPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
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
