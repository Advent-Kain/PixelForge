using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;

namespace PixelForge.Engine.UI;

/// <summary>
/// Save/Load menu with multiple save slots.
/// </summary>
public class SaveLoadMenu : IMenu
{
    private readonly GameEngine _game;
    private readonly bool _isSaveMode;
    private readonly SaveManager _saveManager;
    private int _selectedSlot;
    private List<SaveSlotInfo> _saveSlots = new();
    private readonly int _slotsPerPage = 8;
    private int _scrollOffset;

    private KeyboardState _previousKeyboard;

    public SaveLoadMenu(GameEngine game, bool isSaveMode)
    {
        _game = game;
        _isSaveMode = isSaveMode;
        _saveManager = new SaveManager();
    }

    public void OnOpen()
    {
        _selectedSlot = 0;
        _scrollOffset = 0;
        RefreshSaveSlots();
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Navigate slots
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedSlot = Math.Min(_selectedSlot + 1, _saveSlots.Count - 1);
            if (_selectedSlot >= _scrollOffset + _slotsPerPage)
            {
                _scrollOffset++;
            }
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedSlot = Math.Max(_selectedSlot - 1, 0);
            if (_selectedSlot < _scrollOffset)
            {
                _scrollOffset--;
            }
        }

        // Confirm action
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            if (_isSaveMode)
            {
                PerformSave();
            }
            else
            {
                PerformLoad();
            }
        }

        // Delete save (Delete key)
        if (keyboard.IsKeyDown(Keys.Delete) && _previousKeyboard.IsKeyUp(Keys.Delete))
        {
            DeleteSave();
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

        // Draw save/load window
        var windowRect = new Rectangle(100, 50, viewport.Width - 200, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        string title = _isSaveMode ? "Save Game" : "Load Game";
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, title, titlePos, Color.White);

        // Draw save slots
        int startIndex = _scrollOffset;
        int endIndex = Math.Min(_scrollOffset + _slotsPerPage, _saveSlots.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var slot = _saveSlots[i];
            int displayIndex = i - _scrollOffset;
            Color color = i == _selectedSlot ? Color.Yellow : Color.White;

            Vector2 pos = new Vector2(
                windowRect.X + 40,
                windowRect.Y + 60 + displayIndex * 50
            );

            if (i == _selectedSlot)
            {
                spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
            }

            string slotText = slot.GetDisplayText();
            spriteBatch.DrawString(font, slotText, pos, color);

            // Draw timestamp if save exists
            if (slot.Exists && slot.Data != null)
            {
                string timestamp = slot.Data.Timestamp.ToString("yyyy/MM/dd HH:mm");
                Vector2 timePos = new Vector2(pos.X, pos.Y + 25);
                spriteBatch.DrawString(font, timestamp, timePos, Color.Gray);
            }
        }

        // Draw instructions
        Vector2 instructionsPos = new Vector2(windowRect.X + 20, windowRect.Bottom - 40);
        string instructions = _isSaveMode
            ? "Enter: Save | Delete: Delete Save | Esc: Cancel"
            : "Enter: Load | Delete: Delete Save | Esc: Cancel";
        spriteBatch.DrawString(font, instructions, instructionsPos, Color.Gray);

        // Draw selected slot details
        if (_selectedSlot >= 0 && _selectedSlot < _saveSlots.Count)
        {
            var selectedSlot = _saveSlots[_selectedSlot];
            if (selectedSlot.Exists && selectedSlot.Data != null)
            {
                var detailsRect = new Rectangle(
                    windowRect.X + windowRect.Width / 2 + 20,
                    windowRect.Y + 60,
                    windowRect.Width / 2 - 60,
                    300
                );

                spriteBatch.Draw(pixelTexture, detailsRect, Color.Black * 0.5f);
                DrawBorder(spriteBatch, pixelTexture, detailsRect, Color.Gray, 1);

                Vector2 detailsPos = new Vector2(detailsRect.X + 10, detailsRect.Y + 10);
                spriteBatch.DrawString(font, "Save Details:", detailsPos, Color.Yellow);

                var data = selectedSlot.Data;
                spriteBatch.DrawString(font, $"Map: {data.CurrentMapId ?? "Unknown"}", detailsPos + new Vector2(0, 35), Color.White);
                spriteBatch.DrawString(font, $"Gold: {data.Gold}", detailsPos + new Vector2(0, 65), Color.White);
                int totalHours = (int)data.PlayTime.TotalHours;
                spriteBatch.DrawString(font, $"Play Time: {totalHours:D2}:{data.PlayTime.Minutes:D2}", detailsPos + new Vector2(0, 95), Color.White);

                if (data.Party.Count > 0)
                {
                    spriteBatch.DrawString(font, "Party:", detailsPos + new Vector2(0, 125), Color.Cyan);
                    for (int i = 0; i < Math.Min(data.Party.Count, 4); i++)
                    {
                        var member = data.Party[i];
                        string memberText = $"Lv.{member.Level} {member.Name}";
                        spriteBatch.DrawString(font, memberText, detailsPos + new Vector2(10, 155 + i * 30), Color.White);
                    }
                }
            }
        }
    }

    private void RefreshSaveSlots()
    {
        _saveSlots = _saveManager.GetAllSaveSlots();
    }

    private void PerformSave()
    {
        if (_saveManager.SaveGame(_selectedSlot, _game))
        {
            RefreshSaveSlots();
            // TODO: Show success message
        }
        else
        {
            // TODO: Show error message
        }
    }

    private void PerformLoad()
    {
        var slot = _saveSlots[_selectedSlot];
        if (!slot.Exists)
            return;

        if (_saveManager.LoadGame(_selectedSlot, _game))
        {
            // TODO: Close menu and return to game
        }
        else
        {
            // TODO: Show error message
        }
    }

    private void DeleteSave()
    {
        if (_saveManager.DeleteSave(_selectedSlot))
        {
            RefreshSaveSlots();
            // TODO: Show confirmation message
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
