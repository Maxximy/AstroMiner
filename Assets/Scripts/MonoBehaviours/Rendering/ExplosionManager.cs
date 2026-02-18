using System.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Singleton MonoBehaviour that spawns pooled ParticleSystem debris explosions.
/// Drains the ECS DestructionEvent buffer each frame and plays chunky debris
/// particle effects at asteroid death positions.
/// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
/// </summary>
public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance { get; private set; }

    private GameObjectPool _explosionPool;
    private GameObject _explosionPrefab;

    private EntityManager _em;
    private EntityQuery _destructionBufferQuery;
    private bool _ecsInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            var go = new GameObject("ExplosionManager");
            Instance = go.AddComponent<ExplosionManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _explosionPrefab = CreateExplosionPrefab();
        _explosionPrefab.SetActive(false);
        _explosionPrefab.transform.SetParent(transform);

        // Pool: pre-warm 10, max 25
        var poolParent = new GameObject("ExplosionPool").transform;
        poolParent.SetParent(transform);
        _explosionPool = new GameObjectPool(_explosionPrefab, poolParent, 10, 25);

        Debug.Log("ExplosionManager: initialized with pool of 10 explosions.");
    }

    private GameObject CreateExplosionPrefab()
    {
        var go = new GameObject("ExplosionEffect");
        var ps = go.AddComponent<ParticleSystem>();

        // Main module: chunky, weighty debris
        var main = ps.main;
        main.startLifetime = GameConstants.ExplosionParticleLifetime;
        main.startSpeed = GameConstants.ExplosionParticleSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.41f, 0.08f),  // brown
            new Color(0.4f, 0.4f, 0.4f)       // gray
        );
        main.gravityModifier = GameConstants.ExplosionParticleGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 30;

        // Emission: single burst at time 0
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, GameConstants.ExplosionParticleCount)
        });

        // Shape: small sphere for outward burst
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Size over lifetime: shrink to nothing
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Renderer: use default particle material or URP unlit
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            // Try to find URP particle shader, fallback to default
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.5f, 0.4f, 0.2f));
                renderer.material = mat;
            }
        }

        return go;
    }

    /// <summary>
    /// Plays a debris explosion at the given world position.
    /// </summary>
    public void PlayExplosion(float3 position, float scale)
    {
        if (_explosionPool == null) return;

        var go = _explosionPool.Get();
        go.transform.position = new Vector3(position.x, 0.1f, position.z);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // Optionally scale particle count by asteroid scale
            var emission = ps.emission;
            int particleCount = Mathf.RoundToInt(GameConstants.ExplosionParticleCount * scale);
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, particleCount)
            });

            ps.Play();
            StartCoroutine(ReturnWhenDone(go, ps));
        }
        else
        {
            _explosionPool.Release(go);
        }
    }

    private IEnumerator ReturnWhenDone(GameObject go, ParticleSystem ps)
    {
        // Wait until the particle system finishes
        while (ps != null && ps.IsAlive(true))
        {
            yield return null;
        }

        if (go != null && _explosionPool != null)
        {
            _explosionPool.Release(go);
        }
    }

    private void Update()
    {
        DrainEventBuffer();
    }

    private void DrainEventBuffer()
    {
        if (!_ecsInitialized)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _em = world.EntityManager;
            _destructionBufferQuery = _em.CreateEntityQuery(typeof(DestructionEvent));
            _ecsInitialized = true;
        }

        if (_destructionBufferQuery.CalculateEntityCount() == 0) return;

        var entity = _destructionBufferQuery.GetSingletonEntity();
        var buffer = _em.GetBuffer<DestructionEvent>(entity);

        for (int i = 0; i < buffer.Length; i++)
        {
            var evt = buffer[i];
            PlayExplosion(evt.Position, evt.Scale);
        }

        buffer.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
