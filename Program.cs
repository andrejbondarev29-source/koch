using DvorFight;
using DvorFight.Core;
using DvorFight.Diagnostics;

if (args.Contains("--selftest"))
    return SelfTest.Run();

using var game = new Game1();

int shotIndex = Array.IndexOf(args, "--screenshot");
if (shotIndex >= 0)
    game.ScreenshotPath = shotIndex + 1 < args.Length ? args[shotIndex + 1] : "screenshot.png";

// Dev aid for screenshots / smoke runs: start on a specific screen.
int screenIndex = Array.IndexOf(args, "--screen");
if (screenIndex >= 0 && screenIndex + 1 < args.Length)
{
    game.InitialScreen = args[screenIndex + 1].ToLowerInvariant() switch
    {
        "fighters" or "select" => GameScreen.FighterSelect,
        "fight" => GameScreen.Fight,
        _ => GameScreen.Title,
    };
}
else if (args.Contains("--fight"))
{
    game.InitialScreen = GameScreen.Fight;
}

int fighterIndex = Array.IndexOf(args, "--fighter");
if (fighterIndex >= 0 && fighterIndex + 1 < args.Length)
{
    game.InitialFighter = args[fighterIndex + 1].ToLowerInvariant() switch
    {
        "pantera" => FighterKind.Pantera,
        _ => FighterKind.Andrey,
    };
}

int difficultyIndex = Array.IndexOf(args, "--difficulty");
if (difficultyIndex >= 0 && difficultyIndex + 1 < args.Length)
{
    game.InitialDifficulty = args[difficultyIndex + 1].ToLowerInvariant() switch
    {
        "easy" => BotDifficulty.Easy,
        "hard" => BotDifficulty.Hard,
        _ => BotDifficulty.Normal,
    };
}

game.Run();
return 0;
