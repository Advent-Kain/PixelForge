using FluentAssertions;
using Microsoft.Xna.Framework;
using PixelForge.Engine.Core;
using PixelForge.Engine.Events;
using PixelForge.Shared.Models;

namespace PixelForge.Engine.Tests.Events;

public class EventProcessorFlowControlTests
{
    [Fact]
    public void JumpToLabelSkipsCommands()
    {
        var context = new FakeGameContext();
        var processor = new EventProcessor(context);
        var executed = new List<string>();
        processor.RegisterHandler(999, new RecordingHandler(executed));

        var mapEvent = new MapEvent
        {
            Pages =
            {
                new EventPage
                {
                    Commands =
                    {
                        new EventCommand { Code = 118, Parameters = { "Start" } },
                        new EventCommand { Code = 119, Parameters = { "Target" } },
                        new EventCommand { Code = 999, Parameters = { "Skipped" } },
                        new EventCommand { Code = 118, Parameters = { "Target" } },
                        new EventCommand { Code = 999, Parameters = { "Reached" } }
                    }
                }
            }
        };

        ExecuteUntilComplete(processor, mapEvent);

        executed.Should().Equal("Reached");
    }

    [Fact]
    public void BreakLoopJumpsPastRepeatAbove()
    {
        var context = new FakeGameContext();
        var processor = new EventProcessor(context);
        var executed = new List<string>();
        processor.RegisterHandler(999, new RecordingHandler(executed));

        var mapEvent = new MapEvent
        {
            Pages =
            {
                new EventPage
                {
                    Commands =
                    {
                        new EventCommand { Code = 112, Indent = 0 },
                        new EventCommand { Code = 999, Indent = 1, Parameters = { "LoopBody" } },
                        new EventCommand { Code = 113, Indent = 1 },
                        new EventCommand { Code = 413, Indent = 0 },
                        new EventCommand { Code = 999, Indent = 0, Parameters = { "AfterLoop" } }
                    }
                }
            }
        };

        ExecuteUntilComplete(processor, mapEvent);

        executed.Should().Equal("LoopBody", "AfterLoop");
    }

    private static void ExecuteUntilComplete(EventProcessor processor, MapEvent mapEvent)
    {
        processor.ExecuteEvent(mapEvent);

        var ticks = 0;
        while (processor.IsBusy && ticks < 50)
        {
            processor.Update(new GameTime());
            ticks++;
        }

        processor.IsBusy.Should().BeFalse();
    }

    private sealed class FakeGameContext : IGameContext
    {
        private readonly GameState _gameState = new();

        public GameState GetGameState() => _gameState;

        public ResourceManager GetResourceManager() =>
            throw new NotSupportedException("Resource manager not needed for flow control tests.");

        public void LoadMap(string mapId) =>
            throw new NotSupportedException("Map loading not needed for flow control tests.");
    }

    private sealed class RecordingHandler : IEventCommandHandler
    {
        private readonly List<string> _executed;

        public RecordingHandler(List<string> executed)
        {
            _executed = executed;
        }

        public void Execute(EventContext context)
        {
            var marker = context.Command.Parameters.Count > 0
                ? context.Command.Parameters[0]?.ToString() ?? string.Empty
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(marker))
            {
                _executed.Add(marker);
            }
        }

        public bool IsComplete() => true;
    }
}
