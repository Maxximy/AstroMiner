using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    /// <summary>
    /// Destroys asteroid entities that have reached 0 HP or below.
    /// Uses ECB for structural changes (entity destruction).
    /// Runs regardless of game phase -- dead entities should always be cleaned up.
    /// </summary>
    [BurstCompile]
    public partial struct AsteroidDestructionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get ECB for structural changes
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Iterate all asteroid entities and destroy those with 0 or negative HP
            foreach (var (health, entity) in
                     SystemAPI.Query<RefRO<HealthData>>()
                         .WithAll<AsteroidTag>()
                         .WithEntityAccess())
            {
                if (health.ValueRO.CurrentHP <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
