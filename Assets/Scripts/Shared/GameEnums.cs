/// <summary>
/// Asteroid visual size class, determined by MaxHP thresholds.
/// Maps to per-tier prefab variants (e.g., iron_s, iron_m, iron_l).
/// </summary>
public enum AsteroidSize : byte
{
    Small = 0,
    Medium = 1,
    Large = 2
}

public enum GamePhase
{
    Playing,
    Collecting,
    GameOver,
    Upgrading
}

/// <summary>
/// Tech tree branch categories. Each upgrade node belongs to one branch.
/// Branches radiate outward from the center START node.
/// </summary>
public enum UpgradeBranch
{
    Mining,
    Economy,
    Ship,
    Run,
    Progression
}

/// <summary>
/// All modifiable stats that upgrade nodes can target.
/// Used by StatEffect to define what an upgrade changes.
/// </summary>
public enum StatTarget
{
    // Mining branch
    MiningRadius,
    MiningDamage,
    MiningTickInterval,
    CritChance,
    CritMultiplier,
    DotDamage,
    DotDuration,

    // Economy branch
    ResourceMultiplier,
    LuckyStrikeChance,
    SpawnRateReduction,
    MaxAsteroidsBonus,

    // Ship branch
    SkillUnlock,
    LaserDamage,
    LaserCooldown,
    ChainDamage,
    ChainCooldown,
    ChainTargets,
    EmpDamage,
    EmpCooldown,
    EmpRadius,
    OverchargeCooldown,
    OverchargeDuration,
    OverchargeDamageMultiplier,
    ComboMastery,

    // Run branch
    RunDuration,

    // Progression branch
    AdvanceLevel
}
