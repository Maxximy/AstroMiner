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
        private EntityQuery skillBufferQuery;
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
                skillBufferQuery = em.CreateEntityQuery(typeof(SkillEvent));
                initialized = true;
            }

            DrainDamageEvents();
            DrainDestructionEvents();
            DrainCollectionEvents();
            DrainSkillEvents();
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

                // Crit sound on critical hits (no screen shake -- user decision)
                if (evt.Type == DamageType.Critical)
                {
                    AudioManager.Instance?.PlayCritHit(pos);
                    // NOTE: No CameraShake on crits -- user decision: "no extra screen flash or shake"
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
                ExplosionManager.Instance?.PlayExplosion(evt.Position, evt.Scale, evt.ResourceTier);

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

        private void DrainSkillEvents()
        {
            if (skillBufferQuery.CalculateEntityCount() == 0) return;
            var entity = skillBufferQuery.GetSingletonEntity();
            var buffer = em.GetBuffer<SkillEvent>(entity);

            for (int i = 0; i < buffer.Length; i++)
            {
                var evt = buffer[i];
                switch (evt.SkillType)
                {
                    case 0: // Laser Burst
                        SkillVFXManager.Instance?.PlayLaserBurst(
                            new Vector3(evt.OriginPos.x, 0.1f, evt.OriginPos.y),
                            new Vector3(evt.TargetPos.x, 0.1f, evt.TargetPos.y));
                        AudioManager.Instance?.PlaySkillSfx(0);
                        break;
                    case 1: // Chain Lightning
                        var targets = new Vector3[evt.ChainCount + 1];
                        targets[0] = new Vector3(evt.TargetPos.x, 0.1f, evt.TargetPos.y);
                        if (evt.ChainCount >= 1) targets[1] = new Vector3(evt.Chain1.x, 0.1f, evt.Chain1.y);
                        if (evt.ChainCount >= 2) targets[2] = new Vector3(evt.Chain2.x, 0.1f, evt.Chain2.y);
                        if (evt.ChainCount >= 3) targets[3] = new Vector3(evt.Chain3.x, 0.1f, evt.Chain3.y);
                        if (evt.ChainCount >= 4) targets[4] = new Vector3(evt.Chain4.x, 0.1f, evt.Chain4.y);
                        SkillVFXManager.Instance?.PlayChainLightning(
                            new Vector3(evt.OriginPos.x, 0.1f, evt.OriginPos.y),
                            targets, evt.ChainCount + 1);
                        AudioManager.Instance?.PlaySkillSfx(1);
                        break;
                    case 2: // EMP Pulse
                        SkillVFXManager.Instance?.PlayEmpPulse(
                            new Vector3(evt.TargetPos.x, 0.1f, evt.TargetPos.y));
                        AudioManager.Instance?.PlaySkillSfx(2);
                        break;
                    case 3: // Overcharge
                        SkillVFXManager.Instance?.PlayOverchargeActivation();
                        AudioManager.Instance?.PlaySkillSfx(3);
                        break;
                }
            }
            buffer.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
