using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;

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

    private KeyboardState _previousKeyboard;

    public InventoryMenu(GameEngine game)
    {
        _game = game;
    }

    public void OnOpen()
    {
        _selectedIndex = 0;
        _scrollOffset = 0;
        RefreshInventory();
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

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
    }

    private void RefreshInventory()
    {
        _items.Clear();
        var gameState = _game.GetGameState();

        foreach (var item in gameState.Inventory)
        {
            _items.Add(new InventoryItem
            {
                Id = item.Key,
                Name = item.Key, // TODO: Load from database
                Description = "A consumable item.",
                Count = item.Value
            });
        }
    }

    private void UseSelectedItem()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return;

        var item = _items[_selectedIndex];
        // TODO: Show target selection or use directly
        // For now, just decrement count
        _game.GetGameState().RemoveItem(item.Id, 1);
        RefreshInventory();

        if (_items.Count == 0)
        {
            _selectedIndex = 0;
        }
        else if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = _items.Count - 1;
        }
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
