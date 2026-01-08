using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Graphics;
using PixelForge.Engine.Map;
using PixelForge.Engine.RPG;

namespace PixelForge.Engine.Core;

/// <summary>
/// Main game engine class that manages the game loop and core systems.
/// </summary>
public class GameEngine : Game, IGameContext
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private readonly GameState _gameState;
    private readonly InputManager _inputManager;
    private readonly MapManager _mapManager;
    private readonly ResourceManager _resourceManager;
    private readonly PartyManager _partyManager;
    private readonly GameDatabase _database;

    public GameEngine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState();
        _inputManager = new InputManager();
        _resourceManager = new ResourceManager(Content);
        _mapManager = new MapManager(_resourceManager);
        _partyManager = new PartyManager();
        _inventoryManager = new InventoryManager();

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
    }

    /// <summary>
    /// Load game content.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load default resources
        _resourceManager.Initialize(GraphicsDevice);
    }

    /// <summary>
    /// Update game logic.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Update input
        _inputManager.Update();

        // Exit on Escape key
        if (_inputManager.IsKeyPressed(Keys.Escape))
            Exit();

        // Update game state
        _gameState.Update(gameTime);

        // Update active map
        if (_mapManager.CurrentMap != null)
        {
            _mapManager.Update(gameTime, _inputManager);
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Render the game.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_spriteBatch != null && _mapManager.CurrentMap != null)
        {
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                null
            );

            _mapManager.Draw(_spriteBatch, gameTime);

            _spriteBatch.End();
        }

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
    /// Get the inventory manager.
    /// </summary>
    public InventoryManager GetInventoryManager() => _inventoryManager;
}
