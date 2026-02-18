using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Chain Lightning skill: hits the nearest asteroid to mouse, then chains to nearby asteroids.
    /// Does NOT apply DoT. Emits DamageEvent per target and a single SkillEvent for VFX bridge.
    /// </summary>
    [BurstCompile]
    public partial struct ChainLightningSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 3u);
            state.RequireForUpdate<SkillInputData>();
            state.RequireForUpdate<SkillCooldownData>();
            state.RequireForUpdate<InputData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<CritConfigData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            var skillInput = SystemAPI.GetSingleton<SkillInputData>();
            if (!skillInput.Skill2Pressed)
                return;

            var cooldown = SystemAPI.GetSingletonRW<SkillCooldownData>();
            if (cooldown.ValueRO.Skill2Remaining > 0f)
                return;

            // Activate: set cooldown
            cooldown.ValueRW.Skill2Remaining = cooldown.ValueRO.Skill2MaxCooldown;

            var input = SystemAPI.GetSingleton<InputData>();
            var critConfig = SystemAPI.GetSingleton<CritConfigData>();
            float2 mousePos = input.MouseWorldPos;
            float2 shipPos = new float2(GameConstants.ShipPositionX, GameConstants.ShipPositionZ);

            var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();
            var skillEventBuffer = SystemAPI.GetSingletonBuffer<SkillEvent>();

            // Collect all asteroid entities, positions, and health refs into temp lists
            // We need entity references for visited tracking
            var asteroidEntities = new NativeList<Entity>(64, Allocator.Temp);
            var asteroidPositions = new NativeList<float2>(64, Allocator.Temp);

            foreach (var (transform, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<AsteroidTag>()
                         .WithAll<HealthData>()
                         .WithEntityAccess())
            {
                asteroidEntities.Add(entity);
                asteroidPositions.Add(new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z));
            }

            if (asteroidEntities.Length == 0)
            {
                asteroidEntities.Dispose();
                asteroidPositions.Dispose();
                return;
            }

            // Step 1: Find nearest asteroid to mouse
            var visited = new NativeList<int>(GameConstants.ChainLightningMaxTargets, Allocator.Temp);
            float minDistSq = float.MaxValue;
            int primaryIdx = -1;

            for (int i = 0; i < asteroidPositions.Length; i++)
            {
                float dSq = math.distancesq(asteroidPositions[i], mousePos);
                if (dSq < minDistSq)
                {
                    minDistSq = dSq;
                    primaryIdx = i;
                }
            }

            if (primaryIdx < 0)
            {
                visited.Dispose();
                asteroidEntities.Dispose();
                asteroidPositions.Dispose();
                return;
            }

            visited.Add(primaryIdx);

            // Step 2: Chain to nearby unvisited asteroids
            float maxChainDistSq = GameConstants.ChainLightningMaxChainDist * GameConstants.ChainLightningMaxChainDist;
            int lastIdx = primaryIdx;

            while (visited.Length < GameConstants.ChainLightningMaxTargets)
            {
                float bestDistSq = float.MaxValue;
                int bestIdx = -1;

                for (int i = 0; i < asteroidPositions.Length; i++)
                {
                    // Skip already visited
                    bool alreadyVisited = false;
                    for (int v = 0; v < visited.Length; v++)
                    {
                        if (visited[v] == i)
                        {
                            alreadyVisited = true;
                            break;
                        }
                    }
                    if (alreadyVisited) continue;

                    float dSq = math.distancesq(asteroidPositions[i], asteroidPositions[lastIdx]);
                    if (dSq <= maxChainDistSq && dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestIdx = i;
                    }
                }

                if (bestIdx < 0) break; // No more valid chain targets
                visited.Add(bestIdx);
                lastIdx = bestIdx;
            }

            // Apply damage to all visited targets
            // Collect chain positions for SkillEvent
            float2 chain1 = float2.zero, chain2 = float2.zero, chain3 = float2.zero, chain4 = float2.zero;

            for (int v = 0; v < visited.Length; v++)
            {
                int idx = visited[v];
                Entity entity = asteroidEntities[idx];

                // Apply damage with crit roll
                bool isCrit = rng.NextFloat() < critConfig.CritChance;
                float damage = isCrit
                    ? GameConstants.ChainLightningDamage * critConfig.CritMultiplier
                    : GameConstants.ChainLightningDamage;

                var health = SystemAPI.GetComponentRW<HealthData>(entity);
                health.ValueRW.CurrentHP -= damage;

                // Get position for DamageEvent
                var transform = SystemAPI.GetComponentRO<LocalTransform>(entity);

                // Emit DamageEvent: blue color for Chain Lightning
                damageBuffer.Add(new DamageEvent
                {
                    Position = transform.ValueRO.Position,
                    Amount = damage,
                    Type = isCrit ? DamageType.Critical : DamageType.Skill,
                    ColorR = 128, ColorG = 179, ColorB = 255
                });

                // Record chain position
                switch (v)
                {
                    case 0: chain1 = asteroidPositions[idx]; break;
                    case 1: chain2 = asteroidPositions[idx]; break;
                    case 2: chain3 = asteroidPositions[idx]; break;
                    case 3: chain4 = asteroidPositions[idx]; break;
                }
            }

            // Emit SkillEvent for VFX bridge
            skillEventBuffer.Add(new SkillEvent
            {
                SkillType = 1,
                OriginPos = shipPos,
                TargetPos = visited.Length > 0 ? asteroidPositions[visited[0]] : mousePos,
                Chain1 = chain1,
                Chain2 = chain2,
                Chain3 = chain3,
                Chain4 = chain4,
                ChainCount = visited.Length,
                Radius = 0f
            });

            visited.Dispose();
            asteroidEntities.Dispose();
            asteroidPositions.Dispose();
        }
    }
}
