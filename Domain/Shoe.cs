using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Domain;

/// <summary>Один ботинок из «Вонючих ног» Андрея.</summary>
public sealed class Shoe : Projectile
{
    public Shoe(Fighter owner, float offsetX, float offsetY)
        : base(owner, owner.Facing)
    {
        X = owner.X + (int)owner.Facing * offsetX;
        Y = owner.Y + offsetY;
    }

    public override void Update(float dt, Fighter target, FightSession session)
    {
        Age += dt;
        X += (int)Direction * Balance.ShoeSpeed * dt;

        if (Math.Abs(X - target.X) < Balance.ShoeHitRadius
            && Y > target.Y
            && Y < target.Y + (target.Low ? 95f : 190f))
        {
            session.Hurt(target, Balance.ShoeDamage);
            Dead = true;
        }

        if (Age > Balance.ShoeLifetime)
            Dead = true;
    }

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer)
    {
        int frame = (int)(Age * 15f) % 12;
        var effects = Direction == Face.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        renderer.DrawSprite(spriteBatch, "shoe/flight", frame, X, Balance.Floor - Y, effects, 1f, 65f);
    }
}
