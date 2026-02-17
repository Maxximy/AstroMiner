using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Destroys asteroid entities that have drifted below the bottom of the play area.
/// Uses ECB for structural changes (entity destruction).
/// Only runs during the Playing game phase.
/// </summary>
[BurstCompile]
public partial struct AsteroidBoundsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameStateData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only check bounds during Playing state
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        if (gameState.Phase != GamePhase.Playing)
            return;

        // Get ECB for structural changes
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Iterate all asteroid entities and destroy those below the play area
        foreach (var (transform, entity) in
            SystemAPI.Query<RefRO<LocalTransform>>()
                .WithAll<AsteroidTag>()
                .WithEntityAccess())
        {
            if (transform.ValueRO.Position.z < GameConstants.PlayAreaZMin)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
