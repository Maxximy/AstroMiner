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
    /// Integrates Overcharge buff (damage/radius multipliers) and critical hit rolls.
    /// </summary>
    [BurstCompile]
    public partial struct MiningDamageSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 1u);
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
            float dt = SystemAPI.Time.DeltaTime;

            // Read Overcharge buff -- apply multipliers if active
            var overcharge = SystemAPI.GetSingleton<OverchargeBuffData>();
            bool overchargeActive = overcharge.RemainingDuration > 0f;
            float effectiveRadius = overchargeActive ? config.Radius * overcharge.RadiusMultiplier : config.Radius;
            float effectiveDamage = overchargeActive ? config.DamagePerTick * overcharge.DamageMultiplier : config.DamagePerTick;
            float radiusSq = effectiveRadius * effectiveRadius;

            // Read crit configuration
            var critConfig = SystemAPI.GetSingleton<CritConfigData>();

            // Get DamageEvent buffer for visual/audio feedback
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
                        // Crit roll
                        bool isCrit = rng.NextFloat() < critConfig.CritChance;
                        float damage = isCrit ? effectiveDamage * critConfig.CritMultiplier : effectiveDamage;

                        health.ValueRW.CurrentHP -= damage;

                        // Emit damage event for visual feedback (floating damage numbers)
                        damageBuffer.Add(new DamageEvent
                        {
                            Position = transform.ValueRO.Position,
                            Amount = damage,
                            Type = isCrit ? DamageType.Critical : DamageType.Normal,
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
