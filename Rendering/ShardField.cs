using DvorFight.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Rendering;

/// <summary>
/// Ambient field of flat geometric shards drifting through the select screen. The shape
/// textures are tiny white polygons generated on first draw and tinted per shard, so there
/// are no shipped shard PNGs and no per-frame texture allocation. The field is deterministic
/// (fixed RNG seed), which keeps screenshots and visual reviews reproducible.
/// </summary>
public sealed class ShardField
{
    private const int TextureSize = 64;
    private const float WrapMargin = 70f;

    private readonly List<Shard> _shards = new(48);
    private readonly Random _random = new(1337);
    private Texture2D[]? _textures;

    public ShardField()
    {
        for (int i = 0; i < 26; i++)
        {
            float depth = 0.25f + (float)_random.NextDouble() * 0.75f;
            int color = _random.NextDouble() < 0.28
                ? (_random.NextDouble() < 0.55 ? 3 : 4)
                : _random.NextDouble() < 0.55 ? 0 : _random.NextDouble() < 0.55 ? 1 : 5;

            _shards.Add(new Shard
            {
                Position = new Vector2(
                    _random.Next(0, Balance.ViewWidth),
                    _random.Next(0, Balance.ViewHeight)),
                Velocity = new Vector2(
                    -7f + (float)_random.NextDouble() * 14f,
                    -3f + (float)_random.NextDouble() * 6f),
                Rotation = (float)_random.NextDouble() * MathF.Tau,
                RotationSpeed = -0.55f + (float)_random.NextDouble() * 1.1f,
                Scale = 0.45f + (float)_random.NextDouble() * 0.95f,
                Depth = depth,
                Shape = _random.Next(4),
                ColorIndex = color,
                Phase = (float)_random.NextDouble() * MathF.Tau,
                IsBurst = false,
            });
        }
    }

    public void Update(float dt)
    {
        for (int i = _shards.Count - 1; i >= 0; i--)
        {
            Shard shard = _shards[i];
            shard.Position += shard.Velocity * dt;
            shard.Rotation += shard.RotationSpeed * dt;

            if (shard.IsBurst)
            {
                shard.Life -= dt;
                if (shard.Life <= 0f)
                {
                    _shards.RemoveAt(i);
                    continue;
                }

                shard.Velocity *= MathF.Pow(0.18f, dt);
            }
            else
            {
                if (shard.Position.X < -WrapMargin)
                    shard.Position.X = Balance.ViewWidth + WrapMargin;
                else if (shard.Position.X > Balance.ViewWidth + WrapMargin)
                    shard.Position.X = -WrapMargin;

                if (shard.Position.Y < -WrapMargin)
                    shard.Position.Y = Balance.ViewHeight + WrapMargin;
                else if (shard.Position.Y > Balance.ViewHeight + WrapMargin)
                    shard.Position.Y = -WrapMargin;
            }

            _shards[i] = shard;
        }
    }

    /// <summary>Short burst of shards on a fighter switch; purely cosmetic.</summary>
    public void Burst(float x, float y, int direction)
    {
        for (int i = 0; i < 9; i++)
        {
            float baseAngle = direction >= 0 ? 0f : MathF.PI;
            float angle = baseAngle - 0.9f + (float)_random.NextDouble() * 1.8f;
            float speed = 140f + (float)_random.NextDouble() * 230f;

            _shards.Add(new Shard
            {
                Position = new Vector2(
                    x + ((float)_random.NextDouble() - 0.5f) * 44f,
                    y + ((float)_random.NextDouble() - 0.5f) * 160f),
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Rotation = (float)_random.NextDouble() * MathF.Tau,
                RotationSpeed = -2f + (float)_random.NextDouble() * 4f,
                Scale = 0.55f + (float)_random.NextDouble() * 0.75f,
                Depth = 1f,
                Shape = _random.Next(4),
                ColorIndex = i % 3 == 0 ? 3 : i % 3 == 1 ? 0 : 4,
                Phase = (float)_random.NextDouble() * MathF.Tau,
                IsBurst = true,
                Life = 0.40f + (float)_random.NextDouble() * 0.32f,
                MaxLife = 0.72f,
            });
        }
    }

    public void DrawBack(SpriteBatch spriteBatch, Renderer renderer, float time) =>
        Draw(spriteBatch, renderer, time, front: false);

    public void DrawFront(SpriteBatch spriteBatch, Renderer renderer, float time) =>
        Draw(spriteBatch, renderer, time, front: true);

    private void Draw(SpriteBatch spriteBatch, Renderer renderer, float time, bool front)
    {
        EnsureTextures(renderer.GraphicsDevice);
        var textures = _textures;
        if (textures == null)
            return;

        foreach (Shard shard in _shards)
        {
            bool isFront = shard.IsBurst
                           || (shard.Depth > 0.72f && shard.Position.X < 520f);
            if (isFront != front)
                continue;

            float twinkle = 0.45f + 0.55f * (0.5f + 0.5f * MathF.Sin(time * 1.3f + shard.Phase));
            float alpha = shard.IsBurst
                ? Math.Clamp(shard.Life / shard.MaxLife, 0f, 1f)
                : (0.18f + 0.52f * shard.Depth) * twinkle;

            float size = (8f + 26f * shard.Depth) * shard.Scale;
            float scale = size / TextureSize;
            renderer.DrawTextureEx(
                spriteBatch,
                textures[shard.Shape],
                shard.Position,
                shard.Rotation,
                new Vector2(scale),
                SelectTheme.ShardColors[shard.ColorIndex] * alpha);
        }
    }

    private void EnsureTextures(GraphicsDevice graphicsDevice)
    {
        if (_textures != null)
            return;

        _textures = new Texture2D[4];
        for (int shape = 0; shape < _textures.Length; shape++)
        {
            var pixels = new Color[TextureSize * TextureSize];
            Vector2[] points = ShapePoints(shape);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    if (!PointInPolygon(x + 0.5f, y + 0.5f, points))
                        continue;
                    pixels[y * TextureSize + x] = Color.White;
                }
            }

            var texture = new Texture2D(graphicsDevice, TextureSize, TextureSize);
            texture.SetData(pixels);
            _textures[shape] = texture;
        }
    }

    private static Vector2[] ShapePoints(int shape) => shape switch
    {
        0 => new[]
        {
            new Vector2(32f, 3f),
            new Vector2(60f, 58f),
            new Vector2(5f, 56f),
        },
        1 => new[]
        {
            new Vector2(7f, 57f),
            new Vector2(55f, 6f),
            new Vector2(61f, 20f),
        },
        2 => new[]
        {
            new Vector2(8f, 20f),
            new Vector2(55f, 6f),
            new Vector2(59f, 52f),
            new Vector2(4f, 60f),
        },
        _ => new[]
        {
            new Vector2(32f, 2f),
            new Vector2(62f, 32f),
            new Vector2(32f, 62f),
            new Vector2(2f, 32f),
        },
    };

    private static bool PointInPolygon(float x, float y, Vector2[] points)
    {
        bool inside = false;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            Vector2 pi = points[i];
            Vector2 pj = points[j];
            if ((pi.Y > y) != (pj.Y > y)
                && x < (pj.X - pi.X) * (y - pi.Y) / (pj.Y - pi.Y) + pi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private struct Shard
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float RotationSpeed;
        public float Scale;
        public float Depth;
        public int Shape;
        public int ColorIndex;
        public float Phase;
        public bool IsBurst;
        public float Life;
        public float MaxLife;
    }
}
