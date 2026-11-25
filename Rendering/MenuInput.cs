using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>
/// One frame of menu input, already edge-triggered and translated to virtual-canvas
/// mouse coordinates. Menu screens consume this snapshot only — they never touch
/// MonoGame input directly, which also makes their navigation logic testable headlessly.
/// </summary>
public readonly struct MenuInput
{
    public bool Up { get; init; }
    public bool Down { get; init; }
    public bool Left { get; init; }
    public bool Right { get; init; }
    public bool Confirm { get; init; }
    public bool Back { get; init; }

    public Vector2 MousePosition { get; init; }
    public bool MouseMoved { get; init; }
    public bool MouseClicked { get; init; }

    /// <summary>Direct fighter pick via number keys: 1 or 2 (0 when unused).</summary>
    public int FighterShortcut { get; init; }
}
