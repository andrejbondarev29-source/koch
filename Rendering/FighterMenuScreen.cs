using DvorFight.Config;
using DvorFight.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Quick character-select menu opened by «Играть». Deliberately minimal: a large portrait
/// on the left and a slanted vertical list of fighters on the right, with no stats panels
/// and no start button — a click on a fighter (or <c>Enter</c>) starts the fight. The
/// detailed stats/difficulty screen stays available from «Бойцы / Характеристики».
/// </summary>
public sealed class FighterMenuScreen : MenuScreen
{
    private readonly MenuState _state;
    private readonly ShardField _shards = new();
    private readonly SelectBackdrop _backdrop = new();
    private readonly PortraitStage _portrait = new();

    private int _selected;
    private int _previousSelected;

    private float _time;
    private float _intro;
    private float _slide = 1f;
    private int _slideDir = 1;

    public FighterMenuScreen(MenuState state)
    {
        _state = state;
        _selected = Math.Clamp(FighterCatalog.IndexOf(state.SelectedFighter), 0, Math.Max(0, FighterCatalog.All.Count - 1));
        _previousSelected = _selected;
    }

    internal bool IsTransitioning => _slide < 1f;
    internal float IntroProgress => _intro;
    internal float TransitionProgress => _slide;

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
                SelectFighterIndex(shortcut);
        }

        if (input.Up || input.Down || input.Left || input.Right)
        {
            int delta = 0;
            if (input.Up)
                delta--;
            if (input.Down)
                delta++;
            if (delta == 0)
            {
                if (input.Right)
                    delta = 1;
                else if (input.Left)
                    delta = -1;
            }

            if (delta != 0)
                SelectFighter(delta);
        }

        if (input.MouseMoved)
            UpdateHover(input.MousePosition);

        for (int slot = 0; slot < MenuLayout.VisibleItems; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Clicked(MenuLayout.ItemRect(slot), input))
                continue;

            SelectFighterIndex(index);
            return Start();
        }

        // Clicking the large portrait is the same "start fight" gesture.
        if (Clicked(SelectLayout.PortraitClick, input) && !Contains(MenuLayout.InfoCard, input.MousePosition))
            return Start();

        if (input.Back)
            return MenuCommand.BackToTitle;

        if (input.Confirm)
            return Start();

        return MenuCommand.None;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer, FontSet fonts, float time)
    {
        _ = time;

        _backdrop.Draw(spriteBatch, renderer, _time);
        _shards.DrawBack(spriteBatch, renderer, _time);

        float intro = SelectMotion.EaseOutQuint(SelectMotion.Clamp01(_intro));
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
        MenuUi.DrawList(spriteBatch, renderer, fonts, _selected, _time);
        MenuUi.DrawHints(spriteBatch, renderer, fonts);
    }

    private int VisibleIndex(int slot)
    {
        int index = MenuLayout.FirstVisible(_selected, FighterCatalog.All.Count) + slot;
        return index < FighterCatalog.All.Count ? index : -1;
    }

    private void SelectFighter(int delta)
    {
        int next = Wrap(_selected, delta, FighterCatalog.All.Count);
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
        _shards.Burst(SelectLayout.PortraitCenterX, SelectLayout.PortraitCenterY, delta);
        SyncState();
    }

    private void UpdateHover(Vector2 mouse)
    {
        for (int slot = 0; slot < MenuLayout.VisibleItems; slot++)
        {
            int index = VisibleIndex(slot);
            if (index < 0 || !Contains(MenuLayout.ItemRect(slot), mouse))
                continue;

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
        if (!Contains(SelectLayout.PortraitClick, MousePosition))
            return;

        x = (MousePosition.X - SelectLayout.PortraitCenterX) / SelectLayout.PortraitCenterX * -6f;
        y = (MousePosition.Y - SelectLayout.PortraitCenterY) / SelectLayout.PortraitCenterY * -4f;
    }
}
