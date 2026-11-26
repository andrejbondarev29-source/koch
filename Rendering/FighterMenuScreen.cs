using DvorFight.Config;
using DvorFight.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Quick character-select menu opened by «Играть». Deliberately minimal: a large portrait
/// on the left and a slanted vertical list (fighters + «В БОЙ») on the right, with no stats
/// panels. The detailed stats/difficulty screen stays available from «Бойцы / Характеристики».
/// </summary>
public sealed class FighterMenuScreen : MenuScreen
{
    private readonly MenuState _state;
    private readonly ShardField _shards = new();
    private readonly SelectBackdrop _backdrop = new();
    private readonly PortraitStage _portrait = new();

    private int _selected;
    private int _previousSelected;
    private int _cursor;

    private float _time;
    private float _intro;
    private float _slide = 1f;
    private int _slideDir = 1;

    public FighterMenuScreen(MenuState state)
    {
        _state = state;
        _selected = Math.Clamp(FighterCatalog.IndexOf(state.SelectedFighter), 0, Math.Max(0, FighterCatalog.All.Count - 1));
        _previousSelected = _selected;
        _cursor = _selected;
    }

    internal bool IsTransitioning => _slide < 1f;
    internal float IntroProgress => _intro;
    internal float TransitionProgress => _slide;
    internal int Cursor => _cursor;

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

        if (input.FighterShortcut is >= 1 and <= 9)
        {
            int shortcut = input.FighterShortcut - 1;
            if (shortcut < FighterCatalog.All.Count)
            {
                _cursor = shortcut;
                SelectFighterIndex(shortcut);
            }
        }

        if (input.Up)
            MoveCursor(-1);
        if (input.Down)
            MoveCursor(1);

        if (input.Left)
            SelectFighter(-1);
        if (input.Right)
            SelectFighter(1);

        if (input.MouseMoved)
            UpdateHover(input.MousePosition);

        int total = ItemCount;
        for (int slot = 0; slot < MenuLayout.VisibleItems; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Clicked(MenuLayout.ItemRect(slot), input))
                continue;

            if (index == total - 1)
                return Start();

            _cursor = index;
            SelectFighterIndex(index);
            return MenuCommand.None;
        }

        if (input.Back)
            return MenuCommand.BackToTitle;

        // Enter starts the fight from any row: the chosen fighter is always visible.
        if (input.Confirm)
            return Start();

        return MenuCommand.None;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time)
    {
        _ = time;

        _backdrop.Draw(spriteBatch, renderer, _time);
        _shards.DrawBack(spriteBatch, renderer, _time);

        float intro = SelectMotion.EaseOutQuint(SelectMotion.Clamp01(_intro / 0.75f));
        float p = _slide;
        bool swapping = p < 1f;

        var profile = FighterCatalog.All[_selected];
        float phase = _selected * 1.7f;
        float bob = MathF.Sin(_time * SelectMotion.BreathSpeed + phase) * SelectMotion.BobAmplitude;
        float sway = MathF.Sin(_time * SelectMotion.SwaySpeed + phase) * SelectMotion.SwayAmplitude;
        float tilt = MathF.Sin(_time * 0.70f + phase) * SelectMotion.IdleTilt;
        float idleScale = 1f + MathF.Sin(_time * SelectMotion.BreathSpeed + phase) * SelectMotion.IdleScale;
        float introY = (1f - intro) * 36f;
        float introScale = 0.94f + 0.06f * intro;

        GetMouseParallax(out float parallaxX, out float parallaxY);

        float outX = -_slideDir * SelectMotion.SwapDistance * SelectMotion.EaseInCubic(p);
        float inX = _slideDir * SelectMotion.SwapDistance * (1f - SelectMotion.EaseOutCubic(p));
        float outAlpha = 1f - SelectMotion.SmoothStep(0f, 0.65f, p);
        float inAlpha = SelectMotion.SmoothStep(0.10f, 0.85f, p);

        if (swapping && _previousSelected != _selected)
        {
            var previous = FighterCatalog.All[_previousSelected];
            float previousTilt = MathF.Sin(_time * 0.70f + _previousSelected * 1.7f) * SelectMotion.IdleTilt;
            var previousCenter = new Vector2(
                MenuLayout.PortraitCenterX + sway + parallaxX + outX,
                MenuLayout.PortraitCenterY + bob + parallaxY + introY);

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
            MenuLayout.PortraitCenterX + sway + parallaxX + inX,
            MenuLayout.PortraitCenterY + bob + parallaxY + introY);
        float currentRotation = MathHelper.ToRadians(profile.PortraitRotationDegrees) + tilt
                                - _slideDir * 0.05f * (1f - SelectMotion.EaseOutCubic(p));
        float currentScale = idleScale * introScale
                             * (swapping ? 0.94f + 0.06f * SelectMotion.EaseOutCubic(p) : 1f);

        _portrait.Draw(
            spriteBatch,
            renderer,
            profile,
            currentCenter,
            currentRotation,
            currentScale,
            intro * (swapping ? inAlpha : 1f),
            _time);

        _shards.DrawFront(spriteBatch, renderer, _time);
        SelectUi.DrawRightVeil(spriteBatch, renderer);

        MenuUi.DrawInfoCard(spriteBatch, renderer, fonts, _selected, FighterCatalog.All.Count, _state.Difficulty);
        MenuUi.DrawList(spriteBatch, renderer, fonts, _selected, _cursor, _time);
        MenuUi.DrawHints(spriteBatch, renderer, fonts);
    }

    private int ItemCount => FighterCatalog.All.Count + 1;

    private int VisibleIndex(int slot)
    {
        int index = MenuLayout.FirstVisible(_cursor, ItemCount) + slot;
        return index < ItemCount ? index : -1;
    }

    private void MoveCursor(int delta)
    {
        _cursor = Wrap(_cursor, delta, ItemCount);
        if (_cursor < FighterCatalog.All.Count)
            SelectFighterIndex(_cursor);
    }

    private void SelectFighter(int delta)
    {
        int next = Wrap(_selected, delta, FighterCatalog.All.Count);
        _cursor = next;
        SelectFighterIndex(next);
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
        _shards.Burst(MenuLayout.PortraitCenterX, MenuLayout.PortraitCenterY, delta);
        SyncState();
    }

    private void UpdateHover(Vector2 mouse)
    {
        for (int slot = 0; slot < MenuLayout.VisibleItems; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Contains(MenuLayout.ItemRect(slot), mouse))
                continue;

            _cursor = index;
            if (index < FighterCatalog.All.Count)
                SelectFighterIndex(index);
            return;
        }
    }

    private void SyncState()
    {
        _state.SelectedFighter = FighterCatalog.All[_selected].Kind;
    }

    private MenuCommand Start() =>
        MenuCommand.StartFight(_state.SelectedFighter, _state.Difficulty);

    private void GetMouseParallax(out float x, out float y)
    {
        x = 0f;
        y = 0f;
        var stage = new Rectangle(0, 0, 512, Balance.ViewHeight);
        if (!Contains(stage, MousePosition))
            return;

        x = (MousePosition.X - MenuLayout.PortraitCenterX) / MenuLayout.PortraitCenterX * -6f;
        y = (MousePosition.Y - MenuLayout.PortraitCenterY) / MenuLayout.PortraitCenterY * -4f;
    }
}
