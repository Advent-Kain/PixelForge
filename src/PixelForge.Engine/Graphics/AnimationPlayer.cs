using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Graphics;

/// <summary>
/// Handles playback and rendering of animations.
/// </summary>
public class AnimationPlayer
{
    private readonly ResourceManager _resourceManager;
    private readonly GameDatabase _database;
    private readonly List<ActiveAnimation> _activeAnimations = new();

    public AnimationPlayer(ResourceManager resourceManager, GameDatabase database)
    {
        _resourceManager = resourceManager;
        _database = database;
    }

    /// <summary>
    /// Play an animation at a position.
    /// </summary>
    public void Play(string animationId, Vector2 position)
    {
        var animation = _database.GetAnimation(animationId);
        if (animation == null)
            return;

        _activeAnimations.Add(new ActiveAnimation(animation, position));
    }

    /// <summary>
    /// Update all active animations.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_activeAnimations.Count == 0)
            return;

        for (int i = _activeAnimations.Count - 1; i >= 0; i--)
        {
            var instance = _activeAnimations[i];
            instance.Update(gameTime);
            if (instance.IsComplete)
            {
                _activeAnimations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Draw all active animations.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var instance in _activeAnimations)
        {
            DrawAnimation(spriteBatch, instance);
        }
    }

    private void DrawAnimation(SpriteBatch spriteBatch, ActiveAnimation instance)
    {
        var animation = instance.Animation;
        if (string.IsNullOrWhiteSpace(animation.ImagePath))
            return;

        if (animation.FrameWidth <= 0 || animation.FrameHeight <= 0 || animation.FrameCount <= 0)
            return;

        var texture = _resourceManager.LoadTextureFromFile(animation.ImagePath, spriteBatch.GraphicsDevice);
        if (texture == null)
            return;

        int frameIndex = instance.GetFrameIndex();
        int framesPerRow = Math.Max(1, texture.Width / animation.FrameWidth);
        int sourceX = (frameIndex % framesPerRow) * animation.FrameWidth;
        int sourceY = (frameIndex / framesPerRow) * animation.FrameHeight;

        var sourceRect = new Rectangle(sourceX, sourceY, animation.FrameWidth, animation.FrameHeight);
        var destRect = new Rectangle(
            (int)instance.Position.X,
            (int)instance.Position.Y,
            animation.FrameWidth,
            animation.FrameHeight
        );

        spriteBatch.Draw(texture, destRect, sourceRect, Color.White);
    }

    private sealed class ActiveAnimation
    {
        public ActiveAnimation(Animation animation, Vector2 position)
        {
            Animation = animation;
            Position = position;
        }

        public Animation Animation { get; }
        public Vector2 Position { get; }
        public TimeSpan Elapsed { get; private set; }
        public bool IsComplete { get; private set; }

        public void Update(GameTime gameTime)
        {
            if (IsComplete)
                return;

            Elapsed += gameTime.ElapsedGameTime;

            if (Animation.Loop)
                return;

            var totalDuration = GetTotalDuration();
            if (totalDuration > TimeSpan.Zero && Elapsed >= totalDuration)
            {
                IsComplete = true;
            }
        }

        public int GetFrameIndex()
        {
            if (Animation.FrameCount <= 1)
                return 0;

            var frameDuration = TimeSpan.FromSeconds(Math.Max(0.01f, Animation.FrameDuration));
            var frame = (int)(Elapsed.TotalMilliseconds / frameDuration.TotalMilliseconds);

            if (Animation.Loop)
                return frame % Animation.FrameCount;

            return Math.Clamp(frame, 0, Animation.FrameCount - 1);
        }

        private TimeSpan GetTotalDuration()
        {
            var frameDuration = TimeSpan.FromSeconds(Math.Max(0.01f, Animation.FrameDuration));
            return TimeSpan.FromMilliseconds(frameDuration.TotalMilliseconds * Animation.FrameCount);
        }
    }
}
