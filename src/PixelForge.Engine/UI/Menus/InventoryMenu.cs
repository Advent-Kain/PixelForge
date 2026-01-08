using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.UI;

/// <summary>
/// Inventory menu for items.
/// </summary>
public class InventoryMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedIndex;
    private int _scrollOffset;
    private readonly int _visibleItems = 10;
    private List<InventoryItem> _items = new();
    private bool _isSelectingTarget;
    private int _selectedTargetIndex;
    private string? _pendingItemId;
    private Item? _pendingItemData;

    private KeyboardState _previousKeyboard;

    public InventoryMenu(GameEngine game)
    {
        _game = game;
    }

    public void OnOpen()
    {
        _selectedIndex = 0;
        _scrollOffset = 0;
        _isSelectingTarget = false;
        _selectedTargetIndex = 0;
        _pendingItemId = null;
        _pendingItemData = null;
        RefreshInventory();
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (_isSelectingTarget)
        {
            HandleTargetSelection(keyboard);
            _previousKeyboard = keyboard;
            return;
        }

        if (_items.Count == 0)
        {
            _previousKeyboard = keyboard;
            return;
        }

        // Navigate
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedIndex = Math.Min(_selectedIndex + 1, _items.Count - 1);
            if (_selectedIndex >= _scrollOffset + _visibleItems)
            {
                _scrollOffset++;
            }
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
            if (_selectedIndex < _scrollOffset)
            {
                _scrollOffset--;
            }
        }

        // Use item (Enter key)
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            UseSelectedItem();
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw background
        spriteBatch.Draw(pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw inventory window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, "Items", titlePos, Color.White);

        // Draw gold
        var gameState = _game.GetGameState();
        string goldText = $"Gold: {gameState.PartyGold}";
        Vector2 goldPos = new Vector2(windowRect.Right - 200, windowRect.Y + 10);
        spriteBatch.DrawString(font, goldText, goldPos, Color.Yellow);

        // Draw item list
        if (_items.Count == 0)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 20, windowRect.Y + 60);
            spriteBatch.DrawString(font, "No items", emptyPos, Color.Gray);
        }
        else
        {
            int startIndex = _scrollOffset;
            int endIndex = Math.Min(_scrollOffset + _visibleItems, _items.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var item = _items[i];
                int displayIndex = i - _scrollOffset;
                Color color = i == _selectedIndex ? Color.Yellow : Color.White;

                Vector2 pos = new Vector2(
                    windowRect.X + 40,
                    windowRect.Y + 60 + displayIndex * 35
                );

                if (i == _selectedIndex)
                {
                    spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
                }

                string itemText = $"{item.Name} x{item.Count}";
                spriteBatch.DrawString(font, itemText, pos, color);
            }
        }

        // Draw description for selected item
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            var selectedItem = _items[_selectedIndex];
            var descRect = new Rectangle(
                windowRect.X + 20,
                windowRect.Bottom - 100,
                windowRect.Width - 40,
                80
            );

            spriteBatch.Draw(pixelTexture, descRect, Color.Black * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, descRect, Color.Gray, 1);

            Vector2 descPos = new Vector2(descRect.X + 10, descRect.Y + 10);
            string description = selectedItem.Description ?? "No description.";
            spriteBatch.DrawString(font, description, descPos, Color.White);
        }

        if (_isSelectingTarget && _pendingItemData != null)
        {
            DrawTargetSelection(spriteBatch, font, pixelTexture, windowRect);
        }
    }

    private void RefreshInventory()
    {
        _items.Clear();
        var inventory = _game.GetInventoryManager().GetAllItems();
        var database = _game.GetDatabase();

        foreach (var item in inventory)
        {
            var itemData = database.GetItem(item.Key);
            _items.Add(new InventoryItem
            {
                Id = item.Key,
                Name = itemData?.Name ?? item.Key,
                Description = itemData?.Description,
                Count = item.Value
            });
        }
    }

    private void UseSelectedItem()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return;

        var item = _items[_selectedIndex];
        var itemData = _game.GetDatabase().GetItem(item.Id);
        if (itemData == null)
            return;

        switch (itemData.Scope)
        {
            case SkillScope.AllAllies:
            case SkillScope.AllAlliesDead:
                ApplyItemToTargets(item.Id, itemData, GetTargetCandidates(itemData.Scope));
                break;
            case SkillScope.User:
                var userTarget = GetUserTarget();
                if (userTarget != null)
                {
                    ApplyItemToTargets(item.Id, itemData, new List<GameActor> { userTarget });
                }
                break;
            case SkillScope.OneAlly:
            case SkillScope.OneAllyDead:
                BeginTargetSelection(item.Id, itemData);
                break;
        }
    }

    private void HandleTargetSelection(KeyboardState keyboard)
    {
        if (_pendingItemData == null || string.IsNullOrEmpty(_pendingItemId))
        {
            CancelTargetSelection();
            return;
        }

        var targets = GetTargetCandidates(_pendingItemData.Scope);
        if (targets.Count == 0)
        {
            CancelTargetSelection();
            return;
        }

        _selectedTargetIndex = Math.Clamp(_selectedTargetIndex, 0, targets.Count - 1);

        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedTargetIndex = (_selectedTargetIndex + 1) % targets.Count;
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedTargetIndex = (_selectedTargetIndex - 1 + targets.Count) % targets.Count;
        }

        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            var target = targets[_selectedTargetIndex];
            if (_game.GetInventoryManager().UseItem(_pendingItemId, _pendingItemData, target))
            {
                RefreshInventory();
                AdjustSelectionAfterUse();
            }

            CancelTargetSelection();
        }
        else if (keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
        {
            CancelTargetSelection();
        }
    }

    private void ApplyItemToTargets(string itemId, Item itemData, IReadOnlyList<GameActor> targets)
    {
        if (targets.Count == 0)
            return;

        if (_game.GetInventoryManager().UseItemOnTargets(itemId, itemData, targets))
        {
            RefreshInventory();
            AdjustSelectionAfterUse();
        }
    }

    private void BeginTargetSelection(string itemId, Item itemData)
    {
        _pendingItemId = itemId;
        _pendingItemData = itemData;
        _selectedTargetIndex = 0;
        _isSelectingTarget = true;
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
        _pendingItemId = null;
        _pendingItemData = null;
        _selectedTargetIndex = 0;
    }

    private void AdjustSelectionAfterUse()
    {
        if (_items.Count == 0)
        {
            _selectedIndex = 0;
        }
        else if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = _items.Count - 1;
        }
    }

    private GameActor? GetUserTarget()
    {
        var party = _game.GetPartyManager().Party;
        return party.FirstOrDefault(actor => actor.IsAlive) ?? party.FirstOrDefault();
    }

    private List<GameActor> GetTargetCandidates(SkillScope scope)
    {
        var party = _game.GetPartyManager().Party;

        return scope switch
        {
            SkillScope.OneAlly => party.Where(actor => actor.IsAlive).ToList(),
            SkillScope.AllAllies => party.Where(actor => actor.IsAlive).ToList(),
            SkillScope.OneAllyDead => party.Where(actor => !actor.IsAlive).ToList(),
            SkillScope.AllAlliesDead => party.Where(actor => !actor.IsAlive).ToList(),
            _ => new List<GameActor>()
        };
    }

    private void DrawTargetSelection(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture, Rectangle windowRect)
    {
        var targets = _pendingItemData == null ? new List<GameActor>() : GetTargetCandidates(_pendingItemData.Scope);
        var panelRect = new Rectangle(
            windowRect.Right - 320,
            windowRect.Y + 60,
            280,
            200
        );

        spriteBatch.Draw(pixelTexture, panelRect, Color.Black * 0.6f);
        DrawBorder(spriteBatch, pixelTexture, panelRect, Color.Gray, 1);

        Vector2 titlePos = new Vector2(panelRect.X + 10, panelRect.Y + 10);
        spriteBatch.DrawString(font, "Select Target", titlePos, Color.White);

        if (targets.Count == 0)
        {
            Vector2 emptyPos = new Vector2(panelRect.X + 10, panelRect.Y + 50);
            spriteBatch.DrawString(font, "No valid targets", emptyPos, Color.Gray);
            return;
        }

        _selectedTargetIndex = Math.Clamp(_selectedTargetIndex, 0, targets.Count - 1);

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var stats = target.GetCurrentStats();
            int maxHp = Math.Max(1, stats.MaxHp);
            int currentHp = Math.Clamp(target.CurrentHp, 0, maxHp);
            string targetName = target.ActorData?.Name ?? target.ActorId;
            Color color = i == _selectedTargetIndex ? Color.Yellow : Color.White;

            Vector2 pos = new Vector2(panelRect.X + 30, panelRect.Y + 50 + i * 25);
            if (i == _selectedTargetIndex)
            {
                spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
            }

            spriteBatch.DrawString(font, $"{targetName} ({currentHp}/{maxHp} HP)", pos, color);
        }

        Vector2 hintPos = new Vector2(panelRect.X + 10, panelRect.Bottom - 25);
        spriteBatch.DrawString(font, "Enter: Confirm | Esc: Cancel", hintPos, Color.Gray);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class InventoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Count { get; set; }
    }
}
