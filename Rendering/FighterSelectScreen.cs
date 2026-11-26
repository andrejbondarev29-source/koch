using DvorFight.Config;
using DvorFight.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Detailed character / stats screen: a large animated portrait on the left, the fighter's
/// name and stats on the right, the bot difficulty and a click-to-start prompt. A click on
/// a fighter tab or on the portrait starts the fight; there is no separate start button.
/// Switching fighters plays a short eased slide while the portrait keeps breathing.
/// </summary>
public sealed class FighterSelectScreen : MenuScreen
{
    private enum FocusArea
    {
        Fighters,
        Difficulty,
    }

    private readonly MenuState _state;
    private readonly ShardField _shards = new();
    private readonly SelectBackdrop _backdrop = new();
    private readonly PortraitStage _portrait = new();

    private FocusArea _focus;
    private int _selected;
    private int _previousSelected;
    private int _difficulty;

    private float _time;
    private float _intro;
    private float _slide = 1f;
    private int _slideDir = 1;

    public FighterSelectScreen(MenuState state)
    {
        _state = state;
        _selected = Math.Clamp(FighterCatalog.IndexOf(state.SelectedFighter), 0, Math.Max(0, FighterCatalog.All.Count - 1));
        _previousSelected = _selected;
        _difficulty = IndexOfDifficulty(state.Difficulty);
        _focus = FocusArea.Fighters;
    }

    /// <summary>Test hook: true while the portrait is sliding from the previous fighter.</summary>
    internal bool IsTransitioning => _slide < 1f;

    /// <summary>Test hook: 0..1 screen entrance progress.</summary>
    internal float IntroProgress => _intro;

    /// <summary>Test hook: 0..1 fighter-switch progress.</summary>
    internal float TransitionProgress => _slide;

    /// <summary>Test hook: horizontal offset of the incoming portrait (0 when settled).</summary>
    internal float PortraitOffsetX =>
        _slideDir * SelectMotion.SwapDistance * (1f - SelectMotion.EaseOutCubic(_slide));

    public override MenuCommand Update(MenuInput input, float dt)
    {
        _time += dt;
        _intro = Math.Min(1f, _intro + dt / SelectMotion.IntroDuration);

        if (_slide < 1f)
            _slide = Math.Min(1f, _slide + dt / SelectMotion.SwapDuration);

        _shards.Update(dt);
        RememberMouse(input);

        // Number keys still pick a fighter directly, like the old one-screen menu.
        if (input.FighterShortcut is >= 1 and <= 9)
        {
            int shortcut = input.FighterShortcut - 1;
            if (shortcut < FighterCatalog.All.Count)
            {
                SelectFighterIndex(shortcut);
                _focus = FocusArea.Fighters;
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

        for (int slot = 0; slot < SelectLayout.VisibleTabs; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0)
                continue;
            if (!Clicked(SelectLayout.FighterTab(slot), input))
                continue;

            _focus = FocusArea.Fighters;
            SelectFighterIndex(index);
            return Start();
        }

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            if (!Clicked(SelectLayout.DifficultyPill(i), input))
                continue;

            _difficulty = i;
            _focus = FocusArea.Difficulty;
            SyncState();
        }

        // A click on the large portrait is the primary "start fight" gesture now that
        // there is no start button.
        if (Clicked(SelectLayout.Stage, input))
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
        _ = time; // the screen keeps its own clock so intro/swap start at zero

        _backdrop.Draw(spriteBatch, renderer, _time);
        _shards.DrawBack(spriteBatch, renderer, _time);

        float intro = SelectMotion.EaseOutQuint(SelectMotion.Clamp01(_intro / 0.75f));
        float introText = SelectMotion.EaseOutQuint(SelectMotion.Clamp01((_intro - 0.08f) / 0.70f));
        float p = _slide;
        bool swapping = p < 1f;

        var profile = FighterCatalog.All[_selected];
        float accentPhase = _selected * 1.7f;
        float bob = MathF.Sin(_time * SelectMotion.BreathSpeed + accentPhase) * SelectMotion.BobAmplitude;
        float sway = MathF.Sin(_time * SelectMotion.SwaySpeed + accentPhase) * SelectMotion.SwayAmplitude;
        float tilt = MathF.Sin(_time * 0.70f + accentPhase) * SelectMotion.IdleTilt;
        float idleScale = 1f + MathF.Sin(_time * SelectMotion.BreathSpeed + accentPhase) * SelectMotion.IdleScale;
        float introY = (1f - intro) * 36f;
        float introScale = 0.94f + 0.06f * intro;

        GetMouseParallax(out float parallaxX, out float parallaxY);

        float baseRotation = MathHelper.ToRadians(profile.PortraitRotationDegrees);

        float outX = -_slideDir * SelectMotion.SwapDistance * SelectMotion.EaseInCubic(p);
        float inX = _slideDir * SelectMotion.SwapDistance * (1f - SelectMotion.EaseOutCubic(p));
        float outAlpha = 1f - SelectMotion.SmoothStep(0f, 0.65f, p);
        float inAlpha = SelectMotion.SmoothStep(0.10f, 0.85f, p);

        if (swapping && _previousSelected != _selected)
        {
            var previous = FighterCatalog.All[_previousSelected];
            float previousTilt = MathF.Sin(_time * 0.70f + _previousSelected * 1.7f) * SelectMotion.IdleTilt;
            var previousCenter = new Vector2(
                SelectLayout.PortraitCenterX + sway + parallaxX + outX,
                SelectLayout.PortraitCenterY + bob + parallaxY + introY);

            _portrait.Draw(
                spriteBatch,
                renderer,
                previous,
                previousCenter,
                MathHelper.ToRadians(previous.PortraitRotationDegrees) + previousTilt + _slideDir * 0.05f * p,
                idleScale * introScale * (1f - 0.05f * p),
                intro * outAlpha,
                _time);
        }

        var currentCenter = new Vector2(
            SelectLayout.PortraitCenterX + sway + parallaxX + inX,
            SelectLayout.PortraitCenterY + bob + parallaxY + introY);
        float currentRotation = baseRotation + tilt - _slideDir * 0.05f * (1f - SelectMotion.EaseOutCubic(p));
        float currentScale = idleScale * introScale * (swapping ? 0.94f + 0.06f * SelectMotion.EaseOutCubic(p) : 1f);
        float currentAlpha = intro * (swapping ? inAlpha : 1f);

        _portrait.Draw(
            spriteBatch,
            renderer,
            profile,
            currentCenter,
            currentRotation,
            currentScale,
            currentAlpha,
            _time);

        _shards.DrawFront(spriteBatch, renderer, _time);
        SelectUi.DrawRightVeil(spriteBatch, renderer);

        SelectUi.DrawHeader(spriteBatch, renderer, fonts, _selected, FighterCatalog.All.Count);
        SelectUi.DrawFighterTabs(spriteBatch, renderer, fonts, _selected, FighterCatalog.All.Count,
            _focus == FocusArea.Fighters, MousePosition, _time);

        float textAlpha = introText * (1f - 0.35f * MathF.Sin(MathF.PI * p));
        float textShift = swapping ? _slideDir * 8f * (1f - SelectMotion.EaseOutCubic(p)) : 0f;
        Color accent = SelectTheme.AccentFor(profile.Kind);

        SelectUi.DrawHeroName(spriteBatch, renderer, fonts, profile.Name, textAlpha, textShift);
        SelectUi.DrawTagline(spriteBatch, fonts, profile, accent, textAlpha);
        SelectUi.DrawRule(spriteBatch, renderer, textAlpha);
        SelectUi.DrawStats(spriteBatch, renderer, fonts, profile, textAlpha);
        SelectUi.DrawDescription(spriteBatch, fonts, profile, textAlpha);
        SelectUi.DrawSpecials(spriteBatch, renderer, fonts, profile, accent, textAlpha);

        SelectUi.DrawDifficulty(spriteBatch, renderer, fonts, _difficulty,
            _focus == FocusArea.Difficulty, MousePosition, _time);
        SelectUi.DrawStartPrompt(spriteBatch, renderer, fonts);
        SelectUi.DrawHint(spriteBatch, fonts);
    }

    private void GetMouseParallax(out float x, out float y)
    {
        x = 0f;
        y = 0f;
        if (!Contains(SelectLayout.Stage, MousePosition))
            return;

        x = (MousePosition.X - SelectLayout.PortraitCenterX) / SelectLayout.PortraitCenterX * -6f;
        y = (MousePosition.Y - SelectLayout.PortraitCenterY) / SelectLayout.PortraitCenterY * -4f;
    }

    private void UpdateHover(Vector2 mouse)
    {
        for (int slot = 0; slot < SelectLayout.VisibleTabs; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Contains(SelectLayout.FighterTab(slot), mouse))
                continue;

            _focus = FocusArea.Fighters;
            SelectFighterIndex(index);
            return;
        }

        for (int i = 0; i < DifficultyCatalog.All.Count; i++)
        {
            if (!Contains(SelectLayout.DifficultyPill(i), mouse))
                continue;

            _focus = FocusArea.Difficulty;
            if (i != _difficulty)
            {
                _difficulty = i;
                SyncState();
            }
            return;
        }
    }

    private void MoveFocus(int delta)
    {
        int count = 2;
        _focus = (FocusArea)(((int)_focus + delta + count) % count);
    }

    private void SelectFighter(int delta)
    {
        SelectFighterIndex(Wrap(_selected, delta, FighterCatalog.All.Count));
    }

    private void SelectFighterIndex(int index)
    {
        if (index < 0 || index >= FighterCatalog.All.Count || index == _selected)
            return;

        int delta = index > _selected ? 1 : -1;
        _previousSelected = _selected;
        _selected = index;
        _slide = 0f;
        _slideDir = delta;
        _shards.Burst(SelectLayout.PortraitCenterX, SelectLayout.PortraitCenterY, delta);
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

    private int VisibleIndex(int slot)
    {
        int index = SelectLayout.FirstVisible(_selected, FighterCatalog.All.Count) + slot;
        return index < FighterCatalog.All.Count ? index : -1;
    }
}
