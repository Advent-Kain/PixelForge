using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelForge.Engine.UI;

/// <summary>
/// Manages message windows, choices, and text input.
/// </summary>
public class MessageManager
{
    private static MessageManager? _instance;
    public static MessageManager Instance => _instance ??= new MessageManager();

    private MessageWindow? _currentWindow;
    private ChoiceWindow? _choiceWindow;
    private NumberInputWindow? _numberInputWindow;

    private MessageManager() { }

    /// <summary>
    /// Show a message window.
    /// </summary>
    public void ShowMessage(string message, Action? onComplete = null)
    {
        _currentWindow = new MessageWindow
        {
            Message = message,
            OnComplete = onComplete
        };
    }

    /// <summary>
    /// Show a choice window.
    /// </summary>
    public void ShowChoices(List<string> choices, Action<int>? onSelect = null)
    {
        _choiceWindow = new ChoiceWindow
        {
            Choices = choices,
            OnSelect = onSelect
        };
    }

    /// <summary>
    /// Show a number input window.
    /// </summary>
    public void ShowNumberInput(int digits, Action<int>? onComplete = null)
    {
        _numberInputWindow = new NumberInputWindow
        {
            MaxDigits = digits,
            OnComplete = onComplete
        };
    }

    /// <summary>
    /// Update message windows.
    /// </summary>
    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        if (_currentWindow != null)
        {
            _currentWindow.Update(gameTime, keyboardState);
            if (_currentWindow.IsComplete)
            {
                _currentWindow.OnComplete?.Invoke();
                _currentWindow = null;
            }
        }

        if (_choiceWindow != null)
        {
            _choiceWindow.Update(gameTime, keyboardState);
            if (_choiceWindow.IsComplete)
            {
                _choiceWindow.OnSelect?.Invoke(_choiceWindow.SelectedIndex);
                _choiceWindow = null;
            }
        }

        if (_numberInputWindow != null)
        {
            _numberInputWindow.Update(gameTime, keyboardState);
            if (_numberInputWindow.IsComplete)
            {
                _numberInputWindow.OnComplete?.Invoke(_numberInputWindow.Value);
                _numberInputWindow = null;
            }
        }
    }

    /// <summary>
    /// Draw message windows.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        _currentWindow?.Draw(spriteBatch, font, pixelTexture);
        _choiceWindow?.Draw(spriteBatch, font, pixelTexture);
        _numberInputWindow?.Draw(spriteBatch, font, pixelTexture);
    }

    /// <summary>
    /// Check if any window is active.
    /// </summary>
    public bool IsActive => _currentWindow != null || _choiceWindow != null || _numberInputWindow != null;
}

/// <summary>
/// Message window for displaying text.
/// </summary>
public class MessageWindow
{
    public string Message { get; set; } = string.Empty;
    public Action? OnComplete { get; set; }
    public bool IsComplete { get; private set; }

    private float _displayTime;
    private const float CharDelay = 0.05f;
    private int _displayedChars;
    private bool _fullTextShown;

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        if (!_fullTextShown)
        {
            _displayTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _displayedChars = (int)(_displayTime / CharDelay);

            if (_displayedChars >= Message.Length)
            {
                _displayedChars = Message.Length;
                _fullTextShown = true;
            }
        }

        // Press Enter/Space to close or speed up
        if (keyboardState.IsKeyDown(Keys.Enter) || keyboardState.IsKeyDown(Keys.Space))
        {
            if (_fullTextShown)
                IsComplete = true;
            else
                _fullTextShown = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var windowRect = new Rectangle(
            viewport.Width / 8,
            viewport.Height * 3 / 4,
            viewport.Width * 3 / 4,
            viewport.Height / 6
        );

        // Draw window background
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.8f);

        // Draw border
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw text
        string displayText = Message.Substring(0, _displayedChars);
        Vector2 textPos = new Vector2(windowRect.X + 10, windowRect.Y + 10);
        spriteBatch.DrawString(font, displayText, textPos, Color.White);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}

/// <summary>
/// Choice selection window.
/// </summary>
public class ChoiceWindow
{
    public List<string> Choices { get; set; } = new();
    public Action<int>? OnSelect { get; set; }
    public int SelectedIndex { get; private set; }
    public bool IsComplete { get; private set; }

    private KeyboardState _previousKeyboard;

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        // Navigate choices
        if (keyboardState.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            SelectedIndex = (SelectedIndex + 1) % Choices.Count;
        }
        else if (keyboardState.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            SelectedIndex = (SelectedIndex - 1 + Choices.Count) % Choices.Count;
        }

        // Select choice
        if (keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            IsComplete = true;
        }

        _previousKeyboard = keyboardState;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        int itemHeight = 30;
        int windowHeight = Choices.Count * itemHeight + 20;

        var windowRect = new Rectangle(
            viewport.Width / 2 - 150,
            viewport.Height / 2 - windowHeight / 2,
            300,
            windowHeight
        );

        // Draw window background
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);

        // Draw choices
        for (int i = 0; i < Choices.Count; i++)
        {
            Color color = i == SelectedIndex ? Color.Yellow : Color.White;
            Vector2 pos = new Vector2(windowRect.X + 20, windowRect.Y + 10 + i * itemHeight);

            if (i == SelectedIndex)
            {
                spriteBatch.DrawString(font, "> ", pos - new Vector2(15, 0), Color.Yellow);
            }

            spriteBatch.DrawString(font, Choices[i], pos, color);
        }
    }
}

/// <summary>
/// Number input window.
/// </summary>
public class NumberInputWindow
{
    public int MaxDigits { get; set; } = 4;
    public int Value { get; private set; }
    public Action<int>? OnComplete { get; set; }
    public bool IsComplete { get; private set; }

    private KeyboardState _previousKeyboard;

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        // Handle number input
        for (int i = 0; i <= 9; i++)
        {
            Keys key = Keys.D0 + i;
            if (keyboardState.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key))
            {
                int newValue = Value * 10 + i;
                if (newValue < Math.Pow(10, MaxDigits))
                    Value = newValue;
            }
        }

        // Backspace
        if (keyboardState.IsKeyDown(Keys.Back) && _previousKeyboard.IsKeyUp(Keys.Back))
        {
            Value /= 10;
        }

        // Confirm
        if (keyboardState.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            IsComplete = true;
        }

        _previousKeyboard = keyboardState;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var windowRect = new Rectangle(
            viewport.Width / 2 - 150,
            viewport.Height / 2 - 50,
            300,
            100
        );

        // Draw window
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);

        // Draw prompt
        Vector2 promptPos = new Vector2(windowRect.X + 20, windowRect.Y + 20);
        spriteBatch.DrawString(font, "Enter number:", promptPos, Color.White);

        // Draw value
        string valueStr = Value.ToString().PadLeft(MaxDigits, '_');
        Vector2 valuePos = new Vector2(windowRect.X + 20, windowRect.Y + 50);
        spriteBatch.DrawString(font, valueStr, valuePos, Color.Yellow);
    }
}
