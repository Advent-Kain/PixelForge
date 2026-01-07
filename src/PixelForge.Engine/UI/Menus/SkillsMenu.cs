using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.UI;

/// <summary>
/// Skills menu for viewing and using character skills.
/// </summary>
public class SkillsMenu : IMenu
{
    private readonly GameEngine _game;
    private int _selectedActorIndex;
    private int _selectedSkillIndex;
    private int _scrollOffset;
    private readonly int _visibleSkills = 8;

    private KeyboardState _previousKeyboard;

    public SkillsMenu(GameEngine game)
    {
        _game = game;
    }

    public void OnOpen()
    {
        _selectedActorIndex = 0;
        _selectedSkillIndex = 0;
        _scrollOffset = 0;
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
            _selectedActorIndex = (_selectedActorIndex + 1) % 4;
            _selectedSkillIndex = 0;
            _scrollOffset = 0;
        }
        else if (keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + 4) % 4;
            _selectedSkillIndex = 0;
            _scrollOffset = 0;
        }

        // Navigate skills (Up/Down)
        int skillCount = GetSkillCount();
        if (skillCount > 0)
        {
            if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
            {
                _selectedSkillIndex = Math.Min(_selectedSkillIndex + 1, skillCount - 1);
                if (_selectedSkillIndex >= _scrollOffset + _visibleSkills)
                {
                    _scrollOffset++;
                }
            }
            else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
            {
                _selectedSkillIndex = Math.Max(_selectedSkillIndex - 1, 0);
                if (_selectedSkillIndex < _scrollOffset)
                {
                    _scrollOffset--;
                }
            }

            // Use skill (Enter)
            if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
            {
                UseSelectedSkill();
            }
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

        // Draw skills window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, "Skills", titlePos, Color.White);

        // Draw actor name and MP
        string actorInfo = $"Actor {_selectedActorIndex + 1} - MP: 50/50"; // TODO: Get from party
        Vector2 infoPos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, actorInfo, infoPos, Color.Cyan);

        // Draw skills list
        int skillCount = GetSkillCount();
        if (skillCount == 0)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 40, windowRect.Y + 100);
            spriteBatch.DrawString(font, "No skills learned", emptyPos, Color.Gray);
        }
        else
        {
            int startIndex = _scrollOffset;
            int endIndex = Math.Min(_scrollOffset + _visibleSkills, skillCount);

            for (int i = startIndex; i < endIndex; i++)
            {
                int displayIndex = i - _scrollOffset;
                Color color = i == _selectedSkillIndex ? Color.Yellow : Color.White;

                Vector2 pos = new Vector2(
                    windowRect.X + 40,
                    windowRect.Y + 100 + displayIndex * 35
                );

                if (i == _selectedSkillIndex)
                {
                    spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
                }

                string skillText = $"Skill {i + 1} - MP: 10"; // TODO: Load actual skill data
                spriteBatch.DrawString(font, skillText, pos, color);
            }
        }

        // Draw skill description
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < skillCount)
        {
            var descRect = new Rectangle(
                windowRect.X + 20,
                windowRect.Bottom - 120,
                windowRect.Width - 40,
                100
            );

            spriteBatch.Draw(pixelTexture, descRect, Color.Black * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, descRect, Color.Gray, 1);

            Vector2 descPos = new Vector2(descRect.X + 10, descRect.Y + 10);
            string description = "A powerful skill that deals damage to enemies.";
            spriteBatch.DrawString(font, description, descPos, Color.White);

            // Draw skill properties
            Vector2 propsPos = new Vector2(descRect.X + 10, descRect.Y + 45);
            spriteBatch.DrawString(font, "Type: Magic | Element: Fire", propsPos, Color.Gray);
            spriteBatch.DrawString(font, "Target: One Enemy", propsPos + new Vector2(0, 25), Color.Gray);
        }
    }

    private int GetSkillCount()
    {
        // TODO: Get from actor's learned skills
        return 5; // Placeholder
    }

    private void UseSelectedSkill()
    {
        // TODO: Show target selection or use in battle only
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
