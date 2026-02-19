using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Spawns mineral entities when asteroids reach 0 HP.
    /// Runs BEFORE AsteroidDestructionSystem to read asteroid positions before they are destroyed.
    /// Uses MineralsSpawnedTag to prevent double-spawn on the same dead asteroid.
    /// Reads AsteroidResourceTier to assign per-tier credit values and mineral counts.
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(AsteroidDestructionSystem))]
    public partial struct MineralSpawnSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Random((uint)System.Environment.TickCount | 1u);
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<PlayerBonusData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing && gameState.Phase != GamePhase.Collecting)
                return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Get DestructionEvent buffer for visual/audio feedback (Phase 4)
            var destructionBuffer = SystemAPI.GetSingletonBuffer<DestructionEvent>();

            // Flat mineral drop count from PlayerBonusData (upgraded via tech tree)
            int mineralDropCount = SystemAPI.GetSingleton<PlayerBonusData>().MineralDropCount;

            foreach (var (health, transform, asteroidTier, entity) in
                     SystemAPI.Query<RefRO<HealthData>, RefRO<LocalTransform>, RefRO<AsteroidResourceTier>>()
                         .WithAll<AsteroidTag>()
                         .WithNone<MineralsSpawnedTag>()
                         .WithEntityAccess())
            {
                if (health.ValueRO.CurrentHP <= 0f)
                {
                    // Mark asteroid so we don't spawn minerals again next frame
                    ecb.AddComponent<MineralsSpawnedTag>(entity);

                    float3 asteroidPos = transform.ValueRO.Position;
                    int tier = asteroidTier.ValueRO.Tier;

                    // Emit destruction event for explosion VFX and audio
                    // Scale based on HP for visually larger explosions on tougher asteroids
                    float scale = 1f + tier * 0.15f;
                    destructionBuffer.Add(new DestructionEvent
                    {
                        Position = asteroidPos,
                        Scale = scale,
                        ResourceTier = tier
                    });

                    // Get credit value per mineral based on tier
                    int creditValue = GetCreditValueForTier(tier);
                    int mineralCount = mineralDropCount;

                    for (int i = 0; i < mineralCount; i++)
                    {
                        // Random XZ offset around asteroid position
                        float2 offset = rng.NextFloat2(new float2(-0.5f), new float2(0.5f));
                        float3 mineralPos = new float3(
                            asteroidPos.x + offset.x,
                            0f,
                            asteroidPos.z + offset.y
                        );

                        var mineralEntity = ecb.CreateEntity();
                        ecb.AddComponent(mineralEntity, new MineralTag());
                        ecb.AddComponent(mineralEntity, new MineralData
                        {
                            ResourceTier = tier,
                            CreditValue = creditValue
                        });
                        ecb.AddComponent(mineralEntity, new MineralPullData
                        {
                            CurrentSpeed = GameConstants.MineralInitialSpeed,
                            Acceleration = GameConstants.MineralAcceleration
                        });
                        ecb.AddComponent(mineralEntity, LocalTransform.FromPosition(mineralPos));
                        ecb.AddComponent(mineralEntity, new LocalToWorld());
                    }
                }
            }
        }

        /// <summary>
        /// Burst-compatible credit value lookup by tier index.
        /// </summary>
        [BurstCompile]
        private static int GetCreditValueForTier(int tier)
        {
            if (tier == 0) return GameConstants.IronCreditValue;
            if (tier == 1) return GameConstants.CopperCreditValue;
            if (tier == 2) return GameConstants.SilverCreditValue;
            if (tier == 3) return GameConstants.CobaltCreditValue;
            if (tier == 4) return GameConstants.GoldCreditValue;
            if (tier == 5) return GameConstants.TitaniumCreditValue;
            return GameConstants.IronCreditValue; // fallback
        }

    }
}
