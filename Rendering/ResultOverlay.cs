using DvorFight.Combat;
using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Result panel drawn on top of the finished fight: winner, matchup, difficulty and
/// the rematch / back-to-select controls.
/// </summary>
public sealed class ResultOverlay
{
    public void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, FightSession session)
    {
        bool win = session.Winner != null && ReferenceEquals(session.Winner, session.Player);
        string text = session.Winner == null ? "НИЧЬЯ" : win ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        var color = session.Winner == null
            ? new Color(0xd1, 0xce, 0xd9)
            : win ? new Color(0xff, 0xd9, 0x6c) : new Color(0xfc, 0x89, 0x78);

        renderer.FillRect(spriteBatch,
            new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight),
            new Color(0x08, 0x0a, 0x12) * 0.62f);

        var panel = new Rectangle(200, 140, 560, 200);
        renderer.DrawPanel(spriteBatch, panel,
            new Color(0x17, 0x1a, 0x26) * 0.97f, new Color(0x64, 0x65, 0x78), 3);

        UI.ShadowedText(spriteBatch, fonts.Title, text,
            new Vector2(Balance.ViewWidth / 2f, 194), color, TextAlign.Center, 3f);

        UI.Text(spriteBatch, fonts.Small,
            $"{session.Player.Title} против {session.Bot.Title} · сложность «{DifficultyCatalog.For(session.Difficulty).Title}»",
            new Vector2(Balance.ViewWidth / 2f, 250), MenuTheme.Text, TextAlign.Center);

        UI.Text(spriteBatch, fonts.Small,
            "R / Enter / клик — Реванш      Esc — Выбор бойца",
            new Vector2(Balance.ViewWidth / 2f, 292), Color.White, TextAlign.Center);
    }
}
