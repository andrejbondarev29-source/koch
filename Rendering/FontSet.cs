using FontStashSharp;

namespace DvorFight.Rendering;

/// <summary>
/// The four DynamicSpriteFont sizes used by menus, HUD and the result overlay.
/// Keeping them in one object avoids threading four parameters through every screen.
/// </summary>
public sealed class FontSet
{
    public DynamicSpriteFont Title { get; }
    public DynamicSpriteFont Big { get; }
    public DynamicSpriteFont Name { get; }
    public DynamicSpriteFont Small { get; }

    /// <summary>Large display face used by the fighter-select hero name (64 px).</summary>
    public DynamicSpriteFont Display { get; }

    public FontSet(
        DynamicSpriteFont title,
        DynamicSpriteFont big,
        DynamicSpriteFont name,
        DynamicSpriteFont small,
        DynamicSpriteFont display)
    {
        Title = title;
        Big = big;
        Name = name;
        Small = small;
        Display = display;
    }
}
