using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Draws one large fighter portrait on the left stage: a procedural white "cut paper"
/// plaque, a cold glow, a hard offset shadow and the portrait itself. The animation state
/// (intro, swap progress, breathing) lives in <see cref="FighterSelectScreen"/>; this class
/// only turns the resulting transform into draw calls. The glow texture is created lazily
/// on first draw, so constructing the screen stays headless-safe.
/// </summary>
public sealed class PortraitStage
{
    private Texture2D? _glow;

    public void Draw(
        SpriteBatch spriteBatch,
        Renderer renderer,
        FighterProfile profile,
        Vector2 center,
        float rotation,
        float scale,
        float alpha,
        float time)
    {
        if (alpha <= 0.01f)
            return;

        var portrait = renderer.Assets.Get(profile.PortraitAnimation);
        Texture2D? texture;
        float targetHeight = SelectLayout.PortraitHeight * scale;
        int frame = 0;

        if (portrait != null)
        {
            // An optional second frame is treated as a short blink every few seconds.
            if (portrait.Count > 1)
            {
                float blink = time % 4.2f;
                frame = blink > 4.0f ? 1 : 0;
            }
            texture = portrait.Frame(frame);
        }
        else
        {
            // Asset still pending: fall back to the small in-game stance so the menu never
            // breaks. The self-test reports the missing portrait separately.
            var idle = renderer.Assets.Get(profile.IdleAnimation);
            if (idle == null)
                return;
            frame = (int)(time * 5f) % idle.Count;
            texture = idle.Frame(frame);
            targetHeight = 480f * scale;
        }

        DrawPlaque(spriteBatch, renderer, center, rotation, scale, alpha);
        DrawGlow(spriteBatch, renderer, center, targetHeight, alpha);

        float uniformScale = targetHeight / texture.Height;
        var drawScale = new Vector2(uniformScale);

        // Hard offset "poster" shadow instead of a soft baked one.
        renderer.DrawTextureEx(
            spriteBatch,
            texture,
            center + new Vector2(6f, 8f),
            rotation,
            drawScale,
            SelectTheme.Ink * (0.32f * alpha));

        // A thin cyan echo imitates the rim light of the reference art.
        renderer.DrawTextureEx(
            spriteBatch,
            texture,
            center + new Vector2(-2f, -2f),
            rotation,
            drawScale,
            SelectTheme.Cyan * (0.10f * alpha));

        renderer.DrawTextureEx(
            spriteBatch,
            texture,
            center,
            rotation,
            drawScale,
            Color.White * alpha);
    }

    private static void DrawPlaque(
        SpriteBatch spriteBatch,
        Renderer renderer,
        Vector2 center,
        float rotation,
        float scale,
        float alpha)
    {
        float width = 430f * scale;
        float height = 610f * scale;

        Vector2[] local =
        {
            new(-width / 2f, -height / 2f),
            new(width * 0.16f, -height / 2f - 12f),
            new(width / 2f - 14f, -height * 0.26f),
            new(width / 2f, height * 0.08f),
            new(width * 0.38f, height / 2f - 10f),
            new(0f, height / 2f + 14f),
            new(-width * 0.42f, height / 2f - 2f),
            new(-width / 2f + 12f, height * 0.16f),
            new(-width / 2f, -height * 0.16f),
            new(-width * 0.32f, -height / 2f + 6f),
        };

        float angle = rotation * 0.55f;
        var rotationMatrix = Matrix.CreateRotationZ(angle);
        var points = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++)
            points[i] = Vector2.Transform(local[i], rotationMatrix) + center + new Vector2(0f, -4f);

        renderer.FillPolygon(spriteBatch, points, SelectTheme.Paper * (0.90f * alpha));
        renderer.DrawPolygonBorder(spriteBatch, points, SelectTheme.Cyan * (0.48f * alpha), 2f);

        // One sharp accent cut on the paper edge.
        renderer.DrawLine(
            spriteBatch,
            points[3],
            points[4],
            SelectTheme.Pink * (0.45f * alpha),
            3f);
    }

    private void DrawGlow(
        SpriteBatch spriteBatch,
        Renderer renderer,
        Vector2 center,
        float targetHeight,
        float alpha)
    {
        EnsureGlow(renderer.GraphicsDevice);
        var glow = _glow;
        if (glow == null)
            return;

        float uniformScale = targetHeight * 1.22f / glow.Height;
        renderer.DrawTextureEx(
            spriteBatch,
            glow,
            center,
            0f,
            new Vector2(uniformScale),
            SelectTheme.Cyan * (0.20f * alpha));
    }

    private void EnsureGlow(GraphicsDevice graphicsDevice)
    {
        if (_glow != null)
            return;

        const int size = 128;
        var pixels = new Color[size * size];
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = MathF.Sqrt(dx * dx + dy * dy) / radius;
                float strength = Math.Clamp(1f - distance, 0f, 1f);
                strength *= strength;
                pixels[y * size + x] = new Color(255, 255, 255, (int)(strength * 255f));
            }
        }

        _glow = new Texture2D(graphicsDevice, size, size);
        _glow.SetData(pixels);
    }
}
