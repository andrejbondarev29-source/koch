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

    public FontSet(
        DynamicSpriteFont title,
        DynamicSpriteFont big,
        DynamicSpriteFont name,
        DynamicSpriteFont small)
    {
        Title = title;
        Big = big;
        Name = name;
        Small = small;
    }
}
