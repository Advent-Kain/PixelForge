using Microsoft.Xna.Framework;

namespace PixelForge.Engine.Core;

/// <summary>
/// Represents the current state of a picture shown via event commands.
/// </summary>
public class PictureState
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Origin { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float ScaleX { get; set; } = 100f;
    public float ScaleY { get; set; } = 100f;
    public int Opacity { get; set; } = 255;
    public int BlendMode { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public Vector4 Tone { get; set; }
}
