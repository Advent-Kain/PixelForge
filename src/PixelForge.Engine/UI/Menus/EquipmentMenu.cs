using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;

namespace PixelForge.Engine.UI;

/// <summary>
/// Equipment menu for managing character equipment.
/// </summary>
public class EquipmentMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedActorIndex;
    private int _selectedSlotIndex;
    private readonly (string Label, string Key)[] _slots =
    {
        ("Weapon", "weapon"),
        ("Shield", "shield"),
        ("Head", "head"),
        ("Body", "body"),
        ("Accessory 1", "accessory1"),
        ("Accessory 2", "accessory2")
    };

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
        var party = _game.GetPartyManager().Party;

        // Navigate actors (Left/Right)
        if (party.Count > 0 && keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _selectedActorIndex = (_selectedActorIndex + 1) % party.Count;
        }
        else if (party.Count > 0 && keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + party.Count) % party.Count;
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

        var actor = GetSelectedActor();
        if (actor == null)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
            spriteBatch.DrawString(font, "No party members.", emptyPos, Color.Gray);
            return;
        }

        var actorStats = actor.GetCurrentStats();
        int maxHp = Math.Max(1, actorStats.MaxHp);
        int maxMp = Math.Max(1, actorStats.MaxMp);
        int currentHp = Math.Clamp(actor.CurrentHp, 0, maxHp);
        int currentMp = Math.Clamp(actor.CurrentMp, 0, maxMp);
        string actorName = actor.ActorData?.Name ?? actor.ActorId;
        string className = actor.ClassData?.Name ?? "Unknown Class";

        // Draw actor info
        Vector2 namePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, $"{actorName} - {className} (Lv {actor.Level})", namePos, Color.Cyan);
        spriteBatch.DrawString(font, $"HP: {currentHp}/{maxHp}   MP: {currentMp}/{maxMp}", namePos + new Vector2(0, 25), Color.White);

        // Draw equipment slots
        for (int i = 0; i < _slots.Length; i++)
        {
            Color color = i == _selectedSlotIndex ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(
                windowRect.X + 40,
                windowRect.Y + 120 + i * 40
            );

            if (i == _selectedSlotIndex)
            {
                spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
            }

            string slotText = $"{_slots[i].Label}: {GetEquippedName(actor, _slots[i].Key)}";
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

    private GameActor? GetSelectedActor()
    {
        var party = _game.GetPartyManager().Party;
        if (party.Count == 0)
            return null;

        _selectedActorIndex = Math.Clamp(_selectedActorIndex, 0, party.Count - 1);
        return party[_selectedActorIndex];
    }

    private string GetEquippedName(GameActor actor, string slotKey)
    {
        if (!actor.EquippedItems.TryGetValue(slotKey, out var itemId) || string.IsNullOrEmpty(itemId))
            return "None";

        var equipment = _game.GetDatabase().GetEquipment(itemId);
        return equipment?.Name ?? "Unknown";
    }
}
