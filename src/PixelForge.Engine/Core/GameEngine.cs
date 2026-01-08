using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Events;
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
    private readonly InventoryManager _inventoryManager;
    private readonly GameDatabase _database;

    public GameEngine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState();
        _inputManager = new InputManager();
        _resourceManager = new ResourceManager(Content);
        _database = new GameDatabase();
        _mapManager = new MapManager(_resourceManager, _gameState, _database);
        _partyManager = new PartyManager();
        _inventoryManager = new InventoryManager();
        _mapManager = new MapManager(this);
        _partyManager = new PartyManager();
        _inventoryManager = new InventoryManager();
        _animationPlayer = new AnimationPlayer(_resourceManager, _database);

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

        _eventProcessor.Update(gameTime);
        _animationPlayer.Update(gameTime);

        // Update active map
        if (_mapManager.CurrentMap != null)
        {
            _mapManager.Update(gameTime, _inputManager);
        }

        TryRunCommonEvents();

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
            _animationPlayer.Draw(_spriteBatch);

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
    /// Get the database.
    /// </summary>
    public GameDatabase GetDatabase() => _database;

    /// <summary>
    /// Get the inventory manager.
    /// </summary>
    public InventoryManager GetInventoryManager() => _inventoryManager;

    public void PlayAnimation(string animationId, Vector2 position)
    {
        _animationPlayer.Play(animationId, position);
    }

    private void LoadDatabase()
    {
        _database.LoadFromDirectory("Database");
        _mapManager.RefreshTilesets();
    }

    private void ApplySystemConfig()
    {
        var systemConfig = _database.SystemConfig;
        if (systemConfig.WindowWidth > 0 && systemConfig.WindowHeight > 0)
        {
            _graphics.PreferredBackBufferWidth = systemConfig.WindowWidth;
            _graphics.PreferredBackBufferHeight = systemConfig.WindowHeight;
            _graphics.ApplyChanges();
        }

        if (!string.IsNullOrWhiteSpace(systemConfig.GameTitle))
        {
            Window.Title = systemConfig.GameTitle;
        }

        _partyManager.Clear();
        _gameState.PartyMembers.Clear();

        foreach (var actorId in systemConfig.StartingParty)
        {
            var actorData = _database.GetActor(actorId);
            if (actorData == null)
                continue;

            var classData = !string.IsNullOrEmpty(actorData.ClassId)
                ? _database.GetClass(actorData.ClassId)
                : null;

            var actor = new GameActor
            {
                ActorId = actorId,
                ActorData = actorData,
                ClassData = classData,
                Database = _database,
                Level = actorData.InitialLevel
            };

            var stats = actor.GetCurrentStats();
            actor.CurrentHp = stats.MaxHp;
            actor.CurrentMp = stats.MaxMp;
            actor.CurrentTp = 0;

            _partyManager.AddActor(actor);
            _gameState.PartyMembers.Add(actorId);
        }

        _partyManager.Gold = systemConfig.StartingGold;
        _gameState.PartyGold = systemConfig.StartingGold;

        _gameState.PlayerX = systemConfig.StartingPosition.X;
        _gameState.PlayerY = systemConfig.StartingPosition.Y;

        if (!string.IsNullOrWhiteSpace(systemConfig.StartingMapId))
        {
            LoadMap(systemConfig.StartingMapId);
        }
    }

    private void TryRunCommonEvents()
    {
        if (_eventProcessor.IsBusy)
            return;

        foreach (var commonEvent in _database.CommonEvents.Values)
        {
            if (commonEvent.Trigger == PixelForge.Shared.Models.Database.CommonEventTrigger.None)
                continue;

            if (commonEvent.SwitchId.HasValue && !_gameState.GetSwitch(commonEvent.SwitchId.Value))
                continue;

            _eventProcessor.ExecuteCommonEvent(commonEvent.Id);
            break;
        }
    }
}
