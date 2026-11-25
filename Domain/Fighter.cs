using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Domain;

/// <summary>
/// A playable fighter. Handles physics, cooldowns, the attack state machine and
/// sprite selection. Subclasses define character-specific specials.
/// </summary>
public abstract class Fighter : Entity
{
    public FighterKind Kind { get; }
    public Face Facing { get; protected set; } = Face.Right;

    public float Hp { get; protected set; } = Balance.MaxHp;
    public float Vy { get; protected set; }
    public bool Low { get; set; }

    public float SpecialCooldown { get; protected set; }
    public float XCooldown { get; protected set; }
    public float MorphTimer { get; protected set; }
    public float FlashTimer { get; set; }
    public float PushVelocity { get; set; }
    public float Step { get; protected set; }

    public MoveType Action { get; protected set; } = MoveType.None;
    public float ActionTime { get; protected set; }
    public float ActionDuration { get; protected set; }
    public bool ActionFired { get; protected set; }

    protected int _alt;

    public bool IsBusy => Action != MoveType.None;
    public bool IsMorphed => MorphTimer > 0f;
    public string GroupKey => Kind == FighterKind.Andrey ? "andrey" : "pantera";
    public string Title => Kind == FighterKind.Andrey ? "Андрей" : "Пантера";

    /// <summary>Whether the character's source sprites are drawn facing right.</summary>
    public abstract bool SpritesFaceRight { get; }

    protected Fighter(FighterKind kind)
    {
        Kind = kind;
        Facing = kind == FighterKind.Andrey ? Face.Right : Face.Left;
    }

    public void Reset(float x, Face facing)
    {
        X = x;
        Facing = facing;
        Y = 0f;
        Vy = 0f;
        Low = false;
        Hp = Balance.MaxHp;
        SpecialCooldown = 0f;
        XCooldown = 0f;
        MorphTimer = 0f;
        FlashTimer = 0f;
        PushVelocity = 0f;
        Step = 0f;
        _alt = 0;
        Action = MoveType.None;
        ActionTime = 0f;
        ActionDuration = 0f;
        ActionFired = false;
        Dead = false;
    }

    /// <summary>Handles a discrete command (jump / hit / special) like the original input().</summary>
    public void IssueCommand(Command cmd, FightSession session)
    {
        if (!session.Playing || session.InCutscene || IsBusy)
            return;

        if (cmd == Command.Jump)
        {
            if (Y <= 0f)
            {
                Vy = Balance.JumpVelocity;
                Low = false;
            }
            return;
        }

        MoveType move;
        float duration;

        switch (cmd)
        {
            case Command.Hit:
                move = IsMorphed
                    ? (_alt++ % 2 == 0 ? MoveType.HeavyStraight : MoveType.GroundSmash)
                    : (_alt++ % 2 == 0 ? MoveType.Jab : MoveType.Kick);
                duration = IsMorphed ? Balance.HeavyDuration : Balance.JabKickDuration;
                break;

            case Command.SpecialZ when SpecialCooldown <= 0f:
                if (IsMorphed)
                    return;
                move = Kind == FighterKind.Andrey ? MoveType.ShoesThrow : MoveType.EarBeam;
                duration = Balance.SpecialDuration;
                SpecialCooldown = Balance.SpecialZCooldown;
                break;

            case Command.SpecialX when XCooldown <= 0f:
                OnSpecialX(session);
                return;

            default:
                return;
        }

        session.Audio.Play(SoundFor(move));
        Action = move;
        ActionTime = 0f;
        ActionDuration = duration;
        ActionFired = false;
    }

    /// <summary>Character-specific X special (morph for Andrey, summon for Pantera).</summary>
    protected virtual void OnSpecialX(FightSession session) { }

    public void Update(float dt, float dx, Fighter opponent, FightSession session)
    {
        X = Math.Clamp(X + PushVelocity * dt, Balance.MinX, Balance.MaxX);
        PushVelocity *= MathF.Exp(-Balance.PushDecay * dt);

        SpecialCooldown = Math.Max(0f, SpecialCooldown - dt);
        XCooldown = Math.Max(0f, XCooldown - dt);
        MorphTimer = Math.Max(0f, MorphTimer - dt);
        FlashTimer = Math.Max(0f, FlashTimer - dt);

        if (!IsBusy)
        {
            Facing = opponent.X >= X ? Face.Right : Face.Left;
            if (!Low)
            {
                X = Math.Clamp(X + dx * Balance.WalkSpeed * dt, Balance.MinX, Balance.MaxX);
                Step = dx != 0f ? Step + dt * 9f : 0f;
            }
        }

        Y = Math.Max(0f, Y + Vy * dt);
        Vy = Y > 0f ? Vy - Balance.Gravity * dt : 0f;

        if (IsBusy)
            AdvanceAction(dt, opponent, session);
    }

    public void TakeDamage(float amount) => Hp = Math.Max(0f, Hp - amount);

    public override void Draw(SpriteBatch spriteBatch, Renderer renderer)
    {
        string group = GroupKey;
        string move = "walk";
        int frame = ((int)MathF.Floor(Step) % 4 + 4) % 4;
        bool heavyPose = IsBusy && Action is MoveType.HeavyStraight or MoveType.GroundSmash;

        if (IsMorphed || heavyPose)
        {
            group = "metamorph";
            move = "movement_poses";
            frame = Y > 0f ? 2 : Low ? 3 : (Step != 0f ? 1 : 0);
        }
        else if (Y > 0f)
        {
            move = "jump";
            frame = Vy > 150f ? 1 : 2;
        }
        else if (Low)
        {
            move = "crouch";
            frame = 2;
        }

        if (IsBusy && Action != MoveType.Summon)
        {
            move = MoveToKey(Action);
            frame = Math.Min(3, (int)MathF.Floor(ActionTime / ActionDuration * 4f));
        }

        if (move == "ear_beam" && frame == 2)
            DrawBeam(spriteBatch, renderer);

        float alpha = FlashTimer > 0f && (int)(renderer.Clock * 30f) % 2 == 0 ? 0.55f : 1f;
        var effects = (Facing == Face.Right) == SpritesFaceRight
            ? SpriteEffects.None
            : SpriteEffects.FlipHorizontally;

        renderer.DrawSprite(spriteBatch, $"{group}/{move}", frame, X, Balance.Floor - Y, effects, alpha);
    }

    private void DrawBeam(SpriteBatch spriteBatch, Renderer renderer)
    {
        int left = Facing == Face.Left ? 0 : (int)X;
        int width = Facing == Face.Left ? (int)X : Balance.ViewWidth - (int)X;
        var rect = new Rectangle(left, (int)(Balance.Floor - Y - 154f), width, (int)Balance.BeamThickness);
        renderer.FillRect(spriteBatch, rect, Color.White);
    }

    private void AdvanceAction(float dt, Fighter opponent, FightSession session)
    {
        ActionTime += dt;
        if (!ActionFired && ActionTime >= ActionDuration * FireFactor)
        {
            ActionFired = true;
            FireAction(opponent, session);
        }
        if (ActionTime >= ActionDuration)
            Action = MoveType.None;
    }

    private void FireAction(Fighter opponent, FightSession session)
    {
        switch (Action)
        {
            case MoveType.ShoesThrow:
                for (int i = 0; i < Balance.ShoeCount; i++)
                    session.Spawn(new Shoe(this, 40f + i * 25f, 70f + i * 16f));
                break;

            case MoveType.EarBeam:
            {
                float dist = (opponent.X - X) * (int)Facing;
                if (dist > 0f && dist < Balance.EarBeamRange
                    && !opponent.Low
                    && Math.Abs(opponent.Y - Y) < Balance.EarBeamVertical)
                    session.Hurt(opponent, Balance.EarBeamDamage);
                break;
            }

            case MoveType.Summon:
                break; // Dancer is spawned when the command is issued

            default:
            {
                bool heavy = Action is MoveType.HeavyStraight or MoveType.GroundSmash;
                float dist = (opponent.X - X) * (int)Facing;
                if (dist >= -10f
                    && dist < (heavy ? Balance.HeavyRange : Balance.LightRange)
                    && Math.Abs(Y - opponent.Y) < Balance.MeleeVertical)
                    session.Hurt(opponent, heavy ? Balance.HeavyDamage : Balance.LightDamage);
                break;
            }
        }
    }

    private float FireFactor =>
        Action == MoveType.ShoesThrow ? Balance.ShoesFireFactor : Balance.CommonFireFactor;

    private static SoundType SoundFor(MoveType move) => move switch
    {
        MoveType.EarBeam => SoundType.Beam,
        MoveType.Summon => SoundType.Summon,
        _ => SoundType.Swing,
    };

    private static string MoveToKey(MoveType move) => move switch
    {
        MoveType.Jab => "jab",
        MoveType.Kick => "kick",
        MoveType.HeavyStraight => "heavy_straight",
        MoveType.GroundSmash => "ground_smash",
        MoveType.ShoesThrow => "shoes_throw",
        MoveType.EarBeam => "ear_beam",
        MoveType.Summon => "summon",
        _ => "walk",
    };
}
