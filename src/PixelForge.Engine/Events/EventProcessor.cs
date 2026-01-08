using Microsoft.Xna.Framework;
using PixelForge.Shared.Models;
using PixelForge.Engine.Core;
using System.Threading.Tasks;

namespace PixelForge.Engine.Events;

/// <summary>
/// Processes event commands and manages event execution.
/// </summary>
public class EventProcessor
{
    private readonly IGameContext _game;
    private readonly List<EventCommand> _commands = new();
    private readonly Stack<LoopFrame> _loopStack = new();
    private Dictionary<string, int> _labelIndices = new(StringComparer.Ordinal);
    private EventCommand? _currentCommand;
    private bool _waiting;
    private int _commandIndex;
    private readonly Dictionary<int, IEventCommandHandler> _handlers = new();

    public EventProcessor(IGameContext game)
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
        RegisterHandler(117, new CommonEventHandler());
        RegisterHandler(111, new ConditionalBranchHandler());
        RegisterHandler(112, new LoopHandler());
        RegisterHandler(113, new BreakLoopHandler());
        RegisterHandler(121, new ControlSwitchesHandler());
        RegisterHandler(122, new ControlVariablesHandler());
        RegisterHandler(123, new ControlSelfSwitchHandler());
        RegisterHandler(125, new ChangeGoldHandler());
        RegisterHandler(126, new ChangeItemsHandler());
        RegisterHandler(118, new LabelHandler());
        RegisterHandler(119, new JumpToLabelHandler());
        RegisterHandler(201, new TransferPlayerHandler());
        RegisterHandler(204, new ScrollMapHandler());
        RegisterHandler(205, new SetMovementRouteHandler());
        RegisterHandler(221, new FadeoutScreenHandler());
        RegisterHandler(222, new FadeinScreenHandler());
        RegisterHandler(224, new FlashScreenHandler());
        RegisterHandler(225, new ShakeScreenHandler());
        RegisterHandler(212, new ShowAnimationHandler());
        RegisterHandler(241, new PlayBgmHandler());
        RegisterHandler(250, new PlaySeHandler());
        RegisterHandler(301, new BattleProcessingHandler());
        RegisterHandler(302, new ShopProcessingHandler());
        RegisterHandler(355, new ScriptHandler());
        RegisterHandler(413, new RepeatAboveHandler());
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

        StartCommandList(activePage.Commands);
    }

    /// <summary>
    /// Execute a common event by ID.
    /// </summary>
    public void ExecuteCommonEvent(string commonEventId)
    {
        var commonEvent = _game.GetDatabase().GetCommonEvent(commonEventId);
        if (commonEvent == null)
            return;

        StartCommandList(commonEvent.Commands);
    }

    private void StartCommandList(List<EventCommand> commands)
    {
        _commands.Clear();
        _commands.AddRange(commands);
        _labelIndices = BuildLabelIndex(_commands);
        _loopStack.Clear();
        _currentCommand = null;
        _waiting = false;
        _commandIndex = 0;
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

        if (_commandIndex < _commands.Count)
        {
            _currentCommand = _commands[_commandIndex];
            _commandIndex++;
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
    /// Set waiting for a duration.
    /// </summary>
    public void WaitFor(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        _waiting = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(duration);
            _waiting = false;
        });
    }

    /// <summary>
    /// Jump to a label if it exists.
    /// </summary>
    public void JumpToLabel(string label)
    {
        if (_labelIndices.TryGetValue(label, out var index))
        {
            _commandIndex = Math.Clamp(index + 1, 0, _commands.Count);
        }
    }

    /// <summary>
    /// Mark the start of a loop.
    /// </summary>
    public void PushLoopStart(int indent)
    {
        _loopStack.Push(new LoopFrame { StartIndex = _commandIndex, Indent = indent });
    }

    /// <summary>
    /// Repeat the current loop.
    /// </summary>
    public void RepeatLoop(int indent)
    {
        if (_loopStack.TryPeek(out var frame) && frame.Indent == indent)
        {
            _commandIndex = Math.Clamp(frame.StartIndex, 0, _commands.Count);
        }
    }

    /// <summary>
    /// Break out of the current loop.
    /// </summary>
    public void BreakLoop()
    {
        if (!_loopStack.TryPop(out var frame))
            return;

        for (int i = _commandIndex; i < _commands.Count; i++)
        {
            if (_commands[i].Code == 413 && _commands[i].Indent == frame.Indent)
            {
                _commandIndex = Math.Clamp(i + 1, 0, _commands.Count);
                return;
            }
        }

        _commandIndex = _commands.Count;
    }

    /// <summary>
    /// Check if processor is busy.
    /// </summary>
    public bool IsBusy => _currentCommand != null || _commandIndex < _commands.Count;

    private static Dictionary<string, int> BuildLabelIndex(IEnumerable<EventCommand> commands)
    {
        var labels = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        foreach (var command in commands)
        {
            if (command.Code == 118 && command.Parameters.Count > 0)
            {
                var label = command.Parameters[0]?.ToString();
                if (!string.IsNullOrWhiteSpace(label) && !labels.ContainsKey(label))
                {
                    labels[label] = index;
                }
            }
            index++;
        }

        return labels;
    }

    private readonly record struct LoopFrame
    {
        public required int StartIndex { get; init; }
        public required int Indent { get; init; }
    }
}

/// <summary>
/// Context for event command execution.
/// </summary>
public class EventContext
{
    public required IGameContext Game { get; init; }
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
