using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Collects mineral entities near the ship and awards credits.
    /// Runs after MineralPullSystem so collection happens after movement.
    /// Applies economy bonuses: ResourceMultiplier scales credit value,
    /// LuckyStrike rolls a chance for double credits per mineral.
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(MineralPullSystem))]
    public partial struct MineralCollectionSystem : ISystem
    {
        private Random rng;

        public void OnCreate(ref SystemState state)
        {
            // Unique seed for collection system RNG (OR-mask to differentiate from other systems)
            rng = new Random(((uint)System.Environment.TickCount | 1u) ^ 0xBEEF0003u);

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

            var gameStateRW = SystemAPI.GetSingletonRW<GameStateData>();
            float3 shipPos = new float3(GameConstants.ShipPositionX, 0f, GameConstants.ShipPositionZ);

            // Read economy bonuses
            var bonus = SystemAPI.GetSingleton<PlayerBonusData>();

            // Get CollectionEvent buffer for visual/audio feedback (Phase 4)
            var collectionBuffer = SystemAPI.GetSingletonBuffer<CollectionEvent>();

            foreach (var (mineralData, transform, entity) in
                     SystemAPI.Query<RefRO<MineralData>, RefRO<LocalTransform>>()
                         .WithAll<MineralTag>()
                         .WithEntityAccess())
            {
                float dist = math.distance(transform.ValueRO.Position, shipPos);
                if (dist <= GameConstants.MineralCollectionRadius)
                {
                    // Base credit value from mineral tier
                    int baseCredits = mineralData.ValueRO.CreditValue;

                    // Apply ResourceMultiplier bonus
                    int finalCredits = (int)(baseCredits * bonus.ResourceMultiplier);

                    // Apply Lucky Strike: chance for double credits
                    if (bonus.LuckyStrikeChance > 0f && rng.NextFloat(0f, 1f) < bonus.LuckyStrikeChance)
                    {
                        finalCredits *= 2;
                    }

                    gameStateRW.ValueRW.Credits += finalCredits;

                    // Emit collection event with final credit value for VFX and audio
                    collectionBuffer.Add(new CollectionEvent
                    {
                        ResourceTier = mineralData.ValueRO.ResourceTier,
                        CreditValue = finalCredits,
                        Position = transform.ValueRO.Position
                    });

                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
