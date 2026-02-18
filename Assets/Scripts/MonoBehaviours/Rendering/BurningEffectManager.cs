using System.Collections.Generic;
using ECS.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Manages pooled ember ParticleSystems that track burning asteroid entities.
    /// Queries ECS for entities with BurningData and positions pooled particle effects at them.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public class BurningEffectManager : MonoBehaviour
    {
        public static BurningEffectManager Instance { get; private set; }

        private const int PoolSize = 20;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly Dictionary<Entity, GameObject> activeEffects = new Dictionary<Entity, GameObject>();

        // ECS lazy init
        private EntityManager em;
        private EntityQuery burningQuery;
        private bool ecsInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("BurningEffectManager");
                Instance = go.AddComponent<BurningEffectManager>();
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
            var poolParent = new GameObject("EmberPool").transform;
            poolParent.SetParent(transform);

            for (int i = 0; i < PoolSize; i++)
            {
                var go = CreateEmberEffect();
                go.transform.SetParent(poolParent);
                go.SetActive(false);
                pool.Enqueue(go);
            }

            Debug.Log("BurningEffectManager: initialized with pool of " + PoolSize + " ember effects.");
        }

        private GameObject CreateEmberEffect()
        {
            var go = new GameObject("EmberEffect");
            var ps = go.AddComponent<ParticleSystem>();

            // Main module
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.5f, 0f, 1f),   // orange
                new Color(1f, 0.2f, 0f, 0.5f)   // red (dimmer)
            );
            main.gravityModifier = -0.3f; // Float upward (negative Y)
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 20;

            // Emission: rate over time
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(8f, 12f);

            // Shape: small sphere
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Size over lifetime: shrink
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // Color over lifetime: fade to transparent
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Renderer: URP particle material
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", new Color(1f, 0.5f, 0f));
                    renderer.material = mat;
                }
            }

            return go;
        }

        private void LateUpdate()
        {
            if (!ecsInitialized)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;

                em = world.EntityManager;
                burningQuery = em.CreateEntityQuery(typeof(BurningData), typeof(LocalTransform));
                ecsInitialized = true;
            }

            // Get current burning entities
            var entities = burningQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var activeBurningEntities = new HashSet<Entity>();

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                activeBurningEntities.Add(entity);

                var localTransform = em.GetComponentData<LocalTransform>(entity);
                var worldPos = new Vector3(localTransform.Position.x, 0.1f, localTransform.Position.z);

                if (!activeEffects.ContainsKey(entity))
                {
                    // New burning entity: get from pool
                    if (pool.Count > 0)
                    {
                        var go = pool.Dequeue();
                        go.SetActive(true);
                        go.transform.position = worldPos;
                        var ps = go.GetComponent<ParticleSystem>();
                        if (ps != null) ps.Play();
                        activeEffects[entity] = go;
                    }
                }
                else
                {
                    // Update position of existing effect
                    activeEffects[entity].transform.position = worldPos;
                }
            }

            entities.Dispose();

            // Return effects for entities no longer burning
            var toRemove = new List<Entity>();
            foreach (var kvp in activeEffects)
            {
                if (!activeBurningEntities.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                var go = activeEffects[toRemove[i]];
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop();
                go.SetActive(false);
                pool.Enqueue(go);
                activeEffects.Remove(toRemove[i]);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
