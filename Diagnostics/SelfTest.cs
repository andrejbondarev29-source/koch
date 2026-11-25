using DvorFight.Audio;
using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DvorFight.Diagnostics;

/// <summary>
/// Headless smoke test: runs the real gameplay code (no window, no audio device) for
/// a few simulated fights and asserts that damage, specials and metamorphosis work.
/// Run with: <c>DvorFight.exe --selftest</c>.
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        Console.WriteLine("=== DvorFight headless self-test ===");
        int failures = 0;
        failures += Scenario("Андрей vs бот", FighterKind.Andrey);
        failures += Scenario("Пантера vs бот", FighterKind.Pantera);

        Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
        return failures == 0 ? 0 : 1;
    }

    private static int Scenario(string name, FighterKind playerKind)
    {
        var session = new FightSession(new NullAudioEngine(), Array.Empty<Texture2D>());
        session.Start(playerKind);

        var input = new InputController();
        var keyboard = new KeyboardState(Keys.Right);
        var mouse = new MouseState();

        int frames = 0;
        int peakProjectiles = 0;
        float peakBotDamage = 0f;
        float peakPlayerDamage = 0f;
        bool morphed = false;
        int failures = 0;

        for (int i = 0; i < 60 * 40 && session.Playing; i++)
        {
            input.Update(keyboard, mouse);

            if (i % 14 == 0)
                session.Player.IssueCommand(Command.Hit, session);
            if (!session.Player.IsBusy && session.Player.SpecialCooldown <= 0f)
                session.Player.IssueCommand(Command.SpecialZ, session);
            if (!session.Player.IsBusy && session.Player.XCooldown <= 0f)
                session.Player.IssueCommand(Command.SpecialX, session);

            session.Update(1f / 60f, input);

            frames++;
            peakProjectiles = Math.Max(peakProjectiles, session.Projectiles.Count);
            peakBotDamage = Math.Max(peakBotDamage, Balance.MaxHp - session.Bot.Hp);
            peakPlayerDamage = Math.Max(peakPlayerDamage, Balance.MaxHp - session.Player.Hp);
            if (session.Player.IsMorphed)
                morphed = true;
        }

        string winner = session.Winner == null
            ? (session.Playing ? "бой идёт" : "ничья")
            : ReferenceEquals(session.Winner, session.Player) ? "игрок" : "бот";

        Console.WriteLine($"[{name}] frames={frames} ({frames / 60f:0.0} с) playing={session.Playing} winner={winner}");
        Console.WriteLine($"          playerHp={session.Player.Hp:0} botHp={session.Bot.Hp:0} " +
                          $"peakProjectiles={peakProjectiles} botDmg={peakBotDamage:0} playerDmg={peakPlayerDamage:0} morphed={morphed}");

        if (peakBotDamage <= 0f)
        {
            Console.WriteLine("          FAIL: игрок ни разу не нанёс урон");
            failures++;
        }
        if (peakPlayerDamage <= 0f)
        {
            Console.WriteLine("          FAIL: бот ни разу не нанёс урон (AI не работает)");
            failures++;
        }
        if (peakProjectiles <= 0)
        {
            Console.WriteLine("          FAIL: спецуха не создала снаряд/призыв");
            failures++;
        }
        if (playerKind == FighterKind.Andrey && !morphed)
        {
            Console.WriteLine("          FAIL: метаморфоза не сработала");
            failures++;
        }

        return failures;
    }
}
