using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>Shared palette of the menu screens. Kept separate so every screen looks alike.</summary>
public static class MenuTheme
{
    public static readonly Color NightOverlay = new Color(0x03, 0x04, 0x12) * 0.80f;
    public static readonly Color TitleText = new Color(0xff, 0xf4, 0xdd);
    public static readonly Color Gold = new Color(0xff, 0xcf, 0x53);
    public static readonly Color Text = new Color(0xdf, 0xdc, 0xe8);
    public static readonly Color Muted = new Color(0x9a, 0x98, 0xab);
    public static readonly Color MutedDark = new Color(0x6e, 0x6d, 0x80);

    public static readonly Color PanelFill = new Color(0x13, 0x16, 0x22) * 0.95f;
    public static readonly Color PanelBorder = new Color(0x4d, 0x50, 0x64);

    public static readonly Color ButtonFill = new Color(0x1c, 0x20, 0x2e);
    public static readonly Color ButtonHoverFill = new Color(0x26, 0x2b, 0x3c);
    public static readonly Color ButtonBorder = new Color(0x4a, 0x4d, 0x5f);
    public static readonly Color ButtonFocusedFill = new Color(0x2b, 0x31, 0x46);
    public static readonly Color ChoiceSelectedFill = new Color(0x3a, 0x31, 0x18);

    public static readonly Color Danger = new Color(0xff, 0x8a, 0x7a);

    /// <summary>0..1 smooth pulse used for focused borders.</summary>
    public static float Pulse(float time, float speed = 4.5f) =>
        0.5f + 0.5f * MathF.Sin(time * speed);
}
