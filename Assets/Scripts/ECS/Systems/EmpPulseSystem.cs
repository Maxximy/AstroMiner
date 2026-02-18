using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// EMP Pulse skill: deals AoE damage to all asteroids within blast radius centered on mouse.
    /// Applies DoT (BurningData) to hit asteroids.
    /// Emits DamageEvent per hit and a single SkillEvent for VFX bridge.
    /// </summary>
    [BurstCompile]
    public partial struct EmpPulseSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 4u);
            state.RequireForUpdate<SkillInputData>();
            state.RequireForUpdate<SkillCooldownData>();
            state.RequireForUpdate<InputData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<CritConfigData>();
            state.RequireForUpdate<SkillStatsData>();
            state.RequireForUpdate<SkillUnlockData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            var skillInput = SystemAPI.GetSingleton<SkillInputData>();
            if (!skillInput.Skill3Pressed)
                return;

            var unlocks = SystemAPI.GetSingleton<SkillUnlockData>();
            if (!unlocks.Skill3Unlocked) return;

            var cooldown = SystemAPI.GetSingletonRW<SkillCooldownData>();
            if (cooldown.ValueRO.Skill3Remaining > 0f)
                return;

            // Activate: set cooldown from SkillStatsData
            var stats = SystemAPI.GetSingleton<SkillStatsData>();
            cooldown.ValueRW.Skill3Remaining = stats.EmpCooldown;

            var input = SystemAPI.GetSingleton<InputData>();
            var critConfig = SystemAPI.GetSingleton<CritConfigData>();
            float2 mousePos = input.MouseWorldPos;
            float radiusSq = stats.EmpRadius * stats.EmpRadius;

            // Get ECB for structural changes (adding/refreshing BurningData)
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();
            var skillEventBuffer = SystemAPI.GetSingletonBuffer<SkillEvent>();

            // Iterate all asteroids and check distance from mouse
            foreach (var (transform, health, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthData>>()
                         .WithAll<AsteroidTag>()
                         .WithEntityAccess())
            {
                float2 asteroidPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z);
                float distSq = math.distancesq(asteroidPos, mousePos);

                if (distSq <= radiusSq)
                {
                    // Crit roll
                    bool isCrit = rng.NextFloat() < critConfig.CritChance;
                    float damage = isCrit
                        ? stats.EmpDamage * critConfig.CritMultiplier
                        : stats.EmpDamage;

                    health.ValueRW.CurrentHP -= damage;

                    // Emit DamageEvent: purple color for EMP
                    damageBuffer.Add(new DamageEvent
                    {
                        Position = transform.ValueRO.Position,
                        Amount = damage,
                        Type = isCrit ? DamageType.Critical : DamageType.Skill,
                        ColorR = 180, ColorG = 100, ColorB = 255
                    });

                    // Apply or refresh DoT (BurningData) -- EMP burn
                    var burning = new BurningData
                    {
                        DamagePerTick = stats.EmpDotDamagePerTick,
                        TickInterval = stats.EmpDotTickInterval,
                        RemainingDuration = stats.EmpDotDuration,
                        TickAccumulator = 0f
                    };

                    if (SystemAPI.HasComponent<BurningData>(entity))
                    {
                        ecb.SetComponent(entity, burning);
                    }
                    else
                    {
                        ecb.AddComponent(entity, burning);
                    }
                }
            }

            // Emit SkillEvent for VFX bridge
            skillEventBuffer.Add(new SkillEvent
            {
                SkillType = 2,
                OriginPos = new float2(GameConstants.ShipPositionX, GameConstants.ShipPositionZ),
                TargetPos = mousePos,
                ChainCount = 0,
                Radius = stats.EmpRadius
            });
        }
    }
}
