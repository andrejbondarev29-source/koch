namespace DvorFight.Rendering;

/// <summary>
/// Timing constants and easing curves of the fighter-select screen. This is presentation
/// data, not gameplay balance, so it lives next to the screen instead of
/// <see cref="Config.Balance"/>. The pure functions here are also used by the headless
/// self-test to advance animations deterministically.
/// </summary>
public static class SelectMotion
{
    public const float IntroDuration = 0.60f;
    public const float SwapDuration = 0.34f;
    public const float SwapDistance = 560f;

    public const float BreathSpeed = 1.05f;
    public const float SwaySpeed = 0.55f;
    public const float BobAmplitude = 7f;
    public const float SwayAmplitude = 9f;
    public const float IdleTilt = 0.015f;
    public const float IdleScale = 0.012f;

    public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    public static float EaseOutCubic(float value)
    {
        float t = Clamp01(value);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    public static float EaseInCubic(float value)
    {
        float t = Clamp01(value);
        return t * t * t;
    }

    public static float EaseOutQuint(float value)
    {
        float t = Clamp01(value);
        float inv = 1f - t;
        return 1f - inv * inv * inv * inv * inv;
    }

    public static float SmoothStep(float edge0, float edge1, float value)
    {
        if (edge1 <= edge0)
            return value <= edge0 ? 0f : 1f;

        float t = Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}
