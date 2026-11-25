namespace DvorFight.Core;

/// <summary>Top-level screens of the game.</summary>
public enum GameScreen
{
    Menu,
    Fight,
}

/// <summary>The two playable characters.</summary>
public enum FighterKind
{
    Andrey,
    Pantera,
}

/// <summary>Horizontal facing direction, kept as a sign so it can drive mirroring.</summary>
public enum Face
{
    Left = -1,
    Right = 1,
}

/// <summary>All attack / action animations a fighter can perform.</summary>
public enum MoveType
{
    None,
    Jab,
    Kick,
    HeavyStraight,
    GroundSmash,
    ShoesThrow,
    EarBeam,
    Summon,
}

/// <summary>Kinds of procedurally generated sound effects.</summary>
public enum SoundType
{
    Swing,
    Beam,
    Form,
    Summon,
    Impact,
    Heavy,
}

/// <summary>Commands issued by a human or the AI controller.</summary>
public enum Command
{
    Jump,
    Hit,
    SpecialZ,
    SpecialX,
}
