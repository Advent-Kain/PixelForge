using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.UI;

/// <summary>
/// Status menu showing character stats and information.
/// </summary>
public class StatusMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedActorIndex;

    private KeyboardState _previousKeyboard;

    public StatusMenu(GameEngine game)
    {
        _game = game;
    }

    public void OnOpen()
    {
        _selectedActorIndex = 0;
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

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw background
        spriteBatch.Draw(pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw status window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, "Status", titlePos, Color.White);

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

        // Draw actor name and class
        string actorName = actor.ActorData?.Name ?? actor.ActorId;
        string className = actor.ClassData?.Name ?? "Unknown Class";
        Vector2 namePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, actorName, namePos, Color.Cyan);
        spriteBatch.DrawString(font, className, namePos + new Vector2(0, 30), Color.White);

        // Draw level and experience
        int level = actor.Level;
        int currentExp = actor.Experience;
        int nextLevelExp = actor.GetExpForNextLevel();

        Vector2 levelPos = new Vector2(windowRect.X + 250, windowRect.Y + 50);
        spriteBatch.DrawString(font, $"Level: {level}", levelPos, Color.White);
        spriteBatch.DrawString(font, $"EXP: {currentExp}/{nextLevelExp}", levelPos + new Vector2(0, 30), Color.White);

        // Draw HP and MP
        Vector2 hpPos = new Vector2(windowRect.X + 20, windowRect.Y + 120);
        spriteBatch.DrawString(font, $"HP: {currentHp}/{maxHp}", hpPos, Color.Green);
        spriteBatch.DrawString(font, $"MP: {currentMp}/{maxMp}", hpPos + new Vector2(0, 30), Color.Cyan);

        // Draw HP bar
        var hpBarBack = new Rectangle((int)hpPos.X + 100, (int)hpPos.Y + 5, 200, 20);
        var hpBarFill = new Rectangle((int)hpPos.X + 100, (int)hpPos.Y + 5, (int)(200 * ((float)currentHp / maxHp)), 20);
        spriteBatch.Draw(pixelTexture, hpBarBack, Color.DarkGray);
        spriteBatch.Draw(pixelTexture, hpBarFill, Color.Green);

        // Draw MP bar
        var mpBarBack = new Rectangle((int)hpPos.X + 100, (int)hpPos.Y + 35, 200, 20);
        var mpBarFill = new Rectangle((int)hpPos.X + 100, (int)hpPos.Y + 35, (int)(200 * ((float)currentMp / maxMp)), 20);
        spriteBatch.Draw(pixelTexture, mpBarBack, Color.DarkGray);
        spriteBatch.Draw(pixelTexture, mpBarFill, Color.Cyan);

        // Draw stats
        Vector2 statsPos = new Vector2(windowRect.X + 20, windowRect.Y + 200);
        spriteBatch.DrawString(font, "--- Stats ---", statsPos, Color.Yellow);

        string[] stats = new[]
        {
            $"Attack:        {actorStats.Attack}",
            $"Defense:       {actorStats.Defense}",
            $"M. Attack:     {actorStats.MagicAttack}",
            $"M. Defense:    {actorStats.MagicDefense}",
            $"Agility:       {actorStats.Agility}",
            $"Luck:          {actorStats.Luck}"
        };

        for (int i = 0; i < stats.Length; i++)
        {
            spriteBatch.DrawString(font, stats[i], statsPos + new Vector2(0, 35 + i * 30), Color.White);
        }

        // Draw equipment summary
        Vector2 equipPos = new Vector2(windowRect.X + 350, windowRect.Y + 200);
        spriteBatch.DrawString(font, "--- Equipment ---", equipPos, Color.Yellow);

        var equipment = GetEquipmentSummary(actor);

        for (int i = 0; i < equipment.Length; i++)
        {
            spriteBatch.DrawString(font, equipment[i], equipPos + new Vector2(0, 35 + i * 30), Color.White);
        }

        // Draw status effects
        Vector2 statesPos = new Vector2(windowRect.X + 20, windowRect.Bottom - 80);
        spriteBatch.DrawString(font, "Status: Normal", statesPos, Color.White);

        // Draw navigation hint
        string hint = "< > Change Character";
        Vector2 hintPos = new Vector2(windowRect.X + windowRect.Width / 2 - 80, windowRect.Bottom - 30);
        spriteBatch.DrawString(font, hint, hintPos, Color.Gray);
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

    private string[] GetEquipmentSummary(GameActor actor)
    {
        var database = _game.GetDatabase();

        string ResolveName(EquipSlot slot)
        {
            if (!actor.EquippedItems.TryGetValue(slot, out var itemId) || string.IsNullOrEmpty(itemId))
                return "None";

            var equipment = database.GetEquipment(itemId);
            return equipment?.Name ?? "Unknown";
        }

        return new[]
        {
            $"Weapon:   {ResolveName(EquipSlot.Weapon)}",
            $"Head:     {ResolveName(EquipSlot.Head)}",
            $"Body:     {ResolveName(EquipSlot.Body)}",
            $"Arms:     {ResolveName(EquipSlot.Arms)}",
            $"Legs:     {ResolveName(EquipSlot.Legs)}",
            $"Acc 1:    {ResolveName(EquipSlot.Accessory1)}",
            $"Acc 2:    {ResolveName(EquipSlot.Accessory2)}"
        };
    }
}
