using DvorFight.Core;
using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>
/// Palette of the redesigned fighter-select screen. Kept apart from <see cref="MenuTheme"/>
/// so the title screen and the result overlay keep their warm night-arena look: this screen
/// is a cold, high-contrast "underwater" blue composition (Persona 3 Reload inspired).
/// </summary>
public static class SelectTheme
{
    public static readonly Color Ink = new(0x05, 0x0B, 0x22);
    public static readonly Color DeepBlue = new(0x07, 0x1A, 0x5C);
    public static readonly Color Royal = new(0x0B, 0x35, 0xC8);
    public static readonly Color Azure = new(0x16, 0x68, 0xFF);
    public static readonly Color Cyan = new(0x37, 0xE4, 0xFF);
    public static readonly Color Ice = new(0xBF, 0xF4, 0xFF);
    public static readonly Color Paper = new(0xF5, 0xFB, 0xFF);
    public static readonly Color White = new(0xFF, 0xFF, 0xFF);
    public static readonly Color Pink = new(0xFF, 0x3E, 0x9A);
    public static readonly Color Crimson = new(0xE0, 0x18, 0x45);
    public static readonly Color Lilac = new(0x8B, 0x7B, 0xFF);
    public static readonly Color Muted = new(0x9C, 0xC8, 0xFF);

    private static readonly Color AndreyAccent = new(0x37, 0xE4, 0xFF);
    private static readonly Color PanteraAccent = new(0xFF, 0x3E, 0x9A);

    /// <summary>Opaque background colours, ordered for the ambient shard palette.</summary>
    public static readonly Color[] ShardColors =
    {
        Cyan,
        Azure,
        Ice,
        Pink,
        Crimson,
        Lilac,
    };

    public static Color AccentFor(FighterKind kind) =>
        kind == FighterKind.Pantera ? PanteraAccent : AndreyAccent;
}
