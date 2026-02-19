using System.Collections;
using System.Collections.Generic;
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

        // Mesh explosion tuning
        private const float MeshScatterDuration = 0.4f;
        private const float MeshScatterSpeed = 5f;
        private const float MeshFragmentScale = 5f;

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
            // Create fallback (programmatic particle) prefab
            fallbackPrefab = CreateFallbackPrefab();
            fallbackPrefab.SetActive(false);
            fallbackPrefab.transform.SetParent(transform);

            // Pool: pre-warm 10, max 25
            var poolParent = new GameObject("ExplosionFallbackPool").transform;
            poolParent.SetParent(transform);
            fallbackPool = new GameObjectPool(fallbackPrefab, poolParent, 10, 25);

            Debug.Log("ExplosionManager: initialized with fallback pool.");
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
        /// Plays a debris explosion at the given world position using the fallback particle pool.
        /// </summary>
        public void PlayExplosion(float3 position, float scale, int resourceTier = 0)
        {
            if (fallbackPool == null) return;

            var go = fallbackPool.Get();
            go.transform.position = new Vector3(position.x, 0.1f, position.z);
            go.transform.localScale = Vector3.one * MeshFragmentScale;
            StartCoroutine(MeshExplosionRoutine(go, fallbackPool));
        }

        private IEnumerator MeshExplosionRoutine(GameObject go, GameObjectPool pool)
        {
            var childCount = go.transform.childCount;
            var hasChildren = childCount > 0;

            // Collect the transforms we'll animate (children if any, otherwise root)
            var fragments = new Transform[hasChildren ? childCount : 1];
            var velocities = new Vector3[fragments.Length];
            var rotAxes = new Vector3[fragments.Length];
            var origLocalPos = new Vector3[fragments.Length];
            var origLocalRot = new Quaternion[fragments.Length];

            if (hasChildren)
            {
                for (var i = 0; i < childCount; i++)
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
            for (var i = 0; i < fragments.Length; i++)
            {
                var angle = rng.NextFloat(0f, Mathf.PI * 2f);
                velocities[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * MeshScatterSpeed;
                rotAxes[i] = Random.onUnitSphere;
            }

            // Scatter outward + shrink to nothing
            var elapsed = 0f;
            while (elapsed < MeshScatterDuration)
            {
                var dt = Time.deltaTime;
                var t = elapsed / MeshScatterDuration;
                var shrink = 1f - t; // scale from 1 → 0

                for (var i = 0; i < fragments.Length; i++)
                {
                    fragments[i].position += velocities[i] * dt;
                    fragments[i].Rotate(rotAxes[i], 360f * dt, Space.World);
                    fragments[i].localScale = Vector3.one * (MeshFragmentScale * shrink);
                }
                elapsed += dt;
                yield return null;
            }

            // Reset and return to pool
            go.transform.localScale = Vector3.one;
            if (hasChildren)
            {
                for (var i = 0; i < childCount; i++)
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
