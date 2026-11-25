using DvorFight.Config;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Domain;

/// <summary>A radial burst drawn at an impact point. Purely visual.</summary>
public sealed class Spark
{
    public float X { get; set; }
    public float Y { get; set; }
    public float T { get; set; }
    public bool Heavy { get; set; }

    public void Update(float dt) => T -= dt;

    public void Draw(SpriteBatch spriteBatch, Renderer renderer)
    {
        float speed = Heavy ? Balance.SparkHeavySpeed : Balance.SparkLightSpeed;
        float r0 = 6f;
        float r1 = 10f + T * speed;
        Color color = Heavy ? new Color(0xff, 0xbd, 0x59) : new Color(0xff, 0xf5, 0xcd);

        float angle = 0f;
        for (int k = 0; k < Balance.SparkRays; k++)
        {
            var from = new Vector2(X + MathF.Cos(angle) * r0, Y + MathF.Sin(angle) * r0);
            var to = new Vector2(X + MathF.Cos(angle) * r1, Y + MathF.Sin(angle) * r1);
            renderer.DrawLine(spriteBatch, from, to, color, 3f);
            angle += 0.898f;
        }
    }
}
