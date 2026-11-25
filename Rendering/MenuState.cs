using DvorFight.Core;

namespace DvorFight.Rendering;

/// <summary>
/// Selection kept across menu screens: which fighter and difficulty were chosen.
/// The game shell owns one instance and passes it to the fighter-select screen.
/// </summary>
public sealed class MenuState
{
    public FighterKind SelectedFighter { get; set; } = FighterKind.Andrey;
    public BotDifficulty Difficulty { get; set; } = BotDifficulty.Normal;
}
