using System.Collections;
using System.Collections.Generic;
using Data;
using ECS.Components;
using MonoBehaviours.Pool;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Core
{
    /// <summary>
    /// Syncs ECS asteroid entities to pooled GameObjects each frame.
    /// Selects per-tier, per-size prefabs from AsteroidVisualConfig (scene object).
    /// Falls back to primitive spheres when custom prefabs are not assigned.
    /// Also creates the ship placeholder visual.
    /// </summary>
    public class AsteroidRenderer : MonoBehaviour
    {
        [Header("Fallback Visuals (used when prefab not found)")]
        [SerializeField] private float AsteroidScaleMin = 0.8f;
        [SerializeField] private float AsteroidScaleMax = 1.5f;

        // Fallback colors for sphere primitives
        private static readonly Color[] FallbackColors = new Color[]
        {
            new Color(0.333f, 0.333f, 0.333f), // dark gray
            new Color(0.545f, 0.412f, 0.078f), // brown
            new Color(0.718f, 0.255f, 0.055f), // rust
        };

        private EntityManager em;
        private EntityQuery asteroidQuery;
        private bool initialized;

        // Entity -> GameObject tracking
        private Dictionary<Entity, GameObject> entityToGo;
        private List<Entity> entitiesToRemove;

        // Entity -> variant key (to return to correct pool on destroy)
        private Dictionary<Entity, (int tier, AsteroidSize size)> entityToVariant;

        // Per-variant pools, lazily created on first use
        private Dictionary<(int tier, AsteroidSize size), GameObjectPool> variantPools;

        // Track which variants have no prefab assigned (avoid repeated warning spam)
        private HashSet<(int tier, AsteroidSize size)> missingVariants;

        // Pool hierarchy parent
        private Transform poolsParent;

        // Fallback sphere pool for missing prefabs
        private GameObjectPool fallbackPool;
        private GameObject fallbackPrefab;

        // Ship placeholder (static, not ECS-driven)
        private GameObject shipGo;

        // Per-entity visual rotation (random axis + speed in degrees/sec)
        private Dictionary<Entity, (Vector3 axis, float speed)> entityRotation;

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

            // Query includes AsteroidResourceTier for tier-based prefab selection
            asteroidQuery = em.CreateEntityQuery(
                typeof(AsteroidTag),
                typeof(LocalTransform),
                typeof(AsteroidResourceTier)
            );

            entityToGo = new Dictionary<Entity, GameObject>();
            entitiesToRemove = new List<Entity>();
            entityToVariant = new Dictionary<Entity, (int, AsteroidSize)>();
            entityRotation = new Dictionary<Entity, (Vector3, float)>();
            variantPools = new Dictionary<(int, AsteroidSize), GameObjectPool>();
            missingVariants = new HashSet<(int, AsteroidSize)>();

            poolsParent = new GameObject("AsteroidPools").transform;
            poolsParent.SetParent(transform);

            // Fallback sphere prefab (same as original, for graceful degradation)
            fallbackPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackPrefab.name = "AsteroidFallbackPrefab";
            fallbackPrefab.SetActive(false);
            fallbackPrefab.transform.SetParent(transform);
            var collider = fallbackPrefab.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            int fallbackPreWarm = GameConstants.DefaultMaxAsteroids + 20;
            int fallbackMax = GameConstants.DefaultMaxAsteroids + 50;
            var fallbackPoolParent = new GameObject("FallbackPool").transform;
            fallbackPoolParent.SetParent(poolsParent);
            fallbackPool = new GameObjectPool(fallbackPrefab, fallbackPoolParent, fallbackPreWarm, fallbackMax);

            rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

            CreateShipPlaceholder();

            initialized = true;
            Debug.Log("AsteroidRenderer: initialized with per-variant lazy pooling (fallback sphere ready).");
        }

        /// <summary>
        /// Gets or creates a pool for the given tier+size variant.
        /// Returns null if no prefab is assigned in AsteroidVisualConfig (caller uses fallback).
        /// </summary>
        private GameObjectPool GetOrCreateVariantPool(int tier, AsteroidSize size)
        {
            var key = (tier, size);

            if (variantPools.TryGetValue(key, out var pool))
                return pool;

            if (missingVariants.Contains(key))
                return null;

            var config = AsteroidVisualConfig.Instance;
            var prefab = config != null ? config.GetAsteroidPrefab(tier, size) : null;

            if (prefab == null)
            {
                missingVariants.Add(key);
                Debug.LogWarning($"AsteroidRenderer: No prefab assigned for tier {tier} size {size}. Using fallback sphere.");
                return null;
            }

            var parent = new GameObject($"Pool_tier{tier}_{size}").transform;
            parent.SetParent(poolsParent);

            pool = new GameObjectPool(
                prefab, parent,
                GameConstants.AsteroidPoolPreWarmPerVariant,
                GameConstants.AsteroidPoolMaxPerVariant
            );

            variantPools[key] = pool;
            return pool;
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

        private void ConfigureFallbackVisual(GameObject go, float maxHP)
        {
            // Original sphere behavior: random scale + random color
            float baseScale = rng.NextFloat(AsteroidScaleMin, AsteroidScaleMax);
            float hpRatio = maxHP / GameConstants.DefaultAsteroidHP;
            float hpScaleBonus = (hpRatio - 1f) * 0.3f;
            float scale = baseScale * (1f + hpScaleBonus);
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", FallbackColors[rng.NextInt(FallbackColors.Length)]);

                Color emissive = FallbackColors[rng.NextInt(FallbackColors.Length)] * 0.3f;
                block.SetColor("_EmissionColor", emissive);
                renderer.material.EnableKeyword("_EMISSION");

                renderer.SetPropertyBlock(block);
            }
        }

        private void ConfigurePrefabVisual(GameObject go)
        {
            // Custom mesh prefab: scale jitter + random Y rotation for variety
            float jitter = rng.NextFloat(
                GameConstants.AsteroidMeshScaleJitterMin,
                GameConstants.AsteroidMeshScaleJitterMax
            );
            go.transform.localScale *= jitter;

            float randomYaw = rng.NextFloat(0f, 360f);
            go.transform.rotation = Quaternion.Euler(0f, randomYaw, 0f);
        }

        void LateUpdate()
        {
            if (!initialized) return;

            var entities = asteroidQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var activeEntities = new HashSet<Entity>(entities.Length);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                activeEntities.Add(entity);

                if (!entityToGo.TryGetValue(entity, out var go))
                {
                    // Read tier and HP for variant selection
                    int tier = em.GetComponentData<AsteroidResourceTier>(entity).Tier;

                    float maxHP = GameConstants.DefaultAsteroidHP;
                    if (em.HasComponent<HealthData>(entity))
                        maxHP = em.GetComponentData<HealthData>(entity).MaxHP;

                    AsteroidSize size = AsteroidVisualDefinitions.ClassifySize(maxHP);

                    // Try custom prefab pool, fall back to sphere pool
                    var pool = GetOrCreateVariantPool(tier, size);
                    if (pool != null)
                    {
                        go = pool.Get();
                        ConfigurePrefabVisual(go);
                        entityToVariant[entity] = (tier, size);
                    }
                    else
                    {
                        go = fallbackPool.Get();
                        ConfigureFallbackVisual(go, maxHP);
                        // Sentinel: tier -1 means fallback pool
                        entityToVariant[entity] = (-1, AsteroidSize.Small);
                    }

                    entityToGo[entity] = go;

                    // Assign random rotation axis and speed
                    var axis = rng.NextFloat3Direction();
                    float speed = rng.NextFloat(
                        GameConstants.AsteroidRotationSpeedMin,
                        GameConstants.AsteroidRotationSpeedMax
                    );
                    entityRotation[entity] = (new Vector3(axis.x, axis.y, axis.z), speed);
                }

                // Sync position from ECS
                var lt = em.GetComponentData<LocalTransform>(entity);
                go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);

                // Apply continuous visual rotation
                if (entityRotation.TryGetValue(entity, out var rot))
                    go.transform.Rotate(rot.axis, rot.speed * Time.deltaTime, Space.World);
            }

            // Cleanup pass -- release GameObjects to their correct pools
            entitiesToRemove.Clear();
            foreach (var kvp in entityToGo)
            {
                if (!activeEntities.Contains(kvp.Key))
                {
                    if (entityToVariant.TryGetValue(kvp.Key, out var variant))
                    {
                        if (variant.tier >= 0 && variantPools.TryGetValue(variant, out var pool))
                        {
                            // Reset scale before returning (undo jitter)
                            kvp.Value.transform.localScale = Vector3.one;
                            pool.Release(kvp.Value);
                        }
                        else
                        {
                            fallbackPool.Release(kvp.Value);
                        }
                        entityToVariant.Remove(kvp.Key);
                    }
                    else
                    {
                        fallbackPool.Release(kvp.Value);
                    }
                    entityRotation.Remove(kvp.Key);
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
            if (fallbackPrefab != null) Destroy(fallbackPrefab);
        }
    }
}
