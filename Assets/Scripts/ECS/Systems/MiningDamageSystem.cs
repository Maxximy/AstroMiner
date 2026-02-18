using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Tick-based AoE damage system for the mining circle.
    /// Reads mouse position from InputData singleton and mining config from MiningConfigData.
    /// Applies damage to all asteroids within mining radius at a configurable tick rate.
    /// Resets per-asteroid tick timers when they leave the mining circle.
    /// </summary>
    [BurstCompile]
    public partial struct MiningDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Guard: require GameStateData singleton
            if (!SystemAPI.HasSingleton<GameStateData>())
                return;

            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            // Guard: require valid mouse input
            var input = SystemAPI.GetSingleton<InputData>();
            if (!input.MouseValid)
                return;

            // Read mining configuration
            var config = SystemAPI.GetSingleton<MiningConfigData>();
            float radiusSq = config.Radius * config.Radius;
            float dt = SystemAPI.Time.DeltaTime;

            // Get DamageEvent buffer for visual/audio feedback (Phase 4)
            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();

            // Iterate all asteroids and apply tick-based damage if within mining circle
            foreach (var (transform, health, tickTimer) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthData>, RefRW<DamageTickTimer>>()
                         .WithAll<AsteroidTag>())
            {
                // Distance check on XZ plane (InputBridge writes float2(worldPoint.x, worldPoint.z))
                float2 asteroidPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z);
                float distSq = math.distancesq(asteroidPos, input.MouseWorldPos);

                if (distSq <= radiusSq)
                {
                    // Inside mining circle: accumulate tick timer
                    tickTimer.ValueRW.Elapsed += dt;

                    // Apply damage on each tick interval
                    if (tickTimer.ValueRO.Elapsed >= config.TickInterval)
                    {
                        health.ValueRW.CurrentHP -= config.DamagePerTick;

                        // Emit damage event for visual feedback (floating damage numbers)
                        damageBuffer.Add(new DamageEvent
                        {
                            Position = transform.ValueRO.Position,
                            Amount = config.DamagePerTick,
                            Type = DamageType.Normal,
                            ColorR = 255, ColorG = 255, ColorB = 255
                        });

                        // Subtract rather than reset to preserve fractional accumulation
                        tickTimer.ValueRW.Elapsed -= config.TickInterval;
                    }
                }
                else
                {
                    // Outside mining circle: reset tick timer
                    tickTimer.ValueRW.Elapsed = 0f;
                }
            }
        }
    }
}
