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
    public float Clock { get; set; }

    private readonly Texture2D _pixel;

    public Renderer(GraphicsDevice graphicsDevice, AssetStore assets)
    {
        Assets = assets;
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
}
