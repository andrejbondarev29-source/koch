using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Drawing helpers of the fighter-select screen: angular plaques, the tracked hero name,
/// stat cells, special rows, difficulty pills and the start button. All shapes are
/// procedural polygons, so the screen needs no UI PNGs.
/// </summary>
internal static class SelectUi
{
    private const int StatGap = 12;
    private const int StatWidth = 128;

    public static void DrawRightVeil(SpriteBatch spriteBatch, Renderer renderer)
    {
        int start = SelectLayout.ContentX - 44;
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            renderer.FillRect(spriteBatch,
                new Rectangle(start + i * 4, 0, 4, Balance.ViewHeight),
                SelectTheme.Ink * (0.16f * (1f - t)));
        }

        renderer.FillRect(spriteBatch,
            new Rectangle(SelectLayout.ContentX, 0, Balance.ViewWidth - SelectLayout.ContentX, Balance.ViewHeight),
            SelectTheme.Ink * 0.10f);
    }

    public static void DrawHeader(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, int selected, int total)
    {
        var header = new Rectangle(12, 8, 168, 28);
        Vector2[] points = Plaque(header, 6, 7);
        renderer.FillPolygon(spriteBatch, points, SelectTheme.Ink * 0.78f);
        renderer.DrawPolygonBorder(spriteBatch, points, SelectTheme.Cyan * 0.50f, 1f);
        renderer.FillRect(spriteBatch,
            new Rectangle(header.X + 8, header.Y + 8, 3, header.Height - 16),
            SelectTheme.Cyan * 0.85f);

        UI.Text(spriteBatch, fonts.Small, "ВЫБОР БОЙЦА",
            new Vector2(header.X + 18, header.Y + 5), SelectTheme.Paper);
        UI.Text(spriteBatch, fonts.Small, $"БОЕЦ {selected + 1:00} / {total:00}",
            new Vector2(SelectLayout.ContentX, 16), SelectTheme.Ice);
        UI.Text(spriteBatch, fonts.Small, "Esc — назад",
            new Vector2(SelectLayout.ContentRight, 16), SelectTheme.Muted, TextAlign.Right);
    }

    public static void DrawFighterTabs(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int selected,
        int total,
        bool focused,
        Vector2 mouse,
        float time)
    {
        int first = SelectLayout.FirstVisible(selected, total);
        float pulse = MenuTheme.Pulse(time);

        for (int slot = 0; slot < SelectLayout.VisibleTabs; slot++)
        {
            int index = first + slot;
            if (index >= total)
                break;

            Rectangle rect = SelectLayout.FighterTab(slot);
            bool isSelected = index == selected;
            bool hovered = Contains(rect, mouse);
            Vector2[] points = Plaque(rect, 5, 7);

            Color fill = isSelected
                ? SelectTheme.Cyan * (0.90f + 0.08f * pulse)
                : hovered ? SelectTheme.Ink * 0.88f : SelectTheme.Ink * 0.80f;
            Color border = isSelected
                ? Color.Lerp(SelectTheme.Cyan, SelectTheme.White, pulse * 0.35f)
                : hovered ? SelectTheme.Cyan * 0.55f : SelectTheme.Azure * 0.45f;

            renderer.FillPolygon(spriteBatch, points, fill);
            renderer.DrawPolygonBorder(spriteBatch, points, border,
                isSelected ? (focused ? 2.5f : 1.5f) : 1f);

            if (isSelected)
            {
                renderer.FillRect(spriteBatch,
                    new Rectangle(rect.X + 8, rect.Y + 6, 3, rect.Height - 12),
                    SelectTheme.Ink * 0.75f);
            }

            UI.Text(spriteBatch, fonts.Small, $"{index + 1:00}",
                new Vector2(rect.X + 18, rect.Y + 6),
                isSelected ? SelectTheme.Ink * 0.75f : SelectTheme.Muted);
            UI.Text(spriteBatch, fonts.Name, FighterCatalog.All[index].Name.ToUpperInvariant(),
                new Vector2(rect.X + 42, rect.Y + 3),
                isSelected ? SelectTheme.Ink : SelectTheme.Paper * 0.82f);
        }
    }

    public static void DrawHeroName(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        string name,
        float alpha,
        float shift)
    {
        string text = name.ToUpperInvariant();
        var font = fonts.Display;
        if (font.MeasureString(text).X > SelectLayout.ContentWidth - 8)
            font = fonts.Title;

        var position = new Vector2(SelectLayout.ContentX + shift, 40);

        UI.Text(spriteBatch, font, text, position + new Vector2(5f, 5f), SelectTheme.Ink * (0.55f * alpha));
        UI.Text(spriteBatch, font, text, position + new Vector2(3f, 3f), SelectTheme.Royal * (0.90f * alpha));
        UI.Text(spriteBatch, font, text, position, SelectTheme.Paper * alpha);

        int lineHeight = (int)font.LineHeight;
        renderer.FillRect(spriteBatch,
            new Rectangle((int)position.X, (int)position.Y + lineHeight + 2, 84, 5),
            SelectTheme.Cyan * alpha);
        renderer.FillRect(spriteBatch,
            new Rectangle((int)position.X + 92, (int)position.Y + lineHeight + 2, 16, 5),
            SelectTheme.Pink * alpha);
    }

    public static void DrawTagline(
        SpriteBatch spriteBatch,
        FontSet fonts,
        FighterProfile profile,
        Color accent,
        float alpha)
    {
        UI.Text(spriteBatch, fonts.Name, profile.Tagline,
            new Vector2(SelectLayout.ContentX, 120), accent * alpha);
    }

    public static void DrawRule(SpriteBatch spriteBatch, Renderer renderer, float alpha)
    {
        renderer.FillRect(spriteBatch,
            new Rectangle(SelectLayout.ContentX, 148, SelectLayout.ContentWidth, 2),
            SelectTheme.Cyan * (0.45f * alpha));
    }

    public static void DrawStats(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        FighterProfile profile,
        float alpha)
    {
        string damage = profile.HasHeavyAttacks
            ? $"{Balance.LightDamage:0} / {Balance.HeavyDamage:0}"
            : $"{Balance.LightDamage:0} / —";

        DrawStatCell(spriteBatch, renderer, fonts, 0, "HP", $"{Balance.MaxHp:0}", alpha);
        DrawStatCell(spriteBatch, renderer, fonts, 1, "СКОРОСТЬ", $"{Balance.WalkSpeed:0}", alpha);
        DrawStatCell(spriteBatch, renderer, fonts, 2, "УРОН Л / Т", damage, alpha);
    }

    private static void DrawStatCell(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int index,
        string label,
        string value,
        float alpha)
    {
        var rect = new Rectangle(
            SelectLayout.ContentX + index * (StatWidth + StatGap),
            156,
            StatWidth,
            56);

        Vector2[] points = Plaque(rect, 6, 8);
        renderer.FillPolygon(spriteBatch, points, SelectTheme.Ink * alpha);
        renderer.DrawPolygonBorder(spriteBatch, points, SelectTheme.Cyan * (0.32f * alpha), 1f);
        renderer.FillRect(spriteBatch,
            new Rectangle(rect.X + 12, rect.Bottom - 4, 26, 2),
            SelectTheme.Cyan * (0.80f * alpha));

        UI.Text(spriteBatch, fonts.Small, label, new Vector2(rect.X + 12, rect.Y + 7), SelectTheme.Ice * (0.82f * alpha));
        UI.Text(spriteBatch, fonts.Big, value, new Vector2(rect.X + 12, rect.Y + 24), SelectTheme.Paper * alpha);
    }

    public static void DrawDescription(
        SpriteBatch spriteBatch,
        FontSet fonts,
        FighterProfile profile,
        float alpha)
    {
        MenuScreen.DrawWrapped(
            spriteBatch,
            fonts.Small,
            profile.Description,
            new Vector2(SelectLayout.ContentX, 220),
            SelectLayout.ContentWidth,
            SelectTheme.Paper * (0.86f * alpha),
            2);
    }

    public static void DrawSpecials(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        FighterProfile profile,
        Color accent,
        float alpha)
    {
        DrawSpecialRow(spriteBatch, renderer, fonts, 264, "Z", profile.SpecialZName, profile.SpecialZDetail, accent, alpha);
        renderer.FillRect(spriteBatch,
            new Rectangle(SelectLayout.ContentX, 294, SelectLayout.ContentWidth, 1),
            SelectTheme.Cyan * (0.16f * alpha));
        DrawSpecialRow(spriteBatch, renderer, fonts, 296, "X", profile.SpecialXName, profile.SpecialXDetail, accent, alpha);
    }

    private static void DrawSpecialRow(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int y,
        string key,
        string name,
        string detail,
        Color accent,
        float alpha)
    {
        DrawKeycap(spriteBatch, renderer, fonts, new Rectangle(SelectLayout.ContentX, y, 22, 22), key, accent, alpha);
        UI.Text(spriteBatch, fonts.Name, name, new Vector2(SelectLayout.ContentX + 30, y), SelectTheme.Paper * alpha);
        UI.Text(spriteBatch, fonts.Small, detail,
            new Vector2(SelectLayout.ContentRight, y + 4), SelectTheme.Muted * alpha, TextAlign.Right);
    }

    private static void DrawKeycap(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        Rectangle rect,
        string key,
        Color accent,
        float alpha)
    {
        Vector2[] points = Plaque(rect, 3, 4);
        renderer.FillPolygon(spriteBatch, points, accent * (0.90f * alpha));
        renderer.DrawPolygonBorder(spriteBatch, points, SelectTheme.Paper * (0.45f * alpha), 1f);
        UI.Text(spriteBatch, fonts.Small, key,
            new Vector2(rect.Center.X, rect.Center.Y), SelectTheme.Ink * alpha, TextAlign.Center);
    }

    public static void DrawDifficulty(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        int selected,
        bool focused,
        Vector2 mouse,
        float time)
    {
        UI.Text(spriteBatch, fonts.Small, "СЛОЖНОСТЬ БОТА",
            new Vector2(SelectLayout.ContentX, 328),
            focused ? SelectTheme.Cyan : SelectTheme.Muted);

        float pulse = MenuTheme.Pulse(time);

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            Rectangle rect = SelectLayout.DifficultyPill(i);
            bool isSelected = i == selected;
            bool hovered = Contains(rect, mouse);
            Vector2[] points = Plaque(rect, 6, 7);

            Color fill = isSelected
                ? SelectTheme.Cyan * (0.86f + 0.10f * pulse)
                : hovered ? SelectTheme.Ink * 0.90f : SelectTheme.Ink * 0.80f;
            Color border = isSelected
                ? Color.Lerp(SelectTheme.Cyan, SelectTheme.White, pulse * 0.40f)
                : SelectTheme.Cyan * (hovered ? 0.60f : 0.30f);

            renderer.FillPolygon(spriteBatch, points, fill);
            renderer.DrawPolygonBorder(spriteBatch, points, border, isSelected ? 2f : 1f);

            UI.Text(spriteBatch, fonts.Name, DifficultyCatalog.All[i].Title,
                new Vector2(rect.Center.X, rect.Center.Y),
                isSelected ? SelectTheme.Ink : SelectTheme.Paper,
                TextAlign.Center);
        }

        MenuScreen.DrawWrapped(
            spriteBatch,
            fonts.Small,
            DifficultyCatalog.All[selected].Description,
            new Vector2(SelectLayout.ContentX, 376),
            SelectLayout.ContentWidth,
            SelectTheme.Muted,
            1);
    }

    public static void DrawStartButton(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        bool focused,
        bool hovered,
        float time)
    {
        Rectangle rect = SelectLayout.StartButton;
        Vector2[] points = Plaque(rect, 12, 14);
        float pulse = MenuTheme.Pulse(time);

        Color fill = focused
            ? SelectTheme.Cyan
            : hovered ? SelectTheme.Ice : SelectTheme.Ink * 0.80f;
        Color text = focused || hovered ? SelectTheme.Ink : SelectTheme.Paper;
        Color border = focused
            ? Color.Lerp(SelectTheme.Cyan, SelectTheme.White, pulse * 0.50f)
            : SelectTheme.Cyan * (hovered ? 0.75f : 0.45f);

        renderer.FillPolygon(spriteBatch, points, fill);
        renderer.DrawPolygonBorder(spriteBatch, points, border, focused ? 3f : 2f);

        for (int i = 0; i < 6; i++)
        {
            float x = rect.X + 28 + i * 64;
            renderer.DrawLine(spriteBatch,
                new Vector2(x, rect.Bottom - 6),
                new Vector2(x + 22, rect.Y + 6),
                SelectTheme.Ink * 0.08f,
                6f);
        }

        if (focused)
        {
            renderer.FillRect(spriteBatch,
                new Rectangle(rect.X + 10, rect.Y + 8, 4, rect.Height - 16),
                SelectTheme.Ink * 0.75f);
        }

        UI.Text(spriteBatch, fonts.Big, "НАЧАТЬ БОЙ",
            new Vector2(rect.Center.X - 10, rect.Center.Y), text, TextAlign.Center);

        var arrow = new[]
        {
            new Vector2(rect.Right - 36, rect.Center.Y - 6),
            new Vector2(rect.Right - 36, rect.Center.Y + 6),
            new Vector2(rect.Right - 24, rect.Center.Y),
        };
        renderer.FillPolygon(spriteBatch, arrow, text);
    }

    public static void DrawHint(SpriteBatch spriteBatch, FontSet fonts)
    {
        UI.Text(spriteBatch, fonts.Small,
            "← → БОЕЦ · ↑ ↓ РАЗДЕЛ · ENTER БОЙ",
            new Vector2(SelectLayout.ContentX, 452),
            SelectTheme.Ice * 0.72f,
            TextAlign.Left);
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

    private static bool Contains(Rectangle rectangle, Vector2 point) =>
        point.X >= rectangle.X && point.X < rectangle.Right
        && point.Y >= rectangle.Y && point.Y < rectangle.Bottom;
}
