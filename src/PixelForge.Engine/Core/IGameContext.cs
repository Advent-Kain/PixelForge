namespace PixelForge.Engine.Core;

/// <summary>
/// Minimal game context required by event processing.
/// </summary>
public interface IGameContext
{
    GameState GetGameState();
    ResourceManager GetResourceManager();
    void LoadMap(string mapId);
}
