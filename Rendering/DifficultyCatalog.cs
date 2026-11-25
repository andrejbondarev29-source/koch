using DvorFight.Core;

namespace DvorFight.Rendering;

/// <summary>Menu-facing name and one-line description of a bot difficulty.</summary>
public sealed class DifficultyInfo
{
    public BotDifficulty Difficulty { get; }
    public string Title { get; }
    public string Description { get; }

    public DifficultyInfo(BotDifficulty difficulty, string title, string description)
    {
        Difficulty = difficulty;
        Title = title;
        Description = description;
    }
}

/// <summary>
/// Display data for the difficulty selector, HUD and result overlay. Kept in the
/// rendering layer so the AI domain only deals with behaviour multipliers.
/// </summary>
public static class DifficultyCatalog
{
    public static IReadOnlyList<DifficultyInfo> All { get; } = new[]
    {
        new DifficultyInfo(
            BotDifficulty.Easy, "Лёгкий",
            "Медленные решения, редкие спецухи, атака вблизи."),
        new DifficultyInfo(
            BotDifficulty.Normal, "Нормальный",
            "Базовое поведение — золотая середина для первого боя."),
        new DifficultyInfo(
            BotDifficulty.Hard, "Жёсткий",
            "Быстрые решения, частые спецухи, удар с дистанции."),
    };

    public static DifficultyInfo For(BotDifficulty difficulty) =>
        All.FirstOrDefault(info => info.Difficulty == difficulty) ?? All[1];
}
