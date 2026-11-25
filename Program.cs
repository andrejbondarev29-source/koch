using DvorFight;
using DvorFight.Diagnostics;

if (args.Contains("--selftest"))
    return SelfTest.Run();

using var game = new Game1();
game.Run();
return 0;
