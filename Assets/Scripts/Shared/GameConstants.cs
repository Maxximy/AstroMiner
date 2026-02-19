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
    public const float ShipPositionZ = -10f;

    // ── Asteroid Defaults ─────────────────────────────────────────

    /// <summary>Seconds between asteroid spawns.</summary>
    public const float DefaultSpawnInterval = 1.5f;

    /// <summary>Maximum simultaneous asteroid entities.</summary>
    public const int DefaultMaxAsteroids = 50;

    /// <summary>Default asteroid hit points.</summary>
    public const float DefaultAsteroidHP = 20;

    /// <summary>Minimum downward drift speed (units/sec).</summary>
    public const float DefaultDriftSpeedMin = 1.0f;

    /// <summary>Maximum downward drift speed (units/sec).</summary>
    public const float DefaultDriftSpeedMax = 3.0f;

    /// <summary>Minimum spin speed (radians/sec).</summary>
    public const float DefaultSpinMin = 0.5f;

    /// <summary>Maximum spin speed (radians/sec).</summary>
    public const float DefaultSpinMax = 3.0f;

    // ── Asteroid Size Classification ────────────────────────────────

    /// <summary>HP below this value is Small; at or above is Medium.</summary>
    public const float AsteroidSizeSmallMaxHP = 30f;

    /// <summary>HP at or above this value is Large; below is Medium.</summary>
    public const float AsteroidSizeLargeMinHP = 50f;

    /// <summary>Scale jitter min multiplier for custom mesh asteroids.</summary>
    public const float AsteroidMeshScaleJitterMin = 4.5f;

    /// <summary>Scale jitter max multiplier for custom mesh asteroids.</summary>
    public const float AsteroidMeshScaleJitterMax = 5.5f;

    /// <summary>Minimum visual rotation speed for asteroids (degrees/sec).</summary>
    public const float AsteroidRotationSpeedMin = 15f;

    /// <summary>Maximum visual rotation speed for asteroids (degrees/sec).</summary>
    public const float AsteroidRotationSpeedMax = 90f;

    /// <summary>GameObjects to pre-warm per asteroid variant pool on first use.</summary>
    public const int AsteroidPoolPreWarmPerVariant = 5;

    /// <summary>Maximum pool size per asteroid variant.</summary>
    public const int AsteroidPoolMaxPerVariant = 20;

    // ── Mining Defaults (consumed by Plan 02-02) ──────────────────

    /// <summary>Mining circle radius in world units.</summary>
    public const float DefaultMiningRadius = 2.5f;

    /// <summary>Damage applied per tick to each asteroid in range.</summary>
    public const float DefaultDamagePerTick = 10f;

    /// <summary>Seconds between damage ticks.</summary>
    public const float DefaultTickInterval = 2.0f;

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
    public const float MineralScale = 1.0f;

    // ── Session Defaults (consumed by Plan 03-02) ───────────────

    /// <summary>Default duration of a timed run in seconds.</summary>
    public const float DefaultRunDuration = 20f;

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
    public const float DamagePopupFontSizeNormal = 1f;

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

    /// <summary>HDR intensity multiplier for mineral glow.</summary>
    public const float MineralEmissiveIntensity = 2f;

    /// <summary>Duration of credit counter pop animation (seconds).</summary>
    public const float CreditPopDuration = 0.2f;

    /// <summary>Scale boost factor for credit counter pop.</summary>
    public const float CreditPopScale = 1.3f;

    // ── Audio Defaults ───────────────────────────────────────────

    /// <summary>Maximum simultaneous SFX AudioSources.</summary>
    public const int AudioSFXPoolSize = 20;

    /// <summary>Minimum interval between mining hit SFX plays (seconds).</summary>
    public const float DamageHitSFXCooldown = 0.25f;

    /// <summary>Collection chime batch window (seconds).</summary>
    public const float CollectionChimeBatchWindow = 0.05f;

    /// <summary>Max distance for SFX volume attenuation.</summary>
    public const float SFXMaxDistance = 30f;

    /// <summary>Screen shake duration (seconds, ~3 frames at 60fps).</summary>
    public const float ScreenShakeDuration = 0.05f;

    /// <summary>Screen shake magnitude (world units offset).</summary>
    public const float ScreenShakeMagnitude = 0.15f;

    /// <summary>Timer warning vignette trigger threshold (seconds remaining).</summary>
    public const float TimerWarningThreshold = 10f;

    /// <summary>Timer warning vignette max intensity.</summary>
    public const float TimerWarningMaxIntensity = 0.4f;

    // -- Skill Defaults -------------------------------------------------------

    /// <summary>Laser Burst cooldown in seconds.</summary>
    public const float LaserBurstCooldown = 8f;

    /// <summary>Laser Burst damage per hit.</summary>
    public const float LaserBurstDamage = 150f;

    /// <summary>Laser Burst beam half-width for line collision.</summary>
    public const float LaserBurstBeamHalfWidth = 0.25f;

    /// <summary>Chain Lightning cooldown in seconds.</summary>
    public const float ChainLightningCooldown = 10f;

    /// <summary>Chain Lightning damage per chain target.</summary>
    public const float ChainLightningDamage = 60f;

    /// <summary>Maximum number of chain targets.</summary>
    public const int ChainLightningMaxTargets = 4;

    /// <summary>Maximum distance between chain targets.</summary>
    public const float ChainLightningMaxChainDist = 5f;

    /// <summary>EMP Pulse cooldown in seconds.</summary>
    public const float EmpPulseCooldown = 12f;

    /// <summary>EMP Pulse damage per hit.</summary>
    public const float EmpPulseDamage = 80f;

    /// <summary>EMP Pulse blast radius.</summary>
    public const float EmpPulseRadius = 4f;

    /// <summary>Overcharge cooldown in seconds.</summary>
    public const float OverchargeCooldown = 15f;

    /// <summary>Overcharge buff duration in seconds.</summary>
    public const float OverchargeDuration = 5f;

    /// <summary>Overcharge damage multiplier.</summary>
    public const float OverchargeDamageMultiplier = 2f;

    /// <summary>Overcharge radius multiplier.</summary>
    public const float OverchargeRadiusMultiplier = 1.5f;

    // -- Critical Hit Defaults -----------------------------------------------

    /// <summary>Base critical hit chance (8%).</summary>
    public const float CritChance = 0.08f;

    /// <summary>Critical hit damage multiplier (2x).</summary>
    public const float CritMultiplier = 2f;

    // -- DoT Burning Defaults ------------------------------------------------

    /// <summary>Laser DoT damage per tick.</summary>
    public const float LaserDotDamagePerTick = 5f;

    /// <summary>Laser DoT tick interval in seconds.</summary>
    public const float LaserDotTickInterval = 0.5f;

    /// <summary>Laser DoT total duration in seconds.</summary>
    public const float LaserDotDuration = 3f;

    /// <summary>EMP DoT damage per tick.</summary>
    public const float EmpDotDamagePerTick = 3f;

    /// <summary>EMP DoT tick interval in seconds.</summary>
    public const float EmpDotTickInterval = 0.5f;

    /// <summary>EMP DoT total duration in seconds.</summary>
    public const float EmpDotDuration = 2f;

    // -- Economy Defaults (Phase 6) ------------------------------------------

    /// <summary>Default mineral drop count per asteroid (before upgrades).</summary>
    public const int DefaultMineralDropCount = 1;

    /// <summary>Default resource credit multiplier (1.0 = no bonus).</summary>
    public const float DefaultResourceMultiplier = 1f;

    /// <summary>Default lucky strike chance (0.0 = no chance).</summary>
    public const float DefaultLuckyStrikeChance = 0f;

    /// <summary>Default combo mastery time window in seconds.</summary>
    public const float DefaultComboMasteryWindow = 5f;

    /// <summary>Default combo mastery damage multiplier.</summary>
    public const float DefaultComboMasteryMultiplier = 1.5f;

    // -- Resource Tier Credit Values (Phase 6) --------------------------------

    /// <summary>Credits per Iron mineral (tier 0).</summary>
    public const int IronCreditValue = 10;

    /// <summary>Credits per Copper mineral (tier 1).</summary>
    public const int CopperCreditValue = 25;

    /// <summary>Credits per Silver mineral (tier 2).</summary>
    public const int SilverCreditValue = 75;

    /// <summary>Credits per Cobalt mineral (tier 3).</summary>
    public const int CobaltCreditValue = 150;

    /// <summary>Credits per Gold mineral (tier 4).</summary>
    public const int GoldCreditValue = 400;

    /// <summary>Credits per Titanium mineral (tier 5).</summary>
    public const int TitaniumCreditValue = 1000;
}
