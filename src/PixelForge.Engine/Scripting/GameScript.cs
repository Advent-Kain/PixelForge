using PixelForge.Engine.Core;
using PixelForge.Engine.Events;
using PixelForge.Shared.Models;

namespace PixelForge.Engine.Scripting;

/// <summary>
/// Base class for custom game scripts.
/// </summary>
public abstract class GameScript
{
    public IGameContext? Game { get; private set; }

    public void Attach(IGameContext game)
    {
        Game = game;
    }

    public virtual void Execute(EventContext context)
    {
    }

    public virtual void OnGameStart()
    {
    }

    public virtual void OnMapLoad(MapData map)
    {
    }

    public virtual void Update(float deltaTime)
    {
    }
}
