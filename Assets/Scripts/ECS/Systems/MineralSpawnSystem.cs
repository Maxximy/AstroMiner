using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawns mineral entities when asteroids reach 0 HP.
/// Runs BEFORE AsteroidDestructionSystem to read asteroid positions before they are destroyed.
/// Uses MineralsSpawnedTag to prevent double-spawn on the same dead asteroid.
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(AsteroidDestructionSystem))]
public partial struct MineralSpawnSystem : ISystem
{
    private Unity.Mathematics.Random _rng;

    public void OnCreate(ref SystemState state)
    {
        _rng = new Unity.Mathematics.Random((uint)System.Environment.TickCount | 1u);
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

        // Get DestructionEvent buffer for visual/audio feedback (Phase 4)
        var destructionBuffer = SystemAPI.GetSingletonBuffer<DestructionEvent>();

        foreach (var (health, transform, entity) in
            SystemAPI.Query<RefRO<HealthData>, RefRO<LocalTransform>>()
                .WithAll<AsteroidTag>()
                .WithNone<MineralsSpawnedTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.CurrentHP <= 0f)
            {
                // Mark asteroid so we don't spawn minerals again next frame
                ecb.AddComponent<MineralsSpawnedTag>(entity);

                float3 asteroidPos = transform.ValueRO.Position;

                // Emit destruction event for explosion VFX and audio
                destructionBuffer.Add(new DestructionEvent
                {
                    Position = asteroidPos,
                    Scale = 1.0f,  // default scale; future: read from entity component
                    ResourceTier = 0
                });

                int mineralCount = _rng.NextInt(GameConstants.MinMineralsPerAsteroid,
                    GameConstants.MaxMineralsPerAsteroid + 1);

                for (int i = 0; i < mineralCount; i++)
                {
                    // Random XZ offset around asteroid position
                    float2 offset = _rng.NextFloat2(new float2(-0.5f), new float2(0.5f));
                    float3 mineralPos = new float3(
                        asteroidPos.x + offset.x,
                        0f,
                        asteroidPos.z + offset.y
                    );

                    var mineralEntity = ecb.CreateEntity();
                    ecb.AddComponent(mineralEntity, new MineralTag());
                    ecb.AddComponent(mineralEntity, new MineralData
                    {
                        ResourceTier = 0,
                        CreditValue = GameConstants.DefaultCreditValuePerMineral
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
}
