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

            // Get ECB for structural changes (played back at end of simulation group)
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Random position along the top edge of the play area
            var x = rng.NextFloat(GameConstants.PlayAreaXMin, GameConstants.PlayAreaXMax);
            var driftSpeed = rng.NextFloat(GameConstants.DefaultDriftSpeedMin, GameConstants.DefaultDriftSpeedMax);
            var spinSpeed = rng.NextFloat(GameConstants.DefaultSpinMin, GameConstants.DefaultSpinMax);

            // Create asteroid entity on XZ plane (Y=0, spawn at top Z)
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new AsteroidTag());
            ecb.AddComponent(entity, LocalTransform.FromPosition(new float3(x, 0f, GameConstants.PlayAreaZMax)));
            ecb.AddComponent(entity, new LocalToWorld());
            ecb.AddComponent(entity, new DriftData { Speed = driftSpeed });
            ecb.AddComponent(entity, new SpinData { RadiansPerSecond = spinSpeed });
            ecb.AddComponent(entity, new HealthData
            {
                MaxHP = GameConstants.DefaultAsteroidHP,
                CurrentHP = GameConstants.DefaultAsteroidHP
            });
            ecb.AddComponent(entity, new DamageTickTimer { Elapsed = 0f });
        }
    }
}
