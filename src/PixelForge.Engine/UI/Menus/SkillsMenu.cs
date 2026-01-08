using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

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
        var party = _game.GetPartyManager().Party;

        // Navigate actors (Left/Right)
        if (party.Count > 0 && keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _selectedActorIndex = (_selectedActorIndex + 1) % party.Count;
            _selectedSkillIndex = 0;
            _scrollOffset = 0;
        }
        else if (party.Count > 0 && keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _selectedActorIndex = (_selectedActorIndex - 1 + party.Count) % party.Count;
            _selectedSkillIndex = 0;
            _scrollOffset = 0;
        }

        // Navigate skills (Up/Down)
        int skillCount = GetSkillCount();
        if (skillCount == 0)
        {
            _selectedSkillIndex = 0;
            _scrollOffset = 0;
        }
        else if (_selectedSkillIndex >= skillCount)
        {
            _selectedSkillIndex = skillCount - 1;
            _scrollOffset = Math.Min(_scrollOffset, Math.Max(0, skillCount - _visibleSkills));
        }

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

        // Draw actor name/class/level and HP/MP
        Vector2 infoPos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, $"{actorName} - {className} (Lv {actor.Level})", infoPos, Color.Cyan);
        spriteBatch.DrawString(font, $"HP: {currentHp}/{maxHp}   MP: {currentMp}/{maxMp}", infoPos + new Vector2(0, 25), Color.White);

        // Draw skills list
        int skillCount = GetSkillCount();
        if (skillCount == 0)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 40, windowRect.Y + 110);
            spriteBatch.DrawString(font, "No skills learned", emptyPos, Color.Gray);
        }
        else
        {
            int startIndex = _scrollOffset;
            int endIndex = Math.Min(_scrollOffset + _visibleSkills, skillCount);
            var skillIds = GetSelectedSkillIds();
            var database = _game.GetDatabase();

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

                string skillId = skillIds[i];
                var skill = database.GetSkill(skillId);
                string skillName = skill?.Name ?? "Unknown Skill";
                int mpCost = skill?.MpCost ?? 0;
                string skillText = $"{skillName} - MP: {mpCost}";
                spriteBatch.DrawString(font, skillText, pos, color);
            }
        }

        // Draw skill description
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < skillCount)
        {
            var skill = GetSelectedSkill();
            var descRect = new Rectangle(
                windowRect.X + 20,
                windowRect.Bottom - 120,
                windowRect.Width - 40,
                100
            );

            spriteBatch.Draw(pixelTexture, descRect, Color.Black * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, descRect, Color.Gray, 1);

            Vector2 descPos = new Vector2(descRect.X + 10, descRect.Y + 10);
            string description = skill?.Description ?? "No description available.";
            spriteBatch.DrawString(font, description, descPos, Color.White);

            // Draw skill properties
            Vector2 propsPos = new Vector2(descRect.X + 10, descRect.Y + 45);
            string typeText = skill?.SkillType.ToString() ?? "Unknown";
            string elementText = string.IsNullOrWhiteSpace(skill?.Damage?.Element) ? "None" : skill.Damage.Element;
            string scopeText = skill?.Scope.ToString() ?? "Unknown";
            spriteBatch.DrawString(font, $"Type: {typeText} | Element: {elementText}", propsPos, Color.Gray);
            spriteBatch.DrawString(font, $"Target: {scopeText}", propsPos + new Vector2(0, 25), Color.Gray);
        }
    }

    private int GetSkillCount()
    {
        var actor = GetSelectedActor();
        return actor?.LearnedSkills.Count ?? 0;
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

    private GameActor? GetSelectedActor()
    {
        var party = _game.GetPartyManager().Party;
        if (party.Count == 0)
            return null;

        _selectedActorIndex = Math.Clamp(_selectedActorIndex, 0, party.Count - 1);
        return party[_selectedActorIndex];
    }

    private List<string> GetSelectedSkillIds()
    {
        var actor = GetSelectedActor();
        return actor?.LearnedSkills ?? new List<string>();
    }

    private Skill? GetSelectedSkill()
    {
        var skillIds = GetSelectedSkillIds();
        if (_selectedSkillIndex < 0 || _selectedSkillIndex >= skillIds.Count)
            return null;

        return _game.GetDatabase().GetSkill(skillIds[_selectedSkillIndex]);
    }
}
