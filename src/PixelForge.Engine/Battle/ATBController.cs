using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
    private readonly float _atbSpeed = 1.0f;
    private readonly List<Battler> _readyQueue = new();
    private Battler? _inputBattler;
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
        _inputBattler = null;
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
        if (_inputBattler == null || !_readyQueue.Contains(_inputBattler))
        {
            _inputBattler = _readyQueue.FirstOrDefault(b => b.IsActor);
            ResetInputState();
        }

        if (_inputBattler != null)
        {
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
        if (_inputBattler == action.User)
        {
            _inputBattler = null;
        }
    }

    /// <summary>
    /// Execute a battle action.
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

    private void ExecuteGuard(BattleAction action)
    {
        action.User.IsGuarding = true;
    }
    private void ExecuteItem(BattleAction action)
    {
        BattleItemEffects.ApplyItem(_game, action);
    }

    private int CalculateDamage(Battler attacker, Battler defender)
    {
        return BattleFormulaEvaluator.CalculateAttackDamage(_game, attacker, defender);
    }

    private int CalculateSkillDamage(Battler user, Battler target, Shared.Models.Database.Skill skill)
    {
        return BattleFormulaEvaluator.CalculateSkillDamage(_game, user, target, skill);
    }

    private void ApplyDamage(Battler target, int damage)
    {
        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
    }

    private void ApplyHealing(Battler target, int healing)
    {
        int maxHp = BattleFormulaEvaluator.GetMaxHp(_game, target);
        target.CurrentHp = Math.Min(maxHp, target.CurrentHp + healing);
    }

    private float GetAgilityMultiplier(Battler battler)
    {
        return BattleFormulaEvaluator.GetAgilityMultiplier(_game, battler);
    }

    private IEnumerable<Battler> GetAllBattlers()
    {
        return _state.Party.Concat(_state.Enemies);
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
        if (_inputBattler == null)
            return;

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
                _availableSkills = BattleSelectionHelper.GetAvailableSkills(_game, _inputBattler);
                _skillIndex = 0;
                _inputState = BattleInputState.Skill;
                break;
            case 2:
                QueueImmediateAction(new BattleAction
                {
                    User = _inputBattler,
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
            User = _inputBattler!,
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
            User = _inputBattler!,
            Type = ActionType.Item,
            Item = option.Item,
            ItemId = option.Id
        };

        BeginActionSelection(action, option.Item.Scope);
    }

    private void HandleTargetInput(KeyboardState keyboard)
    {
        if (_pendingAction == null || _inputBattler == null)
            return;

        var candidates = BattleTargeting.GetSelectableTargets(_state, _inputBattler, _pendingScope);
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

        _pendingAction.Targets = BattleTargeting.SelectTargets(_state, _inputBattler, _pendingScope, _targetIndex);
        QueueImmediateAction(_pendingAction);
    }

    private void BeginActionSelection(ActionType actionType, SkillScope scope)
    {
        if (_inputBattler == null)
            return;

        var action = new BattleAction
        {
            User = _inputBattler,
            Type = actionType
        };

        BeginActionSelection(action, scope);
    }

    private void BeginActionSelection(BattleAction action, SkillScope scope)
    {
        if (_inputBattler == null)
            return;

        if (BattleTargeting.RequiresTargetSelection(scope))
        {
            _pendingAction = action;
            _pendingScope = scope;
            _targetIndex = 0;
            _inputState = BattleInputState.Target;
            return;
        }

        action.Targets = BattleTargeting.SelectTargets(_state, _inputBattler, scope, 0);
        QueueImmediateAction(action);
    }

    private void QueueImmediateAction(BattleAction action)
    {
        _state.ActionQueue.Enqueue(action);
        _state.Phase = BattlePhase.Execution;
    }

    private bool CanUseSkill(Skill skill)
    {
        if (_inputBattler == null)
            return false;

        return _inputBattler.CurrentMp >= skill.MpCost &&
               _inputBattler.CurrentTp >= skill.TpCost;
    }

    private bool IsPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}
