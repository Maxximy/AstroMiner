using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MonoBehaviours.Core
{
    public class ECSBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;

            // Create GameState singleton
            var gameStateEntity = em.CreateEntity(typeof(GameStateData));
            em.SetComponentData(gameStateEntity, new GameStateData
            {
                Phase = GamePhase.Playing,
                Timer = GameConstants.DefaultRunDuration,
                Credits = 0
            });

            // Create Input singleton
            var inputEntity = em.CreateEntity(typeof(InputData));
            em.SetComponentData(inputEntity, new InputData
            {
                MouseWorldPos = float2.zero,
                MouseValid = false
            });

            // Create AsteroidSpawnTimer singleton
            var spawnTimerEntity = em.CreateEntity(typeof(AsteroidSpawnTimer));
            em.SetComponentData(spawnTimerEntity, new AsteroidSpawnTimer
            {
                SpawnInterval = GameConstants.DefaultSpawnInterval,
                TimeUntilNextSpawn = 0f,
                MaxActiveAsteroids = GameConstants.DefaultMaxAsteroids
            });

            // Create MiningConfigData singleton
            var miningConfigEntity = em.CreateEntity(typeof(MiningConfigData));
            em.SetComponentData(miningConfigEntity, new MiningConfigData
            {
                Radius = GameConstants.DefaultMiningRadius,
                DamagePerTick = GameConstants.DefaultDamagePerTick,
                TickInterval = GameConstants.DefaultTickInterval
            });

            // Create CollectionEvent buffer entity (Phase 4 visual/audio feedback)
            var collectionEventEntity = em.CreateEntity();
            em.AddBuffer<CollectionEvent>(collectionEventEntity);

            // Create DamageEvent buffer entity (Phase 4 visual/audio feedback)
            var damageEventEntity = em.CreateEntity();
            em.AddBuffer<DamageEvent>(damageEventEntity);

            // Create DestructionEvent buffer entity (Phase 4 explosion/audio feedback)
            var destructionEventEntity = em.CreateEntity();
            em.AddBuffer<DestructionEvent>(destructionEventEntity);

            // Phase 5: Skill system singletons
            var skillInputEntity = em.CreateEntity(typeof(SkillInputData));
            em.SetComponentData(skillInputEntity, new SkillInputData());

            var skillCooldownEntity = em.CreateEntity(typeof(SkillCooldownData));
            em.SetComponentData(skillCooldownEntity, new SkillCooldownData
            {
                Skill1MaxCooldown = GameConstants.LaserBurstCooldown,
                Skill2MaxCooldown = GameConstants.ChainLightningCooldown,
                Skill3MaxCooldown = GameConstants.EmpPulseCooldown,
                Skill4MaxCooldown = GameConstants.OverchargeCooldown
            });

            var critConfigEntity = em.CreateEntity(typeof(CritConfigData));
            em.SetComponentData(critConfigEntity, new CritConfigData
            {
                CritChance = GameConstants.CritChance,
                CritMultiplier = GameConstants.CritMultiplier
            });

            var overchargeBuffEntity = em.CreateEntity(typeof(OverchargeBuffData));
            em.SetComponentData(overchargeBuffEntity, new OverchargeBuffData
            {
                RemainingDuration = 0f,
                DamageMultiplier = GameConstants.OverchargeDamageMultiplier,
                RadiusMultiplier = GameConstants.OverchargeRadiusMultiplier
            });

            // Phase 5: SkillEvent buffer entity
            var skillEventEntity = em.CreateEntity();
            em.AddBuffer<SkillEvent>(skillEventEntity);

            // Phase 5 gap closure: Skill unlock gating
            // All skills default to UNLOCKED. Phase 6 tech tree will change defaults to false
            // and flip to true on Ship branch purchases.
            var skillUnlockEntity = em.CreateEntity(typeof(SkillUnlockData));
            em.SetComponentData(skillUnlockEntity, new SkillUnlockData
            {
                Skill1Unlocked = true,
                Skill2Unlocked = true,
                Skill3Unlocked = true,
                Skill4Unlocked = true
            });

            // Phase 5 gap closure: Runtime-mutable skill stats
            // Seeded from GameConstants. Phase 6 tech tree will modify these at runtime.
            var skillStatsEntity = em.CreateEntity(typeof(SkillStatsData));
            em.SetComponentData(skillStatsEntity, new SkillStatsData
            {
                LaserDamage = GameConstants.LaserBurstDamage,
                LaserCooldown = GameConstants.LaserBurstCooldown,
                LaserBeamHalfWidth = GameConstants.LaserBurstBeamHalfWidth,
                LaserDotDamagePerTick = GameConstants.LaserDotDamagePerTick,
                LaserDotTickInterval = GameConstants.LaserDotTickInterval,
                LaserDotDuration = GameConstants.LaserDotDuration,
                ChainDamage = GameConstants.ChainLightningDamage,
                ChainCooldown = GameConstants.ChainLightningCooldown,
                ChainMaxTargets = GameConstants.ChainLightningMaxTargets,
                ChainMaxDist = GameConstants.ChainLightningMaxChainDist,
                EmpDamage = GameConstants.EmpPulseDamage,
                EmpCooldown = GameConstants.EmpPulseCooldown,
                EmpRadius = GameConstants.EmpPulseRadius,
                EmpDotDamagePerTick = GameConstants.EmpDotDamagePerTick,
                EmpDotTickInterval = GameConstants.EmpDotTickInterval,
                EmpDotDuration = GameConstants.EmpDotDuration,
                OverchargeCooldown = GameConstants.OverchargeCooldown,
                OverchargeDuration = GameConstants.OverchargeDuration,
                OverchargeDamageMultiplier = GameConstants.OverchargeDamageMultiplier,
                OverchargeRadiusMultiplier = GameConstants.OverchargeRadiusMultiplier
            });

            Debug.Log("ECS Bootstrap complete: singletons created (GameState, Input, AsteroidSpawnTimer, MiningConfig, CollectionEventBuffer, DamageEventBuffer, DestructionEventBuffer, SkillInput, SkillCooldown, CritConfig, OverchargeBuff, SkillEventBuffer, SkillUnlock, SkillStats)");
        }
    }
}
