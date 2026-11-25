using DvorFight.Config;
using DvorFight.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Character / stats screen: animated fighter cards driven by <see cref="FighterCatalog"/>,
/// a stats panel whose numbers are read from <see cref="Balance"/>, and the bot difficulty
/// choice that is passed into the fight. Supports any roster size — extra fighters are
/// reached with the arrow keys (two cards are visible at a time).
/// </summary>
public sealed class FighterSelectScreen : MenuScreen
{
    private enum FocusArea
    {
        Fighters,
        Difficulty,
        Start,
    }

    private const int CardWidth = 200;
    private const int CardHeight = 258;
    private const int CardGap = 14;
    private const int CardX = 24;
    private const int CardY = 80;
    private const int VisibleCards = 2;
    private const int CardStep = CardWidth + CardGap;

    private const int PanelX = 452;
    private const int PanelY = 80;
    private const int PanelWidth = 484;
    private const int PanelHeight = 258;

    private const int DifficultyTop = 362;
    private const int DifficultyWidth = 128;
    private const int DifficultyHeight = 34;
    private const int DifficultyGap = 8;
    private const int DifficultyX = 24;

    private static readonly Rectangle StartButtonRect = new(PanelX, 356, PanelWidth, 54);

    private static readonly Color CardFill = new Color(0x18, 0x1b, 0x27);
    private static readonly Color CardFillSelected = new Color(0x27, 0x2d, 0x42);
    private static readonly Color CardFillHover = new Color(0x21, 0x26, 0x36);

    private readonly MenuState _state;
    private FocusArea _focus;
    private int _selected;
    private int _difficulty;

    public FighterSelectScreen(MenuState state, bool focusStart)
    {
        _state = state;
        _selected = Math.Clamp(FighterCatalog.IndexOf(state.SelectedFighter), 0, Math.Max(0, FighterCatalog.All.Count - 1));
        _difficulty = IndexOfDifficulty(state.Difficulty);
        _focus = focusStart ? FocusArea.Start : FocusArea.Fighters;
    }

    public override MenuCommand Update(MenuInput input, float dt)
    {
        RememberMouse(input);

        // Number keys still pick a fighter directly, like the old one-screen menu.
        if (input.FighterShortcut is >= 1 and <= 9)
        {
            int shortcut = input.FighterShortcut - 1;
            if (shortcut < FighterCatalog.All.Count)
            {
                _selected = shortcut;
                _focus = FocusArea.Fighters;
                SyncState();
            }
        }

        if (input.Up)
            MoveFocus(-1);
        if (input.Down)
            MoveFocus(1);

        if (input.Left || input.Right)
        {
            int delta = input.Right ? 1 : -1;
            switch (_focus)
            {
                case FocusArea.Fighters:
                    SelectFighter(delta);
                    break;
                case FocusArea.Difficulty:
                    SelectDifficulty(delta);
                    break;
            }
        }

        if (input.MouseMoved)
            UpdateHover(input.MousePosition);

        for (int slot = 0; slot < VisibleCards; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0)
                continue;
            if (!Clicked(CardRect(slot), input))
                continue;

            _selected = index;
            _focus = FocusArea.Fighters;
            SyncState();
        }

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            if (!Clicked(DifficultyRect(i), input))
                continue;

            _difficulty = i;
            _focus = FocusArea.Difficulty;
            SyncState();
        }

        if (Clicked(StartButtonRect, input))
            return Start();

        if (input.Back)
            return MenuCommand.BackToTitle;

        // Enter starts the fight from any focus area: the chosen fighter and difficulty
        // are always visible, so no extra confirmation step is needed.
        if (input.Confirm)
            return Start();

        return MenuCommand.None;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time)
    {
        renderer.FillRect(spriteBatch,
            new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight), MenuTheme.NightOverlay);

        UI.ShadowedText(spriteBatch, fonts.Title, "БОЙЦЫ",
            new Vector2(Balance.ViewWidth / 2f, 34), MenuTheme.TitleText, TextAlign.Center, 3f);
        UI.Text(spriteBatch, fonts.Small, "ХАРАКТЕРИСТИКИ · ВЫБОР СЛОЖНОСТИ",
            new Vector2(Balance.ViewWidth / 2f, 62), MenuTheme.Gold, TextAlign.Center);

        string roster = FighterCatalog.All.Count > VisibleCards
            ? $"БОЙЦОВ: {FighterCatalog.All.Count} · {_selected + 1}/{FighterCatalog.All.Count}"
            : $"БОЙЦОВ: {FighterCatalog.All.Count}";
        UI.Text(spriteBatch, fonts.Small, roster, new Vector2(24, 24), MenuTheme.Muted, TextAlign.Left);
        UI.Text(spriteBatch, fonts.Small, "Esc — назад", new Vector2(936, 24), MenuTheme.Muted, TextAlign.Right);

        DrawCards(spriteBatch, renderer, fonts, time);
        DrawStatsPanel(spriteBatch, renderer, fonts);
        DrawDifficulty(spriteBatch, renderer, fonts);
        DrawButton(spriteBatch, renderer, fonts, StartButtonRect, "НАЧАТЬ БОЙ",
            _focus == FocusArea.Start, Contains(StartButtonRect, MousePosition), time);

        DrawHint(spriteBatch, fonts,
            "← / → — боец / сложность · ↑ / ↓ — раздел · Enter — в бой · Esc — назад", 440);
    }

    private void DrawCards(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time)
    {
        for (int slot = 0; slot < VisibleCards; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0)
                continue;

            var rect = CardRect(slot);
            var profile = FighterCatalog.All[index];
            bool selected = index == _selected;
            bool hovered = Contains(rect, MousePosition);

            renderer.FillRect(spriteBatch,
                new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height), Color.Black * 0.32f);

            var fill = selected ? CardFillSelected : hovered ? CardFillHover : CardFill;
            var border = selected ? MenuTheme.Gold : hovered ? MenuTheme.ButtonBorder : MenuTheme.PanelBorder;
            int thickness = selected ? 3 : 1;
            if (selected && _focus == FocusArea.Fighters)
                border = Color.Lerp(MenuTheme.Gold, Color.White, MenuTheme.Pulse(time) * 0.35f);
            renderer.DrawPanel(spriteBatch, rect, fill, border, thickness);

            UI.Text(spriteBatch, fonts.Small, $"{index + 1:00}",
                new Vector2(rect.X + 12, rect.Y + 10), MenuTheme.MutedDark);
            if (selected)
            {
                UI.Text(spriteBatch, fonts.Small, "ВЫБРАН",
                    new Vector2(rect.Right - 12, rect.Y + 10), MenuTheme.Gold, TextAlign.Right);
            }

            DrawLivePreview(spriteBatch, renderer, profile, rect, index, time);

            UI.Text(spriteBatch, fonts.Name, profile.Name,
                new Vector2(rect.Center.X, rect.Bottom - 44),
                selected ? MenuTheme.Gold : MenuTheme.Text, TextAlign.Center);
            UI.Text(spriteBatch, fonts.Small, profile.CardTagline,
                new Vector2(rect.Center.X, rect.Bottom - 22), MenuTheme.Muted, TextAlign.Center);
        }
    }

    /// <summary>Cycles the walk animation slowly, so the card shows the fighter's live stance.</summary>
    private static void DrawLivePreview(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FighterProfile profile,
        Rectangle card,
        int index,
        float time)
    {
        int frames = renderer.Assets.Get(profile.IdleAnimation)?.Count ?? 1;
        int frame = (int)(time * 6f + index * 1.7f) % frames;
        float bob = MathF.Sin(time * 2.4f + index * 1.4f) * 2.5f;
        float feetY = card.Bottom - 66 + bob;

        // Soft contact shadow under the animated fighter.
        renderer.FillRect(spriteBatch,
            new Rectangle(card.Center.X - 48, (int)feetY - 3, 96, 5), Color.Black * 0.35f);

        renderer.DrawSprite(spriteBatch, profile.IdleAnimation, frame,
            card.Center.X, feetY, SpriteEffects.None, 1f, CardWidth * 0.78f);
    }

    private void DrawStatsPanel(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts)
    {
        var profile = FighterCatalog.All[_selected];
        var panel = new Rectangle(PanelX, PanelY, PanelWidth, PanelHeight);
        renderer.DrawPanel(spriteBatch, panel, MenuTheme.PanelFill, MenuTheme.PanelBorder, 1);

        int left = panel.X + 20;
        int width = panel.Width - 40;

        UI.ShadowedText(spriteBatch, fonts.Big, profile.Name,
            new Vector2(left, panel.Y + 16), MenuTheme.TitleText, TextAlign.Left, 2f);
        UI.Text(spriteBatch, fonts.Small, profile.Tagline,
            new Vector2(left, panel.Y + 46), MenuTheme.Gold, TextAlign.Left);

        renderer.FillRect(spriteBatch,
            new Rectangle(left, panel.Y + 68, width, 1), MenuTheme.PanelBorder * 0.8f);

        int column = width / 3;
        string damage = profile.HasHeavyAttacks
            ? $"{Balance.LightDamage:0} / {Balance.HeavyDamage:0}"
            : $"{Balance.LightDamage:0} / —";
        DrawStat(spriteBatch, fonts, left, panel.Y + 78, "ЗДОРОВЬЕ", $"{Balance.MaxHp:0}");
        DrawStat(spriteBatch, fonts, left + column, panel.Y + 78, "СКОРОСТЬ", $"{Balance.WalkSpeed:0} px/с");
        DrawStat(spriteBatch, fonts, left + column * 2, panel.Y + 78, "УРОН Л / Т", damage);

        DrawWrapped(spriteBatch, fonts.Small, profile.Description,
            new Vector2(left, panel.Y + 120), width, MenuTheme.Text, 2);

        renderer.FillRect(spriteBatch,
            new Rectangle(left, panel.Y + 158, width, 1), MenuTheme.PanelBorder * 0.8f);

        DrawSpecial(spriteBatch, fonts, left, panel.Y + 166, "Z", profile.SpecialZName, profile.SpecialZDetail);
        DrawSpecial(spriteBatch, fonts, left, panel.Y + 212, "X", profile.SpecialXName, profile.SpecialXDetail);
    }

    private static void DrawStat(
        SpriteBatch spriteBatch,
        FontSet fonts,
        int x,
        int y,
        string label,
        string value)
    {
        UI.Text(spriteBatch, fonts.Small, label, new Vector2(x, y), MenuTheme.Muted);
        UI.Text(spriteBatch, fonts.Name, value, new Vector2(x, y + 18), MenuTheme.Text);
    }

    private static void DrawSpecial(
        SpriteBatch spriteBatch,
        FontSet fonts,
        int x,
        int y,
        string key,
        string name,
        string detail)
    {
        UI.Text(spriteBatch, fonts.Small, key, new Vector2(x, y + 2), MenuTheme.Gold);
        UI.Text(spriteBatch, fonts.Name, name, new Vector2(x + 18, y), MenuTheme.Text);
        UI.Text(spriteBatch, fonts.Small, detail, new Vector2(x + 18, y + 20), MenuTheme.Muted);
    }

    private void DrawDifficulty(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts)
    {
        bool focused = _focus == FocusArea.Difficulty;
        UI.Text(spriteBatch, fonts.Small, "СЛОЖНОСТЬ БОТА",
            new Vector2(DifficultyX, 344), focused ? MenuTheme.Gold : MenuTheme.Muted);

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            var rect = DifficultyRect(i);
            bool selected = i == _difficulty;
            bool hovered = Contains(rect, MousePosition);
            var fill = selected ? (focused ? MenuTheme.ButtonFocusedFill : MenuTheme.ChoiceSelectedFill)
                : hovered ? MenuTheme.ButtonHoverFill : MenuTheme.ButtonFill;
            var border = selected ? MenuTheme.Gold : hovered ? MenuTheme.ButtonBorder : MenuTheme.PanelBorder;

            renderer.DrawPanel(spriteBatch, rect, fill, border, selected ? 2 : 1);
            if (selected)
            {
                renderer.FillRect(spriteBatch,
                    new Rectangle(rect.X + 6, rect.Bottom - 4, rect.Width - 12, 2), MenuTheme.Gold);
            }

            UI.Text(spriteBatch, fonts.Name, DifficultyCatalog.All[i].Title,
                new Vector2(rect.Center.X, rect.Center.Y),
                selected ? MenuTheme.TitleText : MenuTheme.Text, TextAlign.Center);
        }

        DrawWrapped(spriteBatch, fonts.Small, DifficultyCatalog.All[_difficulty].Description,
            new Vector2(DifficultyX, 406), 412, MenuTheme.Muted, 1);
    }

    private void UpdateHover(Vector2 mouse)
    {
        for (int slot = 0; slot < VisibleCards; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Contains(CardRect(slot), mouse))
                continue;

            _focus = FocusArea.Fighters;
            if (index != _selected)
            {
                _selected = index;
                SyncState();
            }
            return;
        }

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            if (!Contains(DifficultyRect(i), mouse))
                continue;

            _focus = FocusArea.Difficulty;
            if (i != _difficulty)
            {
                _difficulty = i;
                SyncState();
            }
            return;
        }

        if (Contains(StartButtonRect, mouse))
            _focus = FocusArea.Start;
    }

    private void MoveFocus(int delta)
    {
        int count = 3;
        _focus = (FocusArea)(((int)_focus + delta + count) % count);
    }

    private void SelectFighter(int delta)
    {
        _selected = Wrap(_selected, delta, FighterCatalog.All.Count);
        SyncState();
    }

    private void SelectDifficulty(int delta)
    {
        _difficulty = Wrap(_difficulty, delta, DifficultyCatalog.All.Count);
        SyncState();
    }

    private void SyncState()
    {
        _state.SelectedFighter = FighterCatalog.All[_selected].Kind;
        _state.Difficulty = DifficultyCatalog.All[_difficulty].Difficulty;
    }

    private MenuCommand Start() =>
        MenuCommand.StartFight(_state.SelectedFighter, _state.Difficulty);

    private static int IndexOfDifficulty(BotDifficulty difficulty)
    {
        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
            if (DifficultyCatalog.All[i].Difficulty == difficulty)
                return i;
        return 0;
    }

    private int FirstVisible
    {
        get
        {
            int count = FighterCatalog.All.Count;
            return Math.Clamp(_selected - 1, 0, Math.Max(0, count - VisibleCards));
        }
    }

    private int VisibleIndex(int slot)
    {
        int index = FirstVisible + slot;
        return index < FighterCatalog.All.Count ? index : -1;
    }

    private static Rectangle CardRect(int slot) =>
        new(CardX + slot * CardStep, CardY, CardWidth, CardHeight);

    private static Rectangle DifficultyRect(int index) =>
        new(DifficultyX + index * (DifficultyWidth + DifficultyGap), DifficultyTop, DifficultyWidth, DifficultyHeight);
}
