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
    public const int ListTop = 172;
    public const int ItemHeight = 68;
    public const int ItemGap = 4;
    public const int VisibleItems = 3;

    public static readonly Rectangle InfoCard = new(24, 24, 210, 66);

    public static int ItemY(int slot) => ListTop + slot * (ItemHeight + ItemGap);

    public static Rectangle ItemRect(int slot) =>
        new(516, ItemY(slot) - 6, 280, 72);

    public static int FirstVisible(int cursor, int count) =>
        Math.Clamp(cursor - 1, 0, Math.Max(0, count - VisibleItems));
}
