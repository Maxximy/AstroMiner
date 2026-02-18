using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Syncs ECS asteroid entities to pooled GameObjects each frame.
/// Handles dynamic entity lifecycle: new entities get a pooled sphere,
/// destroyed entities have their sphere returned to the pool.
/// Also creates the ship placeholder visual.
/// </summary>
public class AsteroidRenderer : MonoBehaviour
{
    [Header("Asteroid Visuals")]
    [SerializeField] private float _asteroidScaleMin = 0.8f;
    [SerializeField] private float _asteroidScaleMax = 1.5f;

    // Asteroid colors: dark gray, brown, rust (warm muted palette)
    private static readonly Color[] AsteroidColors = new Color[]
    {
        new Color(0.333f, 0.333f, 0.333f), // dark gray
        new Color(0.545f, 0.412f, 0.078f), // brown
        new Color(0.718f, 0.255f, 0.055f), // rust
    };

    private EntityManager _em;
    private EntityQuery _asteroidQuery;
    private bool _initialized;

    // Entity -> GameObject tracking for dynamic lifecycle
    private Dictionary<Entity, GameObject> _entityToGO;
    private List<Entity> _entitiesToRemove;

    // Object pool for asteroid spheres
    private GameObjectPool _asteroidPool;
    private GameObject _asteroidPrefab;

    // Ship placeholder (static, not ECS-driven)
    private GameObject _shipGO;

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
            Debug.LogError("AsteroidRenderer: ECS world not ready");
            yield break;
        }

        _em = world.EntityManager;

        // Query for asteroid entities with transforms
        _asteroidQuery = _em.CreateEntityQuery(
            typeof(AsteroidTag),
            typeof(LocalTransform)
        );

        _entityToGO = new Dictionary<Entity, GameObject>();
        _entitiesToRemove = new List<Entity>();

        // Create asteroid prefab (sphere without collider)
        _asteroidPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _asteroidPrefab.name = "AsteroidPrefab";
        _asteroidPrefab.SetActive(false);
        _asteroidPrefab.transform.SetParent(transform);
        var collider = _asteroidPrefab.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // Create pool with headroom for spawn/destroy overlap
        int preWarmCount = GameConstants.DefaultMaxAsteroids + 20;
        int maxSize = GameConstants.DefaultMaxAsteroids + 50;
        var poolParent = new GameObject("AsteroidPool").transform;
        poolParent.SetParent(transform);
        _asteroidPool = new GameObjectPool(_asteroidPrefab, poolParent, preWarmCount, maxSize);

        // Seed RNG for visual randomness (scale, color)
        _rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks | 1u);

        // Create ship placeholder on the XZ plane
        CreateShipPlaceholder();

        _initialized = true;
        Debug.Log($"AsteroidRenderer: initialized. Pool pre-warmed with {preWarmCount} objects.");
    }

    private void CreateShipPlaceholder()
    {
        _shipGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _shipGO.name = "ShipPlaceholder";
        _shipGO.transform.SetParent(transform);

        // Ship positioned on XZ plane (Y=0), near bottom of visible area
        _shipGO.transform.position = new Vector3(0f, 0f, GameConstants.ShipPositionZ);
        _shipGO.transform.localScale = new Vector3(1.0f, 0.1f, 0.5f);
        _shipGO.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        // Remove collider
        var col = _shipGO.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Light gray color via MaterialPropertyBlock
        var renderer = _shipGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.8f));
            renderer.SetPropertyBlock(block);
        }
    }

    private void ConfigureAsteroidVisual(GameObject go)
    {
        // Random scale
        float scale = _rng.NextFloat(_asteroidScaleMin, _asteroidScaleMax);
        go.transform.localScale = Vector3.one * scale;

        // Random color from palette via MaterialPropertyBlock
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", AsteroidColors[_rng.NextInt(AsteroidColors.Length)]);

            // Add subtle emissive tint for visual richness (VISL-03)
            Color emissive = AsteroidColors[_rng.NextInt(AsteroidColors.Length)] * 0.3f;
            block.SetColor("_EmissionColor", emissive);
            renderer.material.EnableKeyword("_EMISSION");

            renderer.SetPropertyBlock(block);
        }
    }

    void LateUpdate()
    {
        if (!_initialized) return;

        // Get all current asteroid entities
        var entities = _asteroidQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        var activeEntities = new HashSet<Entity>(entities.Length);

        // Sync positions -- assign new GameObjects to new entities
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            activeEntities.Add(entity);

            if (!_entityToGO.TryGetValue(entity, out var go))
            {
                // New entity discovered -- assign a pooled GameObject
                go = _asteroidPool.Get();
                ConfigureAsteroidVisual(go);
                _entityToGO[entity] = go;
            }

            // Sync transform from ECS to GameObject
            var lt = _em.GetComponentData<LocalTransform>(entity);
            go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
            go.transform.rotation = lt.Rotation;
        }

        // Cleanup pass -- release GameObjects for destroyed entities
        _entitiesToRemove.Clear();
        foreach (var kvp in _entityToGO)
        {
            if (!activeEntities.Contains(kvp.Key))
            {
                _asteroidPool.Release(kvp.Value);
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
        if (_asteroidPrefab != null) Destroy(_asteroidPrefab);
    }
}
