using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;

namespace DvorFight.Domain;

/// <summary>Пантера: «Ушной луч» (Z) и Призыв танцовщицы (X).</summary>
public sealed class Pantera : Fighter
{
    public override bool SpritesFaceRight => false;

    public Pantera() : base(FighterKind.Pantera) { }

    protected override void OnSpecialX(FightSession session)
    {
        session.Spawn(new Dancer(this));
        XCooldown = Balance.SummonXCooldown;
        session.Audio.Play(SoundType.Summon);
        Action = MoveType.Summon;
        ActionTime = 0f;
        ActionDuration = Balance.SummonActionDuration;
        ActionFired = false;
    }
}
