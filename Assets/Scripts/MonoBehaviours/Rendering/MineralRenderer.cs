using System.Collections;
using System.Collections.Generic;
using Data;
using ECS.Components;
using MonoBehaviours.Core;
using MonoBehaviours.Pool;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Syncs ECS mineral entities to pooled GameObjects each frame.
    /// Uses per-tier mesh prefabs from AsteroidVisualConfig when available,
    /// falls back to procedural spheres for tiers without assigned prefabs.
    /// Applies per-tier colors from ResourceTierDefinitions on fallback spheres.
    /// </summary>
    public class MineralRenderer : MonoBehaviour
    {
        private EntityManager em;
        private EntityQuery mineralQuery;
        private bool initialized;

        // Entity -> GameObject tracking for dynamic lifecycle
        private Dictionary<Entity, GameObject> entityToGo;
        private List<Entity> entitiesToRemove;

        // Track which tier each entity belongs to (-1 = fallback sphere pool)
        private Dictionary<Entity, int> entityToTier;

        // Per-tier mesh pools, lazily created on first use
        private Dictionary<int, GameObjectPool> tierPools;
        private HashSet<int> missingTiers;

        // Fallback sphere pool for tiers without mesh prefabs
        private GameObjectPool fallbackPool;
        private GameObject fallbackPrefab;

        // Pool hierarchy parent
        private Transform poolsParent;

        // Per-entity tumble rotation (random axis + speed in degrees/sec)
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
                Debug.LogError("MineralRenderer: ECS world not ready");
                yield break;
            }

            em = world.EntityManager;

            mineralQuery = em.CreateEntityQuery(
                typeof(MineralTag),
                typeof(LocalTransform),
                typeof(MineralData)
            );

            entityToGo = new Dictionary<Entity, GameObject>();
            entitiesToRemove = new List<Entity>();
            entityToTier = new Dictionary<Entity, int>();
            tierPools = new Dictionary<int, GameObjectPool>();
            missingTiers = new HashSet<int>();
            entityRotation = new Dictionary<Entity, (Vector3, float)>();

            poolsParent = new GameObject("MineralPools").transform;
            poolsParent.SetParent(transform);

            // Fallback sphere prefab (used when no mesh prefab is assigned for a tier)
            fallbackPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackPrefab.name = "MineralFallbackPrefab";
            fallbackPrefab.SetActive(false);
            fallbackPrefab.transform.SetParent(transform);
            var coll = fallbackPrefab.GetComponent<Collider>();
            if (coll != null) Destroy(coll);

            var fallbackPoolParent = new GameObject("MineralFallbackPool").transform;
            fallbackPoolParent.SetParent(poolsParent);
            fallbackPool = new GameObjectPool(fallbackPrefab, fallbackPoolParent, 200, 1200);

            rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

            initialized = true;
            Debug.Log("MineralRenderer: initialized with per-tier lazy pooling (fallback sphere ready).");
        }

        /// <summary>
        /// Gets or creates a pool for the given tier's mineral mesh prefab.
        /// Returns null if no prefab is assigned (caller uses fallback sphere).
        /// </summary>
        private GameObjectPool GetOrCreateTierPool(int tier)
        {
            if (tierPools.TryGetValue(tier, out var pool))
                return pool;

            if (missingTiers.Contains(tier))
                return null;

            var config = AsteroidVisualConfig.Instance;
            var prefab = config != null ? config.GetMineralPrefab(tier) : null;

            if (prefab == null)
            {
                missingTiers.Add(tier);
                Debug.LogWarning($"MineralRenderer: No mineral prefab for tier {tier}. Using fallback sphere.");
                return null;
            }

            var parent = new GameObject($"MineralPool_tier{tier}").transform;
            parent.SetParent(poolsParent);

            pool = new GameObjectPool(prefab, parent, 20, 200);
            tierPools[tier] = pool;
            return pool;
        }

        /// <summary>
        /// Configure a mesh mineral prefab visual (scale + random rotation for variety).
        /// </summary>
        private void ConfigureMeshVisual(GameObject go, int resourceTier)
        {
            go.transform.localScale = Vector3.one * GameConstants.MineralScale;

            // Random Y rotation for variety
            var randomYaw = rng.NextFloat(0f, 360f);
            go.transform.rotation = Quaternion.Euler(0f, randomYaw, 0f);
        }

        /// <summary>
        /// Configure a fallback sphere visual with tier-specific color and emissive glow.
        /// </summary>
        private void ConfigureFallbackVisual(GameObject go, int resourceTier)
        {
            var tierInfo = ResourceTierDefinitions.GetTier(resourceTier);
            var mineralColor = tierInfo.MineralColor;
            var emissiveIntensity = tierInfo.EmissiveIntensity;

            go.transform.localScale = Vector3.one * GameConstants.MineralScale;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", mineralColor);

                var hdrColor = mineralColor * emissiveIntensity;
                block.SetColor("_EmissionColor", hdrColor);
                rend.material.EnableKeyword("_EMISSION");

                rend.SetPropertyBlock(block);
            }

        }

        void LateUpdate()
        {
            if (!initialized) return;

            var entities = mineralQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var activeEntities = new HashSet<Entity>(entities.Length);

            var dt = Time.deltaTime;

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                activeEntities.Add(entity);

                if (!entityToGo.TryGetValue(entity, out var go))
                {
                    // New entity — determine tier and assign pooled GameObject
                    var mineralData = em.GetComponentData<MineralData>(entity);
                    var tier = mineralData.ResourceTier;

                    var pool = GetOrCreateTierPool(tier);
                    if (pool != null)
                    {
                        go = pool.Get();
                        ConfigureMeshVisual(go, tier);
                        entityToTier[entity] = tier;
                    }
                    else
                    {
                        go = fallbackPool.Get();
                        ConfigureFallbackVisual(go, tier);
                        entityToTier[entity] = -1; // sentinel: fallback pool
                    }

                    entityToGo[entity] = go;

                    // Assign random tumble rotation
                    var axis = rng.NextFloat3Direction();
                    var speed = rng.NextFloat(90f, 360f);
                    entityRotation[entity] = (new Vector3(axis.x, axis.y, axis.z), speed);
                }

                // Sync transform from ECS to GameObject
                var lt = em.GetComponentData<LocalTransform>(entity);
                go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);

                // Apply tumble rotation for mesh chunks
                if (entityRotation.TryGetValue(entity, out var rot))
                    go.transform.Rotate(rot.axis, rot.speed * dt, Space.World);
            }

            // Cleanup pass — release GameObjects to their correct pools
            entitiesToRemove.Clear();
            foreach (var kvp in entityToGo)
            {
                if (!activeEntities.Contains(kvp.Key))
                {
                    if (entityToTier.TryGetValue(kvp.Key, out var tier))
                    {
                        if (tier >= 0 && tierPools.TryGetValue(tier, out var pool))
                        {
                            kvp.Value.transform.localScale = Vector3.one;
                            pool.Release(kvp.Value);
                        }
                        else
                        {
                            fallbackPool.Release(kvp.Value);
                        }
                        entityToTier.Remove(kvp.Key);
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
