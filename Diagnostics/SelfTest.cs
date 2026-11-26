using DvorFight.AI;
using DvorFight.Audio;
using DvorFight.Combat;
using DvorFight.Config;
using DvorFight.Core;
using DvorFight.Input;
using DvorFight.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DvorFight.Diagnostics;

/// <summary>
/// Headless smoke test: runs the real gameplay code (no window, no audio device) for
/// a few simulated fights and asserts that damage, specials and metamorphosis work.
/// Also exercises the menu state machines and the bot difficulty profiles without any
/// graphics device. Run with: <c>DvorFight.exe --selftest</c>.
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        Console.WriteLine("=== DvorFight headless self-test ===");
        int failures = 0;
        failures += Scenario("Андрей vs бот", FighterKind.Andrey);
        failures += Scenario("Пантера vs бот", FighterKind.Pantera);
        failures += MenuAndDifficultyChecks();

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

    /// <summary>
    /// Drives the menu screens with synthetic input and checks the difficulty profiles.
    /// No graphics device is needed: menu Update only touches plain state.
    /// </summary>
    private static int MenuAndDifficultyChecks()
    {
        int failures = 0;
        int checks = 0;
        var origin = new Vector2(-100f, -100f);

        void Expect(bool condition, string message)
        {
            checks++;
            if (condition)
                return;
            Console.WriteLine($"          FAIL: {message}");
            failures++;
        }

        // Fighter catalog is data-driven and points at real on-disk animations.
        Expect(FighterCatalog.All.Count >= 2, "в каталоге меньше двух бойцов");
        foreach (var profile in FighterCatalog.All)
        {
            string preview = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites", profile.IdleAnimation, "0.png");
            Expect(File.Exists(preview), $"нет анимации стойки для {profile.Name}: {profile.IdleAnimation}");
            Expect(profile.SpecialZDetail.Contains(Balance.SpecialZCooldown.ToString("0.0")),
                $"откат Z для {profile.Name} не взят из Balance");

            Expect(!string.IsNullOrWhiteSpace(profile.PortraitAnimation),
                $"у бойца {profile.Name} не указан портрет для экрана выбора");

            // The large portraits are delivered as separate art assets. Until they are in
            // the repository the screen falls back to the stance animation; report the
            // missing file as a warning instead of failing the whole self-test.
            string portrait = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites", profile.PortraitAnimation, "0.png");
            if (File.Exists(portrait))
            {
                checks++;
            }
            else
            {
                Console.WriteLine($"          WARN: портрет ещё не добавлен: {profile.PortraitAnimation}/0.png");
            }
        }

        // Difficulty profiles must be ordered from softer to harsher.
        var easy = BotDifficultyProfile.For(BotDifficulty.Easy);
        var normal = BotDifficultyProfile.For(BotDifficulty.Normal);
        var hard = BotDifficultyProfile.For(BotDifficulty.Hard);
        Expect(easy.ThinkScale > normal.ThinkScale && normal.ThinkScale > hard.ThinkScale,
            "сложности не меняют скорость принятия решений");
        Expect(easy.SpecialScale < normal.SpecialScale && normal.SpecialScale < hard.SpecialScale,
            "сложности не меняют частоту спецух");
        Expect(easy.MeleeRangeScale < hard.MeleeRangeScale,
            "сложности не меняют дистанцию удара");
        Expect(DifficultyCatalog.All.Count == 3
               && !string.IsNullOrWhiteSpace(DifficultyCatalog.For(BotDifficulty.Hard).Title)
               && !string.IsNullOrWhiteSpace(DifficultyCatalog.For(BotDifficulty.Hard).Description),
            "у сложности нет подписи/описания для меню");

        // Difficulty reaches the fight session and its bot controller. BotProfile is
        // read from BotController itself, so this fails if SetDifficulty is not wired.
        var session = new FightSession(new NullAudioEngine(), Array.Empty<Texture2D>());
        session.Start(FighterKind.Andrey, BotDifficulty.Hard);
        Expect(session.Difficulty == BotDifficulty.Hard, "в бой не передаётся выбранная сложность");
        Expect(session.BotProfile.Difficulty == BotDifficulty.Hard, "бот не получил профиль сложности");
        Expect(session.BotProfile.ThinkScale < normal.ThinkScale, "бот не применяет быстрый темп «Жёсткого»");

        // Title screen: Down + Enter -> fighter select; Enter -> play; two Downs + Enter -> exit.
        var title = new TitleScreen();
        var next = title.Update(new MenuInput { Down = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.None, "навигация по заголовку сразу запускает экран");
        next = title.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.OpenFighters, "«Бойцы / Характеристики» не открывает выбор бойца");

        title = new TitleScreen();
        next = title.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.Play, "«Играть» не запускает экран выбора");

        title = new TitleScreen();
        title.Update(new MenuInput { Down = true, MousePosition = origin }, 1f / 60f);
        title.Update(new MenuInput { Down = true, MousePosition = origin }, 1f / 60f);
        next = title.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.Exit, "«Выход» не завершает игру");

        // Fighter select: Right picks the second fighter, Down moves focus to difficulty,
        // Right switches Normal -> Hard, Enter starts that exact matchup.
        var state = new MenuState();
        var select = new FighterSelectScreen(state, focusStart: false);
        select.Update(new MenuInput { Right = true, MousePosition = origin }, 1f / 60f);
        Expect(state.SelectedFighter == FighterKind.Pantera, "стрелка вправо не выбирает второго бойца");
        select.Update(new MenuInput { Down = true, MousePosition = origin }, 1f / 60f);
        select.Update(new MenuInput { Right = true, MousePosition = origin }, 1f / 60f);
        Expect(state.Difficulty == BotDifficulty.Hard, "стрелка вправо не переключает сложность");
        next = select.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.StartFight, "Enter не начинает бой со экрана выбора");
        Expect(next.Fighter == FighterKind.Pantera && next.Difficulty == BotDifficulty.Hard,
            "в команду старта не попали выбранные боец/сложность");

        // «Играть» opens the screen with the start button focused, so Enter starts immediately.
        select = new FighterSelectScreen(state, focusStart: true);
        next = select.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.StartFight, "фокус на кнопке старта не начинает бой по Enter");
        next = select.Update(new MenuInput { Back = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.BackToTitle, "Esc не возвращает на стартовый экран");

        // Screen entrance and fighter-switch animations: Update-only, no GraphicsDevice.
        var animState = new MenuState();
        var anim = new FighterSelectScreen(animState, focusStart: false);
        anim.Update(new MenuInput { MousePosition = origin }, 1f / 60f);
        Expect(anim.IntroProgress > 0f, "интро экрана не запускается");

        for (int i = 0; i < 40; i++)
            anim.Update(new MenuInput { MousePosition = origin }, 1f / 60f);
        Expect(anim.IntroProgress >= 1f, "интро экрана не завершается");

        anim.Update(new MenuInput { Right = true, MousePosition = origin }, 1f / 60f);
        Expect(animState.SelectedFighter == FighterKind.Pantera, "смена бойца не обновляет MenuState сразу");
        Expect(anim.IsTransitioning, "смена бойца не запускает слайд портрета");
        float transitionOffset = MathF.Abs(anim.PortraitOffsetX);
        Expect(transitionOffset > 50f, "портрет не уезжает при смене бойца");

        bool monotonic = true;
        float previousOffset = transitionOffset;
        for (int i = 0; i < 30; i++)
        {
            anim.Update(new MenuInput { MousePosition = origin }, 1f / 60f);
            float currentOffset = MathF.Abs(anim.PortraitOffsetX);
            monotonic &= currentOffset <= previousOffset + 0.01f;
            previousOffset = currentOffset;
        }
        Expect(monotonic, "смещение портрета затухает не монотонно");
        Expect(!anim.IsTransitioning && MathF.Abs(anim.PortraitOffsetX) < 0.01f,
            "слайд портрета не завершается");

        // Input must not be blocked while the portrait is still sliding.
        var fastState = new MenuState();
        var fast = new FighterSelectScreen(fastState, focusStart: false);
        fast.Update(new MenuInput { Right = true, MousePosition = origin }, 1f / 60f);
        next = fast.Update(new MenuInput { Confirm = true, MousePosition = origin }, 1f / 60f);
        Expect(next.Type == MenuCommandType.StartFight && next.Fighter == FighterKind.Pantera,
            "Enter во время слайда не стартует выбранного бойца");

        // A second direction key during the slide retargets the animation cleanly.
        var retargetState = new MenuState();
        var retarget = new FighterSelectScreen(retargetState, focusStart: false);
        retarget.Update(new MenuInput { Right = true, MousePosition = origin }, 1f / 60f);
        retarget.Update(new MenuInput { MousePosition = origin }, 0.10f);
        retarget.Update(new MenuInput { Left = true, MousePosition = origin }, 1f / 60f);
        Expect(retargetState.SelectedFighter == FighterKind.Andrey,
            "повторное переключение не возвращает первого бойца");
        for (int i = 0; i < 30; i++)
            retarget.Update(new MenuInput { MousePosition = origin }, 1f / 60f);
        Expect(!retarget.IsTransitioning && MathF.Abs(retarget.PortraitOffsetX) < 0.01f,
            "перенацеленный слайд не завершается");

        // Mouse path: click a title button, the second fighter tab, a difficulty pill and Start.
        title = new TitleScreen();
        next = title.Update(new MenuInput { MouseClicked = true, MousePosition = new Vector2(480, 226) }, 1f / 60f);
        Expect(next.Type == MenuCommandType.Play, "клик по «Играть» не запускает игру");

        state = new MenuState();
        select = new FighterSelectScreen(state, focusStart: false);

        var tabRect = SelectLayout.FighterTab(1);
        select.Update(new MenuInput
        {
            MouseClicked = true,
            MousePosition = new Vector2(tabRect.Center.X, tabRect.Center.Y),
        }, 1f / 60f);
        Expect(state.SelectedFighter == FighterKind.Pantera, "клик по вкладке не выбирает бойца");

        var pillRect = SelectLayout.DifficultyPill(2);
        select.Update(new MenuInput
        {
            MouseClicked = true,
            MousePosition = new Vector2(pillRect.Center.X, pillRect.Center.Y),
        }, 1f / 60f);
        Expect(state.Difficulty == BotDifficulty.Hard, "клик по кнопке сложности не переключает сложность");

        var startRect = SelectLayout.StartButton;
        next = select.Update(new MenuInput
        {
            MouseClicked = true,
            MousePosition = new Vector2(startRect.Center.X, startRect.Center.Y),
        }, 1f / 60f);
        Expect(next.Type == MenuCommandType.StartFight, "клик по «Начать бой» не запускает бой");
        Expect(next.Fighter == FighterKind.Pantera && next.Difficulty == BotDifficulty.Hard,
            "клик по старту не передал выбранные боец/сложность");

        Console.WriteLine($"[Меню и сложности] проверок={checks} провалов={failures}");
        return failures;
    }
}
