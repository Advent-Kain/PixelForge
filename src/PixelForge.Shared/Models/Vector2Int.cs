using System;

namespace PixelForge.Shared.Models;

/// <summary>
/// Represents a 2D vector with integer coordinates.
/// Used for tile positions, map coordinates, and grid-based positioning.
/// </summary>
public record struct Vector2Int(int X, int Y)
{
    public static readonly Vector2Int Zero = new(0, 0);
    public static readonly Vector2Int One = new(1, 1);
    public static readonly Vector2Int Up = new(0, -1);
    public static readonly Vector2Int Down = new(0, 1);
    public static readonly Vector2Int Left = new(-1, 0);
    public static readonly Vector2Int Right = new(1, 0);

    public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2Int operator *(Vector2Int a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2Int operator /(Vector2Int a, int scalar) => new(a.X / scalar, a.Y / scalar);

    public int LengthSquared() => X * X + Y * Y;
    public float Length() => MathF.Sqrt(LengthSquared());

    public override string ToString() => $"({X}, {Y})";
}
