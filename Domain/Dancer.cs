using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Domain;

/// <summary>Танцовщица, которую призывает Пантера. Танцует, затем прыгает по дуге.</summary>
public sealed class Dancer : Projectile
{
    private bool _hit;

    public Dancer(Fighter owner)
        : base(owner, owner.Facing)
    {
        X = owner.X;
        Y = 0f;
    }

    public override void Update(float dt, Fighter target, FightSession session)
    {
        Age += dt;

        if (Age > Balance.SummonWait)
        {
            float q = Math.Min(1f, (Age - Balance.SummonWait) / Balance.SummonLeap);
            X += (int)Direction * Balance.SummonSpeed * dt;
            Y = Balance.SummonArc * MathF.Sin(q * MathF.PI);

            if (!_hit
                && Math.Abs(X - target.X) < Balance.SummonHitRadius
                && Math.Abs(Y - target.Y) < Balance.SummonVertical)
            {
                session.Hurt(target, Balance.SummonDamage);
                _hit = true;
            }
        }

        if (Age > Balance.SummonLifetime)
            Dead = true;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer)
    {
        bool dance = Age < Balance.SummonWait;
        string key = dance ? "dancer/dance" : "dancer/leap";
        int frame = dance
            ? (int)(Age / Balance.SummonWait * 8f)
            : Math.Min(3, (int)((Age - Balance.SummonWait) / 0.18f));
        var effects = Direction == Face.Right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        renderer.DrawSprite(spriteBatch, key, frame, X, Balance.Floor - Y, effects, 1f, 213f);
    }
}
