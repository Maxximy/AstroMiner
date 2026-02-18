using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Ticks DoT damage on all entities with BurningData component.
    /// Accumulates time, applies DamagePerTick at each TickInterval.
    /// Removes BurningData when RemainingDuration expires via ECB.
    /// DoT ticks can crit. Emits DamageEvent with DamageType.Normal (white popups).
    /// </summary>
    [BurstCompile]
    public partial struct BurningDamageSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 5u);
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<CritConfigData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var critConfig = SystemAPI.GetSingleton<CritConfigData>();
            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (burning, health, transform, entity) in
                     SystemAPI.Query<RefRW<BurningData>, RefRW<HealthData>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Decrement remaining duration
                burning.ValueRW.RemainingDuration -= dt;

                // Accumulate tick timer
                burning.ValueRW.TickAccumulator += dt;

                // Apply damage on each tick interval
                while (burning.ValueRO.TickAccumulator >= burning.ValueRO.TickInterval)
                {
                    burning.ValueRW.TickAccumulator -= burning.ValueRO.TickInterval;

                    // Crit roll on DoT tick
                    bool isCrit = rng.NextFloat() < critConfig.CritChance;
                    float damage = isCrit
                        ? burning.ValueRO.DamagePerTick * critConfig.CritMultiplier
                        : burning.ValueRO.DamagePerTick;

                    health.ValueRW.CurrentHP -= damage;

                    // Emit DamageEvent: white for DoT (DamageType.Normal per user decision)
                    damageBuffer.Add(new DamageEvent
                    {
                        Position = transform.ValueRO.Position,
                        Amount = damage,
                        Type = isCrit ? DamageType.Critical : DamageType.Normal,
                        ColorR = 255, ColorG = 255, ColorB = 255
                    });
                }

                // Remove BurningData when expired
                if (burning.ValueRO.RemainingDuration <= 0f)
                {
                    ecb.RemoveComponent<BurningData>(entity);
                }
            }
        }
    }
}
