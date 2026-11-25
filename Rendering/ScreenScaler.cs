using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Fits the fixed 960x480 virtual canvas into the real window with letterboxing,
/// so nothing is ever clipped at any window size or DPI scale.
/// </summary>
public sealed class ScreenScaler
{
    public float Scale { get; private set; } = 1f;
    public Vector2 Offset { get; private set; }
    public Matrix Matrix { get; private set; } = Matrix.Identity;

    public void Update(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        Scale = Math.Min(bounds.Width / (float)Balance.ViewWidth, bounds.Height / (float)Balance.ViewHeight);
        if (Scale <= 0f)
            Scale = 1f;

        float drawnWidth = Balance.ViewWidth * Scale;
        float drawnHeight = Balance.ViewHeight * Scale;
        Offset = new Vector2((bounds.Width - drawnWidth) / 2f, (bounds.Height - drawnHeight) / 2f);
        Matrix = Matrix.CreateScale(Scale, Scale, 1f) * Matrix.CreateTranslation(Offset.X, Offset.Y, 0f);
    }

    /// <summary>Converts a window-space mouse position into virtual-canvas coordinates.</summary>
    public Vector2 ToVirtual(Point screenPoint) =>
        new((screenPoint.X - Offset.X) / Scale, (screenPoint.Y - Offset.Y) / Scale);
}
