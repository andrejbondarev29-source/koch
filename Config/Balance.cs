namespace DvorFight.Config;

/// <summary>
/// Every gameplay number in one place. All values were carried over 1:1 from the
/// original JavaScript build and are documented next to their source location.
/// </summary>
public static class Balance
{
    // --- Screen / world ---
    public const int ViewWidth = 960;
    public const int ViewHeight = 480;
    public const float Floor = 405f;          // y of the ground line, in screen space

    // --- Fighter rendering ---
    public const float SpriteWidth = 230f;     // draw width of a fighter sprite
    public const float SpriteAspect = 252f / 264f; // source PNGs are 264x252

    // --- Movement / physics ---
    public const float Gravity = 1330f;        // px/s^2
    public const float JumpVelocity = 550f;    // px/s
    public const float WalkSpeed = 210f;       // px/s
    public const float MinX = 35f;
    public const float MaxX = 925f;
    public const float PushDecay = 10f;        // exp(-PushDecay * dt) for knockback

    // --- Health ---
    public const float MaxHp = 100f;
    public const float HpBarWidth = 360f;
    public const float HpScale = 3.6f;         // px per hp point

    // --- Melee ---
    public const float LightDamage = 8f;
    public const float HeavyDamage = 18f;
    public const float LightRange = 112f;
    public const float HeavyRange = 155f;
    public const float MeleeVertical = 100f;
    public const float JabKickDuration = 0.44f;
    public const float HeavyDuration = 0.70f;

    // --- Specials ---
    public const float SpecialZCooldown = 3.5f;
    public const float SummonXCooldown = 8f;
    public const float MorphXCooldown = 15f;
    public const float MorphDuration = 7f;
    public const float SpecialDuration = 0.85f;
    public const float SummonActionDuration = 0.4f;
    public const float ShoesFireFactor = 0.74f;
    public const float CommonFireFactor = 0.51f;

    // --- Ear beam (Pantera Z) ---
    public const float EarBeamDamage = 16f;
    public const float EarBeamVertical = 95f;
    public const float EarBeamRange = 420f;     // was unlimited in the original; capped per review
    public const float BeamThickness = 5f;

    // --- Shoes (Andrey Z) ---
    public const float ShoeDamage = 8f;
    public const float ShoeSpeed = 490f;
    public const float ShoeLifetime = 2f;
    public const float ShoeHitRadius = 34f;
    public const float ShoeCount = 2f;

    // --- Dancer summon (Pantera X) ---
    public const float SummonDamage = 21f;
    public const float SummonWait = 1.18f;
    public const float SummonLeap = 0.62f;
    public const float SummonSpeed = 440f;
    public const float SummonArc = 125f;
    public const float SummonLifetime = 1.95f;
    public const float SummonHitRadius = 65f;
    public const float SummonVertical = 100f;

    // --- Hit feedback ---
    public const float HeavyHitStop = 0.092f;
    public const float LightHitStop = 0.044f;
    public const float HeavyShake = 6f;
    public const float LightShake = 2.5f;
    public const float FlashDuration = 0.14f;
    public const float HeavyKnockback = 360f;
    public const float LightKnockback = 150f;

    // --- Body separation ---
    public const float PushApartGap = 55f;
    public const float PushApartVertical = 75f;

    // --- AI ---
    public const float AiThinkMin = 0.28f;
    public const float AiThinkRand = 0.32f;
    public const float AiProbSpecialX = 0.15f;
    public const float AiProbSpecialZ = 0.42f;
    public const float AiProbJump = 0.10f;
    public const float AiApproachDistance = 88f;
    public const float AiMeleeDistance = 115f;

    // --- Spark effect ---
    public const float SparkLifetime = 0.13f;
    public const int SparkRays = 7;
    public const float SparkHeavySpeed = 180f;
    public const float SparkLightSpeed = 100f;
}
