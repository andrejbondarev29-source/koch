using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

public enum TextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>Text helpers used by the menu, the HUD and the result screen.</summary>
public static class UI
{
    private static readonly Color Shadow = new Color(0, 0, 0) * 0.55f;

    public static void Text(
        SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        TextAlign align = TextAlign.Left)
    {
        spriteBatch.DrawString(font, text, Align(position, font.MeasureString(text), align), color);
    }

    /// <summary>Draws text with a soft drop shadow, for titles sitting on busy backgrounds.</summary>
    public static void ShadowedText(
        SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        TextAlign align = TextAlign.Left)
    {
        var size = font.MeasureString(text);
        var target = Align(position, size, align);
        spriteBatch.DrawString(font, text, target + new Vector2(2f, 2f), Shadow);
        spriteBatch.DrawString(font, text, target, color);
    }

    private static Vector2 Align(Vector2 position, Vector2 size, TextAlign align) => align switch
    {
        TextAlign.Center => new Vector2(position.X - size.X / 2f, position.Y - size.Y / 2f),
        TextAlign.Right => new Vector2(position.X - size.X, position.Y),
        _ => position,
    };
}
