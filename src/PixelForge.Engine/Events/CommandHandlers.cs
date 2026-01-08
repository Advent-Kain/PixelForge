using Microsoft.Xna.Framework.Media;
using PixelForge.Engine.UI;

namespace PixelForge.Engine.Events;

/// <summary>
/// Show Message command handler (Code: 101).
/// </summary>
public class ShowMessageHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        _complete = false;
        var message = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0].ToString() ?? string.Empty
            : string.Empty;

        // Show message window
        MessageManager.Instance.ShowMessage(message, () => _complete = true);
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Show Choices command handler (Code: 102).
/// </summary>
public class ShowChoicesHandler : IEventCommandHandler
{
    private bool _complete;
    private int _result;

    public void Execute(EventContext context)
    {
        _complete = false;
        var choices = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0] as List<string> ?? new List<string>()
            : new List<string>();

        MessageManager.Instance.ShowChoices(choices, (choice) =>
        {
            _result = choice;
            _complete = true;
        });
    }

    public bool IsComplete() => _complete;
    public int GetResult() => _result;
}

/// <summary>
/// Input Number command handler (Code: 103).
/// </summary>
public class InputNumberHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        _complete = false;
        var variableId = context.Command.Parameters.Count > 0
            ? Convert.ToInt32(context.Command.Parameters[0])
            : 0;
        var digits = context.Command.Parameters.Count > 1
            ? Convert.ToInt32(context.Command.Parameters[1])
            : 4;

        MessageManager.Instance.ShowNumberInput(digits, (number) =>
        {
            context.Game.GetGameState().SetVariable(variableId, number);
            _complete = true;
        });
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Conditional Branch command handler (Code: 111).
/// </summary>
public class ConditionalBranchHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        // Conditional logic handled by interpreter
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Loop command handler (Code: 112).
/// </summary>
public class LoopHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        context.Processor.PushLoopStart(context.Command.Indent);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Break Loop command handler (Code: 113).
/// </summary>
public class BreakLoopHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        context.Processor.BreakLoop();
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Control Switches command handler (Code: 121).
/// </summary>
public class ControlSwitchesHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var startId = Convert.ToInt32(context.Command.Parameters[0]);
        var endId = Convert.ToInt32(context.Command.Parameters[1]);
        var value = Convert.ToBoolean(context.Command.Parameters[2]);

        var gameState = context.Game.GetGameState();
        for (int i = startId; i <= endId; i++)
        {
            gameState.SetSwitch(i, value);
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Control Variables command handler (Code: 122).
/// </summary>
public class ControlVariablesHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var startId = Convert.ToInt32(context.Command.Parameters[0]);
        var endId = Convert.ToInt32(context.Command.Parameters[1]);
        var operation = Convert.ToInt32(context.Command.Parameters[2]); // 0=set, 1=add, etc.
        var value = Convert.ToInt32(context.Command.Parameters[3]);

        var gameState = context.Game.GetGameState();
        for (int i = startId; i <= endId; i++)
        {
            int currentValue = gameState.GetVariable(i);
            int newValue = operation switch
            {
                0 => value, // Set
                1 => currentValue + value, // Add
                2 => currentValue - value, // Sub
                3 => currentValue * value, // Mul
                4 => value != 0 ? currentValue / value : currentValue, // Div
                5 => value != 0 ? currentValue % value : currentValue, // Mod
                _ => currentValue
            };
            gameState.SetVariable(i, newValue);
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Control Self Switch command handler (Code: 123).
/// </summary>
public class ControlSelfSwitchHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var selfSwitch = context.Command.Parameters[0].ToString() ?? "A";
        var value = Convert.ToBoolean(context.Command.Parameters[1]);

        var mapId = context.Game.GetGameState().CurrentMapId ?? string.Empty;
        // TODO: Get event ID from context
        var eventId = "event_id";

        context.Game.GetGameState().SetSelfSwitch(mapId, eventId, selfSwitch, value);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Label command handler (Code: 118).
/// </summary>
public class LabelHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        // Labels are used for flow control and do not execute logic.
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Jump to Label command handler (Code: 119).
/// </summary>
public class JumpToLabelHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var label = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0]?.ToString() ?? string.Empty
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(label))
        {
            context.Processor.JumpToLabel(label);
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Change Gold command handler (Code: 125).
/// </summary>
public class ChangeGoldHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var operation = Convert.ToInt32(context.Command.Parameters[0]); // 0=increase, 1=decrease
        var amount = Convert.ToInt32(context.Command.Parameters[1]);

        var gameState = context.Game.GetGameState();
        if (operation == 0)
            gameState.PartyGold += amount;
        else
            gameState.PartyGold = Math.Max(0, gameState.PartyGold - amount);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Change Items command handler (Code: 126).
/// </summary>
public class ChangeItemsHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var itemId = context.Command.Parameters[0].ToString() ?? string.Empty;
        var operation = Convert.ToInt32(context.Command.Parameters[1]); // 0=increase, 1=decrease
        var amount = Convert.ToInt32(context.Command.Parameters[2]);

        var gameState = context.Game.GetGameState();
        if (operation == 0)
            gameState.AddItem(itemId, amount);
        else
            gameState.RemoveItem(itemId, amount);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Transfer Player command handler (Code: 201).
/// </summary>
public class TransferPlayerHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        var mapId = context.Command.Parameters[0].ToString() ?? string.Empty;
        var x = Convert.ToInt32(context.Command.Parameters[1]);
        var y = Convert.ToInt32(context.Command.Parameters[2]);

        context.Game.LoadMap(mapId);
        // TODO: Set player position
        _complete = true;
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Scroll Map command handler (Code: 204).
/// </summary>
public class ScrollMapHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        // TODO: Implement map scrolling
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Set Movement Route command handler (Code: 205).
/// </summary>
public class SetMovementRouteHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        // TODO: Implement movement routes
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Fadeout Screen command handler (Code: 221).
/// </summary>
public class FadeoutScreenHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = context.Command.Parameters.Count > 0
            ? Convert.ToInt32(context.Command.Parameters[0])
            : 0;

        context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Fadein Screen command handler (Code: 222).
/// </summary>
public class FadeinScreenHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = context.Command.Parameters.Count > 0
            ? Convert.ToInt32(context.Command.Parameters[0])
            : 0;

        context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Flash Screen command handler (Code: 224).
/// </summary>
public class FlashScreenHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = context.Command.Parameters.Count > 1
            ? Convert.ToInt32(context.Command.Parameters[1])
            : 0;

        context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Shake Screen command handler (Code: 225).
/// </summary>
public class ShakeScreenHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = context.Command.Parameters.Count > 2
            ? Convert.ToInt32(context.Command.Parameters[2])
            : 0;

        context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Play BGM command handler (Code: 241).
/// </summary>
public class PlayBgmHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var path = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0]?.ToString() ?? string.Empty
            : string.Empty;
        var volume = context.Command.Parameters.Count > 1
            ? Convert.ToSingle(context.Command.Parameters[1])
            : 100f;

        if (string.IsNullOrWhiteSpace(path))
            return;

        var song = context.Game.GetResourceManager().LoadSong(path);
        if (song == null)
            return;

        MediaPlayer.Volume = Math.Clamp(volume / 100f, 0f, 1f);
        MediaPlayer.Play(song);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Play SE command handler (Code: 250).
/// </summary>
public class PlaySeHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var path = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0]?.ToString() ?? string.Empty
            : string.Empty;
        var volume = context.Command.Parameters.Count > 1
            ? Convert.ToSingle(context.Command.Parameters[1])
            : 100f;
        var pitch = context.Command.Parameters.Count > 2
            ? Convert.ToSingle(context.Command.Parameters[2])
            : 0f;
        var pan = context.Command.Parameters.Count > 3
            ? Convert.ToSingle(context.Command.Parameters[3])
            : 0f;

        if (string.IsNullOrWhiteSpace(path))
            return;

        var effect = context.Game.GetResourceManager().LoadSoundEffect(path);
        if (effect == null)
            return;

        var clampedVolume = Math.Clamp(volume / 100f, 0f, 1f);
        var clampedPitch = Math.Clamp(pitch, -1f, 1f);
        var clampedPan = Math.Clamp(pan, -1f, 1f);
        effect.Play(clampedVolume, clampedPitch, clampedPan);
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Battle Processing command handler (Code: 301).
/// </summary>
public class BattleProcessingHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        var troopId = context.Command.Parameters[0].ToString() ?? string.Empty;
        // TODO: Start battle
        _complete = true;
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Shop Processing command handler (Code: 302).
/// </summary>
public class ShopProcessingHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        // TODO: Open shop
        _complete = true;
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Script command handler (Code: 355).
/// </summary>
public class ScriptHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        var script = context.Command.Parameters[0].ToString() ?? string.Empty;
        // TODO: Execute C# script
        _complete = true;
    }

    public bool IsComplete() => _complete;
}

/// <summary>
/// Repeat Above command handler (Code: 413).
/// </summary>
public class RepeatAboveHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        context.Processor.RepeatLoop(context.Command.Indent);
    }

    public bool IsComplete() => true;
}
