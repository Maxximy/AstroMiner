/// <summary>
/// Central static class for gameplay tuning values.
/// Uses const fields so they are accessible from Burst-compiled ISystem code.
/// </summary>
public static class GameConstants
{
    // ── Play Area Bounds (XZ plane, Y=0) ──────────────────────────

    /// <summary>Left horizontal boundary of the play area.</summary>
    public const float PlayAreaXMin = -12f;

    /// <summary>Right horizontal boundary of the play area.</summary>
    public const float PlayAreaXMax = 12f;

    /// <summary>Bottom boundary (asteroids destroyed below this Z).</summary>
    public const float PlayAreaZMin = -8f;

    /// <summary>Top boundary (asteroids spawn at this Z).</summary>
    public const float PlayAreaZMax = 12f;

    /// <summary>Ship placeholder Z position (near bottom of visible area).</summary>
    public const float ShipPositionZ = -6f;

    // ── Asteroid Defaults ─────────────────────────────────────────

    /// <summary>Seconds between asteroid spawns.</summary>
    public const float DefaultSpawnInterval = 1.5f;

    /// <summary>Maximum simultaneous asteroid entities.</summary>
    public const int DefaultMaxAsteroids = 50;

    /// <summary>Default asteroid hit points.</summary>
    public const float DefaultAsteroidHP = 100f;

    /// <summary>Minimum downward drift speed (units/sec).</summary>
    public const float DefaultDriftSpeedMin = 1.0f;

    /// <summary>Maximum downward drift speed (units/sec).</summary>
    public const float DefaultDriftSpeedMax = 3.0f;

    /// <summary>Minimum spin speed (radians/sec).</summary>
    public const float DefaultSpinMin = 0.5f;

    /// <summary>Maximum spin speed (radians/sec).</summary>
    public const float DefaultSpinMax = 3.0f;

    // ── Mining Defaults (consumed by Plan 02-02) ──────────────────

    /// <summary>Mining circle radius in world units.</summary>
    public const float DefaultMiningRadius = 2.5f;

    /// <summary>Damage applied per tick to each asteroid in range.</summary>
    public const float DefaultDamagePerTick = 10f;

    /// <summary>Seconds between damage ticks.</summary>
    public const float DefaultTickInterval = 0.25f;

    // ── Mineral Defaults ────────────────────────────────────────

    /// <summary>Minimum minerals spawned per destroyed asteroid.</summary>
    public const int MinMineralsPerAsteroid = 3;

    /// <summary>Maximum minerals spawned per destroyed asteroid.</summary>
    public const int MaxMineralsPerAsteroid = 8;

    /// <summary>Credits awarded per mineral for the default Iron tier.</summary>
    public const int DefaultCreditValuePerMineral = 10;

    /// <summary>Initial speed of mineral particles toward the ship (units/sec).</summary>
    public const float MineralInitialSpeed = 1f;

    /// <summary>Acceleration applied to mineral particles each frame (units/sec^2).</summary>
    public const float MineralAcceleration = 3f;

    /// <summary>Distance from ship center at which minerals are collected.</summary>
    public const float MineralCollectionRadius = 0.8f;

    /// <summary>Visual scale of mineral GameObjects (uniform XYZ).</summary>
    public const float MineralScale = 0.3f;

    // ── Session Defaults (consumed by Plan 03-02) ───────────────

    /// <summary>Default duration of a timed run in seconds.</summary>
    public const float DefaultRunDuration = 10f;

    /// <summary>Grace period after run ends for collecting remaining minerals.</summary>
    public const float CollectingGracePeriod = 2f;

    // ── Ship Position ───────────────────────────────────────────

    /// <summary>Ship X coordinate (for mineral pull target).</summary>
    public const float ShipPositionX = 0f;

    // ── Feedback / VFX Defaults ────────────────────────────────

    /// <summary>Duration of damage popup float-up animation (seconds).</summary>
    public const float DamagePopupDuration = 0.8f;

    /// <summary>Speed at which damage popups rise (world units/sec).</summary>
    public const float DamagePopupRiseSpeed = 1.5f;

    /// <summary>Time before popup starts fading (seconds).</summary>
    public const float DamagePopupFadeDelay = 0.3f;

    /// <summary>Font size for normal damage numbers (world-space units).</summary>
    public const float DamagePopupFontSizeNormal = 3f;

    /// <summary>Font size for critical hit numbers (world-space units).</summary>
    public const float DamagePopupFontSizeCrit = 5f;

    /// <summary>Scale multiplier for critical hit popup.</summary>
    public const float DamagePopupCritScale = 1.5f;

    /// <summary>Number of debris particles per asteroid explosion.</summary>
    public const int ExplosionParticleCount = 20;

    /// <summary>Lifetime of debris particles (seconds).</summary>
    public const float ExplosionParticleLifetime = 0.7f;

    /// <summary>Outward velocity of debris particles (units/sec).</summary>
    public const float ExplosionParticleSpeed = 4f;

    /// <summary>Gravity applied to debris particles (units/sec^2).</summary>
    public const float ExplosionParticleGravity = 2.5f;

    /// <summary>Trail duration for mineral flight trails (seconds).</summary>
    public const float MineralTrailDuration = 0.3f;

    /// <summary>Trail start width for mineral flight trails.</summary>
    public const float MineralTrailStartWidth = 0.15f;

    /// <summary>HDR intensity multiplier for mineral glow.</summary>
    public const float MineralEmissiveIntensity = 2f;

    /// <summary>Duration of credit counter pop animation (seconds).</summary>
    public const float CreditPopDuration = 0.2f;

    /// <summary>Scale boost factor for credit counter pop.</summary>
    public const float CreditPopScale = 1.3f;
}
