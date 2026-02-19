using System.Collections;
using System.Collections.Generic;
using MonoBehaviours.Core;
using MonoBehaviours.Pool;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Singleton MonoBehaviour that spawns pooled ParticleSystem debris explosions.
    /// Called by FeedbackEventBridge when DestructionEvents are drained from ECS buffers.
    /// Supports per-tier custom explosion prefabs from AsteroidVisualConfig (scene object),
    /// with fallback to a programmatic particle system when prefabs are not assigned.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// </summary>
    public class ExplosionManager : MonoBehaviour
    {
        public static ExplosionManager Instance { get; private set; }

        // Fallback: programmatic particle pool (original behavior)
        private GameObjectPool fallbackPool;
        private GameObject fallbackPrefab;

        // Per-tier explosion pools, lazily created on first use
        private Dictionary<int, GameObjectPool> tierPools;

        // Track which tiers have no prefab assigned (avoid repeated warning spam)
        private HashSet<int> missingTiers;

        // Mesh explosion tuning
        private const float MeshScatterDuration = 0.25f;
        private const float MeshScatterSpeed = 5f;
        private const float MeshFlySpeed = 8f;
        private const float MeshFlyAcceleration = 12f;
        private const float MeshCollectRadius = 1f;
        private const float MeshFragmentScale = 1.5f;

        private static readonly Vector3 ShipPos = new(GameConstants.ShipPositionX, 0f, GameConstants.ShipPositionZ);

        // Note: ECS event draining is handled centrally by FeedbackEventBridge.
        // ExplosionManager only exposes the public PlayExplosion() method for dispatching.

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
            tierPools = new Dictionary<int, GameObjectPool>();
            missingTiers = new HashSet<int>();

            // Create fallback (programmatic particle) prefab
            fallbackPrefab = CreateFallbackPrefab();
            fallbackPrefab.SetActive(false);
            fallbackPrefab.transform.SetParent(transform);

            // Pool: pre-warm 10, max 25
            var poolParent = new GameObject("ExplosionFallbackPool").transform;
            poolParent.SetParent(transform);
            fallbackPool = new GameObjectPool(fallbackPrefab, poolParent, 10, 25);

            Debug.Log("ExplosionManager: initialized with fallback pool + lazy tier pools.");
        }

        private GameObject CreateFallbackPrefab()
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
        /// Gets or creates a pool for the given tier's explosion prefab.
        /// Returns null if no prefab is assigned in AsteroidVisualConfig (caller uses fallback).
        /// </summary>
        private GameObjectPool GetOrCreateTierPool(int tier)
        {
            if (tierPools.TryGetValue(tier, out var pool))
                return pool;

            if (missingTiers.Contains(tier))
                return null;

            var config = AsteroidVisualConfig.Instance;
            var prefab = config != null ? config.GetDestroyPrefab(tier) : null;

            if (prefab == null)
            {
                missingTiers.Add(tier);
                Debug.LogWarning($"ExplosionManager: No destroy prefab assigned for tier {tier}. Using fallback.");
                return null;
            }

            var parent = new GameObject($"ExplosionPool_tier{tier}").transform;
            parent.SetParent(transform);

            pool = new GameObjectPool(prefab, parent, 3, 10);
            tierPools[tier] = pool;
            return pool;
        }

        /// <summary>
        /// Plays a debris explosion at the given world position.
        /// Uses per-tier prefab if available, falls back to programmatic particles.
        /// Supports both ParticleSystem prefabs and mesh-based destroy prefabs.
        /// </summary>
        public void PlayExplosion(float3 position, float scale, int resourceTier)
        {
            GameObjectPool pool = GetOrCreateTierPool(resourceTier) ?? fallbackPool;
            if (pool == null) return;

            var go = pool.Get();
            go.transform.position = new Vector3(position.x, 0.1f, position.z);

            var ps = go.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                // Scale particle count by asteroid scale
                var emission = ps.emission;
                int particleCount = Mathf.RoundToInt(GameConstants.ExplosionParticleCount * scale);
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, particleCount)
                });

                ps.Play();
                StartCoroutine(ReturnWhenDone(go, ps, pool));
            }
            else
            {
                // Mesh-based destroy prefab: scatter outward, then fly fragments to ship
                go.transform.localScale = Vector3.one * MeshFragmentScale;
                StartCoroutine(MeshExplosionRoutine(go, pool));
            }
        }

        /// <summary>
        /// Backward-compatible overload (defaults to tier 0).
        /// </summary>
        public void PlayExplosion(float3 position, float scale)
        {
            PlayExplosion(position, scale, 0);
        }

        private IEnumerator MeshExplosionRoutine(GameObject go, GameObjectPool pool)
        {
            int childCount = go.transform.childCount;
            bool hasChildren = childCount > 0;

            // Collect the transforms we'll animate (children if any, otherwise root)
            var fragments = new Transform[hasChildren ? childCount : 1];
            var velocities = new Vector3[fragments.Length];
            var rotAxes = new Vector3[fragments.Length];
            var collected = new bool[fragments.Length];
            var origLocalPos = new Vector3[fragments.Length];
            var origLocalRot = new Quaternion[fragments.Length];

            if (hasChildren)
            {
                for (int i = 0; i < childCount; i++)
                {
                    fragments[i] = go.transform.GetChild(i);
                    origLocalPos[i] = fragments[i].localPosition;
                    origLocalRot[i] = fragments[i].localRotation;
                }
            }
            else
            {
                fragments[0] = go.transform;
                origLocalPos[0] = Vector3.zero;
                origLocalRot[0] = Quaternion.identity;
            }

            // Random scatter directions
            var rng = new Unity.Mathematics.Random((uint)Time.frameCount | 7u);
            for (int i = 0; i < fragments.Length; i++)
            {
                float angle = rng.NextFloat(0f, Mathf.PI * 2f);
                velocities[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * MeshScatterSpeed;
                rotAxes[i] = Random.onUnitSphere;
            }

            // ── Phase 1: Scatter outward ──
            float elapsed = 0f;
            while (elapsed < MeshScatterDuration)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < fragments.Length; i++)
                {
                    fragments[i].position += velocities[i] * dt;
                    fragments[i].Rotate(rotAxes[i], 360f * dt, Space.World);
                }
                elapsed += dt;
                yield return null;
            }

            // ── Phase 2: Fly to ship + collect ──
            var speeds = new float[fragments.Length];
            for (int i = 0; i < speeds.Length; i++)
                speeds[i] = MeshFlySpeed;

            int collectedCount = 0;
            while (collectedCount < fragments.Length)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < fragments.Length; i++)
                {
                    if (collected[i]) continue;

                    Vector3 toShip = ShipPos - fragments[i].position;
                    float dist = toShip.magnitude;

                    if (dist < MeshCollectRadius)
                    {
                        collected[i] = true;
                        collectedCount++;
                        if (hasChildren)
                            fragments[i].gameObject.SetActive(false);
                        continue;
                    }

                    speeds[i] += MeshFlyAcceleration * dt;
                    Vector3 dir = toShip / dist;
                    fragments[i].position += dir * speeds[i] * dt;
                    fragments[i].Rotate(rotAxes[i], 360f * dt, Space.World);

                    // Shrink as it approaches ship
                    float shrink = Mathf.Clamp01(dist / 5f);
                    fragments[i].localScale = Vector3.one * (MeshFragmentScale * shrink);
                }
                yield return null;
            }

            // ── Reset and return to pool ──
            go.transform.localScale = Vector3.one;
            if (hasChildren)
            {
                for (int i = 0; i < childCount; i++)
                {
                    var child = go.transform.GetChild(i);
                    child.gameObject.SetActive(true);
                    child.localPosition = origLocalPos[i];
                    child.localRotation = origLocalRot[i];
                    child.localScale = Vector3.one;
                }
            }
            pool.Release(go);
        }

        private IEnumerator ReturnWhenDone(GameObject go, ParticleSystem ps, GameObjectPool pool)
        {
            while (ps != null && ps.IsAlive(true))
            {
                yield return null;
            }

            if (go != null && pool != null)
            {
                go.transform.localScale = Vector3.one;
                pool.Release(go);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
