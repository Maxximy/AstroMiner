using System.Collections;
using System.Collections.Generic;
using Data;
using ECS.Components;
using MonoBehaviours.Pool;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Syncs ECS mineral entities to pooled GameObjects each frame.
    /// Handles dynamic entity lifecycle: new entities get a pooled sphere,
    /// destroyed entities have their sphere returned to the pool.
    /// Applies per-tier colors from ResourceTierDefinitions based on MineralData.ResourceTier.
    /// </summary>
    public class MineralRenderer : MonoBehaviour
    {
        private EntityManager em;
        private EntityQuery mineralQuery;
        private bool initialized;

        // Entity -> GameObject tracking for dynamic lifecycle
        private Dictionary<Entity, GameObject> entityToGo;
        private List<Entity> entitiesToRemove;

        // Object pool for mineral spheres
        private GameObjectPool mineralPool;
        private GameObject mineralPrefab;

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

            // Query for mineral entities with transforms and mineral data (for tier lookup)
            mineralQuery = em.CreateEntityQuery(
                typeof(MineralTag),
                typeof(LocalTransform),
                typeof(MineralData)
            );

            entityToGo = new Dictionary<Entity, GameObject>();
            entitiesToRemove = new List<Entity>();

            // Create mineral prefab (small sphere without collider)
            mineralPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mineralPrefab.name = "MineralPrefab";
            mineralPrefab.SetActive(false);
            mineralPrefab.transform.SetParent(transform);
            var coll = mineralPrefab.GetComponent<Collider>();
            if (coll != null) Destroy(coll);

            // Create pool with capacity for 1000+ minerals
            int preWarmCount = 200;
            int maxSize = 1200;
            var poolParent = new GameObject("MineralPool").transform;
            poolParent.SetParent(transform);
            mineralPool = new GameObjectPool(mineralPrefab, poolParent, preWarmCount, maxSize);

            // Seed RNG for visual randomness
            rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

            initialized = true;
            Debug.Log($"MineralRenderer: initialized. Pool pre-warmed with {preWarmCount} objects.");
        }

        /// <summary>
        /// Configure mineral visual appearance based on resource tier.
        /// Applies tier-specific color, emissive glow, and trail from ResourceTierDefinitions.
        /// </summary>
        private void ConfigureMineralVisual(GameObject go, int resourceTier)
        {
            var tierInfo = ResourceTierDefinitions.GetTier(resourceTier);
            Color mineralColor = tierInfo.MineralColor;
            float emissiveIntensity = tierInfo.EmissiveIntensity;

            // Uniform scale for minerals
            go.transform.localScale = Vector3.one * GameConstants.MineralScale;

            // Set mineral color via MaterialPropertyBlock (per-tier color)
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", mineralColor);

                // HDR emissive color for mineral glow
                Color hdrColor = mineralColor * emissiveIntensity;
                block.SetColor("_EmissionColor", hdrColor);
                rend.material.EnableKeyword("_EMISSION");

                rend.SetPropertyBlock(block);
            }

            // TrailRenderer for mineral flight trails
            var trail = go.GetComponent<TrailRenderer>();
            if (trail == null) trail = go.AddComponent<TrailRenderer>();
            trail.time = GameConstants.MineralTrailDuration;
            trail.startWidth = GameConstants.MineralTrailStartWidth;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.1f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            // HDR emissive trail material matching tier color
            var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (trailMat != null)
            {
                Color trailColor = mineralColor * emissiveIntensity;
                trailMat.SetColor("_BaseColor", trailColor);
                trail.material = trailMat;
            }

            // Clear trail on initial config to prevent ghost trails
            trail.Clear();
        }

        void LateUpdate()
        {
            if (!initialized) return;

            // Get all current mineral entities
            var entities = mineralQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var activeEntities = new HashSet<Entity>(entities.Length);

            // Sync positions -- assign new GameObjects to new entities
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                activeEntities.Add(entity);

                if (!entityToGo.TryGetValue(entity, out var go))
                {
                    // New entity discovered -- read tier and assign a pooled GameObject
                    var mineralData = em.GetComponentData<MineralData>(entity);
                    go = mineralPool.Get();
                    ConfigureMineralVisual(go, mineralData.ResourceTier);
                    entityToGo[entity] = go;
                }

                // Sync transform from ECS to GameObject
                var lt = em.GetComponentData<LocalTransform>(entity);
                go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
            }

            // Cleanup pass -- release GameObjects for destroyed entities
            entitiesToRemove.Clear();
            foreach (var kvp in entityToGo)
            {
                if (!activeEntities.Contains(kvp.Key))
                {
                    // Clear trail to prevent ghost trails on pool reuse
                    var trail = kvp.Value.GetComponent<TrailRenderer>();
                    if (trail != null) trail.Clear();

                    mineralPool.Release(kvp.Value);
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
            if (mineralPrefab != null) Destroy(mineralPrefab);
        }
    }
}
