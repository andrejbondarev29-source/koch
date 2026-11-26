using DvorFight.Core;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Drawing helpers of the quick character-select menu: the small status card, the slanted
/// vertical name list and the bottom-right key hints. Text is drawn glyph by glyph with a
/// slanted baseline, which gives the angular Persona-style rhythm without a shader.
/// </summary>
internal static class MenuUi
{
    private const float ListSlope = -0.14f;

    public static void DrawInfoCard(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int selected,
        int total,
        BotDifficulty difficulty)
    {
        Rectangle card = MenuLayout.InfoCard;
        Vector2[] points = Plaque(card, 8, 10);

        renderer.FillPolygon(spriteBatch, points, SelectTheme.Paper);
        renderer.DrawPolygonBorder(spriteBatch, points, SelectTheme.Ink, 2f);
        renderer.FillRect(spriteBatch,
            new Rectangle(card.X + 10, card.Y + 10, 4, card.Height - 20),
            SelectTheme.Crimson);

        UI.Text(spriteBatch, fonts.Name, $"БОЕЦ {selected + 1:00} / {total:00}",
            new Vector2(card.X + 20, card.Y + 6), SelectTheme.Ink);
        UI.Text(spriteBatch, fonts.Small, "СЛОЖНОСТЬ",
            new Vector2(card.X + 20, card.Y + 34), SelectTheme.Ink * 0.65f);
        UI.Text(spriteBatch, fonts.Small, DifficultyCatalog.For(difficulty).Title.ToUpperInvariant(),
            new Vector2(card.X + 108, card.Y + 34), SelectTheme.Ink);
    }

    public static void DrawList(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int selected,
        float time)
    {
        int total = FighterCatalog.All.Count;
        int first = MenuLayout.FirstVisible(selected, total);
        float pulse = MenuTheme.Pulse(time, 3.2f);
        int visible = Math.Min(total, MenuLayout.VisibleItems);
        int railHeight = visible * (MenuLayout.ItemHeight + MenuLayout.ItemGap) + 8;

        // Thin red rail behind the list, like the accent bars of the reference.
        renderer.FillRect(spriteBatch,
            new Rectangle(MenuLayout.ListX - 20, 160, 2, railHeight),
            SelectTheme.Crimson * 0.35f);

        for (int slot = 0; slot < MenuLayout.VisibleItems; slot++)
        {
            int index = first + slot;
            if (index >= total)
                break;

            Rectangle rect = MenuLayout.ItemRect(slot);
            bool isSelected = index == selected;
            string label = FighterCatalog.All[index].Name.ToUpperInvariant();
            var font = fonts.Title;
            float textWidth = font.MeasureString(label).X;

            if (isSelected)
            {
                float barY = rect.Y + 56f;
                float rightX = MenuLayout.ListX + textWidth + 18f;
                float rightY = barY + (rightX - MenuLayout.ListX) * ListSlope;
                var bar = new[]
                {
                    new Vector2(MenuLayout.ListX - 20f, barY),
                    new Vector2(rightX, rightY),
                    new Vector2(rightX, rightY + 7f),
                    new Vector2(MenuLayout.ListX - 32f, barY + 7f),
                };
                renderer.FillQuad(spriteBatch, bar, SelectTheme.Crimson * (0.72f + 0.22f * pulse));

                var marker = new[]
                {
                    new Vector2(MenuLayout.ListX - 34f, rect.Y + 24f),
                    new Vector2(MenuLayout.ListX - 34f, rect.Y + 42f),
                    new Vector2(MenuLayout.ListX - 24f, rect.Y + 33f),
                };
                renderer.FillPolygon(spriteBatch, marker, SelectTheme.Crimson);
            }

            Color main = isSelected ? SelectTheme.Paper : SelectTheme.Cyan;
            Color shadow = isSelected ? SelectTheme.Crimson : Color.Transparent;

            DrawSlantedText(
                spriteBatch,
                font,
                label,
                new Vector2(MenuLayout.ListX, rect.Y + 5f),
                ListSlope,
                main,
                shadow,
                5f,
                5f);
        }
    }

    public static void DrawHints(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts)
    {
        renderer.FillRect(spriteBatch,
            new Rectangle(MenuLayout.ListX - 20, 452, 9, 4),
            SelectTheme.Crimson);
        UI.Text(spriteBatch, fonts.Small, "КЛИК / ENTER — В БОЙ · ESC — НАЗАД",
            new Vector2(MenuLayout.ListX, 446), SelectTheme.Ice * 0.78f);
    }

    private static void DrawSlantedText(
        SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 origin,
        float slope,
        Color color,
        Color shadow,
        float shadowX,
        float shadowY)
    {
        float advance = 0f;

        foreach (char character in text)
        {
            string glyph = character.ToString();
            var position = origin + new Vector2(advance, advance * slope);
            var pixelPosition = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));

            if (shadow != Color.Transparent)
            {
                var shadowPosition = new Vector2(
                    MathF.Round(pixelPosition.X + shadowX),
                    MathF.Round(pixelPosition.Y + shadowY));
                spriteBatch.DrawString(font, glyph, shadowPosition, shadow);
            }

            spriteBatch.DrawString(font, glyph, pixelPosition, color);
            advance += font.MeasureString(glyph).X - 1f;
        }
    }

    private static Vector2[] Plaque(Rectangle rect, int skew, int notch) => new[]
    {
        new Vector2(rect.X + skew, rect.Y),
        new Vector2(rect.Right - notch, rect.Y),
        new Vector2(rect.Right, rect.Y + notch),
        new Vector2(rect.Right - skew, rect.Bottom),
        new Vector2(rect.X + notch, rect.Bottom),
        new Vector2(rect.X, rect.Bottom - notch),
    };
}
