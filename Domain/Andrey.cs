using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;

namespace DvorFight.Domain;

/// <summary>Андрей: «Вонючие ноги» (Z) и Метаморфоза (X).</summary>
public sealed class Andrey : Fighter
{
    public override bool SpritesFaceRight => true;

    public Andrey() : base(FighterKind.Andrey) { }

    protected override void OnSpecialX(FightSession session)
    {
        MorphTimer = Balance.MorphDuration;
        XCooldown = Balance.MorphXCooldown;
        _alt = 0;
        session.Audio.Play(SoundType.Form);
        if (ReferenceEquals(this, session.Player))
            session.BeginMorph(this); // in-engine flash instead of the original MP4 cutscene
    }
}
