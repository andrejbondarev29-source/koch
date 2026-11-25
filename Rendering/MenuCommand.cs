using DvorFight.Core;

namespace DvorFight.Rendering;

/// <summary>What a menu screen asks the game shell to do after an update.</summary>
public enum MenuCommandType
{
    None,
    /// <summary>«Играть»: open fighter select with the start button focused.</summary>
    Play,
    /// <summary>«Бойцы / Характеристики»: open fighter select with the roster focused.</summary>
    OpenFighters,
    StartFight,
    BackToTitle,
    Exit,
}

/// <summary>
/// Result of one menu-screen update. Screens return this instead of mutating the game
/// shell, so navigation logic stays independent of MonoGame and is unit-testable.
/// </summary>
public readonly record struct MenuCommand(
    MenuCommandType Type,
    FighterKind Fighter = FighterKind.Andrey,
    BotDifficulty Difficulty = BotDifficulty.Normal)
{
    public static MenuCommand None => new(MenuCommandType.None);
    public static MenuCommand PlayGame => new(MenuCommandType.Play);
    public static MenuCommand OpenFighters => new(MenuCommandType.OpenFighters);
    public static MenuCommand BackToTitle => new(MenuCommandType.BackToTitle);
    public static MenuCommand ExitGame => new(MenuCommandType.Exit);

    public static MenuCommand StartFight(FighterKind fighter, BotDifficulty difficulty) =>
        new(MenuCommandType.StartFight, fighter, difficulty);
}
