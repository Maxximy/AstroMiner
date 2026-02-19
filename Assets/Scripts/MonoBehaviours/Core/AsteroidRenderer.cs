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
    /// Syncs ECS asteroid entities to pooled GameObjects each frame.
    /// Handles dynamic entity lifecycle: new entities get a pooled sphere,
    /// destroyed entities have their sphere returned to the pool.
    /// Also creates the ship placeholder visual.
    /// </summary>
    public class AsteroidRenderer : MonoBehaviour
    {
        [Header("Asteroid Visuals")]
        [SerializeField] private float AsteroidScaleMin = 0.8f;
        [SerializeField] private float AsteroidScaleMax = 1.5f;

        // Asteroid colors: dark gray, brown, rust (warm muted palette)
        private static readonly Color[] AsteroidColors = new Color[]
        {
            new Color(0.333f, 0.333f, 0.333f), // dark gray
            new Color(0.545f, 0.412f, 0.078f), // brown
            new Color(0.718f, 0.255f, 0.055f), // rust
        };

        private EntityManager em;
        private EntityQuery asteroidQuery;
        private bool initialized;

        // Entity -> GameObject tracking for dynamic lifecycle
        private Dictionary<Entity, GameObject> entityToGo;
        private List<Entity> entitiesToRemove;

        // Object pool for asteroid spheres
        private GameObjectPool asteroidPool;
        private GameObject asteroidPrefab;

        // Ship placeholder (static, not ECS-driven)
        private GameObject shipGo;

        // Deterministic RNG for visual randomness
        private Unity.Mathematics.Random rng;

        void Start()
        {
            StartCoroutine(InitializeNextFrame());
        }

        private IEnumerator InitializeNextFrame()
        {
            // Wait one frame for ECSBootstrap to create singletons
            yield return null;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("AsteroidRenderer: ECS world not ready");
                yield break;
            }

            em = world.EntityManager;

            // Query for asteroid entities with transforms
            asteroidQuery = em.CreateEntityQuery(
                typeof(AsteroidTag),
                typeof(LocalTransform)
            );

            entityToGo = new Dictionary<Entity, GameObject>();
            entitiesToRemove = new List<Entity>();

            // Create asteroid prefab (sphere without collider)
            asteroidPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroidPrefab.name = "AsteroidPrefab";
            asteroidPrefab.SetActive(false);
            asteroidPrefab.transform.SetParent(transform);
            var collider = asteroidPrefab.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Create pool with headroom for spawn/destroy overlap
            int preWarmCount = GameConstants.DefaultMaxAsteroids + 20;
            int maxSize = GameConstants.DefaultMaxAsteroids + 50;
            var poolParent = new GameObject("AsteroidPool").transform;
            poolParent.SetParent(transform);
            asteroidPool = new GameObjectPool(asteroidPrefab, poolParent, preWarmCount, maxSize);

            // Seed RNG for visual randomness (scale, color)
            rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

            // Create ship placeholder on the XZ plane
            CreateShipPlaceholder();

            initialized = true;
            Debug.Log($"AsteroidRenderer: initialized. Pool pre-warmed with {preWarmCount} objects.");
        }

        private void CreateShipPlaceholder()
        {
            shipGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shipGo.name = "ShipPlaceholder";
            shipGo.transform.SetParent(transform);

            // Ship positioned on XZ plane (Y=0), near bottom of visible area
            shipGo.transform.position = new Vector3(0f, 0f, GameConstants.ShipPositionZ);
            shipGo.transform.localScale = new Vector3(1.0f, 0.1f, 0.5f);
            shipGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Remove collider
            var col = shipGo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Light gray color via MaterialPropertyBlock
            var renderer = shipGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.8f));
                renderer.SetPropertyBlock(block);
            }
        }

        private void ConfigureAsteroidVisual(GameObject go, float maxHP)
        {
            // Base random scale plus HP-based size scaling
            // Higher HP asteroids are larger: linear scaling, 30% of HP increase maps to size
            float baseScale = rng.NextFloat(AsteroidScaleMin, AsteroidScaleMax);
            float hpRatio = maxHP / GameConstants.DefaultAsteroidHP;
            float hpScaleBonus = (hpRatio - 1f) * 0.3f;
            float scale = baseScale * (1f + hpScaleBonus);
            go.transform.localScale = Vector3.one * scale;

            // Random color from palette via MaterialPropertyBlock
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", AsteroidColors[rng.NextInt(AsteroidColors.Length)]);

                // Add subtle emissive tint for visual richness (VISL-03)
                Color emissive = AsteroidColors[rng.NextInt(AsteroidColors.Length)] * 0.3f;
                block.SetColor("_EmissionColor", emissive);
                renderer.material.EnableKeyword("_EMISSION");

                renderer.SetPropertyBlock(block);
            }
        }

        void LateUpdate()
        {
            if (!initialized) return;

            // Get all current asteroid entities
            var entities = asteroidQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var activeEntities = new HashSet<Entity>(entities.Length);

            // Sync positions -- assign new GameObjects to new entities
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                activeEntities.Add(entity);

                if (!entityToGo.TryGetValue(entity, out var go))
                {
                    // New entity discovered -- assign a pooled GameObject
                    // Read HealthData for HP-based size scaling
                    float maxHP = GameConstants.DefaultAsteroidHP;
                    if (em.HasComponent<HealthData>(entity))
                    {
                        maxHP = em.GetComponentData<HealthData>(entity).MaxHP;
                    }

                    go = asteroidPool.Get();
                    ConfigureAsteroidVisual(go, maxHP);
                    entityToGo[entity] = go;
                }

                // Sync transform from ECS to GameObject
                var lt = em.GetComponentData<LocalTransform>(entity);
                go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
                go.transform.rotation = lt.Rotation;
            }

            // Cleanup pass -- release GameObjects for destroyed entities
            entitiesToRemove.Clear();
            foreach (var kvp in entityToGo)
            {
                if (!activeEntities.Contains(kvp.Key))
                {
                    asteroidPool.Release(kvp.Value);
                    entitiesToRemove.Add(kvp.Key);
                }
            }
            foreach (var entity in entitiesToRemove)
            {
                entityToGo.Remove(entity);
            }

            entities.Dispose();
        }

        void OnDestroy()
        {
            if (asteroidPrefab != null) Destroy(asteroidPrefab);
        }
    }
}
