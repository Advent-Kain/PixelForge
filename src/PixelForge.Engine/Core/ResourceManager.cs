using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace PixelForge.Engine.Core;

/// <summary>
/// Manages loading and caching of game resources (textures, audio, etc.).
/// </summary>
public class ResourceManager
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, SoundEffect> _soundEffects = new();
    private readonly Dictionary<string, Song> _songs = new();
    private Texture2D? _pixelTexture;

    public ResourceManager(ContentManager content)
    {
        _content = content;
    }

    /// <summary>
    /// Initialize the resource manager.
    /// </summary>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        // Create a 1x1 white pixel texture for debugging and primitives
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Microsoft.Xna.Framework.Color.White });
    }

    /// <summary>
    /// Get a 1x1 white pixel texture.
    /// </summary>
    public Texture2D GetPixelTexture()
    {
        if (_pixelTexture == null)
            throw new InvalidOperationException("ResourceManager not initialized");
        return _pixelTexture;
    }

    /// <summary>
    /// Load a texture from file or cache.
    /// </summary>
    public Texture2D? LoadTexture(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (_textures.TryGetValue(path, out var texture))
            return texture;

        try
        {
            texture = _content.Load<Texture2D>(path);
            _textures[path] = texture;
            return texture;
        }
        catch (ContentLoadException)
        {
            // Return null if texture not found
            return null;
        }
    }

    /// <summary>
    /// Load a texture from a file path (outside of Content pipeline).
    /// </summary>
    public Texture2D? LoadTextureFromFile(string filePath, GraphicsDevice graphicsDevice)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        if (_textures.TryGetValue(filePath, out var texture))
            return texture;

        try
        {
            using var stream = File.OpenRead(filePath);
            texture = Texture2D.FromStream(graphicsDevice, stream);
            _textures[filePath] = texture;
            return texture;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Load a sound effect.
    /// </summary>
    public SoundEffect? LoadSoundEffect(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (_soundEffects.TryGetValue(path, out var soundEffect))
            return soundEffect;

        try
        {
            soundEffect = _content.Load<SoundEffect>(path);
            _soundEffects[path] = soundEffect;
            return soundEffect;
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// Load a song.
    /// </summary>
    public Song? LoadSong(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (_songs.TryGetValue(path, out var song))
            return song;

        try
        {
            song = _content.Load<Song>(path);
            _songs[path] = song;
            return song;
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// Unload all resources.
    /// </summary>
    public void UnloadAll()
    {
        _textures.Clear();
        _soundEffects.Clear();
        _songs.Clear();
        _content.Unload();
    }

    /// <summary>
    /// Unload a specific texture.
    /// </summary>
    public void UnloadTexture(string path)
    {
        if (_textures.TryGetValue(path, out var texture))
        {
            texture.Dispose();
            _textures.Remove(path);
        }
    }
}
