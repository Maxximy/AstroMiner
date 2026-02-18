using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Syncs ECS mineral entities to pooled GameObjects each frame.
/// Handles dynamic entity lifecycle: new entities get a pooled sphere,
/// destroyed entities have their sphere returned to the pool.
/// Mirrors AsteroidRenderer's pattern for consistency.
/// </summary>
public class MineralRenderer : MonoBehaviour
{
    // Default mineral color: warm gold/amber for Iron tier
    private static readonly Color DefaultMineralColor = new Color(0.9f, 0.75f, 0.3f);

    private EntityManager _em;
    private EntityQuery _mineralQuery;
    private bool _initialized;

    // Entity -> GameObject tracking for dynamic lifecycle
    private Dictionary<Entity, GameObject> _entityToGO;
    private List<Entity> _entitiesToRemove;

    // Object pool for mineral spheres
    private GameObjectPool _mineralPool;
    private GameObject _mineralPrefab;

    // Deterministic RNG for visual randomness
    private Unity.Mathematics.Random _rng;

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

        _em = world.EntityManager;

        // Query for mineral entities with transforms
        _mineralQuery = _em.CreateEntityQuery(
            typeof(MineralTag),
            typeof(LocalTransform)
        );

        _entityToGO = new Dictionary<Entity, GameObject>();
        _entitiesToRemove = new List<Entity>();

        // Create mineral prefab (small sphere without collider)
        _mineralPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _mineralPrefab.name = "MineralPrefab";
        _mineralPrefab.SetActive(false);
        _mineralPrefab.transform.SetParent(transform);
        var collider = _mineralPrefab.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // Create pool with capacity for 1000+ minerals
        int preWarmCount = 200;
        int maxSize = 1200;
        var poolParent = new GameObject("MineralPool").transform;
        poolParent.SetParent(transform);
        _mineralPool = new GameObjectPool(_mineralPrefab, poolParent, preWarmCount, maxSize);

        // Seed RNG for visual randomness
        _rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

        _initialized = true;
        Debug.Log($"MineralRenderer: initialized. Pool pre-warmed with {preWarmCount} objects.");
    }

    private void ConfigureMineralVisual(GameObject go)
    {
        // Uniform scale for minerals
        go.transform.localScale = Vector3.one * GameConstants.MineralScale;

        // Set mineral color via MaterialPropertyBlock (warm gold/amber for Iron tier)
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", DefaultMineralColor);

            // HDR emissive color for mineral glow (VISL-04)
            Color hdrColor = DefaultMineralColor * GameConstants.MineralEmissiveIntensity;
            block.SetColor("_EmissionColor", hdrColor);
            renderer.material.EnableKeyword("_EMISSION");

            renderer.SetPropertyBlock(block);
        }

        // TrailRenderer for mineral flight trails (FEED-06)
        var trail = go.GetComponent<TrailRenderer>();
        if (trail == null) trail = go.AddComponent<TrailRenderer>();
        trail.time = GameConstants.MineralTrailDuration;
        trail.startWidth = GameConstants.MineralTrailStartWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        // HDR emissive trail material matching mineral color
        var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (trailMat != null)
        {
            Color trailColor = DefaultMineralColor * GameConstants.MineralEmissiveIntensity;
            trailMat.SetColor("_BaseColor", trailColor);
            trail.material = trailMat;
        }

        // Clear trail on initial config to prevent ghost trails
        trail.Clear();
    }

    void LateUpdate()
    {
        if (!_initialized) return;

        // Get all current mineral entities
        var entities = _mineralQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        var activeEntities = new HashSet<Entity>(entities.Length);

        // Sync positions -- assign new GameObjects to new entities
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            activeEntities.Add(entity);

            if (!_entityToGO.TryGetValue(entity, out var go))
            {
                // New entity discovered -- assign a pooled GameObject
                go = _mineralPool.Get();
                ConfigureMineralVisual(go);
                _entityToGO[entity] = go;
            }

            // Sync transform from ECS to GameObject
            var lt = _em.GetComponentData<LocalTransform>(entity);
            go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
        }

        // Cleanup pass -- release GameObjects for destroyed entities
        _entitiesToRemove.Clear();
        foreach (var kvp in _entityToGO)
        {
            if (!activeEntities.Contains(kvp.Key))
            {
                // Clear trail to prevent ghost trails on pool reuse
                var trail = kvp.Value.GetComponent<TrailRenderer>();
                if (trail != null) trail.Clear();

                _mineralPool.Release(kvp.Value);
                _entitiesToRemove.Add(kvp.Key);
            }
        }
        foreach (var entity in _entitiesToRemove)
        {
            _entityToGO.Remove(entity);
        }

        entities.Dispose();
    }

    void OnDestroy()
    {
        if (_mineralPrefab != null) Destroy(_mineralPrefab);
    }
}
