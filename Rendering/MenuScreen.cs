using DvorFight.Config;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Base class for the multi-screen menu. Owns the shared navigation/drawing helpers so
/// every concrete screen only describes its own layout and state machine.
/// </summary>
public abstract class MenuScreen
{
    /// <summary>Consumes one input snapshot and optionally asks the shell to change screens.</summary>
    public abstract MenuCommand Update(MenuInput input, float dt);

    /// <summary>Draws the screen on top of the arena background (already drawn by the shell).</summary>
    public abstract void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time);

    /// <summary>Last mouse position in virtual-canvas space, used for hover highlights in Draw.</summary>
    protected Vector2 MousePosition { get; private set; }

    protected void RememberMouse(MenuInput input) => MousePosition = input.MousePosition;

    protected static int Wrap(int index, int delta, int count)
    {
        if (count <= 0)
            return 0;
        return ((index + delta) % count + count) % count;
    }

    protected static bool Contains(Rectangle rectangle, Vector2 point) =>
        point.X >= rectangle.X && point.X < rectangle.Right
        && point.Y >= rectangle.Y && point.Y < rectangle.Bottom;

    protected static bool Clicked(Rectangle rectangle, MenuInput input) =>
        input.MouseClicked && Contains(rectangle, input.MousePosition);

    /// <summary>Rounded, centered hint text near the bottom of the screen.</summary>
    protected static void DrawHint(SpriteBatch spriteBatch, FontSet fonts, string text, float y)
    {
        UI.Text(spriteBatch, fonts.Small, text,
            new Vector2(Balance.ViewWidth / 2f, y), MenuTheme.Muted, TextAlign.Center);
    }

    /// <summary>Standard menu button: panel, focus bar, centered label.</summary>
    protected static void DrawButton(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FontSet fonts,
        Rectangle rectangle,
        string label,
        bool focused,
        bool hovered,
        float time,
        Color? accent = null)
    {
        var border = focused
            ? (accent ?? MenuTheme.Gold)
            : hovered ? MenuTheme.ButtonBorder : MenuTheme.PanelBorder;
        var fill = focused ? MenuTheme.ButtonFocusedFill
            : hovered ? MenuTheme.ButtonHoverFill
            : MenuTheme.ButtonFill;

        renderer.DrawPanel(spriteBatch, rectangle, fill, border, focused ? 2 : 1);

        if (focused)
        {
            float pulse = MenuTheme.Pulse(time);
            var accentColor = Color.Lerp(accent ?? MenuTheme.Gold, Color.White, pulse * 0.45f);
            renderer.FillRect(spriteBatch,
                new Rectangle(rectangle.X + 8, rectangle.Y + 8, 4, rectangle.Height - 16),
                accentColor);
        }

        var textColor = focused ? MenuTheme.TitleText : MenuTheme.Text;
        UI.Text(spriteBatch, fonts.Big, label,
            new Vector2(rectangle.Center.X, rectangle.Center.Y), textColor, TextAlign.Center);
    }

    /// <summary>Word-wrap that respects the real dynamic-font metrics.</summary>
    protected static List<string> WrapText(DynamicSpriteFont font, string text, float maxWidth)
    {
        var lines = new List<string>();
        string current = "";

        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length == 0 || font.MeasureString(candidate).X <= maxWidth)
            {
                current = candidate;
            }
            else
            {
                lines.Add(current);
                current = word;
            }
        }

        if (current.Length > 0)
            lines.Add(current);

        return lines;
    }

    /// <summary>Draws wrapped small text starting at the given top-left point.</summary>
    protected static void DrawWrapped(
        SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        float maxWidth,
        Color color,
        int maxLines = int.MaxValue)
    {
        var lines = WrapText(font, text, maxWidth);
        int count = Math.Min(lines.Count, maxLines);
        for (int i = 0; i < count; i++)
        {
            string line = lines[i];
            if (i == count - 1 && lines.Count > count)
                line += "…";
            UI.Text(spriteBatch, font, line,
                new Vector2(position.X, position.Y + i * (font.LineHeight)), color);
        }
    }
}
