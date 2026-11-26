using Microsoft.Xna.Framework;

namespace DvorFight.Rendering;

/// <summary>
/// Suppresses the second press of a double-click that opened a menu screen. The guard is
/// armed only when a transition is triggered by a mouse click; keyboard transitions stay
/// immediately clickable. Pure state, so the behaviour can be tested headlessly.
/// </summary>
internal sealed class MenuClickGuard
{
    private const double WindowSeconds = 0.55;
    private const float PositionTolerance = 16f;

    private bool _armed;
    private double _armedAt;
    private Vector2 _armedPosition;

    public void Arm(double now, Vector2 position)
    {
        _armed = true;
        _armedAt = now;
        _armedPosition = position;
    }

    public bool ShouldSuppress(double now, in MenuInput input)
    {
        if (!_armed)
            return false;

        if (now - _armedAt > WindowSeconds)
        {
            _armed = false;
            return false;
        }

        return input.MouseClicked
               && Vector2.Distance(input.MousePosition, _armedPosition) <= PositionTolerance;
    }
}
