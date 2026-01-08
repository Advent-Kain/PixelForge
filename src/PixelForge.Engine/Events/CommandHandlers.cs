using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using PixelForge.Engine.Core;
using PixelForge.Engine.UI;
using PixelForge.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

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
/// Common Event command handler (Code: 117).
/// </summary>
public class CommonEventHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var commonEventId = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0]?.ToString() ?? string.Empty
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(commonEventId))
        {
            context.Processor.ExecuteCommonEvent(commonEventId);
        }
    }

    public bool IsComplete() => true;
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
/// Exit Event Processing command handler (Code: 115).
/// </summary>
public class ExitEventProcessingHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        context.Processor.EndEventProcessing();
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
/// Change Party Member command handler (Code: 129).
/// </summary>
public class ChangePartyMemberHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var actorId = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), string.Empty);
        var operation = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(1), 0); // 0=add, 1=remove

        if (string.IsNullOrWhiteSpace(actorId))
            return;

        var party = context.Game.GetGameState().PartyMembers;
        if (operation == 0)
        {
            if (!party.Contains(actorId))
                party.Add(actorId);
        }
        else
        {
            party.Remove(actorId);
        }
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
        var targetId = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), "player");
        var routeData = context.Command.Parameters.ElementAtOrDefault(1);
        var route = EventCommandParameterReader.GetMoveRoute(routeData);

        if (route == null)
            return;

        context.Game.GetGameState().MovementRoutes[targetId] = route;

        if (route.Wait)
        {
            var durationFrames = Math.Max(route.Commands.Count, 1);
            context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Erase Event command handler (Code: 214).
/// </summary>
public class EraseEventHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        context.Processor.EndEventProcessing();
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
/// Tint Screen command handler (Code: 223).
/// </summary>
public class TintScreenHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(1), 0);
        var wait = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(2), false);

        if (wait && durationFrames > 0)
        {
            context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
        }
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
/// Wait command handler (Code: 230).
/// </summary>
public class WaitHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        if (durationFrames > 0)
        {
            context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
        }
/// Show Animation command handler (Code: 212).
/// </summary>
public class ShowAnimationHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var animationId = context.Command.Parameters.Count > 0
            ? context.Command.Parameters[0]?.ToString() ?? string.Empty
            : string.Empty;
        var x = context.Command.Parameters.Count > 1
            ? Convert.ToSingle(context.Command.Parameters[1])
            : 0f;
        var y = context.Command.Parameters.Count > 2
            ? Convert.ToSingle(context.Command.Parameters[2])
            : 0f;

        if (string.IsNullOrWhiteSpace(animationId))
            return;

        context.Game.PlayAnimation(animationId, new Microsoft.Xna.Framework.Vector2(x, y));
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
/// Fadeout BGM command handler (Code: 242).
/// </summary>
public class FadeoutBgmHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var duration = TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0));

        if (duration > TimeSpan.Zero)
        {
            context.Processor.WaitFor(duration);
            _ = Task.Run(async () =>
            {
                await Task.Delay(duration);
                MediaPlayer.Stop();
            });
        }
        else
        {
            MediaPlayer.Stop();
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Play BGS command handler (Code: 245).
/// </summary>
public class PlayBgsHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var path = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), string.Empty);
        var volume = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(1), 100f);
        var pitch = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(2), 0f);
        var pan = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(3), 0f);

        if (string.IsNullOrWhiteSpace(path))
            return;

        var effect = context.Game.GetResourceManager().LoadSoundEffect(path);
        if (effect == null)
            return;

        EventAudioState.BgsInstance?.Stop();
        EventAudioState.BgsInstance?.Dispose();

        var instance = effect.CreateInstance();
        instance.IsLooped = true;
        instance.Volume = Math.Clamp(volume / 100f, 0f, 1f);
        instance.Pitch = Math.Clamp(pitch, -1f, 1f);
        instance.Pan = Math.Clamp(pan, -1f, 1f);
        instance.Play();

        EventAudioState.BgsInstance = instance;
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Fadeout BGS command handler (Code: 246).
/// </summary>
public class FadeoutBgsHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var duration = TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0));
        var instance = EventAudioState.BgsInstance;
        if (instance == null)
            return;

        if (duration > TimeSpan.Zero)
        {
            context.Processor.WaitFor(duration);
            _ = Task.Run(async () =>
            {
                await Task.Delay(duration);
                instance.Stop();
                instance.Dispose();
            });
        }
        else
        {
            instance.Stop();
            instance.Dispose();
        }

        EventAudioState.BgsInstance = null;
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Play ME command handler (Code: 249).
/// </summary>
public class PlayMeHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var path = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), string.Empty);
        var volume = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(1), 100f);
        var pitch = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(2), 0f);
        var pan = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(3), 0f);

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
/// Stop SE command handler (Code: 251).
/// </summary>
public class StopSeHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        // SoundEffect.Stop() is not globally available, so this is a no-op placeholder.
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Show Picture command handler (Code: 231).
/// </summary>
public class ShowPictureHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var pictureId = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var name = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(1), string.Empty);
        var origin = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(2), 0);
        var x = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(3), 0f);
        var y = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(4), 0f);
        var scaleX = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(5), 100f);
        var scaleY = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(6), 100f);
        var opacity = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(7), 255);
        var blendMode = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(8), 0);

        if (pictureId <= 0)
            return;

        context.Game.GetGameState().Pictures[pictureId] = new PictureState
        {
            Id = pictureId,
            Name = name,
            Origin = origin,
            X = x,
            Y = y,
            ScaleX = scaleX,
            ScaleY = scaleY,
            Opacity = opacity,
            BlendMode = blendMode,
            Rotation = 0f,
            RotationSpeed = 0f,
            Tone = Vector4.Zero
        };
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Move Picture command handler (Code: 232).
/// </summary>
public class MovePictureHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var pictureId = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var origin = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(1), 0);
        var x = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(2), 0f);
        var y = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(3), 0f);
        var scaleX = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(4), 100f);
        var scaleY = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(5), 100f);
        var opacity = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(6), 255);
        var blendMode = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(7), 0);
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(8), 0);
        var wait = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(9), false);

        if (pictureId <= 0)
            return;

        if (!context.Game.GetGameState().Pictures.TryGetValue(pictureId, out var picture))
        {
            picture = new PictureState { Id = pictureId };
            context.Game.GetGameState().Pictures[pictureId] = picture;
        }

        picture.Origin = origin;
        picture.X = x;
        picture.Y = y;
        picture.ScaleX = scaleX;
        picture.ScaleY = scaleY;
        picture.Opacity = opacity;
        picture.BlendMode = blendMode;

        if (wait && durationFrames > 0)
        {
            context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Rotate Picture command handler (Code: 233).
/// </summary>
public class RotatePictureHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var pictureId = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var speed = EventCommandParameterReader.GetFloat(context.Command.Parameters.ElementAtOrDefault(1), 0f);

        if (pictureId <= 0)
            return;

        if (context.Game.GetGameState().Pictures.TryGetValue(pictureId, out var picture))
        {
            picture.RotationSpeed = speed;
        }
    }

    public bool IsComplete() => true;
}

/// <summary>
/// Tint Picture command handler (Code: 234).
/// </summary>
public class TintPictureHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var pictureId = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        var tone = ParseTone(context.Command.Parameters.ElementAtOrDefault(1));
        var durationFrames = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(2), 0);
        var wait = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(3), false);

        if (pictureId <= 0)
            return;

        if (context.Game.GetGameState().Pictures.TryGetValue(pictureId, out var picture))
        {
            picture.Tone = tone;
        }

        if (wait && durationFrames > 0)
        {
            context.Processor.WaitFor(TimeSpan.FromMilliseconds(durationFrames * (1000.0 / 60.0)));
        }
    }

    public bool IsComplete() => true;

    private static Vector4 ParseTone(object? toneData)
    {
        var toneValues = EventCommandParameterReader.GetList(toneData);
        var red = toneValues.Count > 0 ? EventCommandParameterReader.GetFloat(toneValues[0], 0f) : 0f;
        var green = toneValues.Count > 1 ? EventCommandParameterReader.GetFloat(toneValues[1], 0f) : 0f;
        var blue = toneValues.Count > 2 ? EventCommandParameterReader.GetFloat(toneValues[2], 0f) : 0f;
        var gray = toneValues.Count > 3 ? EventCommandParameterReader.GetFloat(toneValues[3], 0f) : 0f;
        return new Vector4(red, green, blue, gray);
    }
}

/// <summary>
/// Erase Picture command handler (Code: 235).
/// </summary>
public class ErasePictureHandler : IEventCommandHandler
{
    public void Execute(EventContext context)
    {
        var pictureId = EventCommandParameterReader.GetInt(context.Command.Parameters.ElementAtOrDefault(0), 0);
        if (pictureId <= 0)
            return;

        context.Game.GetGameState().Pictures.Remove(pictureId);
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
        var troopId = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), string.Empty);
        var canEscape = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(1), false);
        var canLose = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(2), false);

        if (context.Game is IEventCommandHost host)
        {
            host.StartBattle(troopId, canEscape, canLose);
        }

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
        var goodsParameter = context.Command.Parameters.ElementAtOrDefault(0);
        var purchaseOnly = EventCommandParameterReader.GetBool(context.Command.Parameters.ElementAtOrDefault(1), false);
        var goods = ParseShopGoods(goodsParameter);

        if (context.Game is IEventCommandHost host)
        {
            host.OpenShop(goods, purchaseOnly);
        }

        _complete = true;
    }

    public bool IsComplete() => _complete;

    private static IReadOnlyList<ShopGood> ParseShopGoods(object? goodsParameter)
    {
        if (goodsParameter is IReadOnlyList<ShopGood> goods)
            return goods;

        var goodsList = new List<ShopGood>();
        var items = EventCommandParameterReader.GetList(goodsParameter);
        foreach (var item in items)
        {
            if (item is ShopGood shopGood)
            {
                goodsList.Add(shopGood);
                continue;
            }

            var entry = EventCommandParameterReader.GetList(item);
            if (entry.Count < 4)
                continue;

            var type = EventCommandParameterReader.GetInt(entry[0], 0);
            var itemId = EventCommandParameterReader.GetString(entry[1], string.Empty);
            var priceType = EventCommandParameterReader.GetInt(entry[2], 0);
            var price = EventCommandParameterReader.GetInt(entry[3], 0);

            if (string.IsNullOrWhiteSpace(itemId))
                itemId = EventCommandParameterReader.GetInt(entry[1], 0).ToString();

            goodsList.Add(new ShopGood(type, itemId, priceType, price));
        }

        return goodsList;
    }
}

/// <summary>
/// Script command handler (Code: 355).
/// </summary>
public class ScriptHandler : IEventCommandHandler
{
    private bool _complete;

    public void Execute(EventContext context)
    {
        var script = EventCommandParameterReader.GetString(context.Command.Parameters.ElementAtOrDefault(0), string.Empty);

        if (context.Game is IEventCommandHost host)
        {
            host.ExecuteScript(script);
        }

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

internal static class EventAudioState
{
    public static SoundEffectInstance? BgsInstance { get; set; }
}
