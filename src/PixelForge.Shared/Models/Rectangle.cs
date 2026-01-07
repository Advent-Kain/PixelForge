namespace PixelForge.Shared.Models;

/// <summary>
/// Represents a rectangle with integer coordinates.
/// Used for collision detection, tile selection, and UI bounds.
/// </summary>
public record struct Rectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Right => X + Width;
    public int Top => Y;
    public int Bottom => Y + Height;

    public Vector2Int Position => new(X, Y);
    public Vector2Int Size => new(Width, Height);
    public Vector2Int Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Vector2Int point) =>
        point.X >= X && point.X < X + Width &&
        point.Y >= Y && point.Y < Y + Height;

    public bool Intersects(Rectangle other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;

    public static Rectangle FromPoints(Vector2Int min, Vector2Int max) =>
        new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
}
