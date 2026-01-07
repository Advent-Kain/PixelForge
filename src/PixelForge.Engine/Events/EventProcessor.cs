using Microsoft.Xna.Framework;
using PixelForge.Shared.Models;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.Events;

/// <summary>
/// Processes event commands and manages event execution.
/// </summary>
public class EventProcessor
{
    private readonly GameEngine _game;
    private readonly Queue<EventCommand> _commandQueue = new();
    private EventCommand? _currentCommand;
    private bool _waiting;
    private readonly Dictionary<int, IEventCommandHandler> _handlers = new();

    public EventProcessor(GameEngine game)
    {
        _game = game;
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Register default command handlers.
    /// </summary>
    private void RegisterDefaultHandlers()
    {
        RegisterHandler(101, new ShowMessageHandler());
        RegisterHandler(102, new ShowChoicesHandler());
        RegisterHandler(103, new InputNumberHandler());
        RegisterHandler(111, new ConditionalBranchHandler());
        RegisterHandler(121, new ControlSwitchesHandler());
        RegisterHandler(122, new ControlVariablesHandler());
        RegisterHandler(123, new ControlSelfSwitchHandler());
        RegisterHandler(125, new ChangeGoldHandler());
        RegisterHandler(126, new ChangeItemsHandler());
        RegisterHandler(201, new TransferPlayerHandler());
        RegisterHandler(204, new ScrollMapHandler());
        RegisterHandler(205, new SetMovementRouteHandler());
        RegisterHandler(301, new BattleProcessingHandler());
        RegisterHandler(302, new ShopProcessingHandler());
        RegisterHandler(355, new ScriptHandler());
    }

    /// <summary>
    /// Register a command handler.
    /// </summary>
    public void RegisterHandler(int code, IEventCommandHandler handler)
    {
        _handlers[code] = handler;
    }

    /// <summary>
    /// Execute an event.
    /// </summary>
    public void ExecuteEvent(MapEvent mapEvent)
    {
        var activePage = GetActivePage(mapEvent);
        if (activePage == null)
            return;

        foreach (var command in activePage.Commands)
        {
            _commandQueue.Enqueue(command);
        }
    }

    /// <summary>
    /// Update event processing.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_waiting)
            return;

        if (_currentCommand != null)
        {
            var handler = GetHandler(_currentCommand.Code);
            if (handler != null && handler.IsComplete())
            {
                _currentCommand = null;
            }
            else
            {
                return;
            }
        }

        if (_commandQueue.Count > 0)
        {
            _currentCommand = _commandQueue.Dequeue();
            ExecuteCommand(_currentCommand);
        }
    }

    /// <summary>
    /// Execute a single command.
    /// </summary>
    private void ExecuteCommand(EventCommand command)
    {
        var handler = GetHandler(command.Code);
        if (handler != null)
        {
            var context = new EventContext
            {
                Game = _game,
                Command = command,
                Processor = this
            };
            handler.Execute(context);
        }
    }

    /// <summary>
    /// Get handler for command code.
    /// </summary>
    private IEventCommandHandler? GetHandler(int code)
    {
        return _handlers.TryGetValue(code, out var handler) ? handler : null;
    }

    /// <summary>
    /// Get the active page for an event.
    /// </summary>
    private EventPage? GetActivePage(MapEvent mapEvent)
    {
        foreach (var page in mapEvent.Pages)
        {
            if (CheckConditions(page.Conditions))
                return page;
        }
        return null;
    }

    /// <summary>
    /// Check if page conditions are met.
    /// </summary>
    private bool CheckConditions(EventConditions conditions)
    {
        var gameState = _game.GetGameState();

        if (conditions.Switch1.HasValue && !gameState.GetSwitch(conditions.Switch1.Value))
            return false;

        if (conditions.Switch2.HasValue && !gameState.GetSwitch(conditions.Switch2.Value))
            return false;

        if (conditions.Variable.HasValue && conditions.VariableValue.HasValue)
        {
            if (gameState.GetVariable(conditions.Variable.Value) < conditions.VariableValue.Value)
                return false;
        }

        if (!string.IsNullOrEmpty(conditions.Item) && !gameState.HasItem(conditions.Item))
            return false;

        return true;
    }

    /// <summary>
    /// Set waiting state.
    /// </summary>
    public void SetWaiting(bool waiting)
    {
        _waiting = waiting;
    }

    /// <summary>
    /// Check if processor is busy.
    /// </summary>
    public bool IsBusy => _currentCommand != null || _commandQueue.Count > 0;
}

/// <summary>
/// Context for event command execution.
/// </summary>
public class EventContext
{
    public required GameEngine Game { get; init; }
    public required EventCommand Command { get; init; }
    public required EventProcessor Processor { get; init; }
}

/// <summary>
/// Interface for event command handlers.
/// </summary>
public interface IEventCommandHandler
{
    void Execute(EventContext context);
    bool IsComplete();
}
