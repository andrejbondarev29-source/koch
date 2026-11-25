using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Title screen: game name over the night arena, plus «Играть», «Бойцы / Характеристики»
/// and «Выход». Navigation works with arrows / W-S / Enter and with the mouse.
/// </summary>
public sealed class TitleScreen : MenuScreen
{
    private const string GameTitle = "ДВОРОВОЙ ФАЙТИНГ";
    private static readonly string[] ButtonLabels =
    {
        "ИГРАТЬ",
        "БОЙЦЫ / ХАРАКТЕРИСТИКИ",
        "ВЫХОД",
    };

    private const int ButtonWidth = 400;
    private const int ButtonHeight = 44;
    private const int ButtonTop = 204;
    private const int ButtonStep = 54;

    private int _selected;
    private float _time;
    private readonly Ember[] _embers = new Ember[26];

    public TitleScreen()
    {
        for (int i = 0; i < _embers.Length; i++)
        {
            _embers[i] = new Ember
            {
                X = Random.Shared.Next(20, Balance.ViewWidth - 20),
                Y = Random.Shared.Next(40, Balance.ViewHeight),
                Speed = 8f + (float)Random.Shared.NextDouble() * 22f,
                Size = Random.Shared.Next(1, 3),
                Phase = (float)Random.Shared.NextDouble() * MathF.Tau,
            };
        }
    }

    public override MenuCommand Update(MenuInput input, float dt)
    {
        _time += dt;
        RememberMouse(input);
        UpdateEmbers(dt);

        if (input.Up)
            _selected = Wrap(_selected, -1, ButtonLabels.Length);
        if (input.Down)
            _selected = Wrap(_selected, 1, ButtonLabels.Length);

        for (int i = 0; i < ButtonLabels.Length; i++)
        {
            var rect = ButtonRect(i);
            if (input.MouseMoved && Contains(rect, input.MousePosition))
                _selected = i;
            if (Clicked(rect, input))
            {
                _selected = i;
                return Activate(_selected);
            }
        }

        if (input.Confirm)
            return Activate(_selected);

        return MenuCommand.None;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time)
    {
        // Dusk arena -> night arena: cool dark tint keeps the skyline as a backdrop.
        renderer.FillRect(spriteBatch,
            new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight), MenuTheme.NightOverlay);

        DrawEmbers(spriteBatch, renderer, time);

        var titleSize = fonts.Title.MeasureString(GameTitle);
        float titleLeft = Balance.ViewWidth / 2f - titleSize.X / 2f;
        if (titleLeft > 110f)
        {
            renderer.FillRect(spriteBatch, new Rectangle((int)titleLeft - 108, 103, 78, 2), MenuTheme.Gold * 0.65f);
            renderer.FillRect(spriteBatch, new Rectangle((int)(titleLeft + titleSize.X + 30), 103, 78, 2), MenuTheme.Gold * 0.65f);
        }

        UI.ShadowedText(spriteBatch, fonts.Title, GameTitle,
            new Vector2(Balance.ViewWidth / 2f, 104), MenuTheme.TitleText, TextAlign.Center, 3f);
        UI.Text(spriteBatch, fonts.Small, "НОЧНАЯ АРЕНА · УЛИЧНЫЙ ТУРНИР",
            new Vector2(Balance.ViewWidth / 2f, 148), MenuTheme.Gold, TextAlign.Center);

        for (int i = 0; i < ButtonLabels.Length; i++)
        {
            var rect = ButtonRect(i);
            bool focused = i == _selected;
            bool hovered = Contains(rect, MousePosition);
            DrawButton(spriteBatch, renderer, fonts, rect, ButtonLabels[i], focused, hovered, time,
                accent: i == 2 ? MenuTheme.Danger : MenuTheme.Gold);
        }

        DrawHint(spriteBatch, fonts,
            "↑ / ↓ или W / S — выбор · Enter — подтвердить · мышь — навести и клик", 414);
        UI.Text(spriteBatch, fonts.Small, ".NET 8 · MonoGame DesktopGL",
            new Vector2(Balance.ViewWidth / 2f, 444), MenuTheme.MutedDark, TextAlign.Center);
    }

    private static Rectangle ButtonRect(int index) =>
        new((Balance.ViewWidth - ButtonWidth) / 2, ButtonTop + index * ButtonStep, ButtonWidth, ButtonHeight);

    private static MenuCommand Activate(int index) => index switch
    {
        0 => MenuCommand.PlayGame,
        1 => MenuCommand.OpenFighters,
        _ => MenuCommand.ExitGame,
    };

    private void UpdateEmbers(float dt)
    {
        for (int i = 0; i < _embers.Length; i++)
        {
            ref Ember ember = ref _embers[i];
            ember.Y -= ember.Speed * dt;
            ember.X += MathF.Sin(_time * 0.9f + ember.Phase) * 5f * dt;
            if (ember.Y > 24f)
                continue;

            ember.Y = Balance.ViewHeight + Random.Shared.Next(10, 80);
            ember.X = Random.Shared.Next(20, Balance.ViewWidth - 20);
        }
    }

    private void DrawEmbers(SpriteBatch spriteBatch, Renderer renderer, float time)
    {
        foreach (var ember in _embers)
        {
            float alpha = 0.10f + 0.22f * (0.5f + 0.5f * MathF.Sin(time * 2f + ember.Phase));
            var color = Color.Lerp(MenuTheme.Gold, new Color(0xff, 0x9a, 0x4a), 0.5f) * alpha;
            renderer.FillRect(spriteBatch,
                new Rectangle((int)ember.X, (int)ember.Y, ember.Size, ember.Size + 1), color);
        }
    }

    private struct Ember
    {
        public float X;
        public float Y;
        public float Speed;
        public int Size;
        public float Phase;
    }
}
