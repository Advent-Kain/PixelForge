using FluentAssertions;
using Microsoft.Xna.Framework;
using PixelForge.Engine.Core;
using PixelForge.Engine.Events;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models;
using Xunit;

namespace PixelForge.Engine.Tests.Events;

public class EventProcessorCommandHandlerTests
{
    [Fact]
    public void BattleProcessingNotifiesHost()
    {
        var context = new RecordingGameContext();
        var processor = new EventProcessor(context);

        var mapEvent = new MapEvent
        {
            Pages =
            {
                new EventPage
                {
                    Commands =
                    {
                        new EventCommand
                        {
                            Code = 301,
                            Parameters = { "Troop01", true, false }
                        }
                    }
                }
            }
        };

        ExecuteUntilComplete(processor, mapEvent);

        context.BattleRequest.Should().BeEquivalentTo(new BattleRequest("Troop01", true, false));
    }

    [Fact]
    public void ShopProcessingNotifiesHost()
    {
        var context = new RecordingGameContext();
        var processor = new EventProcessor(context);

        var goods = new List<object>
        {
            new List<object> { 0, "Potion", 0, 50 },
            new List<object> { 1, "IronSword", 1, 250 }
        };

        var mapEvent = new MapEvent
        {
            Pages =
            {
                new EventPage
                {
                    Commands =
                    {
                        new EventCommand
                        {
                            Code = 302,
                            Parameters = { goods, true }
                        }
                    }
                }
            }
        };

        ExecuteUntilComplete(processor, mapEvent);

        context.ShopRequest.Should().NotBeNull();
        context.ShopRequest!.PurchaseOnly.Should().BeTrue();
        context.ShopRequest.Goods.Should().Equal(
            new ShopGood(0, "Potion", 0, 50),
            new ShopGood(1, "IronSword", 1, 250));
    }

    [Fact]
    public void ScriptExecutionNotifiesHost()
    {
        var context = new RecordingGameContext();
        var processor = new EventProcessor(context);

        var mapEvent = new MapEvent
        {
            Pages =
            {
                new EventPage
                {
                    Commands =
                    {
                        new EventCommand
                        {
                            Code = 355,
                            Parameters = { "Game.Log(\"Hello\")" }
                        }
                    }
                }
            }
        };

        ExecuteUntilComplete(processor, mapEvent);

        context.Script.Should().Be("Game.Log(\"Hello\")");
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

    private sealed class RecordingGameContext : IGameContext, IEventCommandHost
    {
        private readonly GameState _gameState = new();

        public BattleRequest? BattleRequest { get; private set; }
        public ShopRequest? ShopRequest { get; private set; }
        public string? Script { get; private set; }

        public GameState GetGameState() => _gameState;

        public ResourceManager GetResourceManager() =>
            throw new NotSupportedException("Resource manager not needed for these tests.");

        public GameDatabase GetDatabase() => new();

        public void LoadMap(string mapId) =>
            throw new NotSupportedException("Map loading not needed for these tests.");

        public void PlayAnimation(string animationId, Vector2 position)
        {
        }

        public void StartBattle(string troopId, bool canEscape, bool canLose)
        {
            BattleRequest = new BattleRequest(troopId, canEscape, canLose);
        }

        public void OpenShop(IReadOnlyList<ShopGood> goods, bool purchaseOnly)
        {
            ShopRequest = new ShopRequest(goods.ToList(), purchaseOnly);
        }

        public void ExecuteScript(string script)
        {
            Script = script;
        }
    }

    private sealed record BattleRequest(string TroopId, bool CanEscape, bool CanLose);
    private sealed record ShopRequest(IReadOnlyList<ShopGood> Goods, bool PurchaseOnly);
}
