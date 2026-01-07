using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.UI;

/// <summary>
/// Equipment menu for managing character equipment.
/// </summary>
public class EquipmentMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedActorIndex;
    private int _selectedSlotIndex;
    private readonly string[] _slots = new[] { "Weapon", "Shield", "Head", "Body", "Accessory 1", "Accessory 2" };

    private KeyboardState _previousKeyboard;

    public EquipmentMenu(GameEngine game)
    {
        _game = game;
    }

    public void OnOpen()
    {
        _selectedActorIndex = 0;
        _selectedSlotIndex = 0;
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Navigate actors (Left/Right)
        if (keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _selectedActorIndex = (_selectedActorIndex + 1) % 4; // Max 4 party members
        }
        else if (keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + 4) % 4;
        }

        // Navigate slots (Up/Down)
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedSlotIndex = (_selectedSlotIndex + 1) % _slots.Length;
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedSlotIndex = (_selectedSlotIndex - 1 + _slots.Length) % _slots.Length;
        }

        // Change equipment (Enter)
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            ChangeEquipment();
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

        // Draw equipment window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, "Equipment", titlePos, Color.White);

        // Draw actor name
        string actorName = $"Actor {_selectedActorIndex + 1}"; // TODO: Get from party
        Vector2 namePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, actorName, namePos, Color.Cyan);

        // Draw equipment slots
        for (int i = 0; i < _slots.Length; i++)
        {
            Color color = i == _selectedSlotIndex ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(
                windowRect.X + 40,
                windowRect.Y + 100 + i * 40
            );

            if (i == _selectedSlotIndex)
            {
                spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
            }

            string slotText = $"{_slots[i]}: None"; // TODO: Show equipped item
            spriteBatch.DrawString(font, slotText, pos, color);
        }

        // Draw stats comparison
        var statsRect = new Rectangle(
            windowRect.X + windowRect.Width / 2,
            windowRect.Y + 100,
            windowRect.Width / 2 - 40,
            300
        );

        Vector2 statsPos = new Vector2(statsRect.X + 10, statsRect.Y);
        spriteBatch.DrawString(font, "Stats:", statsPos, Color.White);
        spriteBatch.DrawString(font, "ATK: 25", statsPos + new Vector2(0, 30), Color.White);
        spriteBatch.DrawString(font, "DEF: 15", statsPos + new Vector2(0, 60), Color.White);
        spriteBatch.DrawString(font, "M.ATK: 20", statsPos + new Vector2(0, 90), Color.White);
        spriteBatch.DrawString(font, "M.DEF: 12", statsPos + new Vector2(0, 120), Color.White);
    }

    private void ChangeEquipment()
    {
        // TODO: Show equipment selection window
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
