using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Collects mineral entities near the ship and awards credits.
/// Runs after MineralPullSystem so collection happens after movement.
/// Directly increments GameStateData.Credits (no DynamicBuffer events until Phase 4).
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(MineralPullSystem))]
public partial struct MineralCollectionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameStateData>();
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

        foreach (var (mineralData, transform, entity) in
            SystemAPI.Query<RefRO<MineralData>, RefRO<LocalTransform>>()
                .WithAll<MineralTag>()
                .WithEntityAccess())
        {
            float dist = math.distance(transform.ValueRO.Position, shipPos);
            if (dist <= GameConstants.MineralCollectionRadius)
            {
                gameStateRW.ValueRW.Credits += mineralData.ValueRO.CreditValue;
                ecb.DestroyEntity(entity);
            }
        }
    }
}
