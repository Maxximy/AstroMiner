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
}
