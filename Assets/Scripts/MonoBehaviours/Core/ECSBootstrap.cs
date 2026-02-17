using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ECSBootstrap : MonoBehaviour
{
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;

        // Create GameState singleton
        var gameStateEntity = em.CreateEntity(typeof(GameStateData));
        em.SetComponentData(gameStateEntity, new GameStateData
        {
            Phase = GamePhase.Playing,
            Timer = GameConstants.DefaultRunDuration,
            Credits = 0
        });

        // Create Input singleton
        var inputEntity = em.CreateEntity(typeof(InputData));
        em.SetComponentData(inputEntity, new InputData
        {
            MouseWorldPos = float2.zero,
            MouseValid = false
        });

        // Create AsteroidSpawnTimer singleton
        var spawnTimerEntity = em.CreateEntity(typeof(AsteroidSpawnTimer));
        em.SetComponentData(spawnTimerEntity, new AsteroidSpawnTimer
        {
            SpawnInterval = GameConstants.DefaultSpawnInterval,
            TimeUntilNextSpawn = 0f,
            MaxActiveAsteroids = GameConstants.DefaultMaxAsteroids
        });

        // Create MiningConfigData singleton
        var miningConfigEntity = em.CreateEntity(typeof(MiningConfigData));
        em.SetComponentData(miningConfigEntity, new MiningConfigData
        {
            Radius = GameConstants.DefaultMiningRadius,
            DamagePerTick = GameConstants.DefaultDamagePerTick,
            TickInterval = GameConstants.DefaultTickInterval
        });

        // Create CollectionEvent buffer entity (Phase 4 will drain for SFX/VFX)
        var collectionEventEntity = em.CreateEntity();
        em.AddBuffer<CollectionEvent>(collectionEventEntity);

        Debug.Log("ECS Bootstrap complete: singletons created (GameState, Input, AsteroidSpawnTimer, MiningConfig, CollectionEventBuffer)");
    }
}
