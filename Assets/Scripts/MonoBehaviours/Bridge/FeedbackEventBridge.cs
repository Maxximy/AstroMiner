using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

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

    private EntityManager _em;
    private EntityQuery _damageBufferQuery;
    private EntityQuery _destructionBufferQuery;
    private EntityQuery _collectionBufferQuery;
    private bool _initialized;

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
        if (!_initialized)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            _em = world.EntityManager;

            _damageBufferQuery = _em.CreateEntityQuery(typeof(DamageEvent));
            _destructionBufferQuery = _em.CreateEntityQuery(typeof(DestructionEvent));
            _collectionBufferQuery = _em.CreateEntityQuery(typeof(CollectionEvent));
            _initialized = true;
        }

        DrainDamageEvents();
        DrainDestructionEvents();
        DrainCollectionEvents();
    }

    private void DrainDamageEvents()
    {
        if (_damageBufferQuery.CalculateEntityCount() == 0) return;
        var entity = _damageBufferQuery.GetSingletonEntity();
        var buffer = _em.GetBuffer<DamageEvent>(entity);

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
        if (_destructionBufferQuery.CalculateEntityCount() == 0) return;
        var entity = _destructionBufferQuery.GetSingletonEntity();
        var buffer = _em.GetBuffer<DestructionEvent>(entity);

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
        if (_collectionBufferQuery.CalculateEntityCount() == 0) return;
        var entity = _collectionBufferQuery.GetSingletonEntity();
        var buffer = _em.GetBuffer<CollectionEvent>(entity);

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
