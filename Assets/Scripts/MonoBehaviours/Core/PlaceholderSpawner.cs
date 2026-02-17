using System.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawns placeholder ECS entities (asteroids and minerals) for Phase 1 benchmarking.
/// Creates entities one frame after Start to ensure ECSBootstrap has run.
/// </summary>
public class PlaceholderSpawner : MonoBehaviour
{
    [Header("Spawn Counts")]
    [SerializeField] private int _asteroidCount = 100;
    [SerializeField] private int _mineralCount = 1000;

    [Header("Play Area Bounds")]
    [SerializeField] private float _xMin = -12f;
    [SerializeField] private float _xMax = 12f;
    [SerializeField] private float _yMin = -2f;
    [SerializeField] private float _yMax = 15f;

    [Header("Asteroid Settings")]
    [SerializeField] private float _asteroidDriftMin = 0.5f;
    [SerializeField] private float _asteroidDriftMax = 2.0f;
    [SerializeField] private float _asteroidSpinMin = 0.5f;
    [SerializeField] private float _asteroidSpinMax = 3.0f;

    [Header("Mineral Settings")]
    [SerializeField] private float _mineralDriftMin = 0.3f;
    [SerializeField] private float _mineralDriftMax = 1.0f;

    /// <summary>
    /// Total number of asteroid entities spawned.
    /// </summary>
    public int SpawnedAsteroidCount { get; private set; }

    /// <summary>
    /// Total number of mineral entities spawned.
    /// </summary>
    public int SpawnedMineralCount { get; private set; }

    void Start()
    {
        // Replaced by AsteroidSpawnSystem in Phase 2
        Debug.Log("PlaceholderSpawner disabled -- replaced by AsteroidSpawnSystem");
        enabled = false;
        return;
    }

    private IEnumerator SpawnEntitiesNextFrame()
    {
        yield return null;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("PlaceholderSpawner: ECS world not ready");
            yield break;
        }

        var em = world.EntityManager;

        // Asteroid archetype: position + drift + spin + tag
        var asteroidArchetype = em.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalToWorld),
            typeof(DriftData),
            typeof(SpinData),
            typeof(PlaceholderTag)
        );

        // Mineral archetype: position + drift + tag (no spin)
        var mineralArchetype = em.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalToWorld),
            typeof(DriftData),
            typeof(PlaceholderTag)
        );

        var rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        // Spawn asteroids
        for (int i = 0; i < _asteroidCount; i++)
        {
            var entity = em.CreateEntity(asteroidArchetype);

            float x = rng.NextFloat(_xMin, _xMax);
            float y = rng.NextFloat(_yMin, _yMax);

            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, y, 0f)));
            em.SetComponentData(entity, new DriftData
            {
                Speed = rng.NextFloat(_asteroidDriftMin, _asteroidDriftMax)
            });
            em.SetComponentData(entity, new SpinData
            {
                RadiansPerSecond = rng.NextFloat(_asteroidSpinMin, _asteroidSpinMax)
            });
        }
        SpawnedAsteroidCount = _asteroidCount;

        // Spawn minerals
        for (int i = 0; i < _mineralCount; i++)
        {
            var entity = em.CreateEntity(mineralArchetype);

            float x = rng.NextFloat(_xMin, _xMax);
            float y = rng.NextFloat(_yMin, _yMax);

            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, y, 0f)));
            em.SetComponentData(entity, new DriftData
            {
                Speed = rng.NextFloat(_mineralDriftMin, _mineralDriftMax)
            });
        }
        SpawnedMineralCount = _mineralCount;

        int totalEntities = em.CreateEntityQuery(typeof(LocalTransform)).CalculateEntityCount();
        Debug.Log($"Spawned {SpawnedAsteroidCount} asteroids + {SpawnedMineralCount} minerals. Total entities: {totalEntities}");
    }
}
