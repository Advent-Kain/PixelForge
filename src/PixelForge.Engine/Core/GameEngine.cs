using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Battle;
using PixelForge.Engine.Events;
using PixelForge.Engine.Graphics;
using PixelForge.Engine.Map;
using PixelForge.Engine.RPG;
using PixelForge.Engine.UI;
using PixelForge.Engine.UI.Battle;
using PixelForge.Shared.Models;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Core;

/// <summary>
/// Main game engine class that manages the game loop and core systems.
/// </summary>
public class GameEngine : Game, IGameContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture2D? _pixelTexture;
    private readonly GameState _gameState;
    private readonly InputManager _inputManager;
    private readonly MapManager _mapManager;
    private readonly ResourceManager _resourceManager;
    private readonly AnimationPlayer _animationPlayer;
    private readonly PartyManager _partyManager;
    private readonly InventoryManager _inventoryManager;
    private readonly GameDatabase _database;
    private readonly BattleSystem _battleSystem;
    private readonly BattleMenuManager _battleMenuManager;
    private readonly MenuManager _menuManager;
    private readonly EventProcessor _commonEventProcessor;

    public GameEngine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState();
        _inputManager = new InputManager();
        _resourceManager = new ResourceManager(Content);
        _mapManager = new MapManager(this);
        _partyManager = new PartyManager();
        _inventoryManager = new InventoryManager();
        _database = new GameDatabase();
        _animationPlayer = new AnimationPlayer(_resourceManager, _database);
        _battleSystem = new BattleSystem(this);
        _battleMenuManager = new BattleMenuManager(this);
        _menuManager = new MenuManager(this);
        _commonEventProcessor = new EventProcessor(this);

        // Default window size
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    /// <summary>
    /// Initialize the game engine.
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();

        // Set target frame rate to 60 FPS
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        IsFixedTimeStep = true;

        _inputManager.Initialize();

        LoadDatabase();
        ApplySystemConfig();
    }

    /// <summary>
    /// Load game content.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a 1x1 pixel texture for UI rendering
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Load default resources
        _resourceManager.Initialize(GraphicsDevice);

        // Try to load font from content, or use a placeholder
        try
        {
            _font = Content.Load<SpriteFont>("Fonts/Default");
        }
        catch
        {
            // Font not found - will be null and UI won't render text
        }
    }

    /// <summary>
    /// Update game logic.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Update input
        _inputManager.Update();

        // Update game state
        _gameState.Update(gameTime);
        _animationPlayer.Update(gameTime);

        // Handle battle updates
        if (_battleSystem.IsActive)
        {
            // Update battle menu if active
            if (_battleMenuManager.IsActive)
            {
                _battleMenuManager.Update(gameTime);
            }

            _battleSystem.Update(gameTime);
        }
        // Handle menu updates
        else if (_menuManager.IsOpen)
        {
            _menuManager.Update(gameTime);
        }
        // Update active map when not in battle or menu
        else
        {
            if (_mapManager.CurrentMap != null)
            {
                _mapManager.Update(gameTime, _inputManager);
            }

            // Check for menu open (not during battle)
            _menuManager.Update(gameTime);
        }

        TryRunCommonEvents(gameTime);

        base.Update(gameTime);
    }

    /// <summary>
    /// Render the game.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_spriteBatch == null)
        {
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );

        // Draw map (even during battle as background)
        if (_mapManager.CurrentMap != null)
        {
            _mapManager.Draw(_spriteBatch, gameTime);
        }

        _animationPlayer.Draw(_spriteBatch);

        // Draw battle UI
        if (_battleSystem.IsActive && _font != null && _pixelTexture != null)
        {
            _battleMenuManager.Draw(_spriteBatch, _font, _pixelTexture);
        }

        // Draw menu UI
        if (_menuManager.IsOpen && _font != null && _pixelTexture != null)
        {
            _menuManager.Draw(_spriteBatch, _font, _pixelTexture);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Load a map by ID.
    /// </summary>
    public void LoadMap(string mapId)
    {
        _gameState.CurrentMapId = mapId;
        _mapManager.LoadMap(mapId);
    }

    /// <summary>
    /// Get the current game state.
    /// </summary>
    public GameState GetGameState() => _gameState;

    /// <summary>
    /// Get the resource manager.
    /// </summary>
    public ResourceManager GetResourceManager() => _resourceManager;

    /// <summary>
    /// Get the map manager.
    /// </summary>
    public MapManager GetMapManager() => _mapManager;

    /// <summary>
    /// Get the party manager.
    /// </summary>
    public PartyManager GetPartyManager() => _partyManager;

    /// <summary>
    /// Get the database.
    /// </summary>
    public GameDatabase GetDatabase() => _database;

    /// <summary>
    /// Get the inventory manager.
    /// </summary>
    public InventoryManager GetInventoryManager() => _inventoryManager;

    /// <summary>
    /// Get the battle system.
    /// </summary>
    public BattleSystem GetBattleSystem() => _battleSystem;

    /// <summary>
    /// Get the battle menu manager.
    /// </summary>
    public BattleMenuManager GetBattleMenuManager() => _battleMenuManager;

    /// <summary>
    /// Get the menu manager.
    /// </summary>
    public MenuManager GetMenuManager() => _menuManager;

    public void PlayAnimation(string animationId, Vector2 position)
    {
        _animationPlayer.Play(animationId, position);
    }

    private void LoadDatabase()
    {
        // Placeholder for database load logic when runtime content loading is implemented.
    }

    private void TryRunCommonEvents(GameTime gameTime)
    {
        if (_commonEventProcessor.IsBusy)
        {
            _commonEventProcessor.Update(gameTime);
            return;
        }

        foreach (var commonEvent in _database.CommonEvents.Values)
        {
            if (commonEvent.Trigger != CommonEventTrigger.Autorun
                && commonEvent.Trigger != CommonEventTrigger.Parallel)
            {
                continue;
            }

            if (commonEvent.SwitchId.HasValue
                && !_gameState.GetSwitch(commonEvent.SwitchId.Value))
            {
                continue;
            }

            _commonEventProcessor.ExecuteCommonEvent(commonEvent.Id);
            _commonEventProcessor.Update(gameTime);
            break;
        }
    }

    private void ApplySystemConfig()
    {
        var projectFile = LoadProjectFile();
        if (projectFile?.GameSettings == null)
            return;

        var settings = projectFile.GameSettings;

        if (!string.IsNullOrWhiteSpace(settings.Title))
        {
            Window.Title = settings.Title;
        }

        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            _graphics.PreferredBackBufferWidth = settings.WindowWidth;
            _graphics.PreferredBackBufferHeight = settings.WindowHeight;
            _graphics.ApplyChanges();
        }

        if (Enum.TryParse<BattleMode>(settings.BattleMode, true, out var battleMode))
        {
            _battleSystem.SetDefaultMode(battleMode);
        }
    }

    private ProjectFile? LoadProjectFile()
    {
        try
        {
            var projectFilePath = Path.Combine(AppContext.BaseDirectory, "project.json");
            if (!File.Exists(projectFilePath))
                return null;

            var json = File.ReadAllText(projectFilePath);
            return JsonSerializer.Deserialize<ProjectFile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
