using Unity.Entities;

namespace ECS.Components
{
    /// <summary>
    /// Tag component to identify asteroid entities.
    /// Used by movement, bounds, destruction, and rendering systems.
    /// </summary>
    public struct AsteroidTag : IComponentData
    {
    }

    /// <summary>
    /// Health data for any damageable entity.
    /// Reusable beyond asteroids (e.g., future boss entities).
    /// </summary>
    public struct HealthData : IComponentData
    {
        /// <summary>Maximum hit points (for UI health bars, healing caps).</summary>
        public float MaxHP;

        /// <summary>Current hit points. Entity destroyed when this reaches 0.</summary>
        public float CurrentHP;
    }

    /// <summary>
    /// Per-entity damage tick accumulator.
    /// Tracks elapsed time since last damage application from the mining circle.
    /// Used by MiningDamageSystem (Plan 02-02).
    /// </summary>
    public struct DamageTickTimer : IComponentData
    {
        /// <summary>Seconds elapsed since last damage tick.</summary>
        public float Elapsed;
    }

    /// <summary>
    /// Tracks which resource tier an asteroid drops when destroyed.
    /// Set by AsteroidSpawnSystem based on the current level's drop table.
    /// Read by MineralSpawnSystem to assign mineral credit values and colors.
    /// </summary>
    public struct AsteroidResourceTier : IComponentData
    {
        /// <summary>Resource tier index (0=Iron, 1=Copper, 2=Silver, 3=Cobalt, 4=Gold, 5=Titanium).</summary>
        public int Tier;
    }

    /// <summary>
    /// Singleton component controlling asteroid spawn timing and limits.
    /// Created by ECSBootstrap, read/written by AsteroidSpawnSystem.
    /// </summary>
    public struct AsteroidSpawnTimer : IComponentData
    {
        /// <summary>Seconds between spawn attempts.</summary>
        public float SpawnInterval;

        /// <summary>Countdown until next spawn (decremented each frame).</summary>
        public float TimeUntilNextSpawn;

        /// <summary>Maximum simultaneous asteroid entities allowed.</summary>
        public int MaxActiveAsteroids;
    }

    /// <summary>
    /// Singleton component holding mining circle configuration.
    /// Created by ECSBootstrap, read by MiningDamageSystem (Plan 02-02)
    /// and MiningCircleVisual (Plan 02-02).
    /// </summary>
    public struct MiningConfigData : IComponentData
    {
        /// <summary>Mining circle radius in world units.</summary>
        public float Radius;

        /// <summary>Damage applied per tick to each asteroid in range.</summary>
        public float DamagePerTick;

        /// <summary>Seconds between damage ticks per asteroid.</summary>
        public float TickInterval;
    }
}