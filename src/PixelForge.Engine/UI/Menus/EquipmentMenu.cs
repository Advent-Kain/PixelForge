using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.UI;

/// <summary>
/// Equipment menu for managing character equipment.
/// </summary>
public class EquipmentMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedActorIndex;
    private int _selectedSlotIndex;

    // Standard equipment slots: Weapon, Head, Body, Legs, Accessory1, Accessory2
    private static readonly (string Name, EquipSlot Slot)[] Slots = new[]
    {
        ("Weapon", EquipSlot.Weapon),
        ("Head", EquipSlot.Head),
        ("Body", EquipSlot.Body),
        ("Arms", EquipSlot.Arms),
        ("Legs", EquipSlot.Legs),
        ("Accessory 1", EquipSlot.Accessory1),
        ("Accessory 2", EquipSlot.Accessory2)
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
        var partyManager = _game.GetPartyManager();
        int partyCount = Math.Max(1, partyManager.Party.Count);

        // Navigate actors (Left/Right)
        if (partyManager.Party.Count > 0 && keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _selectedActorIndex = (_selectedActorIndex + 1) % partyCount;
        }
        else if (partyManager.Party.Count > 0 && keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + partyCount) % partyCount;
        }

        // Navigate slots (Up/Down)
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedSlotIndex = (_selectedSlotIndex + 1) % Slots.Length;
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedSlotIndex = (_selectedSlotIndex - 1 + Slots.Length) % Slots.Length;
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
        var viewport = spriteBatch.GraphicsDevice?.Viewport ?? new Viewport(0, 0, 800, 600);
        var partyManager = _game.GetPartyManager();
        var database = _game.GetDatabase();

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

        // Get current actor
        GameActor? currentActorNullable = null;
        if (_selectedActorIndex < partyManager.Party.Count)
        {
            currentActorNullable = partyManager.Party[_selectedActorIndex];
        }

        if (currentActorNullable == null)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
            spriteBatch.DrawString(font, "No party members.", emptyPos, Color.Gray);
            return;
        }

        // After null check, assign to non-nullable variable
        GameActor currentActor = currentActorNullable;

        var currentStats = currentActor.GetCurrentStats();
        int maxHp = Math.Max(1, currentStats.MaxHp);
        int maxMp = Math.Max(1, currentStats.MaxMp);
        int currentHp = Math.Clamp(currentActor.CurrentHp, 0, maxHp);
        int currentMp = Math.Clamp(currentActor.CurrentMp, 0, maxMp);
        string className = currentActor.ClassData?.Name ?? "Unknown Class";

        // Draw actor name
        string actorName = currentActor?.ActorData?.Name ?? $"Actor {_selectedActorIndex + 1}";
        Vector2 namePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, $"{actorName} - {className} (Lv {currentActor.Level})", namePos, Color.Cyan);
        spriteBatch.DrawString(font, $"HP: {currentHp}/{maxHp}   MP: {currentMp}/{maxMp}", namePos + new Vector2(0, 25), Color.White);

        // Draw equipment slots
        for (int i = 0; i < Slots.Length; i++)
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

            // Get equipped item name
            string equippedName = GetEquippedName(currentActor, Slots[i].Slot);

            string slotText = $"{Slots[i].Name}: {equippedName}";
            spriteBatch.DrawString(font, slotText, pos, color);
        }

        // Draw current stats
        var statsRect = new Rectangle(
            windowRect.X + windowRect.Width / 2,
            windowRect.Y + 100,
            windowRect.Width / 2 - 40,
            300
        );

        Vector2 statsPos = new Vector2(statsRect.X + 10, statsRect.Y);
        spriteBatch.DrawString(font, "Stats:", statsPos, Color.White);

        if (currentActor != null)
        {
            var stats = currentActor.GetCurrentStats();
            spriteBatch.DrawString(font, $"HP: {currentActor.CurrentHp}/{stats.MaxHp}", statsPos + new Vector2(0, 25), Color.LightGreen);
            spriteBatch.DrawString(font, $"MP: {currentActor.CurrentMp}/{stats.MaxMp}", statsPos + new Vector2(0, 50), Color.LightBlue);
            spriteBatch.DrawString(font, $"ATK: {stats.Attack}", statsPos + new Vector2(0, 80), Color.White);
            spriteBatch.DrawString(font, $"DEF: {stats.Defense}", statsPos + new Vector2(0, 105), Color.White);
            spriteBatch.DrawString(font, $"M.ATK: {stats.MagicAttack}", statsPos + new Vector2(0, 130), Color.White);
            spriteBatch.DrawString(font, $"M.DEF: {stats.MagicDefense}", statsPos + new Vector2(0, 155), Color.White);
            spriteBatch.DrawString(font, $"AGI: {stats.Agility}", statsPos + new Vector2(0, 180), Color.White);
            spriteBatch.DrawString(font, $"LUK: {stats.Luck}", statsPos + new Vector2(0, 205), Color.White);

            // Show equipment-granted skills
            var equipSkills = currentActor.GetEquipmentSkills().ToList();
            if (equipSkills.Count > 0)
            {
                spriteBatch.DrawString(font, "Equipment Skills:", statsPos + new Vector2(0, 240), Color.Yellow);
                int skillY = 265;
                foreach (var skillId in equipSkills.Take(4))
                {
                    var skill = database.GetSkill(skillId);
                    string skillName = skill?.Name ?? skillId;
                    spriteBatch.DrawString(font, $"  {skillName}", statsPos + new Vector2(0, skillY), Color.LightCyan);
                    skillY += 22;
                }
            }
        }
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

    private string GetEquippedName(GameActor actor, EquipSlot slot)
    {
        if (!actor.EquippedItems.TryGetValue(slot, out var itemId) || string.IsNullOrEmpty(itemId))
            return "None";

        var equipment = _game.GetDatabase().GetEquipment(itemId);
        return equipment?.Name ?? "Unknown";
    }
}
