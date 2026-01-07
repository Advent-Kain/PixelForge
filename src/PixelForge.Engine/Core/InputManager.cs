using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PixelForge.Engine.Core;

/// <summary>
/// Manages keyboard, mouse, and gamepad input.
/// </summary>
public class InputManager
{
    private KeyboardState _currentKeyboardState;
    private KeyboardState _previousKeyboardState;
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;
    private GamePadState _currentGamePadState;
    private GamePadState _previousGamePadState;

    public void Initialize()
    {
        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
    }

    public void Update()
    {
        _previousKeyboardState = _currentKeyboardState;
        _previousMouseState = _currentMouseState;
        _previousGamePadState = _currentGamePadState;

        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
    }

    // Keyboard methods
    public bool IsKeyDown(Keys key) => _currentKeyboardState.IsKeyDown(key);
    public bool IsKeyUp(Keys key) => _currentKeyboardState.IsKeyUp(key);
    public bool IsKeyPressed(Keys key) =>
        _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    public bool IsKeyReleased(Keys key) =>
        _currentKeyboardState.IsKeyUp(key) && _previousKeyboardState.IsKeyDown(key);

    // Mouse methods
    public Point MousePosition => _currentMouseState.Position;
    public bool IsLeftMouseButtonDown => _currentMouseState.LeftButton == ButtonState.Pressed;
    public bool IsRightMouseButtonDown => _currentMouseState.RightButton == ButtonState.Pressed;
    public bool IsLeftMouseButtonPressed =>
        _currentMouseState.LeftButton == ButtonState.Pressed &&
        _previousMouseState.LeftButton == ButtonState.Released;
    public bool IsRightMouseButtonPressed =>
        _currentMouseState.RightButton == ButtonState.Pressed &&
        _previousMouseState.RightButton == ButtonState.Released;

    // GamePad methods
    public bool IsButtonDown(Buttons button) => _currentGamePadState.IsButtonDown(button);
    public bool IsButtonPressed(Buttons button) =>
        _currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button);
    public Vector2 LeftThumbstick => _currentGamePadState.ThumbSticks.Left;
    public Vector2 RightThumbstick => _currentGamePadState.ThumbSticks.Right;

    // Direction input (combines keyboard and gamepad)
    public Vector2 GetDirectionInput()
    {
        Vector2 direction = Vector2.Zero;

        // Keyboard
        if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up))
            direction.Y -= 1;
        if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down))
            direction.Y += 1;
        if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left))
            direction.X -= 1;
        if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right))
            direction.X += 1;

        // GamePad
        if (_currentGamePadState.IsConnected)
        {
            Vector2 thumbstick = LeftThumbstick;
            if (thumbstick.Length() > 0.1f)
                direction = thumbstick;
        }

        if (direction.Length() > 1)
            direction.Normalize();

        return direction;
    }
}
