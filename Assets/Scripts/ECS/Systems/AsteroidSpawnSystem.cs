using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Spawns asteroid entities at the top of the play area at configurable intervals.
    /// Uses ECB for structural changes (entity creation).
    /// Only runs during the Playing game phase.
    /// Reads RunConfigData for per-level HP multiplier and resource tier weights.
    /// </summary>
    [BurstCompile]
    public partial struct AsteroidSpawnSystem : ISystem
    {
        private Random rng;
        private EntityQuery asteroidQuery;

        public void OnCreate(ref SystemState state)
        {
            // Seed the RNG with a non-zero value
            rng = new Random((uint)System.Environment.TickCount | 1u);

            // Query to count existing asteroids
            asteroidQuery = state.GetEntityQuery(ComponentType.ReadOnly<AsteroidTag>());

            // Require singletons before running
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<AsteroidSpawnTimer>();
            state.RequireForUpdate<RunConfigData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only spawn during Playing state
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            // Read/write the spawn timer singleton
            var spawnTimer = SystemAPI.GetSingletonRW<AsteroidSpawnTimer>();
            spawnTimer.ValueRW.TimeUntilNextSpawn -= SystemAPI.Time.DeltaTime;

            if (spawnTimer.ValueRO.TimeUntilNextSpawn > 0f)
                return;

            // Reset timer for next spawn
            spawnTimer.ValueRW.TimeUntilNextSpawn = spawnTimer.ValueRO.SpawnInterval;

            // Check if we've hit the asteroid cap
            var currentCount = asteroidQuery.CalculateEntityCount();
            if (currentCount >= spawnTimer.ValueRO.MaxActiveAsteroids)
                return;

            // Read RunConfigData for tier weights and HP multiplier
            var runConfig = SystemAPI.GetSingleton<RunConfigData>();

            // Get ECB for structural changes (played back at end of simulation group)
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Random position along the top edge of the play area
            var x = rng.NextFloat(GameConstants.PlayAreaXMin, GameConstants.PlayAreaXMax);
            var driftSpeed = rng.NextFloat(GameConstants.DefaultDriftSpeedMin, GameConstants.DefaultDriftSpeedMax);
            var spinSpeed = rng.NextFloat(GameConstants.DefaultSpinMin, GameConstants.DefaultSpinMax);

            // Pick resource tier via weighted random from RunConfigData tier weights
            int tier = PickResourceTier(ref rng, runConfig);

            // Calculate HP based on level multiplier and tier bonus
            // Higher tiers have slightly more HP: base * (1 + tier * 0.2) * levelMultiplier
            float baseHP = GameConstants.DefaultAsteroidHP;
            float hp = baseHP * (1f + tier * 0.2f) * runConfig.AsteroidHPMultiplier;

            // Create asteroid entity on XZ plane (Y=0, spawn at top Z)
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new AsteroidTag());
            ecb.AddComponent(entity, LocalTransform.FromPosition(new float3(x, 0f, GameConstants.PlayAreaZMax)));
            ecb.AddComponent(entity, new LocalToWorld());
            ecb.AddComponent(entity, new DriftData { Speed = driftSpeed });
            ecb.AddComponent(entity, new SpinData { RadiansPerSecond = spinSpeed });
            ecb.AddComponent(entity, new HealthData
            {
                MaxHP = hp,
                CurrentHP = hp
            });
            ecb.AddComponent(entity, new DamageTickTimer { Elapsed = 0f });
            ecb.AddComponent(entity, new AsteroidResourceTier { Tier = tier });
        }

        /// <summary>
        /// Weighted random selection from the 6 tier weights in RunConfigData.
        /// Burst-compatible: uses only unmanaged data from the singleton.
        /// </summary>
        [BurstCompile]
        private static int PickResourceTier(ref Random rng, RunConfigData config)
        {
            float totalWeight = config.TierWeight0 + config.TierWeight1 + config.TierWeight2
                              + config.TierWeight3 + config.TierWeight4 + config.TierWeight5;

            // Fallback: if no weights set, return tier 0 (Iron)
            if (totalWeight <= 0f)
                return 0;

            float roll = rng.NextFloat(0f, totalWeight);

            roll -= config.TierWeight0;
            if (roll <= 0f) return 0;

            roll -= config.TierWeight1;
            if (roll <= 0f) return 1;

            roll -= config.TierWeight2;
            if (roll <= 0f) return 2;

            roll -= config.TierWeight3;
            if (roll <= 0f) return 3;

            roll -= config.TierWeight4;
            if (roll <= 0f) return 4;

            // Remaining weight belongs to tier 5
            return 5;
        }
    }
}
