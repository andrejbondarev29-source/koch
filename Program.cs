using DvorFight;
using DvorFight.Diagnostics;

if (args.Contains("--selftest"))
    return SelfTest.Run();

using var game = new Game1();

int shotIndex = Array.IndexOf(args, "--screenshot");
if (shotIndex >= 0)
    game.ScreenshotPath = shotIndex + 1 < args.Length ? args[shotIndex + 1] : "screenshot.png";
if (args.Contains("--fight"))
    game.AutoStartFight = true;

game.Run();
return 0;
