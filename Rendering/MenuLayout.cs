using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>
/// Layout of the quick character-select menu (the screen opened by «Играть»).
/// The reference is a Persona 3 Reload-style list: a huge portrait on the left and a
/// slanted vertical menu of names on the right, with a small status card top-left.
/// </summary>
internal static class MenuLayout
{
    public const int ListX = 552;
    public const int ListWidth = 372;
    public const int ListTop = 148;
    public const int ItemHeight = 68;
    public const int ItemGap = 4;
    public const int VisibleItems = 4;

    public const float PortraitCenterX = 250f;
    public const float PortraitCenterY = 240f;

    public static readonly Rectangle InfoCard = new(24, 24, 210, 66);
    public static readonly Rectangle ListArea = new(516, 130, 424, 304);

    public static int ItemY(int slot) => ListTop + slot * (ItemHeight + ItemGap);

    public static Rectangle ItemRect(int slot) =>
        new(516, ItemY(slot), 424, ItemHeight);

    public static int FirstVisible(int cursor, int count) =>
        Math.Clamp(cursor - 1, 0, Math.Max(0, count - VisibleItems));
}
