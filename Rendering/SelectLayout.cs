using DvorFight.Config;
using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>
/// All rectangles of the fighter-select screen in virtual-canvas coordinates (960x480).
/// The drawing code and the headless self-test share these helpers, so a layout change
/// can never silently leave the tests clicking the old spots.
/// </summary>
internal static class SelectLayout
{
    public const int StageWidth = 512;
    public const int ContentX = 528;
    public const int ContentWidth = 408;
    public const int ContentRight = ContentX + ContentWidth;
    public const int VisibleTabs = 2;

    public const float PortraitCenterX = 250f;
    public const float PortraitCenterY = 240f;
    public const float PortraitHeight = 560f;

    public static readonly Rectangle Stage = new(0, 0, StageWidth, Balance.ViewHeight);

    public static readonly Rectangle HeroName = new(ContentX, 40, ContentWidth, 76);
    public static readonly Rectangle StartButton = new(ContentX, 398, ContentWidth, 46);

    public static Rectangle FighterTab(int slot) =>
        new(20, 384 + slot * 36, 148, 30);

    public static Rectangle DifficultyPill(int index) =>
        new(ContentX + index * 140, 340, 128, 30);

    public static int FirstVisible(int selected, int total) =>
        Math.Clamp(selected - 1, 0, Math.Max(0, total - VisibleTabs));
}
