using DvorFight.Config;
using DvorFight.Core;

namespace DvorFight.AI;

/// <summary>
/// One bot difficulty preset: the multipliers that the <see cref="BotController"/>
/// applies to its base behaviour. Every numeric value comes from <see cref="Balance"/>.
/// Display names/descriptions for the menu live in <see cref="Rendering.DifficultyCatalog"/>,
/// so the AI layer stays free of UI strings.
/// </summary>
public sealed class BotDifficultyProfile
{
    public BotDifficulty Difficulty { get; }
    public float ThinkScale { get; }
    public float SpecialScale { get; }
    public float JumpScale { get; }
    public float MeleeRangeScale { get; }

    private BotDifficultyProfile(
        BotDifficulty difficulty,
        float thinkScale,
        float specialScale,
        float jumpScale,
        float meleeRangeScale)
    {
        Difficulty = difficulty;
        ThinkScale = thinkScale;
        SpecialScale = specialScale;
        JumpScale = jumpScale;
        MeleeRangeScale = meleeRangeScale;
    }

    private static readonly BotDifficultyProfile Easy = new(
        BotDifficulty.Easy,
        Balance.BotEasyThinkScale, Balance.BotEasySpecialScale,
        Balance.BotEasyJumpScale, Balance.BotEasyMeleeRangeScale);

    private static readonly BotDifficultyProfile Normal = new(
        BotDifficulty.Normal,
        Balance.BotNormalThinkScale, Balance.BotNormalSpecialScale,
        Balance.BotNormalJumpScale, Balance.BotNormalMeleeRangeScale);

    private static readonly BotDifficultyProfile Hard = new(
        BotDifficulty.Hard,
        Balance.BotHardThinkScale, Balance.BotHardSpecialScale,
        Balance.BotHardJumpScale, Balance.BotHardMeleeRangeScale);

    public static BotDifficultyProfile For(BotDifficulty difficulty) => difficulty switch
    {
        BotDifficulty.Easy => Easy,
        BotDifficulty.Hard => Hard,
        _ => Normal,
    };
}
