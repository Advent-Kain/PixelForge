using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;

namespace PixelForge.Engine.UI;

/// <summary>
/// Manages the game menu system.
/// </summary>
public class MenuManager
{
    private readonly GameEngine _game;
    private MenuState _currentState = MenuState.Closed;
    private IMenu? _currentMenu;

    // Menu instances
    private MainMenu? _mainMenu;
    private InventoryMenu? _inventoryMenu;
    private EquipmentMenu? _equipmentMenu;
    private SkillsMenu? _skillsMenu;
    private StatusMenu? _statusMenu;
    private SaveLoadMenu? _saveMenu;
    private SaveLoadMenu? _loadMenu;

    private KeyboardState _previousKeyboard;

    public MenuManager(GameEngine game)
    {
        _game = game;
        InitializeMenus();
    }

    public bool IsOpen => _currentState != MenuState.Closed;

    /// <summary>
    /// Initialize all menu instances.
    /// </summary>
    private void InitializeMenus()
    {
        _mainMenu = new MainMenu(this);
        _inventoryMenu = new InventoryMenu(_game);
        _equipmentMenu = new EquipmentMenu(_game);
        _skillsMenu = new SkillsMenu(_game);
        _statusMenu = new StatusMenu(_game);
        _saveMenu = new SaveLoadMenu(_game, true);
        _loadMenu = new SaveLoadMenu(_game, false);
    }

    /// <summary>
    /// Open the main menu.
    /// </summary>
    public void OpenMenu()
    {
        if (_currentState != MenuState.Closed)
            return;

        _currentState = MenuState.Main;
        _currentMenu = _mainMenu;
        _currentMenu?.OnOpen();
    }

    /// <summary>
    /// Close the menu.
    /// </summary>
    public void CloseMenu()
    {
        _currentMenu?.OnClose();
        _currentState = MenuState.Closed;
        _currentMenu = null;
    }

    /// <summary>
    /// Navigate to a specific menu.
    /// </summary>
    public void NavigateTo(MenuState state)
    {
        _currentMenu?.OnClose();

        _currentState = state;
        _currentMenu = state switch
        {
            MenuState.Main => _mainMenu,
            MenuState.Inventory => _inventoryMenu,
            MenuState.Equipment => _equipmentMenu,
            MenuState.Skills => _skillsMenu,
            MenuState.Status => _statusMenu,
            MenuState.Save => _saveMenu,
            MenuState.Load => _loadMenu,
            _ => null
        };

        _currentMenu?.OnOpen();
    }

    /// <summary>
    /// Update the menu.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_currentState == MenuState.Closed)
        {
            // Check for menu open input
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
            {
                OpenMenu();
            }
            _previousKeyboard = keyboard;
            return;
        }

        _currentMenu?.Update(gameTime);

        // Check for menu close
        var currentKeyboard = Keyboard.GetState();
        if (currentKeyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
        {
            if (_currentState == MenuState.Main)
            {
                CloseMenu();
            }
            else
            {
                NavigateTo(MenuState.Main);
            }
        }
        _previousKeyboard = currentKeyboard;
    }

    /// <summary>
    /// Draw the menu.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        if (_currentState == MenuState.Closed)
            return;

        _currentMenu?.Draw(spriteBatch, font, pixelTexture);
    }
}

/// <summary>
/// Menu states.
/// </summary>
public enum MenuState
{
    Closed,
    Main,
    Inventory,
    Equipment,
    Skills,
    Status,
    Save,
    Load,
    Options
}

/// <summary>
/// Interface for menu screens.
/// </summary>
public interface IMenu
{
    void OnOpen();
    void OnClose();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture);
}
