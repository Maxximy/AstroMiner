using ECS.Components;
using MonoBehaviours.Audio;
using MonoBehaviours.Rendering;
using Unity.Entities;
using UnityEngine;

namespace MonoBehaviours.Bridge
{
    /// <summary>
    /// Central dispatcher that drains all three ECS event buffers (DamageEvent,
    /// DestructionEvent, CollectionEvent) each frame in LateUpdate and dispatches
    /// to all feedback managers: DamagePopupManager, ExplosionManager, AudioManager,
    /// and CameraShake.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// </summary>
    public class FeedbackEventBridge : MonoBehaviour
    {
        public static FeedbackEventBridge Instance { get; private set; }

        private EntityManager em;
        private EntityQuery damageBufferQuery;
        private EntityQuery destructionBufferQuery;
        private EntityQuery collectionBufferQuery;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("FeedbackEventBridge");
                Instance = go.AddComponent<FeedbackEventBridge>();
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

        private void LateUpdate()
        {
            if (!initialized)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                em = world.EntityManager;

                damageBufferQuery = em.CreateEntityQuery(typeof(DamageEvent));
                destructionBufferQuery = em.CreateEntityQuery(typeof(DestructionEvent));
                collectionBufferQuery = em.CreateEntityQuery(typeof(CollectionEvent));
                initialized = true;
            }

            DrainDamageEvents();
            DrainDestructionEvents();
            DrainCollectionEvents();
        }

        private void DrainDamageEvents()
        {
            if (damageBufferQuery.CalculateEntityCount() == 0) return;
            var entity = damageBufferQuery.GetSingletonEntity();
            var buffer = em.GetBuffer<DamageEvent>(entity);

            for (int i = 0; i < buffer.Length; i++)
            {
                var evt = buffer[i];
                var pos = new Vector3(evt.Position.x, evt.Position.y, evt.Position.z);

                // Visual: damage popup
                DamagePopupManager.Instance?.Spawn(evt.Position, evt.Amount, evt.Type, evt.ColorR, evt.ColorG, evt.ColorB);

                // Audio: mining hit SFX
                AudioManager.Instance?.PlayDamageHit(pos);

                // Screen shake on critical hits (Phase 5 will produce these)
                if (evt.Type == DamageType.Critical)
                {
                    CameraShake.Instance?.Shake();
                }
            }
            buffer.Clear();
        }

        private void DrainDestructionEvents()
        {
            if (destructionBufferQuery.CalculateEntityCount() == 0) return;
            var entity = destructionBufferQuery.GetSingletonEntity();
            var buffer = em.GetBuffer<DestructionEvent>(entity);

            for (int i = 0; i < buffer.Length; i++)
            {
                var evt = buffer[i];
                var pos = new Vector3(evt.Position.x, evt.Position.y, evt.Position.z);

                // Visual: explosion particles
                ExplosionManager.Instance?.PlayExplosion(evt.Position, evt.Scale);

                // Audio: destruction SFX
                AudioManager.Instance?.PlayDestruction(pos);
            }
            buffer.Clear();
        }

        private void DrainCollectionEvents()
        {
            if (collectionBufferQuery.CalculateEntityCount() == 0) return;
            var entity = collectionBufferQuery.GetSingletonEntity();
            var buffer = em.GetBuffer<CollectionEvent>(entity);

            for (int i = 0; i < buffer.Length; i++)
            {
                var evt = buffer[i];

                // Audio: collection chime (batched)
                AudioManager.Instance?.QueueCollectionChime(evt.ResourceTier);
            }
            buffer.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
