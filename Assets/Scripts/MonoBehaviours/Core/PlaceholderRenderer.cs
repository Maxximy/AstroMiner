using System.Collections;
using System.Collections.Generic;
using ECS.Components;
using MonoBehaviours.Pool;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Core
{
    /// <summary>
    /// Syncs ECS placeholder entity transforms to pooled GameObjects each frame.
    /// Creates asteroid spheres, mineral cubes, and a ship placeholder.
    /// Uses GameObjectPool for zero-allocation rendering.
    /// </summary>
    public class PlaceholderRenderer : MonoBehaviour
    {
        [Header("Asteroid Visuals")]
        [SerializeField] private float AsteroidScaleMin = 1.0f;
        [SerializeField] private float AsteroidScaleMax = 1.5f;

        [Header("Mineral Visuals")]
        [SerializeField] private float MineralScaleMin = 0.15f;
        [SerializeField] private float MineralScaleMax = 0.25f;

        // Asteroid colors: dark gray, brown, rust (warm muted palette)
        private static readonly Color[] AsteroidColors = new Color[]
        {
            new Color(0.333f, 0.333f, 0.333f), // dark gray #555555
            new Color(0.545f, 0.412f, 0.078f), // brown #8B6914
            new Color(0.718f, 0.255f, 0.055f), // rust #B7410E
        };

        // Mineral colors: cyan, green, gold, purple
        private static readonly Color[] MineralColors = new Color[]
        {
            new Color(0.0f, 0.9f, 0.9f),   // cyan
            new Color(0.2f, 0.9f, 0.3f),   // green
            new Color(1.0f, 0.84f, 0.0f),  // gold
            new Color(0.7f, 0.3f, 0.9f),   // purple
        };

        private EntityManager em;
        private EntityQuery placeholderQuery;
        private bool initialized;

        // Entity -> GameObject mapping
        private Dictionary<Entity, GameObject> entityToGo;
        private GameObjectPool asteroidPool;
        private GameObjectPool mineralPool;

        // Prefab prototypes (created at runtime from primitives)
        private GameObject asteroidPrefab;
        private GameObject mineralPrefab;

        // Ship placeholder (static, not ECS-driven)
        private GameObject shipGo;

        void Start()
        {
            // Replaced by AsteroidRenderer in Phase 2
            Debug.Log("PlaceholderRenderer disabled -- replaced by AsteroidRenderer");
            enabled = false;
            return;
        }

        private IEnumerator InitializeNextFrame()
        {
            // Wait for PlaceholderSpawner to finish (it also waits one frame)
            yield return null;
            yield return null;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("PlaceholderRenderer: ECS world not ready");
                yield break;
            }

            em = world.EntityManager;
            placeholderQuery = em.CreateEntityQuery(
                typeof(PlaceholderTag),
                typeof(LocalTransform)
            );

            entityToGo = new Dictionary<Entity, GameObject>();

            // Create prefab prototypes (hidden, used only for pooling)
            CreatePrefabs();

            // Create pools
            var spawner = FindAnyObjectByType<PlaceholderSpawner>();
            int asteroidCount = spawner != null ? spawner.SpawnedAsteroidCount : 100;
            int mineralCount = spawner != null ? spawner.SpawnedMineralCount : 1000;
            int totalCount = asteroidCount + mineralCount;

            var asteroidParent = new GameObject("AsteroidPool").transform;
            asteroidParent.SetParent(transform);
            asteroidPool = new GameObjectPool(asteroidPrefab, asteroidParent, asteroidCount, asteroidCount + 50);

            var mineralParent = new GameObject("MineralPool").transform;
            mineralParent.SetParent(transform);
            mineralPool = new GameObjectPool(mineralPrefab, mineralParent, mineralCount, mineralCount + 50);

            // Assign pooled GameObjects to ECS entities
            AssignGameObjects(asteroidCount);

            // Create ship placeholder
            CreateShipPlaceholder();

            initialized = true;
            Debug.Log($"PlaceholderRenderer: initialized {entityToGo.Count} GameObjects from pools. Pool active: asteroids={asteroidPool.CountActive}, minerals={mineralPool.CountActive}");
        }

        private void CreatePrefabs()
        {
            // Asteroid prefab: sphere primitive
            asteroidPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroidPrefab.name = "AsteroidPrefab";
            asteroidPrefab.SetActive(false);
            asteroidPrefab.transform.SetParent(transform);
            // Remove collider (not needed for visuals)
            var asteroidCollider = asteroidPrefab.GetComponent<Collider>();
            if (asteroidCollider != null) Destroy(asteroidCollider);

            // Mineral prefab: cube primitive
            mineralPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mineralPrefab.name = "MineralPrefab";
            mineralPrefab.SetActive(false);
            mineralPrefab.transform.SetParent(transform);
            // Remove collider
            var mineralCollider = mineralPrefab.GetComponent<Collider>();
            if (mineralCollider != null) Destroy(mineralCollider);
        }

        private void AssignGameObjects(int asteroidCount)
        {
            var entities = placeholderQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var rng = new Unity.Mathematics.Random((uint)(System.DateTime.Now.Ticks & 0xFFFFFFFF));

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                bool isAsteroid = em.HasComponent<SpinData>(entity);

                GameObject go;
                if (isAsteroid)
                {
                    go = asteroidPool.Get();
                    float scale = rng.NextFloat(AsteroidScaleMin, AsteroidScaleMax);
                    go.transform.localScale = Vector3.one * scale;

                    // Random asteroid color
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Use MaterialPropertyBlock to avoid material instancing overhead
                        var block = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block);
                        block.SetColor("_BaseColor", AsteroidColors[rng.NextInt(AsteroidColors.Length)]);
                        renderer.SetPropertyBlock(block);
                    }
                }
                else
                {
                    go = mineralPool.Get();
                    float scale = rng.NextFloat(MineralScaleMin, MineralScaleMax);
                    go.transform.localScale = Vector3.one * scale;

                    // Random mineral color
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var block = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block);
                        block.SetColor("_BaseColor", MineralColors[rng.NextInt(MineralColors.Length)]);
                        renderer.SetPropertyBlock(block);
                    }
                }

                entityToGo[entity] = go;
            }

            entities.Dispose();
        }

        private void CreateShipPlaceholder()
        {
            // Ship: flattened cube as a diamond shape, light gray, at bottom of play area
            shipGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shipGo.name = "ShipPlaceholder";
            shipGo.transform.SetParent(transform);
            shipGo.transform.position = new Vector3(0f, -3f, 0f);
            shipGo.transform.localScale = new Vector3(1.0f, 0.1f, 0.5f);
            shipGo.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            // Remove collider
            var col = shipGo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Light gray color
            var renderer = shipGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.8f));
                renderer.SetPropertyBlock(block);
            }
        }

        void LateUpdate()
        {
            if (!initialized) return;

            // Sync ECS entity transforms to GameObjects
            var entities = placeholderQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (entityToGo.TryGetValue(entity, out var go) && go != null)
                {
                    var lt = em.GetComponentData<LocalTransform>(entity);
                    go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
                    go.transform.rotation = lt.Rotation;
                }
            }

            entities.Dispose();
        }

        void OnDestroy()
        {
            // Clean up prefabs
            if (asteroidPrefab != null) Destroy(asteroidPrefab);
            if (mineralPrefab != null) Destroy(mineralPrefab);
        }
    }
}
