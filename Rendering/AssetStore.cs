using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Loads every sprite animation from <c>Content/Sprites</c> at startup and indexes
/// them by key (e.g. <c>"andrey/walk"</c>). Textures are loaded at runtime from PNG,
/// so the MonoGame Content Pipeline is not required.
/// </summary>
public sealed class AssetStore
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, SpriteAnimation> _animations = new();

    public AssetStore(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Load()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Sprite folder not found: {root}");

        foreach (string dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
        {
            string[] files = Directory.GetFiles(dir, "*.png")
                .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                .ToArray();

            if (files.Length == 0)
                continue;

            string key = Path.GetRelativePath(root, dir).Replace('\\', '/');
            _animations[key] = new SpriteAnimation(files.Select(LoadTexture).ToArray());
        }
    }

    /// <summary>Loads the metamorphosis cutscene frames (may be empty).</summary>
    public Texture2D[] LoadCutsceneFrames()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Content", "MorphFrames");
        if (!Directory.Exists(dir))
            return Array.Empty<Texture2D>();

        return Directory.GetFiles(dir, "*.png")
            .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
            .Select(LoadTexture)
            .ToArray();
    }

    public SpriteAnimation? Get(string key) =>
        _animations.TryGetValue(key, out var animation) ? animation : null;

    private Texture2D LoadTexture(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(_graphicsDevice, stream);
    }
}
