using System.Collections;
using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Core
{
    /// <summary>
    /// Spawns placeholder ECS entities (asteroids and minerals) for Phase 1 benchmarking.
    /// Creates entities one frame after Start to ensure ECSBootstrap has run.
    /// </summary>
    public class PlaceholderSpawner : MonoBehaviour
    {
        [Header("Spawn Counts")]
        [SerializeField] private int AsteroidCount = 100;
        [SerializeField] private int MineralCount = 1000;

        [Header("Play Area Bounds")]
        [SerializeField] private float XMin = -12f;
        [SerializeField] private float XMax = 12f;
        [SerializeField] private float YMin = -2f;
        [SerializeField] private float YMax = 15f;

        [Header("Asteroid Settings")]
        [SerializeField] private float AsteroidDriftMin = 0.5f;
        [SerializeField] private float AsteroidDriftMax = 2.0f;
        [SerializeField] private float AsteroidSpinMin = 0.5f;
        [SerializeField] private float AsteroidSpinMax = 3.0f;

        [Header("Mineral Settings")]
        [SerializeField] private float MineralDriftMin = 0.3f;
        [SerializeField] private float MineralDriftMax = 1.0f;

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
            for (int i = 0; i < AsteroidCount; i++)
            {
                var entity = em.CreateEntity(asteroidArchetype);

                float x = rng.NextFloat(XMin, XMax);
                float y = rng.NextFloat(YMin, YMax);

                em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, y, 0f)));
                em.SetComponentData(entity, new DriftData
                {
                    Speed = rng.NextFloat(AsteroidDriftMin, AsteroidDriftMax)
                });
                em.SetComponentData(entity, new SpinData
                {
                    RadiansPerSecond = rng.NextFloat(AsteroidSpinMin, AsteroidSpinMax)
                });
            }
            SpawnedAsteroidCount = AsteroidCount;

            // Spawn minerals
            for (int i = 0; i < MineralCount; i++)
            {
                var entity = em.CreateEntity(mineralArchetype);

                float x = rng.NextFloat(XMin, XMax);
                float y = rng.NextFloat(YMin, YMax);

                em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, y, 0f)));
                em.SetComponentData(entity, new DriftData
                {
                    Speed = rng.NextFloat(MineralDriftMin, MineralDriftMax)
                });
            }
            SpawnedMineralCount = MineralCount;

            int totalEntities = em.CreateEntityQuery(typeof(LocalTransform)).CalculateEntityCount();
            Debug.Log($"Spawned {SpawnedAsteroidCount} asteroids + {SpawnedMineralCount} minerals. Total entities: {totalEntities}");
        }
    }
}
