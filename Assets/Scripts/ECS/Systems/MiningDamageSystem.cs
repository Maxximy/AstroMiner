using System;
using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

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
        private float tickElapsed;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CritConfigData>();
            state.RequireForUpdate<OverchargeBuffData>();
            state.RequireForUpdate<MiningConfigData>();
            state.RequireForUpdate<InputData>();
            rng = new Random((uint)Environment.TickCount | 1u);
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing) return;

            var input = SystemAPI.GetSingleton<InputData>();
            if (!input.MouseValid) return;

            var config = SystemAPI.GetSingleton<MiningConfigData>();

            // Accumulate global tick timer
            tickElapsed += SystemAPI.Time.DeltaTime;
            if (tickElapsed < config.TickInterval) return;
            tickElapsed -= config.TickInterval;

            // Read Overcharge buff -- apply multipliers if active
            var overcharge = SystemAPI.GetSingleton<OverchargeBuffData>();
            var overchargeActive = overcharge.RemainingDuration > 0f;
            var effectiveRadius = overchargeActive ? config.Radius * overcharge.RadiusMultiplier : config.Radius;
            var effectiveDamage = overchargeActive
                ? config.DamagePerTick * overcharge.DamageMultiplier
                : config.DamagePerTick;
            var radiusSq = effectiveRadius * effectiveRadius;

            var critConfig = SystemAPI.GetSingleton<CritConfigData>();
            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();

            // Damage all asteroids currently inside the mining circle
            foreach (var (transform, health) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthData>>()
                         .WithAll<AsteroidTag>())
            {
                var asteroidPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z);
                var distSq = math.distancesq(asteroidPos, input.MouseWorldPos);

                if (distSq <= radiusSq)
                {
                    var isCrit = rng.NextFloat() < critConfig.CritChance;
                    var damage = isCrit ? effectiveDamage * critConfig.CritMultiplier : effectiveDamage;

                    health.ValueRW.CurrentHP -= damage;

                    damageBuffer.Add(new DamageEvent
                    {
                        Position = transform.ValueRO.Position,
                        Amount = damage,
                        Type = isCrit ? DamageType.Critical : DamageType.Normal,
                        ColorR = 255, ColorG = 255, ColorB = 255
                    });
                }
            }
        }
    }
}