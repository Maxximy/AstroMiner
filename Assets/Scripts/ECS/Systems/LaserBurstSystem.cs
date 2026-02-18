using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Laser Burst skill: damages all asteroids in a line from ship to mouse position.
    /// Applies DoT (BurningData) to hit asteroids.
    /// Emits DamageEvent per hit and a single SkillEvent for VFX bridge.
    /// </summary>
    [BurstCompile]
    public partial struct LaserBurstSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 2u);
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
            if (!skillInput.Skill1Pressed)
                return;

            var unlocks = SystemAPI.GetSingleton<SkillUnlockData>();
            if (!unlocks.Skill1Unlocked) return;

            var cooldown = SystemAPI.GetSingletonRW<SkillCooldownData>();
            if (cooldown.ValueRO.Skill1Remaining > 0f)
                return;

            // Activate: set cooldown from SkillStatsData
            var stats = SystemAPI.GetSingleton<SkillStatsData>();
            cooldown.ValueRW.Skill1Remaining = stats.LaserCooldown;

            // Read input and crit config
            var input = SystemAPI.GetSingleton<InputData>();
            var critConfig = SystemAPI.GetSingleton<CritConfigData>();

            // Compute line from ship to mouse
            float2 shipPos = new float2(GameConstants.ShipPositionX, GameConstants.ShipPositionZ);
            float2 mousePos = input.MouseWorldPos;

            // Get ECB for structural changes (adding/refreshing BurningData)
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Get event buffers
            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();
            var skillEventBuffer = SystemAPI.GetSingletonBuffer<SkillEvent>();

            // Iterate all asteroids and check line collision
            foreach (var (transform, health, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthData>>()
                         .WithAll<AsteroidTag>()
                         .WithEntityAccess())
            {
                float2 asteroidPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z);
                float asteroidRadius = transform.ValueRO.Scale * 0.5f;
                float hitDist = asteroidRadius + stats.LaserBeamHalfWidth;

                float distSq = PointToSegmentDistSq(asteroidPos, shipPos, mousePos);
                if (distSq <= hitDist * hitDist)
                {
                    // Crit roll
                    bool isCrit = rng.NextFloat() < critConfig.CritChance;
                    float damage = isCrit
                        ? stats.LaserDamage * critConfig.CritMultiplier
                        : stats.LaserDamage;

                    health.ValueRW.CurrentHP -= damage;

                    // Emit DamageEvent: cyan color for Laser
                    damageBuffer.Add(new DamageEvent
                    {
                        Position = transform.ValueRO.Position,
                        Amount = damage,
                        Type = isCrit ? DamageType.Critical : DamageType.Skill,
                        ColorR = 0, ColorG = 255, ColorB = 255
                    });

                    // Apply or refresh DoT (BurningData)
                    var burning = new BurningData
                    {
                        DamagePerTick = stats.LaserDotDamagePerTick,
                        TickInterval = stats.LaserDotTickInterval,
                        RemainingDuration = stats.LaserDotDuration,
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
                SkillType = 0,
                OriginPos = shipPos,
                TargetPos = mousePos,
                ChainCount = 0,
                Radius = 0f
            });
        }

        /// <summary>
        /// Computes the squared distance from point p to the line segment a-b.
        /// Burst-compatible, no managed allocations.
        /// </summary>
        static float PointToSegmentDistSq(float2 p, float2 a, float2 b)
        {
            float2 ab = b - a;
            float2 ap = p - a;
            float abLenSq = math.dot(ab, ab);
            if (abLenSq < 0.0001f) return math.distancesq(p, a);
            float t = math.saturate(math.dot(ap, ab) / abLenSq);
            float2 closest = a + t * ab;
            return math.distancesq(p, closest);
        }
    }
}
