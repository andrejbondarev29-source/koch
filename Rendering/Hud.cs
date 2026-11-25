using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Domain;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>Draws the health bars, fighter names and the player's cooldown readout.</summary>
public sealed class Hud
{
    public void Draw(
        SpriteBatch spriteBatch,
        Renderer renderer,
        DynamicSpriteFont nameFont,
        DynamicSpriteFont smallFont,
        FightSession session)
    {
        DrawHpBar(spriteBatch, renderer, 28f, session.Player, rightAnchored: false);
        DrawHpBar(spriteBatch, renderer, 572f, session.Bot, rightAnchored: true);

        UI.ShadowedText(spriteBatch, nameFont, $"{session.Player.Title} · ТЫ", new Vector2(28, 32), Color.White, TextAlign.Left, 1f);
        UI.ShadowedText(spriteBatch, nameFont, $"{session.Bot.Title} · БОТ", new Vector2(932, 32), Color.White, TextAlign.Right, 1f);

        string z = session.Player.SpecialCooldown > 0f
            ? session.Player.SpecialCooldown.ToString("0.0")
            : "готово";
        string x = session.Player.XCooldown > 0f
            ? session.Player.XCooldown.ToString("0.0")
            : "готово";
        string morph = session.Player.IsMorphed
            ? $"     Метаморфоза {Math.Ceiling(session.Player.MorphTimer)} с"
            : "";

        UI.Text(spriteBatch, smallFont, $"Z  {z}     X  {x}{morph}", new Vector2(28, 451), Color.White, TextAlign.Left);
    }

    private static void DrawHpBar(SpriteBatch spriteBatch, Renderer renderer, float x, Fighter fighter, bool rightAnchored)
    {
        var bar = new Rectangle((int)x, 40, (int)Balance.HpBarWidth, 17);
        renderer.FillRect(spriteBatch, bar, new Color(0x20, 0x20, 0x2e));

        var color = rightAnchored ? new Color(0xfc, 0x89, 0x78) : new Color(0xed, 0xcd, 0x68);
        int width = (int)(fighter.Hp * Balance.HpScale);
        int left = rightAnchored
            ? (int)(x + (Balance.MaxHp - fighter.Hp) * Balance.HpScale)
            : (int)x;

        renderer.FillRect(spriteBatch, new Rectangle(left, 40, width, 17), color);
        renderer.DrawBorder(spriteBatch, bar, new Color(0x44, 0x47, 0x59), 1);
    }
}
