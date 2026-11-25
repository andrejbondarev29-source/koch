using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>A fixed-length sequence of frames for one animation.</summary>
public sealed class SpriteAnimation
{
    private readonly Texture2D[] _frames;

    public SpriteAnimation(Texture2D[] frames)
    {
        _frames = frames;
    }

    public int Count => _frames.Length;

    /// <summary>Returns frame <paramref name="index"/>, clamped into valid range.</summary>
    public Texture2D Frame(int index) => _frames[Math.Clamp(index, 0, _frames.Length - 1)];
}
