using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Drifts asteroid entities along -Z and spins them around Y.
/// Only processes entities with AsteroidTag to avoid affecting other entity types.
/// Only runs during the Playing game phase.
/// </summary>
[BurstCompile]
public partial struct AsteroidMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameStateData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only move entities during Playing state
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        if (gameState.Phase != GamePhase.Playing)
            return;

        var dt = SystemAPI.Time.DeltaTime;

        // Schedule drift and spin jobs for asteroid-tagged entities only
        new AsteroidDriftJob { DeltaTime = dt }.ScheduleParallel();
        new AsteroidSpinJob { DeltaTime = dt }.ScheduleParallel();
    }
}

/// <summary>
/// Drifts asteroid entities downward along -Z axis.
/// Filtered to AsteroidTag entities only.
/// </summary>
[BurstCompile]
[WithAll(typeof(AsteroidTag))]
public partial struct AsteroidDriftJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, in DriftData drift)
    {
        // Drift along -Z (top of screen = +Z, bottom = -Z)
        transform.Position.z -= drift.Speed * DeltaTime;
    }
}

/// <summary>
/// Spins asteroid entities around the Y axis.
/// Filtered to AsteroidTag entities only.
/// </summary>
[BurstCompile]
[WithAll(typeof(AsteroidTag))]
public partial struct AsteroidSpinJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, in SpinData spin)
    {
        transform = transform.RotateY(spin.RadiansPerSecond * DeltaTime);
    }
}
