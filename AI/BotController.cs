using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;

namespace DvorFight.AI;

/// <summary>
/// A simple rule-based opponent. Picks an action on a timer and can duck under an
/// incoming ear beam (the original bot could not crouch at all).
/// </summary>
public sealed class BotController
{
    private float _think;
    private float _crouchTimer;

    /// <summary>Horizontal input the bot is applying this frame (-1, 0, 1).</summary>
    public float Dx { get; private set; }

    public void Reset()
    {
        _think = 0.6f;
        _crouchTimer = 0f;
        Dx = 0f;
    }

    public void Think(float dt, FightSession session)
    {
        var bot = session.Bot;
        var player = session.Player;

        // React to a winding-up ear beam by crouching — makes Pantera's Z avoidable by the AI.
        bool incomingBeam = player.Action == MoveType.EarBeam && player.ActionTime > 0.15f && !player.ActionFired;
        if (incomingBeam)
            _crouchTimer = 0.45f;

        _crouchTimer = Math.Max(0f, _crouchTimer - dt);
        bot.Low = _crouchTimer > 0f && bot.Y <= 0f && !bot.IsBusy;

        _think -= dt;
        if (_think > 0f)
            return;

        _think = Balance.AiThinkMin + (float)Random.Shared.NextDouble() * Balance.AiThinkRand;

        float dist = Math.Abs(bot.X - player.X);
        double r = Random.Shared.NextDouble();

        if (r < Balance.AiProbSpecialX)
            bot.IssueCommand(Command.SpecialX, session);
        else if (r < Balance.AiProbSpecialX + Balance.AiProbSpecialZ)
            bot.IssueCommand(Command.SpecialZ, session);
        else if (dist < Balance.AiMeleeDistance)
            bot.IssueCommand(Command.Hit, session);
        else if (r > 1.0 - Balance.AiProbJump)
            bot.IssueCommand(Command.Jump, session);

        Dx = dist > Balance.AiApproachDistance ? Math.Sign(player.X - bot.X) : 0f;
    }
}
