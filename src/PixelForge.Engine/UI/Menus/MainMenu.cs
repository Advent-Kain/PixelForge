using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelForge.Engine.UI;

/// <summary>
/// Main menu with option selection.
/// </summary>
public class MainMenu : IMenu
{
    private readonly MenuManager _menuManager;
    private int _selectedIndex;
    private readonly string[] _options = new[]
    {
        "Items",
        "Skills",
        "Equipment",
        "Status",
        "Save",
        "Load",
        "Options",
        "Return to Game"
    };

    private KeyboardState _previousKeyboard;

    public MainMenu(MenuManager menuManager)
    {
        _menuManager = menuManager;
    }

    public void OnOpen()
    {
        _selectedIndex = 0;
    }

    public void OnClose()
    {
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Navigate
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % _options.Length;
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + _options.Length) % _options.Length;
        }

        // Select
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            SelectOption();
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw semi-transparent background
        spriteBatch.Draw(pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw menu window
        int windowWidth = 300;
        int windowHeight = _options.Length * 40 + 40;
        var windowRect = new Rectangle(
            viewport.Width / 2 - windowWidth / 2,
            viewport.Height / 2 - windowHeight / 2,
            windowWidth,
            windowHeight
        );

        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        spriteBatch.DrawString(font, "Menu", titlePos, Color.White);

        // Draw options
        for (int i = 0; i < _options.Length; i++)
        {
            Color color = i == _selectedIndex ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(windowRect.X + 40, windowRect.Y + 50 + i * 35);

            if (i == _selectedIndex)
            {
                spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
            }

            spriteBatch.DrawString(font, _options[i], pos, color);
        }
    }

    private void SelectOption()
    {
        switch (_selectedIndex)
        {
            case 0: _menuManager.NavigateTo(MenuState.Inventory); break;
            case 1: _menuManager.NavigateTo(MenuState.Skills); break;
            case 2: _menuManager.NavigateTo(MenuState.Equipment); break;
            case 3: _menuManager.NavigateTo(MenuState.Status); break;
            case 4: _menuManager.NavigateTo(MenuState.Save); break;
            case 5: _menuManager.NavigateTo(MenuState.Load); break;
            case 6: _menuManager.NavigateTo(MenuState.Options); break;
            case 7: _menuManager.CloseMenu(); break;
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
