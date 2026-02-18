using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Drifts placeholder entities downward based on their DriftData.Speed.
    /// </summary>
    [BurstCompile]
    public partial struct PlaceholderDriftJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(ref LocalTransform transform, in DriftData drift)
        {
            transform.Position.y -= drift.Speed * DeltaTime;
        }
    }

    /// <summary>
    /// Spins placeholder asteroid entities based on their SpinData.RadiansPerSecond.
    /// </summary>
    [BurstCompile]
    public partial struct PlaceholderSpinJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(ref LocalTransform transform, in SpinData spin)
        {
            transform = transform.RotateY(spin.RadiansPerSecond * DeltaTime);
        }
    }

    /// <summary>
    /// ECS system that drives drift and spin for all placeholder entities.
    /// DISABLED in Phase 2: Replaced by AsteroidMovementSystem which filters by AsteroidTag.
    /// PlaceholderDriftJob/PlaceholderSpinJob have no tag filter and would double-process asteroid entities.
    /// </summary>
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct PlaceholderMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only move entities during Playing state
            if (!SystemAPI.HasSingleton<GameStateData>())
                return;

            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            var dt = SystemAPI.Time.DeltaTime;

            // Schedule drift and spin jobs in parallel
            new PlaceholderDriftJob { DeltaTime = dt }.ScheduleParallel();
            new PlaceholderSpinJob { DeltaTime = dt }.ScheduleParallel();
        }
    }
}