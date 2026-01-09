using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.UI.Battle;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Traditional turn-based battle controller.
/// </summary>
public class TurnBasedController : IBattleController
{
    private readonly GameEngine _game;
    private readonly BattleState _state;
    private readonly RPG.GameDatabase _database;
    private readonly BattleMenuManager _battleMenu;
    private List<Battler> _turnOrder = new();
    private int _currentBattlerIndex;
    private Battler? _currentBattler;
    private bool _waitingForInput;

    // Default attack formula when no weapon is equipped
    private const string DefaultAttackFormula = "a.atk * 4 - b.def * 2";

    public TurnBasedController(GameEngine game, BattleState state)
    {
        _game = game;
        _state = state;
        _database = game.GetDatabase();
        _battleMenu = game.GetBattleMenuManager();
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

        ResetInputState();

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

        // If we're already waiting for input, don't open menu again
        if (_waitingForInput)
            return;

        // Open battle menu for player input
        _waitingForInput = true;
        _battleMenu.Open(_currentBattler, _state, OnActionSelected);
    }

    /// <summary>
    /// Callback when player selects an action from the menu.
    /// </summary>
    private void OnActionSelected(BattleAction? action)
    {
        _waitingForInput = false;

        if (action == null)
        {
            // Player cancelled or tried to escape
            return;
        }

        // Handle escape action
        if (action.Type == ActionType.Escape)
        {
            // Simple escape check - 50% base chance
            if (Random.Shared.Next(100) < 50)
            {
                _state.Phase = BattlePhase.Escape;
            }
            else
            {
                // Escape failed, end turn
                _state.Phase = BattlePhase.TurnEnd;
            }
            return;
        }

        // Queue the action for execution
        _state.ActionQueue.Enqueue(action);
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
        action.User.IsGuarding = false;
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

            // Check hit rate (base 95% for physical attacks)
            if (!DamageFormulaEvaluator.CheckHit(95f, action.User.Stats.Agility, target.Stats.Agility))
                continue; // Miss

            // Calculate damage using formula
            int damage = CalculateDamage(action.User, target);

            // Check for critical hit (base 4% + luck difference)
            if (DamageFormulaEvaluator.IsCriticalHit(4f, action.User.Stats.Luck, target.Stats.Luck))
            {
                damage = DamageFormulaEvaluator.ApplyCritical(damage);
            }

            // Apply damage (uses Battler's method which accounts for guard)
            target.ApplyDamage(damage);
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
            // Allow targeting dead allies for revival skills
            if (!target.IsAlive && action.Skill.Scope != SkillScope.OneAllyDead && action.Skill.Scope != SkillScope.AllAlliesDead)
                continue;

            // Calculate and apply damage/healing based on damage type
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

            // Apply additional effects (states, buffs, etc.)
            ApplySkillEffects(target, action.Skill);
        }
    }

    /// <summary>
    /// Execute guard.
    /// </summary>
    private void ExecuteGuard(BattleAction action)
    {
        action.User.IsGuarding = true;
    }

    /// <summary>
    /// Execute item use.
    /// </summary>
    private void ExecuteItem(BattleAction action)
    {
        if (action.Item == null)
            return;

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive && action.Item.Scope != SkillScope.OneAllyDead && action.Item.Scope != SkillScope.AllAlliesDead)
                continue;

            ApplyItemEffects(target, action.Item);
        }
    }

    /// <summary>
    /// Apply item effects to a target.
    /// </summary>
    private void ApplyItemEffects(Battler target, Item item)
    {
        foreach (var effect in item.Effects)
        {
            switch (effect.Code)
            {
                case EffectCode.RecoverHp:
                    // Value1 = percentage, Value2 = flat amount
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
                        {
                            target.States.Add(state);
                        }
                    }
                    break;

                case EffectCode.RemoveState:
                    if (effect.DataId != null)
                    {
                        target.States.RemoveAll(s => s.Id == effect.DataId);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// End current turn.
    /// </summary>
    private void EndTurn()
    {
        // Clear guard state at end of turn
        if (_currentBattler != null)
        {
            _currentBattler.IsGuarding = false;
        }

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

        // Get enemy data for AI patterns
        var enemyData = _currentBattler.EnemyId != null ? _database.GetEnemy(_currentBattler.EnemyId) : null;

        // Check if enemy has skills to use
        if (enemyData?.Actions != null && enemyData.Actions.Count > 0)
        {
            var validActions = enemyData.Actions.Where(a => CanUseEnemyAction(a, _currentBattler)).ToList();
            if (validActions.Count > 0)
            {
                // Weight-based random selection
                int totalWeight = validActions.Sum(a => a.Rating);
                int roll = new Random().Next(totalWeight);
                int cumulative = 0;

                foreach (var enemyAction in validActions)
                {
                    cumulative += enemyAction.Rating;
                    if (roll < cumulative)
                    {
                        var skill = _database.GetSkill(enemyAction.SkillId);
                        if (skill != null)
                        {
                            var targets = SelectTargetsForSkill(skill, _currentBattler);
                            var action = new BattleAction
                            {
                                User = _currentBattler,
                                Type = ActionType.Skill,
                                Skill = skill,
                                Targets = targets
                            };
                            _state.ActionQueue.Enqueue(action);
                            _state.Phase = BattlePhase.Execution;
                            return;
                        }
                        break;
                    }
                }
            }
        }

        // Default: Random attack on random target
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
    /// Check if enemy can use an action.
    /// </summary>
    private bool CanUseEnemyAction(EnemyAction action, Battler enemy)
    {
        var skill = _database.GetSkill(action.SkillId);
        if (skill == null)
            return false;

        if (enemy.CurrentMp < skill.MpCost)
            return false;

        if (enemy.CurrentTp < skill.TpCost)
            return false;

        // Check conditions
        if (action.Condition != null)
        {
            switch (action.Condition.Type)
            {
                case ActionConditionType.TurnCount:
                    if (_state.TurnCount < action.Condition.TurnStart ||
                        (_state.TurnCount - action.Condition.TurnStart) % action.Condition.TurnEnd != 0)
                        return false;
                    break;

                case ActionConditionType.HpBelow:
                    float hpPercent = (float)enemy.CurrentHp / enemy.MaxHp * 100;
                    if (hpPercent > action.Condition.Value)
                        return false;
                    break;

                case ActionConditionType.MpBelow:
                    float mpPercent = (float)enemy.CurrentMp / enemy.MaxMp * 100;
                    if (mpPercent > action.Condition.Value)
                        return false;
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Select targets for a skill based on scope.
    /// </summary>
    private List<Battler> SelectTargetsForSkill(Skill skill, Battler user)
    {
        var random = new Random();
        var targets = new List<Battler>();

        var allies = user.IsActor ? _state.Party : _state.Enemies;
        var enemies = user.IsActor ? _state.Enemies : _state.Party;

        switch (skill.Scope)
        {
            case SkillScope.OneEnemy:
                var enemy = enemies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (enemy != null) targets.Add(enemy);
                break;

            case SkillScope.AllEnemies:
                targets.AddRange(enemies.Where(b => b.IsAlive));
                break;

            case SkillScope.RandomEnemies:
            case SkillScope.TwoRandomEnemies:
            case SkillScope.ThreeRandomEnemies:
            case SkillScope.FourRandomEnemies:
                int count = skill.Scope switch
                {
                    SkillScope.TwoRandomEnemies => 2,
                    SkillScope.ThreeRandomEnemies => 3,
                    SkillScope.FourRandomEnemies => 4,
                    _ => 1
                };
                var randomTargets = enemies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).Take(count);
                targets.AddRange(randomTargets);
                break;

            case SkillScope.OneAlly:
                var ally = allies.Where(b => b.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (ally != null) targets.Add(ally);
                break;

            case SkillScope.AllAllies:
                targets.AddRange(allies.Where(b => b.IsAlive));
                break;

            case SkillScope.OneAllyDead:
                var deadAlly = allies.Where(b => !b.IsAlive).FirstOrDefault();
                if (deadAlly != null) targets.Add(deadAlly);
                break;

            case SkillScope.AllAlliesDead:
                targets.AddRange(allies.Where(b => !b.IsAlive));
                break;

            case SkillScope.User:
                targets.Add(user);
                break;
        }

        return targets;
    }

    /// <summary>
    /// Calculate physical damage using formula evaluator.
    /// </summary>
    private int CalculateDamage(Battler attacker, Battler defender)
    {
        // Use default attack formula
        int baseDamage = DamageFormulaEvaluator.Evaluate(DefaultAttackFormula, attacker.Stats, defender.Stats);

        // Apply variance (20% default)
        int finalDamage = DamageFormulaEvaluator.ApplyVariance(baseDamage, 20);

        return Math.Max(1, finalDamage);
    }

    /// <summary>
    /// Calculate skill damage using the skill's damage formula.
    /// </summary>
    private int CalculateSkillDamage(Battler user, Battler target, Skill skill)
    {
        var damageResult = DamageFormulaEvaluator.CalculateFullDamage(
            skill.Damage,
            user.Stats,
            target.Stats,
            skill.HitRate,
            skill.CriticalRate
        );

        if (damageResult.IsMiss)
            return 0;

        return damageResult.Damage;
    }

    /// <summary>
    /// Apply skill effects (states, buffs, etc.)
    /// </summary>
    private void ApplySkillEffects(Battler target, Skill skill)
    {
        foreach (var effect in skill.Effects)
        {
            switch (effect.Code)
            {
                case EffectCode.AddState:
                    if (effect.DataId != null)
                    {
                        // Check chance (Value1 is percentage)
                        if (new Random().NextDouble() * 100 < effect.Value1)
                        {
                            var state = _database.GetState(effect.DataId);
                            if (state != null && !target.States.Any(s => s.Id == state.Id))
                            {
                                target.States.Add(state);
                            }
                        }
                    }
                    break;

                case EffectCode.RemoveState:
                    if (effect.DataId != null)
                    {
                        target.States.RemoveAll(s => s.Id == effect.DataId);
                    }
                    break;

                case EffectCode.RecoverHp:
                    int hpRecovery = (int)(target.MaxHp * effect.Value1 / 100) + (int)effect.Value2;
                    target.ApplyHealing(hpRecovery);
                    break;

                case EffectCode.RecoverMp:
                    int mpRecovery = (int)(target.MaxMp * effect.Value1 / 100) + (int)effect.Value2;
                    target.CurrentMp = Math.Min(target.MaxMp, target.CurrentMp + mpRecovery);
                    break;
            }
        }
    }

    private void ResetInputState()
    {
        _inputState = BattleInputState.Command;
        _commandIndex = 0;
        _skillIndex = 0;
        _itemIndex = 0;
        _targetIndex = 0;
        _pendingAction = null;
        _pendingScope = SkillScope.None;
        _availableSkills.Clear();
        _availableItems.Clear();
    }

    private void HandleCommandInput(KeyboardState keyboard)
    {
        if (IsPressed(keyboard, Keys.Down))
        {
            _commandIndex = (_commandIndex + 1) % 4;
        }
        else if (IsPressed(keyboard, Keys.Up))
        {
            _commandIndex = (_commandIndex - 1 + 4) % 4;
        }

        if (!IsPressed(keyboard, Keys.Enter))
            return;

        switch (_commandIndex)
        {
            case 0:
                BeginActionSelection(ActionType.Attack, SkillScope.OneEnemy);
                break;
            case 1:
                _availableSkills = BattleSelectionHelper.GetAvailableSkills(_game, _currentBattler!);
                _skillIndex = 0;
                _inputState = BattleInputState.Skill;
                break;
            case 2:
                QueueImmediateAction(new BattleAction
                {
                    User = _currentBattler!,
                    Type = ActionType.Guard
                });
                break;
            case 3:
                _availableItems = BattleSelectionHelper.GetAvailableItems(_game);
                _itemIndex = 0;
                _inputState = BattleInputState.Item;
                break;
        }
    }

    private void HandleSkillInput(KeyboardState keyboard)
    {
        if (_availableSkills.Count == 0)
        {
            if (IsPressed(keyboard, Keys.Escape))
            {
                _inputState = BattleInputState.Command;
            }
            return;
        }

        if (IsPressed(keyboard, Keys.Down))
        {
            _skillIndex = (_skillIndex + 1) % _availableSkills.Count;
        }
        else if (IsPressed(keyboard, Keys.Up))
        {
            _skillIndex = (_skillIndex - 1 + _availableSkills.Count) % _availableSkills.Count;
        }

        if (IsPressed(keyboard, Keys.Escape))
        {
            _inputState = BattleInputState.Command;
            return;
        }

        if (!IsPressed(keyboard, Keys.Enter))
            return;

        var skill = _availableSkills[_skillIndex];
        if (!CanUseSkill(skill))
            return;

        var action = new BattleAction
        {
            User = _currentBattler!,
            Type = ActionType.Skill,
            Skill = skill
        };

        BeginActionSelection(action, skill.Scope);
    }

    private void HandleItemInput(KeyboardState keyboard)
    {
        if (_availableItems.Count == 0)
        {
            if (IsPressed(keyboard, Keys.Escape))
            {
                _inputState = BattleInputState.Command;
            }
            return;
        }

        if (IsPressed(keyboard, Keys.Down))
        {
            _itemIndex = (_itemIndex + 1) % _availableItems.Count;
        }
        else if (IsPressed(keyboard, Keys.Up))
        {
            _itemIndex = (_itemIndex - 1 + _availableItems.Count) % _availableItems.Count;
        }

        if (IsPressed(keyboard, Keys.Escape))
        {
            _inputState = BattleInputState.Command;
            return;
        }

        if (!IsPressed(keyboard, Keys.Enter))
            return;

        var option = _availableItems[_itemIndex];
        var action = new BattleAction
        {
            User = _currentBattler!,
            Type = ActionType.Item,
            Item = option.Item,
            ItemId = option.Id
        };

        BeginActionSelection(action, option.Item.Scope);
    }

    private void HandleTargetInput(KeyboardState keyboard)
    {
        if (_pendingAction == null || _currentBattler == null)
            return;

        var candidates = BattleTargeting.GetSelectableTargets(_state, _currentBattler, _pendingScope);
        if (candidates.Count == 0)
        {
            _inputState = BattleInputState.Command;
            return;
        }

        if (IsPressed(keyboard, Keys.Left) || IsPressed(keyboard, Keys.Up))
        {
            _targetIndex = (_targetIndex - 1 + candidates.Count) % candidates.Count;
        }
        else if (IsPressed(keyboard, Keys.Right) || IsPressed(keyboard, Keys.Down))
        {
            _targetIndex = (_targetIndex + 1) % candidates.Count;
        }

        if (IsPressed(keyboard, Keys.Escape))
        {
            _inputState = BattleInputState.Command;
            return;
        }

        if (!IsPressed(keyboard, Keys.Enter))
            return;

        _pendingAction.Targets = BattleTargeting.SelectTargets(_state, _currentBattler, _pendingScope, _targetIndex);
        QueueImmediateAction(_pendingAction);
    }

    private void BeginActionSelection(ActionType actionType, SkillScope scope)
    {
        var action = new BattleAction
        {
            User = _currentBattler!,
            Type = actionType
        };

        BeginActionSelection(action, scope);
    }

    private void BeginActionSelection(BattleAction action, SkillScope scope)
    {
        if (_currentBattler == null)
            return;

        if (BattleTargeting.RequiresTargetSelection(scope))
        {
            _pendingAction = action;
            _pendingScope = scope;
            _targetIndex = 0;
            _inputState = BattleInputState.Target;
            return;
        }

        action.Targets = BattleTargeting.SelectTargets(_state, _currentBattler, scope, 0);
        QueueImmediateAction(action);
    }

    private void QueueImmediateAction(BattleAction action)
    {
        _state.ActionQueue.Enqueue(action);
        _state.Phase = BattlePhase.Execution;
    }

    private bool CanUseSkill(Skill skill)
    {
        if (_currentBattler == null)
            return false;

        return _currentBattler.CurrentMp >= skill.MpCost &&
               _currentBattler.CurrentTp >= skill.TpCost;
    }

    private bool IsPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}

public enum BattleInputState
{
    Command,
    Skill,
    Item,
    Target
}
