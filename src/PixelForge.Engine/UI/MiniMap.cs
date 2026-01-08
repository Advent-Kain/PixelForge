using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelForge.Shared.Models;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace PixelForge.Engine.UI;

/// <summary>
/// Mini-map display showing player position and map overview.
/// </summary>
public class MiniMap
{
    private readonly int _size = 150;
    private readonly int _margin = 10;
    private Vector2 _playerPosition;
    private MapData? _currentMap;
    private bool _visible = true;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    /// <summary>
    /// Update mini-map with current map and player position.
    /// </summary>
    public void Update(MapData? map, Vector2 playerPosition)
    {
        _currentMap = map;
        _playerPosition = playerPosition;
    }

    /// <summary>
    /// Draw the mini-map.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, GraphicsDevice graphicsDevice)
    {
        if (!_visible || _currentMap == null)
            return;

        var viewport = graphicsDevice.Viewport;

        // Position in top-right corner
        var position = new Vector2(
            viewport.Width - _size - _margin,
            _margin
        );

        var miniMapRect = new XnaRectangle(
            (int)position.X,
            (int)position.Y,
            _size,
            _size
        );

        // Draw background
        spriteBatch.Draw(pixelTexture, miniMapRect, Color.Black * 0.7f);

        // Draw border
        DrawBorder(spriteBatch, pixelTexture, miniMapRect, Color.White, 2);

        // Draw simplified map
        DrawMapOverview(spriteBatch, pixelTexture, miniMapRect);

        // Draw player marker
        DrawPlayerMarker(spriteBatch, pixelTexture, miniMapRect);

        // Draw events as dots
        DrawEventMarkers(spriteBatch, pixelTexture, miniMapRect);
    }

    /// <summary>
    /// Draw simplified map overview.
    /// </summary>
    private void DrawMapOverview(SpriteBatch spriteBatch, Texture2D pixelTexture, XnaRectangle miniMapRect)
    {
        if (_currentMap == null)
            return;

        // Calculate scale
        float scaleX = (float)miniMapRect.Width / _currentMap.Width;
        float scaleY = (float)miniMapRect.Height / _currentMap.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Draw tiles as colored pixels
        var groundLayer = _currentMap.Layers.FirstOrDefault(l => l.Type == LayerType.Tile);
        if (groundLayer != null)
        {
            for (int y = 0; y < groundLayer.Height && y < _currentMap.Height; y++)
            {
                for (int x = 0; x < groundLayer.Width && x < _currentMap.Width; x++)
                {
                    var tile = groundLayer.GetTile(x, y);
                    if (tile == null || tile.IsEmpty)
                        continue;

                    int pixelX = (int)(miniMapRect.X + x * scale);
                    int pixelY = (int)(miniMapRect.Y + y * scale);

                    var pixelRect = new XnaRectangle(pixelX, pixelY, Math.Max(1, (int)scale), Math.Max(1, (int)scale));

                    // Color based on tile type (simplified)
                    Color tileColor = new Color(50, 100, 50); // Green for ground
                    spriteBatch.Draw(pixelTexture, pixelRect, tileColor);
                }
            }
        }

        // Draw collision layer as darker color
        var collisionLayer = _currentMap.Layers.FirstOrDefault(l => l.Type == LayerType.Collision);
        if (collisionLayer != null)
        {
            for (int y = 0; y < collisionLayer.Height && y < _currentMap.Height; y++)
            {
                for (int x = 0; x < collisionLayer.Width && x < _currentMap.Width; x++)
                {
                    var tile = collisionLayer.GetTile(x, y);
                    if (tile == null || tile.IsEmpty)
                        continue;

                    int pixelX = (int)(miniMapRect.X + x * scale);
                    int pixelY = (int)(miniMapRect.Y + y * scale);

                    var pixelRect = new XnaRectangle(pixelX, pixelY, Math.Max(1, (int)scale), Math.Max(1, (int)scale));

                    Color collisionColor = new Color(100, 100, 100); // Gray for walls
                    spriteBatch.Draw(pixelTexture, pixelRect, collisionColor);
                }
            }
        }
    }

    /// <summary>
    /// Draw player position marker.
    /// </summary>
    private void DrawPlayerMarker(SpriteBatch spriteBatch, Texture2D pixelTexture, XnaRectangle miniMapRect)
    {
        if (_currentMap == null)
            return;

        // Calculate scale
        float scaleX = (float)miniMapRect.Width / _currentMap.Width;
        float scaleY = (float)miniMapRect.Height / _currentMap.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Player position on mini-map
        int playerX = (int)(miniMapRect.X + _playerPosition.X * scale);
        int playerY = (int)(miniMapRect.Y + _playerPosition.Y * scale);

        // Draw player as colored square
        var playerRect = new XnaRectangle(playerX - 2, playerY - 2, 5, 5);
        spriteBatch.Draw(pixelTexture, playerRect, Color.Yellow);

        // Draw direction indicator (small arrow)
        var arrowRect = new XnaRectangle(playerX - 1, playerY - 4, 3, 2);
        spriteBatch.Draw(pixelTexture, arrowRect, Color.Yellow);
    }

    /// <summary>
    /// Draw event markers as dots.
    /// </summary>
    private void DrawEventMarkers(SpriteBatch spriteBatch, Texture2D pixelTexture, XnaRectangle miniMapRect)
    {
        if (_currentMap == null)
            return;

        // Calculate scale
        float scaleX = (float)miniMapRect.Width / _currentMap.Width;
        float scaleY = (float)miniMapRect.Height / _currentMap.Height;
        float scale = Math.Min(scaleX, scaleY);

        // Draw events as small dots
        foreach (var evt in _currentMap.Events)
        {
            int eventX = (int)(miniMapRect.X + evt.Position.X * scale);
            int eventY = (int)(miniMapRect.Y + evt.Position.Y * scale);

            var eventRect = new XnaRectangle(eventX - 1, eventY - 1, 3, 3);
            spriteBatch.Draw(pixelTexture, eventRect, Color.Cyan);
        }
    }

    /// <summary>
    /// Toggle mini-map visibility.
    /// </summary>
    public void Toggle()
    {
        _visible = !_visible;
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, XnaRectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new XnaRectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
