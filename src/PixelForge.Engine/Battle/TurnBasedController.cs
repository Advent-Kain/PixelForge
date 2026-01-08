using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

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
    private BattleInputState _inputState = BattleInputState.Command;
    private int _commandIndex;
    private int _skillIndex;
    private int _itemIndex;
    private int _targetIndex;
    private BattleAction? _pendingAction;
    private SkillScope _pendingScope = SkillScope.None;
    private List<Skill> _availableSkills = new();
    private List<BattleItemOption> _availableItems = new();
    private KeyboardState _previousKeyboard;

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

        var keyboard = Keyboard.GetState();

        switch (_inputState)
        {
            case BattleInputState.Command:
                HandleCommandInput(keyboard);
                break;
            case BattleInputState.Skill:
                HandleSkillInput(keyboard);
                break;
            case BattleInputState.Item:
                HandleItemInput(keyboard);
                break;
            case BattleInputState.Target:
                HandleTargetInput(keyboard);
                break;
        }

        _previousKeyboard = keyboard;
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
        action.User.IsGuarding = true;
    }

    /// <summary>
    /// Execute item use.
    /// </summary>
    private void ExecuteItem(BattleAction action)
    {
        BattleItemEffects.ApplyItem(_game, action);
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
        return BattleFormulaEvaluator.CalculateAttackDamage(_game, attacker, defender);
    }

    /// <summary>
    /// Calculate skill damage.
    /// </summary>
    private int CalculateSkillDamage(Battler user, Battler target, Shared.Models.Database.Skill skill)
    {
        return BattleFormulaEvaluator.CalculateSkillDamage(_game, user, target, skill);
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
        int maxHp = BattleFormulaEvaluator.GetMaxHp(_game, target);
        target.CurrentHp = Math.Min(maxHp, target.CurrentHp + healing);
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
