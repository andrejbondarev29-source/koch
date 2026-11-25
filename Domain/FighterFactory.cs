using DvorFight.Core;

namespace DvorFight.Domain;

/// <summary>
/// Single construction point for fighters. <see cref="Combat.FightSession"/> uses it for
/// both the player and the bot, so adding a future character means: add a
/// <see cref="Fighter"/> subclass, one case here and one <see cref="Rendering.FighterCatalog"/>
/// entry — the menu and the fight pick it up without touching the shell.
/// </summary>
public static class FighterFactory
{
    public static Fighter Create(FighterKind kind) => kind switch
    {
        FighterKind.Andrey => new Andrey(),
        FighterKind.Pantera => new Pantera(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестный боец"),
    };

    /// <summary>Default opponent: the other base-roster fighter (extend when a third joins).</summary>
    public static FighterKind OpponentFor(FighterKind kind) =>
        kind == FighterKind.Andrey ? FighterKind.Pantera : FighterKind.Andrey;
}
