using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelForge.Shared.Models;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.Graphics;

/// <summary>
/// Handles rendering of tiles and tilesets.
/// </summary>
public class TileRenderer
{
    private readonly ResourceManager _resourceManager;
    private readonly Dictionary<int, Tileset> _tilesets = new();

    public TileRenderer(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Register a tileset for rendering.
    /// </summary>
    public void RegisterTileset(Tileset tileset)
    {
        _tilesets[tileset.Id] = tileset;
    }

    /// <summary>
    /// Draw a tile.
    /// </summary>
    public void DrawTile(
        SpriteBatch spriteBatch,
        TileData tile,
        Vector2 position,
        int tileWidth,
        int tileHeight,
        float opacity = 1.0f)
    {
        if (tile.IsEmpty)
            return;

        if (!_tilesets.TryGetValue(tile.TilesetId, out var tileset))
            return;

        var texture = _resourceManager.LoadTextureFromFile(
            tileset.ImagePath,
            spriteBatch.GraphicsDevice
        );

        if (texture == null)
            return;

        // Calculate source rectangle from tile ID
        int tilesPerRow = texture.Width / tileset.TileWidth;
        int sourceX = (tile.TileId % tilesPerRow) * tileset.TileWidth;
        int sourceY = (tile.TileId / tilesPerRow) * tileset.TileHeight;

        var sourceRect = new Microsoft.Xna.Framework.Rectangle(
            sourceX,
            sourceY,
            tileset.TileWidth,
            tileset.TileHeight
        );

        // Calculate sprite effects for flipping
        SpriteEffects effects = SpriteEffects.None;
        if (tile.FlipH)
            effects |= SpriteEffects.FlipHorizontally;
        if (tile.FlipV)
            effects |= SpriteEffects.FlipVertically;

        // Calculate rotation
        float rotation = tile.Rotation * MathHelper.PiOver2;
        Vector2 origin = tile.Rotation != 0
            ? new Vector2(tileset.TileWidth / 2f, tileset.TileHeight / 2f)
            : Vector2.Zero;

        var destRect = new Microsoft.Xna.Framework.Rectangle(
            (int)position.X,
            (int)position.Y,
            tileWidth,
            tileHeight
        );

        spriteBatch.Draw(
            texture,
            destRect,
            sourceRect,
            Color.White * opacity,
            rotation,
            origin,
            effects,
            0f
        );
    }

    /// <summary>
    /// Draw an entire layer.
    /// </summary>
    public void DrawLayer(
        SpriteBatch spriteBatch,
        MapLayer layer,
        int tileWidth,
        int tileHeight,
        Vector2 cameraOffset = default)
    {
        if (!layer.Visible)
            return;

        for (int y = 0; y < layer.Height; y++)
        {
            for (int x = 0; x < layer.Width; x++)
            {
                var tile = layer.GetTile(x, y);
                if (tile == null || tile.IsEmpty)
                    continue;

                Vector2 position = new Vector2(
                    x * tileWidth - cameraOffset.X,
                    y * tileHeight - cameraOffset.Y
                );

                DrawTile(spriteBatch, tile, position, tileWidth, tileHeight, layer.Opacity);
            }
        }
    }
}

/// <summary>
/// Represents a tileset with its texture and properties.
/// </summary>
public class Tileset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int TileWidth { get; set; } = 48;
    public int TileHeight { get; set; } = 48;
    public int Spacing { get; set; }
    public int Margin { get; set; }
}
