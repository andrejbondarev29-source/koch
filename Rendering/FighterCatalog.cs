using DvorFight.Config;
using DvorFight.Core;

namespace DvorFight.Rendering;

/// <summary>
/// Menu-side description of a fighter: name, card text and specials summary.
/// All numbers are formatted straight from <see cref="Balance"/>, never duplicated here.
/// The fighter-select screen builds its cards from <see cref="FighterCatalog.All"/>,
/// so a future character only needs a new <see cref="Domain.Fighter"/> subclass and one
/// entry in this catalog — the menu layout adapts by itself.
/// </summary>
public sealed class FighterProfile
{
    public FighterKind Kind { get; }
    public string Name { get; }
    public string Tagline { get; }
    public string Description { get; }
    public string IdleAnimation { get; }

    /// <summary>Asset key of the large select-screen portrait (e.g. <c>portraits/andrey</c>).</summary>
    public string PortraitAnimation { get; }

    /// <summary>Base tilt of the portrait in degrees; the screen adds the breathing sway on top.</summary>
    public float PortraitRotationDegrees { get; }

    public string SpecialZName { get; }
    public string SpecialZDetail { get; }
    public string SpecialXName { get; }
    public string SpecialXDetail { get; }

    /// <summary>Whether the fighter ever gets heavy (morphed) normal attacks.</summary>
    public bool HasHeavyAttacks { get; }

    /// <summary>Short label shown inside the narrow card (first tagline segment).</summary>
    public string CardTagline => Tagline.Split('·')[0].Trim();

    public FighterProfile(
        FighterKind kind,
        string name,
        string tagline,
        string description,
        string idleAnimation,
        string portraitAnimation,
        bool hasHeavyAttacks,
        string specialZName,
        string specialZDetail,
        string specialXName,
        string specialXDetail,
        float portraitRotationDegrees = -8f)
    {
        Kind = kind;
        Name = name;
        Tagline = tagline;
        Description = description;
        IdleAnimation = idleAnimation;
        PortraitAnimation = portraitAnimation;
        HasHeavyAttacks = hasHeavyAttacks;
        SpecialZName = specialZName;
        SpecialZDetail = specialZDetail;
        SpecialXName = specialXName;
        SpecialXDetail = specialXDetail;
        PortraitRotationDegrees = portraitRotationDegrees;
    }
}

public static class FighterCatalog
{
    public static IReadOnlyList<FighterProfile> All { get; } = new[]
    {
        new FighterProfile(
            FighterKind.Andrey,
            "Андрей",
            "Вонючие ноги · Метаморфоза",
            "Дворовый силач: издалека кидает шузы, а в морфе превращается в танк с тяжёлыми ударами.",
            "andrey/walk",
            "portraits/andrey",
            hasHeavyAttacks: true,
            "Вонючие ноги",
            $"{Balance.ShoeCount:0} шуза · урон {Balance.ShoeDamage:0} · откат {Balance.SpecialZCooldown:0.0} с",
            "Метаморфоза",
            $"тяжёлые удары · {Balance.MorphDuration:0} с · откат {Balance.MorphXCooldown:0} с"),

        new FighterProfile(
            FighterKind.Pantera,
            "Пантера",
            "Ушной луч · Призыв танцовщицы",
            "Держит дистанцию: прожигает ушным лучом и призывает танцовщицу добить соперника.",
            "pantera/walk",
            "portraits/pantera",
            hasHeavyAttacks: false,
            "Ушной луч",
            $"урон {Balance.EarBeamDamage:0} · дальность {Balance.EarBeamRange:0} · откат {Balance.SpecialZCooldown:0.0} с",
            "Призыв танцовщицы",
            $"урон {Balance.SummonDamage:0} · откат {Balance.SummonXCooldown:0} с"),
    };

    public static int IndexOf(FighterKind kind)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Kind == kind)
                return i;
        return 0;
    }
}
