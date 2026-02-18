using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Accelerates mineral entities toward the ship position.
    /// Uses IJobEntity for Burst-compiled parallel processing of 1000+ minerals.
    /// </summary>
    [BurstCompile]
    public partial struct MineralPullSystem : ISystem
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

            var job = new MineralPullJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ShipPosition = new float3(GameConstants.ShipPositionX, 0f, GameConstants.ShipPositionZ)
            };
            job.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job that moves each mineral toward the ship with increasing speed.
    /// Guard against zero-distance division for minerals already at ship position.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(MineralTag))]
    public partial struct MineralPullJob : IJobEntity
    {
        public float DeltaTime;
        public float3 ShipPosition;

        public void Execute(ref LocalTransform transform, ref MineralPullData pull)
        {
            float3 toShip = ShipPosition - transform.Position;
            float dist = math.length(toShip);

            // Guard against zero-distance division
            if (dist > 0.01f)
            {
                float3 direction = toShip / dist;
                pull.CurrentSpeed += pull.Acceleration * DeltaTime;
                transform.Position += direction * pull.CurrentSpeed * DeltaTime;
            }
        }
    }
}