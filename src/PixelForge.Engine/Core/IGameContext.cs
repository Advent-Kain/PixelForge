namespace PixelForge.Engine.Core;

/// <summary>
/// Minimal game context required by event processing.
/// </summary>
public interface IGameContext
{
    GameState GetGameState();
    ResourceManager GetResourceManager();
    RPG.GameDatabase GetDatabase();
    void LoadMap(string mapId);
    void PlayAnimation(string animationId, Microsoft.Xna.Framework.Vector2 position);
}
