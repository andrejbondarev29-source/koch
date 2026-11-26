using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Low-level drawing helpers shared by every entity: sprite blitting, rectangles
/// and lines. Holds the current game clock for time-based visual effects.
/// </summary>
public sealed class Renderer
{
    public AssetStore Assets { get; }
    public GraphicsDevice GraphicsDevice { get; }
    public float Clock { get; set; }

    private readonly Texture2D _pixel;

    public Renderer(GraphicsDevice graphicsDevice, AssetStore assets)
    {
        Assets = assets;
        GraphicsDevice = graphicsDevice;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>Draws an animation frame with its bottom-center anchored at (x, yScreen).</summary>
    public void DrawSprite(
        SpriteBatch spriteBatch,
        string key,
        int frame,
        float x,
        float yScreen,
        SpriteEffects effects,
        float alpha,
        float size = Balance.SpriteWidth)
    {
        var animation = Assets.Get(key);
        if (animation == null)
            return;

        Texture2D texture = animation.Frame(frame);
        float width = size;
        float height = size * Balance.SpriteAspect;
        var scale = new Vector2(width / texture.Width, height / texture.Height);
        var origin = new Vector2(texture.Width / 2f, texture.Height);

        spriteBatch.Draw(texture, new Vector2(x, yScreen), null, Color.White * alpha, 0f, origin, scale, effects, 0f);
    }

    public void FillRect(SpriteBatch spriteBatch, Rectangle rectangle, Color color) =>
        spriteBatch.Draw(_pixel, rectangle, color);

    /// <summary>Outlines a rectangle with a solid <paramref name="thickness"/>-pixel border.</summary>
    public void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness)
    {
        if (thickness <= 0)
            return;

        FillRect(spriteBatch, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        FillRect(spriteBatch, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        FillRect(spriteBatch, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        FillRect(spriteBatch, new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    /// <summary>Fills a rectangle and outlines it — the basic building block of UI panels.</summary>
    public void DrawPanel(SpriteBatch spriteBatch, Rectangle rectangle, Color fill, Color border, int thickness = 3)
    {
        FillRect(spriteBatch, rectangle, fill);
        DrawBorder(spriteBatch, rectangle, border, thickness);
    }

    public void DrawLine(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color, float thickness)
    {
        var delta = to - from;
        float length = delta.Length();
        if (length <= 0f)
            return;

        float rotation = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(_pixel, from, null, color, rotation, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Draws a whole texture centred on <paramref name="center"/> with rotation and a
    /// uniform scale. Unlike <see cref="DrawSprite"/> it makes no assumption about the
    /// source aspect ratio — the fighter portraits are 3:4, not 264x252.
    /// </summary>
    public void DrawTextureEx(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 center,
        float rotation,
        Vector2 scale,
        Color tint,
        SpriteEffects effects = SpriteEffects.None)
    {
        var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        spriteBatch.Draw(texture, center, null, tint, rotation, origin, scale, effects, 0f);
    }

    /// <summary>Fills a convex or concave polygon by rasterising it one scanline at a time.</summary>
    public void FillPolygon(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color color)
    {
        if (points.Count < 3)
            return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < points.Count; i++)
        {
            minY = MathF.Min(minY, points[i].Y);
            maxY = MathF.Max(maxY, points[i].Y);
        }

        int top = Math.Max(0, (int)MathF.Floor(minY));
        int bottom = Math.Min(Balance.ViewHeight, (int)MathF.Ceiling(maxY));
        var intersections = new List<float>(points.Count);

        for (int y = top; y < bottom; y++)
        {
            float scanY = y + 0.5f;
            intersections.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                if (MathF.Abs(a.Y - b.Y) < 0.0001f)
                    continue;
                if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY))
                {
                    float t = (scanY - a.Y) / (b.Y - a.Y);
                    intersections.Add(a.X + t * (b.X - a.X));
                }
            }

            intersections.Sort();
            for (int i = 0; i + 1 < intersections.Count; i += 2)
            {
                int left = (int)MathF.Round(intersections[i]);
                int right = (int)MathF.Round(intersections[i + 1]);
                if (right > left)
                    FillRect(spriteBatch, new Rectangle(left, y, right - left, 1), color);
            }
        }
    }

    public void FillQuad(SpriteBatch spriteBatch, Vector2[] points, Color color) =>
        FillPolygon(spriteBatch, points, color);

    /// <summary>Outlines a polygon edge by edge; used for the angular menu plaques.</summary>
    public void DrawPolygonBorder(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color color, float thickness)
    {
        for (int i = 0; i < points.Count; i++)
            DrawLine(spriteBatch, points[i], points[(i + 1) % points.Count], color, thickness);
    }
}
