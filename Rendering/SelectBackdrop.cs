using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Procedural "underwater" backdrop of the fighter-select screen: a deep blue gradient,
/// caustic light on the surface, a cold glow behind the portrait and a vignette. The heavy
/// per-pixel part is baked once into a 960x480 texture; the moving glints and bubbles are
/// cheap draw calls. No GPU resources are created until <see cref="Draw"/> is called, so
/// the screen still constructs fine in the headless self-test.
/// </summary>
public sealed class SelectBackdrop
{
    private static readonly Color Top = new(0x07, 0x52, 0xD8);
    private static readonly Color Middle = new(0x05, 0x2A, 0xA8);
    private static readonly Color Bottom = new(0x03, 0x08, 0x26);

    private readonly Glint[] _glints;
    private readonly Bubble[] _bubbles;

    private Texture2D? _texture;

    public SelectBackdrop()
    {
        var random = new Random(20261126);

        _glints = new Glint[14];
        for (int i = 0; i < _glints.Length; i++)
        {
            _glints[i] = new Glint
            {
                X = random.Next(0, Balance.ViewWidth),
                Y = random.Next(8, 118),
                Length = 18f + (float)random.NextDouble() * 72f,
                Speed = 10f + (float)random.NextDouble() * 22f,
                Phase = (float)random.NextDouble() * MathF.Tau,
            };
        }

        _bubbles = new Bubble[16];
        for (int i = 0; i < _bubbles.Length; i++)
        {
            _bubbles[i] = new Bubble
            {
                X = random.Next(20, Balance.ViewWidth - 20),
                Y = random.Next(0, Balance.ViewHeight),
                Speed = 6f + (float)random.NextDouble() * 14f,
                Size = random.Next(1, 4),
                Phase = (float)random.NextDouble() * MathF.Tau,
            };
        }
    }

    public void Draw(SpriteBatch spriteBatch, Renderer renderer, float time)
    {
        EnsureTexture(renderer.GraphicsDevice);
        var texture = _texture;
        if (texture == null)
            return;

        spriteBatch.Draw(texture, Vector2.Zero, Color.White);
        DrawSurfaceGlints(spriteBatch, renderer, time);
        DrawBubbles(spriteBatch, renderer, time);
    }

    private void DrawSurfaceGlints(SpriteBatch spriteBatch, Renderer renderer, float time)
    {
        foreach (var glint in _glints)
        {
            float x = (glint.X + time * glint.Speed) % (Balance.ViewWidth + 180f) - 90f;
            float alpha = 0.05f + 0.11f * (0.5f + 0.5f * MathF.Sin(time * 0.9f + glint.Phase));
            renderer.DrawLine(spriteBatch,
                new Vector2(x, glint.Y),
                new Vector2(x + glint.Length, glint.Y),
                SelectTheme.Ice * alpha,
                1.5f);
        }
    }

    private void DrawBubbles(SpriteBatch spriteBatch, Renderer renderer, float time)
    {
        foreach (var bubble in _bubbles)
        {
            float y = (bubble.Y - time * bubble.Speed) % Balance.ViewHeight;
            if (y < 0f)
                y += Balance.ViewHeight;

            float x = bubble.X + MathF.Sin(time * 0.4f + bubble.Phase) * 14f;
            float alpha = 0.10f + 0.08f * (0.5f + 0.5f * MathF.Sin(time + bubble.Phase));
            renderer.FillRect(spriteBatch,
                new Rectangle((int)x, (int)y, bubble.Size, bubble.Size),
                SelectTheme.Ice * alpha);
        }
    }

    private void EnsureTexture(GraphicsDevice graphicsDevice)
    {
        if (_texture != null)
            return;

        int width = Balance.ViewWidth;
        int height = Balance.ViewHeight;
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color row = t < 0.55f
                ? Color.Lerp(Top, Middle, t / 0.55f)
                : Color.Lerp(Middle, Bottom, (t - 0.55f) / 0.45f);

            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);

                Color color = row;

                // Cold light pool in the upper-right corner, like the surface seen from below.
                float glowDistance = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.70f, 0.02f));
                float glow = MathF.Max(0f, 1f - glowDistance / 0.78f);
                color = Color.Lerp(color, SelectTheme.Cyan, glow * 0.26f);

                // Pale wash behind the portrait — the "negative space" of the reference.
                float leftDistance = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.18f, 0.42f));
                float leftWash = MathF.Max(0f, 1f - leftDistance / 0.62f);
                color = Color.Lerp(color, SelectTheme.Ice, leftWash * 0.10f);

                if (y < 150)
                {
                    float band = MathF.Sin(x * 0.042f + y * 0.13f)
                                 * MathF.Sin(x * 0.017f - y * 0.06f + 1.7f);
                    float caustic = MathF.Max(0f, band - 0.35f) * 0.9f;
                    caustic *= 1f - y / 170f;
                    color = Color.Lerp(color, SelectTheme.Ice, caustic * 0.42f);
                }

                float dx = nx - 0.54f;
                float dy = ny - 0.50f;
                float vignette = Math.Clamp(1f - 0.52f * (dx * dx + dy * dy), 0.58f, 1f);
                color = new Color(
                    (byte)Math.Clamp((int)(color.R * vignette), 0, 255),
                    (byte)Math.Clamp((int)(color.G * vignette), 0, 255),
                    (byte)Math.Clamp((int)(color.B * vignette), 0, 255));

                int hash = unchecked(x * 73856093 ^ y * 19349663);
                int grain = (hash & 7) - 3;
                pixels[y * width + x] = new Color(
                    (byte)Math.Clamp(color.R + grain, 0, 255),
                    (byte)Math.Clamp(color.G + grain, 0, 255),
                    (byte)Math.Clamp(color.B + grain, 0, 255));
            }
        }

        _texture = new Texture2D(graphicsDevice, width, height);
        _texture.SetData(pixels);
    }

    private struct Glint
    {
        public float X;
        public float Y;
        public float Length;
        public float Speed;
        public float Phase;
    }

    private struct Bubble
    {
        public float X;
        public float Y;
        public float Speed;
        public int Size;
        public float Phase;
    }
}
