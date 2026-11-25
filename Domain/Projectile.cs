using DvorFight.Combat;
using DvorFight.Core;
using DvorFight.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Domain;

/// <summary>
/// A flying object spawned by a special. Owned by a fighter and travelling in a direction.
/// </summary>
public abstract class Projectile : Entity
{
    public Fighter Owner { get; }
    public Face Direction { get; }

    protected Projectile(Fighter owner, Face direction)
    {
        Owner = owner;
        Direction = direction;
    }

    public abstract void Update(float dt, Fighter target, FightSession session);
}
