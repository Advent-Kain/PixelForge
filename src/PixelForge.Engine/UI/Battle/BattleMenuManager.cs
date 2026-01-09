using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Battle;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.UI.Battle;

/// <summary>
/// Manages the battle menu system for player action selection.
/// </summary>
public class BattleMenuManager
{
    private readonly GameEngine _game;
    private readonly GameDatabase _database;

    private BattleMenuState _currentState = BattleMenuState.Hidden;
    private Battler? _currentBattler;
    private BattleState? _battleState;

    // Menu selection indices
    private int _commandIndex;
    private int _skillIndex;
    private int _itemIndex;
    private int _targetIndex;
    private int _scrollOffset;

    // Available options
    private List<Skill> _availableSkills = new();
    private List<InventoryItem> _availableItems = new();
    private List<Battler> _availableTargets = new();

    // Selected action data
    private ActionType _selectedActionType;
    private Skill? _selectedSkill;
    private Item? _selectedItem;

    // Input tracking
    private KeyboardState _previousKeyboard;

    // Constants
    private const int MaxVisibleItems = 6;
    private const int CommandWindowWidth = 160;
    private const int CommandWindowHeight = 160;
    private const int SubMenuWidth = 280;
    private const int SubMenuHeight = 200;
    private const int TargetWindowWidth = 200;

    // Commands available in battle
    private readonly string[] _commands = { "Attack", "Skills", "Items", "Guard", "Escape" };

    // Result callback
    private Action<BattleAction?>? _onActionSelected;

    public BattleMenuManager(GameEngine game)
    {
        _game = game;
        _database = game.GetDatabase();
    }

    public bool IsActive => _currentState != BattleMenuState.Hidden;

    /// <summary>
    /// Open the battle menu for a battler.
    /// </summary>
    public void Open(Battler battler, BattleState battleState, Action<BattleAction?> onActionSelected)
    {
        _currentBattler = battler;
        _battleState = battleState;
        _onActionSelected = onActionSelected;

        _currentState = BattleMenuState.Command;
        _commandIndex = 0;
        _skillIndex = 0;
        _itemIndex = 0;
        _targetIndex = 0;
        _scrollOffset = 0;

        // Load available skills for this battler
        LoadAvailableSkills();

        // Load available items from inventory
        LoadAvailableItems();
    }

    /// <summary>
    /// Close the battle menu.
    /// </summary>
    public void Close()
    {
        _currentState = BattleMenuState.Hidden;
        _currentBattler = null;
        _battleState = null;
        _onActionSelected = null;
    }

    /// <summary>
    /// Update the battle menu.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_currentState == BattleMenuState.Hidden)
            return;

        var keyboard = Keyboard.GetState();

        switch (_currentState)
        {
            case BattleMenuState.Command:
                UpdateCommandMenu(keyboard);
                break;
            case BattleMenuState.Skills:
                UpdateSkillMenu(keyboard);
                break;
            case BattleMenuState.Items:
                UpdateItemMenu(keyboard);
                break;
            case BattleMenuState.Target:
                UpdateTargetMenu(keyboard);
                break;
        }

        _previousKeyboard = keyboard;
    }

    /// <summary>
    /// Draw the battle menu.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        if (_currentState == BattleMenuState.Hidden)
            return;

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Always draw the command window
        DrawCommandWindow(spriteBatch, font, pixelTexture, viewport);

        // Draw sub-menus based on state
        switch (_currentState)
        {
            case BattleMenuState.Skills:
                DrawSkillWindow(spriteBatch, font, pixelTexture, viewport);
                break;
            case BattleMenuState.Items:
                DrawItemWindow(spriteBatch, font, pixelTexture, viewport);
                break;
            case BattleMenuState.Target:
                DrawTargetWindow(spriteBatch, font, pixelTexture, viewport);
                break;
        }

        // Draw battler status
        DrawBattlerStatus(spriteBatch, font, pixelTexture, viewport);
    }

    #region Update Methods

    private void UpdateCommandMenu(KeyboardState keyboard)
    {
        // Navigation
        if (IsKeyPressed(keyboard, Keys.Down))
            _commandIndex = (_commandIndex + 1) % _commands.Length;
        else if (IsKeyPressed(keyboard, Keys.Up))
            _commandIndex = (_commandIndex - 1 + _commands.Length) % _commands.Length;

        // Selection
        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Z))
        {
            SelectCommand();
        }

        // Cancel - try to escape
        if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
        {
            // Cancel returns null action
            _onActionSelected?.Invoke(null);
        }
    }

    private void UpdateSkillMenu(KeyboardState keyboard)
    {
        if (_availableSkills.Count == 0)
        {
            if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
                _currentState = BattleMenuState.Command;
            return;
        }

        // Navigation
        if (IsKeyPressed(keyboard, Keys.Down))
        {
            _skillIndex = (_skillIndex + 1) % _availableSkills.Count;
            UpdateScrollOffset(_skillIndex, _availableSkills.Count);
        }
        else if (IsKeyPressed(keyboard, Keys.Up))
        {
            _skillIndex = (_skillIndex - 1 + _availableSkills.Count) % _availableSkills.Count;
            UpdateScrollOffset(_skillIndex, _availableSkills.Count);
        }

        // Selection
        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Z))
        {
            var skill = _availableSkills[_skillIndex];
            if (CanUseSkill(skill))
            {
                _selectedSkill = skill;
                _selectedActionType = ActionType.Skill;
                SetupTargetSelection(skill.Scope);
            }
        }

        // Cancel
        if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
        {
            _currentState = BattleMenuState.Command;
        }
    }

    private void UpdateItemMenu(KeyboardState keyboard)
    {
        if (_availableItems.Count == 0)
        {
            if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
                _currentState = BattleMenuState.Command;
            return;
        }

        // Navigation
        if (IsKeyPressed(keyboard, Keys.Down))
        {
            _itemIndex = (_itemIndex + 1) % _availableItems.Count;
            UpdateScrollOffset(_itemIndex, _availableItems.Count);
        }
        else if (IsKeyPressed(keyboard, Keys.Up))
        {
            _itemIndex = (_itemIndex - 1 + _availableItems.Count) % _availableItems.Count;
            UpdateScrollOffset(_itemIndex, _availableItems.Count);
        }

        // Selection
        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Z))
        {
            var invItem = _availableItems[_itemIndex];
            var item = _database.GetItem(invItem.ItemId);
            if (item != null)
            {
                _selectedItem = item;
                _selectedActionType = ActionType.Item;
                SetupTargetSelection(item.Scope);
            }
        }

        // Cancel
        if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
        {
            _currentState = BattleMenuState.Command;
        }
    }

    private void UpdateTargetMenu(KeyboardState keyboard)
    {
        if (_availableTargets.Count == 0)
        {
            // No valid targets, go back
            _currentState = _selectedActionType == ActionType.Skill ? BattleMenuState.Skills :
                           _selectedActionType == ActionType.Item ? BattleMenuState.Items :
                           BattleMenuState.Command;
            return;
        }

        // Navigation
        if (IsKeyPressed(keyboard, Keys.Down) || IsKeyPressed(keyboard, Keys.Right))
        {
            _targetIndex = (_targetIndex + 1) % _availableTargets.Count;
        }
        else if (IsKeyPressed(keyboard, Keys.Up) || IsKeyPressed(keyboard, Keys.Left))
        {
            _targetIndex = (_targetIndex - 1 + _availableTargets.Count) % _availableTargets.Count;
        }

        // Selection
        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Z))
        {
            ConfirmAction();
        }

        // Cancel
        if (IsKeyPressed(keyboard, Keys.Escape) || IsKeyPressed(keyboard, Keys.X))
        {
            _currentState = _selectedActionType == ActionType.Skill ? BattleMenuState.Skills :
                           _selectedActionType == ActionType.Item ? BattleMenuState.Items :
                           BattleMenuState.Command;
        }
    }

    #endregion

    #region Draw Methods

    private void DrawCommandWindow(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Viewport viewport)
    {
        // Position at bottom-left
        var rect = new Rectangle(20, viewport.Height - CommandWindowHeight - 20, CommandWindowWidth, CommandWindowHeight);

        // Background
        spriteBatch.Draw(pixel, rect, Color.Black * 0.85f);
        DrawBorder(spriteBatch, pixel, rect, Color.White, 2);

        // Title
        string title = _currentBattler?.ActorId ?? "Battle";
        spriteBatch.DrawString(font, title, new Vector2(rect.X + 10, rect.Y + 8), Color.Cyan);

        // Commands
        for (int i = 0; i < _commands.Length; i++)
        {
            bool isSelected = i == _commandIndex && _currentState == BattleMenuState.Command;
            Color color = isSelected ? Color.Yellow : Color.White;

            // Dim unavailable options
            if (i == 1 && _availableSkills.Count == 0) color = Color.Gray;
            if (i == 2 && _availableItems.Count == 0) color = Color.Gray;

            Vector2 pos = new Vector2(rect.X + 30, rect.Y + 35 + i * 24);

            if (isSelected)
                spriteBatch.DrawString(font, ">", pos - new Vector2(15, 0), Color.Yellow);

            spriteBatch.DrawString(font, _commands[i], pos, color);
        }
    }

    private void DrawSkillWindow(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Viewport viewport)
    {
        // Position next to command window
        var rect = new Rectangle(CommandWindowWidth + 40, viewport.Height - SubMenuHeight - 20, SubMenuWidth, SubMenuHeight);

        // Background
        spriteBatch.Draw(pixel, rect, Color.Black * 0.85f);
        DrawBorder(spriteBatch, pixel, rect, Color.White, 2);

        // Title
        spriteBatch.DrawString(font, "Skills", new Vector2(rect.X + 10, rect.Y + 8), Color.Cyan);

        if (_availableSkills.Count == 0)
        {
            spriteBatch.DrawString(font, "No skills available", new Vector2(rect.X + 20, rect.Y + 40), Color.Gray);
            return;
        }

        // Skills list
        int visibleCount = Math.Min(MaxVisibleItems, _availableSkills.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            int skillIdx = _scrollOffset + i;
            if (skillIdx >= _availableSkills.Count) break;

            var skill = _availableSkills[skillIdx];
            bool isSelected = skillIdx == _skillIndex;
            bool canUse = CanUseSkill(skill);

            Color color = !canUse ? Color.Gray : isSelected ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(rect.X + 25, rect.Y + 35 + i * 26);

            if (isSelected)
                spriteBatch.DrawString(font, ">", pos - new Vector2(15, 0), Color.Yellow);

            spriteBatch.DrawString(font, skill.Name, pos, color);

            // MP cost
            string cost = $"{skill.MpCost} MP";
            Vector2 costPos = new Vector2(rect.Right - 60, pos.Y);
            spriteBatch.DrawString(font, cost, costPos, canUse ? Color.LightBlue : Color.Gray);
        }

        // Scroll indicators
        if (_scrollOffset > 0)
            spriteBatch.DrawString(font, "^", new Vector2(rect.X + rect.Width / 2, rect.Y + 32), Color.White);
        if (_scrollOffset + MaxVisibleItems < _availableSkills.Count)
            spriteBatch.DrawString(font, "v", new Vector2(rect.X + rect.Width / 2, rect.Bottom - 20), Color.White);
    }

    private void DrawItemWindow(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Viewport viewport)
    {
        // Position next to command window
        var rect = new Rectangle(CommandWindowWidth + 40, viewport.Height - SubMenuHeight - 20, SubMenuWidth, SubMenuHeight);

        // Background
        spriteBatch.Draw(pixel, rect, Color.Black * 0.85f);
        DrawBorder(spriteBatch, pixel, rect, Color.White, 2);

        // Title
        spriteBatch.DrawString(font, "Items", new Vector2(rect.X + 10, rect.Y + 8), Color.Cyan);

        if (_availableItems.Count == 0)
        {
            spriteBatch.DrawString(font, "No items available", new Vector2(rect.X + 20, rect.Y + 40), Color.Gray);
            return;
        }

        // Items list
        int visibleCount = Math.Min(MaxVisibleItems, _availableItems.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            int itemIdx = _scrollOffset + i;
            if (itemIdx >= _availableItems.Count) break;

            var invItem = _availableItems[itemIdx];
            var item = _database.GetItem(invItem.ItemId);
            bool isSelected = itemIdx == _itemIndex;

            Color color = isSelected ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(rect.X + 25, rect.Y + 35 + i * 26);

            if (isSelected)
                spriteBatch.DrawString(font, ">", pos - new Vector2(15, 0), Color.Yellow);

            string name = item?.Name ?? invItem.ItemId;
            spriteBatch.DrawString(font, name, pos, color);

            // Quantity
            string qty = $"x{invItem.Quantity}";
            Vector2 qtyPos = new Vector2(rect.Right - 50, pos.Y);
            spriteBatch.DrawString(font, qty, qtyPos, Color.LightGreen);
        }
    }

    private void DrawTargetWindow(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Viewport viewport)
    {
        // Position at top-right
        var rect = new Rectangle(viewport.Width - TargetWindowWidth - 20, 20, TargetWindowWidth, 40 + _availableTargets.Count * 28);

        // Background
        spriteBatch.Draw(pixel, rect, Color.Black * 0.85f);
        DrawBorder(spriteBatch, pixel, rect, Color.Yellow, 2);

        // Title
        spriteBatch.DrawString(font, "Select Target", new Vector2(rect.X + 10, rect.Y + 8), Color.Yellow);

        // Targets
        for (int i = 0; i < _availableTargets.Count; i++)
        {
            var target = _availableTargets[i];
            bool isSelected = i == _targetIndex;

            Color color = isSelected ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(rect.X + 25, rect.Y + 35 + i * 26);

            if (isSelected)
                spriteBatch.DrawString(font, ">", pos - new Vector2(15, 0), Color.Yellow);

            string name = target.IsActor ? (target.ActorId ?? "Party") : (target.EnemyId ?? "Enemy");
            string hp = $"HP: {target.CurrentHp}/{target.MaxHp}";

            spriteBatch.DrawString(font, name, pos, color);
            spriteBatch.DrawString(font, hp, new Vector2(pos.X + 100, pos.Y), target.IsAlive ? Color.LightGreen : Color.Red);
        }
    }

    private void DrawBattlerStatus(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Viewport viewport)
    {
        if (_currentBattler == null || _battleState == null)
            return;

        // Draw party status at bottom
        int partyBoxWidth = 180;
        int partyBoxHeight = 60;
        int startX = viewport.Width - 20 - _battleState.Party.Count * (partyBoxWidth + 10);

        for (int i = 0; i < _battleState.Party.Count; i++)
        {
            var member = _battleState.Party[i];
            bool isActive = member == _currentBattler;

            var rect = new Rectangle(startX + i * (partyBoxWidth + 10), viewport.Height - partyBoxHeight - 20, partyBoxWidth, partyBoxHeight);

            // Background
            spriteBatch.Draw(pixel, rect, Color.Black * 0.85f);
            DrawBorder(spriteBatch, pixel, rect, isActive ? Color.Yellow : Color.White, isActive ? 3 : 2);

            // Name
            string name = member.ActorId ?? $"Actor {i + 1}";
            spriteBatch.DrawString(font, name, new Vector2(rect.X + 8, rect.Y + 5), isActive ? Color.Yellow : Color.White);

            // HP bar
            DrawBar(spriteBatch, pixel, new Rectangle(rect.X + 8, rect.Y + 25, rect.Width - 16, 12),
                member.CurrentHp, member.MaxHp, Color.DarkGreen, Color.Green);

            // MP bar
            DrawBar(spriteBatch, pixel, new Rectangle(rect.X + 8, rect.Y + 40, rect.Width - 16, 10),
                member.CurrentMp, member.MaxMp, Color.DarkBlue, Color.Blue);
        }
    }

    private void DrawBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int current, int max, Color bgColor, Color fillColor)
    {
        spriteBatch.Draw(pixel, rect, bgColor);

        if (max > 0)
        {
            float ratio = Math.Clamp((float)current / max, 0, 1);
            var fillRect = new Rectangle(rect.X, rect.Y, (int)(rect.Width * ratio), rect.Height);
            spriteBatch.Draw(pixel, fillRect, fillColor);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    #endregion

    #region Helper Methods

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void SelectCommand()
    {
        switch (_commandIndex)
        {
            case 0: // Attack
                _selectedActionType = ActionType.Attack;
                SetupTargetSelection(SkillScope.OneEnemy);
                break;

            case 1: // Skills
                if (_availableSkills.Count > 0)
                {
                    _currentState = BattleMenuState.Skills;
                    _skillIndex = 0;
                    _scrollOffset = 0;
                }
                break;

            case 2: // Items
                if (_availableItems.Count > 0)
                {
                    _currentState = BattleMenuState.Items;
                    _itemIndex = 0;
                    _scrollOffset = 0;
                }
                break;

            case 3: // Guard
                ConfirmGuard();
                break;

            case 4: // Escape
                ConfirmEscape();
                break;
        }
    }

    private void SetupTargetSelection(SkillScope scope)
    {
        _availableTargets.Clear();

        if (_battleState == null)
            return;

        switch (scope)
        {
            case SkillScope.OneEnemy:
            case SkillScope.RandomEnemies:
            case SkillScope.TwoRandomEnemies:
            case SkillScope.ThreeRandomEnemies:
            case SkillScope.FourRandomEnemies:
                _availableTargets.AddRange(_battleState.Enemies.Where(e => e.IsAlive));
                break;

            case SkillScope.AllEnemies:
                // All enemies - no selection needed, but show them
                _availableTargets.AddRange(_battleState.Enemies.Where(e => e.IsAlive));
                break;

            case SkillScope.OneAlly:
                _availableTargets.AddRange(_battleState.Party.Where(p => p.IsAlive));
                break;

            case SkillScope.AllAllies:
                _availableTargets.AddRange(_battleState.Party.Where(p => p.IsAlive));
                break;

            case SkillScope.OneAllyDead:
                _availableTargets.AddRange(_battleState.Party.Where(p => !p.IsAlive));
                break;

            case SkillScope.AllAlliesDead:
                _availableTargets.AddRange(_battleState.Party.Where(p => !p.IsAlive));
                break;

            case SkillScope.User:
                if (_currentBattler != null)
                    _availableTargets.Add(_currentBattler);
                break;

            default:
                _availableTargets.AddRange(_battleState.Enemies.Where(e => e.IsAlive));
                break;
        }

        _targetIndex = 0;
        _currentState = BattleMenuState.Target;
    }

    private void ConfirmAction()
    {
        if (_currentBattler == null)
            return;

        var action = new BattleAction
        {
            User = _currentBattler,
            Type = _selectedActionType,
            Skill = _selectedSkill,
            Item = _selectedItem,
            Targets = GetSelectedTargets()
        };

        _onActionSelected?.Invoke(action);
        Close();
    }

    private void ConfirmGuard()
    {
        if (_currentBattler == null)
            return;

        var action = new BattleAction
        {
            User = _currentBattler,
            Type = ActionType.Guard,
            Targets = new List<Battler> { _currentBattler }
        };

        _onActionSelected?.Invoke(action);
        Close();
    }

    private void ConfirmEscape()
    {
        if (_currentBattler == null)
            return;

        var action = new BattleAction
        {
            User = _currentBattler,
            Type = ActionType.Escape,
            Targets = new List<Battler>()
        };

        _onActionSelected?.Invoke(action);
        Close();
    }

    private List<Battler> GetSelectedTargets()
    {
        var targets = new List<Battler>();

        var scope = _selectedSkill?.Scope ?? _selectedItem?.Scope ?? SkillScope.OneEnemy;

        switch (scope)
        {
            case SkillScope.AllEnemies:
            case SkillScope.AllAllies:
            case SkillScope.AllAlliesDead:
                targets.AddRange(_availableTargets);
                break;

            case SkillScope.TwoRandomEnemies:
                targets.AddRange(_availableTargets.OrderBy(_ => Random.Shared.Next()).Take(2));
                break;

            case SkillScope.ThreeRandomEnemies:
                targets.AddRange(_availableTargets.OrderBy(_ => Random.Shared.Next()).Take(3));
                break;

            case SkillScope.FourRandomEnemies:
                targets.AddRange(_availableTargets.OrderBy(_ => Random.Shared.Next()).Take(4));
                break;

            default:
                if (_targetIndex >= 0 && _targetIndex < _availableTargets.Count)
                    targets.Add(_availableTargets[_targetIndex]);
                break;
        }

        return targets;
    }

    private void LoadAvailableSkills()
    {
        _availableSkills.Clear();

        if (_currentBattler == null)
            return;

        foreach (var skillId in _currentBattler.LearnedSkills)
        {
            var skill = _database.GetSkill(skillId);
            if (skill != null && skill.Occasion != ItemOccasion.MenuOnly && skill.Occasion != ItemOccasion.Never)
            {
                _availableSkills.Add(skill);
            }
        }
    }

    private void LoadAvailableItems()
    {
        _availableItems.Clear();

        var inventory = _game.GetInventoryManager();
        if (inventory == null)
            return;

        // InventoryManager.GetAllItems() returns Dictionary<string, int>
        foreach (var kvp in inventory.GetAllItems())
        {
            var item = _database.GetItem(kvp.Key);
            if (item != null && item.Occasion != ItemOccasion.MenuOnly && item.Occasion != ItemOccasion.Never)
            {
                _availableItems.Add(new InventoryItem { ItemId = kvp.Key, Quantity = kvp.Value });
            }
        }
    }

    private bool CanUseSkill(Skill skill)
    {
        if (_currentBattler == null)
            return false;

        return _currentBattler.CurrentMp >= skill.MpCost &&
               _currentBattler.CurrentTp >= skill.TpCost;
    }

    private void UpdateScrollOffset(int selectedIndex, int totalCount)
    {
        if (selectedIndex < _scrollOffset)
            _scrollOffset = selectedIndex;
        else if (selectedIndex >= _scrollOffset + MaxVisibleItems)
            _scrollOffset = selectedIndex - MaxVisibleItems + 1;
    }

    #endregion
}

/// <summary>
/// Battle menu states.
/// </summary>
public enum BattleMenuState
{
    Hidden,
    Command,
    Skills,
    Items,
    Target
}

/// <summary>
/// Inventory item reference for battle menu.
/// </summary>
public class InventoryItem
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
