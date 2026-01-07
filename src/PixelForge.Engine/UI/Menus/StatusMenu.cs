using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;

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

        // Navigate actors (Left/Right)
        if (keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _selectedActorIndex = (_selectedActorIndex + 1) % 4; // Max 4 party members
        }
        else if (keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + 4) % 4;
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

        // Draw actor name and class
        string actorName = $"Hero"; // TODO: Get from party
        string className = $"Warrior";
        Vector2 namePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, actorName, namePos, Color.Cyan);
        spriteBatch.DrawString(font, className, namePos + new Vector2(0, 30), Color.White);

        // Draw level and experience
        int level = 5; // TODO: Get from actor
        int currentExp = 450;
        int nextLevelExp = 600;

        Vector2 levelPos = new Vector2(windowRect.X + 250, windowRect.Y + 50);
        spriteBatch.DrawString(font, $"Level: {level}", levelPos, Color.White);
        spriteBatch.DrawString(font, $"EXP: {currentExp}/{nextLevelExp}", levelPos + new Vector2(0, 30), Color.White);

        // Draw HP and MP
        int currentHp = 280;
        int maxHp = 350;
        int currentMp = 80;
        int maxMp = 120;

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
            "Attack:        45",
            "Defense:       32",
            "M. Attack:     38",
            "M. Defense:    28",
            "Agility:       41",
            "Luck:          25"
        };

        for (int i = 0; i < stats.Length; i++)
        {
            spriteBatch.DrawString(font, stats[i], statsPos + new Vector2(0, 35 + i * 30), Color.White);
        }

        // Draw equipment summary
        Vector2 equipPos = new Vector2(windowRect.X + 350, windowRect.Y + 200);
        spriteBatch.DrawString(font, "--- Equipment ---", equipPos, Color.Yellow);

        string[] equipment = new[]
        {
            "Weapon:   Iron Sword",
            "Shield:   Wooden Shield",
            "Head:     Leather Cap",
            "Body:     Chain Mail",
            "Acc 1:    Power Ring",
            "Acc 2:    None"
        };

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
}
