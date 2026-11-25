using DvorFight.AI;
using DvorFight.Audio;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Domain;
using DvorFight.Input;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DvorFight.Combat;

/// <summary>
/// Owns one fight: the two fighters, projectiles, effects, hit-stop, screen shake,
/// the metamorphosis cutscene and the win/loss state.
/// </summary>
public sealed class FightSession
{
    public Fighter Player { get; private set; } = null!;
    public Fighter Bot { get; private set; } = null!;
    public bool Playing { get; private set; }
    public IAudioEngine Audio { get; }

    public List<Projectile> Projectiles { get; } = new();
    public List<Spark> Sparks { get; } = new();

    public float HitStop { get; private set; }
    public float Shake { get; private set; }
    public float Clock { get; private set; }
    public float MorphFlash { get; private set; }

    /// <summary>Winner of the finished fight, or <c>null</c> for a draw.</summary>
    public Fighter? Winner { get; private set; }

    /// <summary>Active metamorphosis cutscene, if any. While set the world is paused.</summary>
    public MorphCutscene? ActiveCutscene { get; private set; }

    public bool InCutscene => ActiveCutscene != null;

    private readonly BotController _bot = new();
    private readonly Texture2D[] _morphFrames;

    public FightSession(IAudioEngine audio, Texture2D[] morphFrames)
    {
        Audio = audio;
        _morphFrames = morphFrames;
    }

    public void Start(FighterKind playerKind)
    {
        Player = playerKind == FighterKind.Andrey ? new Andrey() : new Pantera();
        Bot = playerKind == FighterKind.Andrey ? new Pantera() : new Andrey();
        Player.Reset(240f, Face.Right);
        Bot.Reset(720f, Face.Left);
        Projectiles.Clear();
        Sparks.Clear();
        HitStop = 0f;
        Shake = 0f;
        MorphFlash = 0f;
        Clock = 0f;
        Playing = true;
        Winner = null;
        ActiveCutscene = null;
        _bot.Reset();
    }

    public void Update(float dt, InputController input)
    {
        Shake = Math.Max(0f, Shake - dt * 27f);
        MorphFlash = Math.Max(0f, MorphFlash - dt * 1.5f);

        // During the cutscene the whole world is frozen (as in the original build).
        if (ActiveCutscene != null)
        {
            ActiveCutscene.Update(dt);
            if (ActiveCutscene.Finished)
                ActiveCutscene = null;
            return;
        }

        // Hit-stop freezes the world for a few frames on impact.
        if (HitStop > 0f)
        {
            HitStop -= dt;
            return;
        }

        Clock += dt;

        _bot.Think(dt, this);

        Player.Low = input.IsDown && Player.Y <= 0f && !Player.IsBusy;
        float playerDx = (input.IsRight ? 1f : 0f) - (input.IsLeft ? 1f : 0f);

        if (Playing)
        {
            Player.Update(dt, playerDx, Bot, this);
            Bot.Update(dt, _bot.Dx, Player, this);
        }

        SeparateBodies();

        foreach (var projectile in Projectiles)
            projectile.Update(dt, projectile.Owner == Player ? Bot : Player, this);
        Projectiles.RemoveAll(p => p.Dead);

        foreach (var spark in Sparks)
            spark.Update(dt);
        Sparks.RemoveAll(s => s.T <= 0f);

        if (Playing && (Player.Hp <= 0f || Bot.Hp <= 0f))
        {
            Playing = false;
            Winner = Player.Hp == Bot.Hp ? null : (Player.Hp > Bot.Hp ? Player : Bot);
        }
    }

    public void Hurt(Fighter target, float damage)
    {
        if (!Playing)
            return;

        bool heavy = damage >= Balance.HeavyDamage;
        Fighter attacker = ReferenceEquals(target, Player) ? Bot : Player;

        float dir = Math.Sign(target.X - attacker.X);
        if (dir == 0f)
            dir = (int)attacker.Facing;

        target.TakeDamage(damage);
        target.FlashTimer = Balance.FlashDuration;
        target.PushVelocity = dir * (heavy ? Balance.HeavyKnockback : Balance.LightKnockback);
        HitStop = Math.Max(HitStop, heavy ? Balance.HeavyHitStop : Balance.LightHitStop);
        Shake = Math.Max(Shake, heavy ? Balance.HeavyShake : Balance.LightShake);
        Sparks.Add(new Spark
        {
            X = target.X - dir * 23f,
            Y = Balance.Floor - target.Y - (target.Low ? 60f : 108f),
            T = Balance.SparkLifetime,
            Heavy = heavy,
        });
        Audio.Play(heavy ? SoundType.Heavy : SoundType.Impact);
    }

    public void Spawn(Projectile projectile) => Projectiles.Add(projectile);

    public void BeginMorph(Fighter fighter)
    {
        Shake = Math.Max(Shake, Balance.HeavyShake);
        if (_morphFrames.Length > 0)
            ActiveCutscene = new MorphCutscene(_morphFrames);
        else
            MorphFlash = 1f;
    }

    public void Draw(SpriteBatch spriteBatch, Renderer renderer)
    {
        if (ActiveCutscene != null)
        {
            ActiveCutscene.Draw(spriteBatch);
            return;
        }

        foreach (var fighter in new[] { Player, Bot })
            renderer.FillRect(spriteBatch,
                new Rectangle((int)fighter.X - 48, (int)Balance.Floor - 2, 96, 6),
                new Color(0x24, 0x23, 0x31) * 0.5f);

        Player.Draw(spriteBatch, renderer);
        Bot.Draw(spriteBatch, renderer);

        foreach (var projectile in Projectiles)
            projectile.Draw(spriteBatch, renderer);

        foreach (var spark in Sparks)
            spark.Draw(spriteBatch, renderer);

        if (MorphFlash > 0f)
            renderer.FillRect(spriteBatch,
                new Rectangle(0, 0, Balance.ViewWidth, Balance.ViewHeight),
                Color.White * MorphFlash);
    }

    private void SeparateBodies()
    {
        float gap = Bot.X - Player.X;
        if (Math.Abs(gap) < Balance.PushApartGap && Math.Abs(Player.Y - Bot.Y) < Balance.PushApartVertical)
        {
            float s = Math.Sign(gap);
            if (s == 0f)
                s = 1f;
            float d = (Balance.PushApartGap - Math.Abs(gap)) / 2f;
            Player.X = Math.Clamp(Player.X - s * d, Balance.MinX, Balance.MaxX);
            Bot.X = Math.Clamp(Bot.X + s * d, Balance.MinX, Balance.MaxX);
        }
    }
}
