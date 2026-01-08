using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Quest;

namespace PixelForge.Engine.UI;

/// <summary>
/// Quest log menu showing active and completed quests.
/// </summary>
public class QuestLogMenu : IMenu
{
    private readonly QuestManager _questManager;
    private int _selectedIndex;
    private QuestTab _currentTab = QuestTab.Active;

    private KeyboardState _previousKeyboard;

    public QuestLogMenu(QuestManager questManager)
    {
        _questManager = questManager;
    }

    public void OnOpen()
    {
        _selectedIndex = 0;
        _currentTab = QuestTab.Active;
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Switch tabs
        if (keyboard.IsKeyDown(Keys.Tab) && _previousKeyboard.IsKeyUp(Keys.Tab))
        {
            _currentTab = _currentTab == QuestTab.Active ? QuestTab.Completed : QuestTab.Active;
            _selectedIndex = 0;
        }

        var quests = GetCurrentQuests();
        if (quests.Count == 0)
        {
            _previousKeyboard = keyboard;
            return;
        }

        // Navigate
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % quests.Count;
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + quests.Count) % quests.Count;
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

        // Draw quest log window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title and tab
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        string title = $"Quest Log - {(_currentTab == QuestTab.Active ? "Active" : "Completed")}";
        spriteBatch.DrawString(font, title, titlePos, Color.White);

        // Draw quest list
        var quests = GetCurrentQuests();
        if (quests.Count == 0)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 40, windowRect.Y + 60);
            string emptyText = _currentTab == QuestTab.Active ? "No active quests" : "No completed quests";
            spriteBatch.DrawString(font, emptyText, emptyPos, Color.Gray);
        }
        else
        {
            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                Color color = i == _selectedIndex ? Color.Yellow : Color.White;

                Vector2 pos = new Vector2(
                    windowRect.X + 40,
                    windowRect.Y + 60 + i * 40
                );

                if (i == _selectedIndex)
                {
                    spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
                }

                spriteBatch.DrawString(font, quest.QuestData.Title, pos, color);

                // Progress indicator
                int completed = quest.QuestData.Objectives.Count(o => o.IsComplete);
                int total = quest.QuestData.Objectives.Count;
                string progress = $"({completed}/{total})";
                spriteBatch.DrawString(font, progress, pos + new Vector2(300, 0), Color.Gray);
            }
        }

        // Draw quest details
        if (quests.Count > 0 && _selectedIndex >= 0 && _selectedIndex < quests.Count)
        {
            var selectedQuest = quests[_selectedIndex];
            var detailsRect = new Rectangle(
                windowRect.X + 450,
                windowRect.Y + 60,
                windowRect.Width - 470,
                windowRect.Height - 120
            );

            spriteBatch.Draw(pixelTexture, detailsRect, Color.Black * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, detailsRect, Color.Gray, 1);

            Vector2 detailsPos = new Vector2(detailsRect.X + 10, detailsRect.Y + 10);

            // Title
            spriteBatch.DrawString(font, selectedQuest.QuestData.Title, detailsPos, Color.Yellow);

            // Description
            Vector2 descPos = detailsPos + new Vector2(0, 35);
            DrawWrappedText(spriteBatch, font, selectedQuest.QuestData.Description, descPos, detailsRect.Width - 20, Color.White);

            // Objectives
            Vector2 objPos = detailsPos + new Vector2(0, 100);
            spriteBatch.DrawString(font, "Objectives:", objPos, Color.Cyan);

            for (int i = 0; i < selectedQuest.QuestData.Objectives.Count; i++)
            {
                var objective = selectedQuest.QuestData.Objectives[i];
                Vector2 oPos = objPos + new Vector2(10, 30 + i * 30);

                string checkbox = objective.IsComplete ? "[X]" : "[ ]";
                Color oColor = objective.IsComplete ? Color.Green : Color.White;

                string objText = $"{checkbox} {objective.Description}";
                if (objective.TargetCount > 1)
                {
                    objText += $" ({objective.CurrentCount}/{objective.TargetCount})";
                }

                spriteBatch.DrawString(font, objText, oPos, oColor);
            }

            // Rewards
            if (_currentTab == QuestTab.Active)
            {
                Vector2 rewardPos = new Vector2(detailsRect.X + 10, detailsRect.Y + detailsRect.Height - 120);
                spriteBatch.DrawString(font, "Rewards:", rewardPos, Color.Cyan);

                var rewards = selectedQuest.QuestData.Rewards;
                int rewardIndex = 0;

                if (rewards.Experience > 0)
                {
                    spriteBatch.DrawString(font, $"EXP: {rewards.Experience}", rewardPos + new Vector2(10, 30 + rewardIndex * 25), Color.White);
                    rewardIndex++;
                }

                if (rewards.Gold > 0)
                {
                    spriteBatch.DrawString(font, $"Gold: {rewards.Gold}", rewardPos + new Vector2(10, 30 + rewardIndex * 25), Color.Yellow);
                    rewardIndex++;
                }

                foreach (var item in rewards.Items)
                {
                    spriteBatch.DrawString(font, $"{item.Key} x{item.Value}", rewardPos + new Vector2(10, 30 + rewardIndex * 25), Color.White);
                    rewardIndex++;
                }
            }
        }

        // Instructions
        Vector2 instructPos = new Vector2(windowRect.X + 20, windowRect.Bottom - 25);
        spriteBatch.DrawString(font, "Tab: Switch Tab | Esc: Close", instructPos, Color.Gray);
    }

    private List<ActiveQuest> GetCurrentQuests()
    {
        return _currentTab == QuestTab.Active
            ? _questManager.GetActiveQuests()
            : new List<ActiveQuest>(); // TODO: Get completed quests
    }

    private void DrawWrappedText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, float maxWidth, Color color)
    {
        string[] words = text.Split(' ');
        string line = string.Empty;
        float y = position.Y;

        foreach (string word in words)
        {
            string testLine = line + word + " ";
            Vector2 size = font.MeasureString(testLine);

            if (size.X > maxWidth)
            {
                spriteBatch.DrawString(font, line, new Vector2(position.X, y), color);
                line = word + " ";
                y += size.Y;
            }
            else
            {
                line = testLine;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            spriteBatch.DrawString(font, line, new Vector2(position.X, y), color);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}

public enum QuestTab
{
    Active,
    Completed
}
