using Data;
using ECS.Components;
using MonoBehaviours.Core;
using MonoBehaviours.Save;
using Unity.Entities;
using UnityEngine;

namespace States
{
    public class PlayingState : IGameState
    {
        private EntityManager em;
        private Entity gameStateEntity;
        private bool resolved;

        /// <summary>
        /// Static flag ensuring saved credits are loaded into ECS only once per session.
        /// Persists across state re-entries within the same application lifetime.
        /// </summary>
        private static bool saveLoaded = false;

        public void Enter(GameManager manager)
        {
            Debug.Log("Entering Playing state");

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("PlayingState: ECS world not available");
                return;
            }

            em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(GameStateData));
            if (query.CalculateEntityCount() == 0)
            {
                Debug.LogError("PlayingState: GameStateData singleton not found");
                return;
            }

            gameStateEntity = query.GetSingletonEntity();
            resolved = true;

            // On first run of the session, load saved credits from prior session
            if (!saveLoaded)
            {
                SaveManager.Instance?.LoadIntoECS();
                saveLoaded = true;
            }

            // Apply level configuration to ECS singletons before setting up the run
            ApplyLevelConfig();

            // Initialize timer for this run from RunConfigData
            var data = em.GetComponentData<GameStateData>(gameStateEntity);

            var runConfigQuery = em.CreateEntityQuery(typeof(RunConfigData));
            if (runConfigQuery.CalculateEntityCount() > 0)
            {
                var runConfig = runConfigQuery.GetSingleton<RunConfigData>();
                data.Timer = runConfig.RunDuration;
            }
            else
            {
                data.Timer = GameConstants.DefaultRunDuration; // Fallback
            }

            em.SetComponentData(gameStateEntity, data);

            // Snapshot credits at run start for "credits this run" calculation
            manager.CreditsAtRunStart = data.Credits;
        }

        /// <summary>
        /// Reads RunConfigData.CurrentLevel and applies the corresponding level configuration
        /// to RunConfigData (tier weights, HP multiplier) and AsteroidSpawnTimer (spawn settings).
        /// Called at the start of each run to ensure correct level parameters.
        /// </summary>
        private void ApplyLevelConfig()
        {
            var runConfigQuery = em.CreateEntityQuery(typeof(RunConfigData));
            if (runConfigQuery.CalculateEntityCount() == 0) return;

            var runConfigEntity = runConfigQuery.GetSingletonEntity();
            var runConfig = em.GetComponentData<RunConfigData>(runConfigEntity);

            // Get level configuration from definitions
            var levelConfig = LevelConfigDefinitions.GetLevelConfig(runConfig.CurrentLevel);

            // Apply HP multiplier
            runConfig.AsteroidHPMultiplier = levelConfig.AsteroidHPMultiplier;

            // Apply spawn interval override (keep RunConfigData defaults if -1)
            if (levelConfig.SpawnIntervalOverride > 0f)
                runConfig.SpawnInterval = levelConfig.SpawnIntervalOverride;
            else
                runConfig.SpawnInterval = GameConstants.DefaultSpawnInterval;

            // Apply max active asteroids override
            if (levelConfig.MaxActiveAsteroidsOverride > 0)
                runConfig.MaxActiveAsteroids = levelConfig.MaxActiveAsteroidsOverride;
            else
                runConfig.MaxActiveAsteroids = GameConstants.DefaultMaxAsteroids;

            // Reset all tier weights to 0
            runConfig.TierWeight0 = 0f;
            runConfig.TierWeight1 = 0f;
            runConfig.TierWeight2 = 0f;
            runConfig.TierWeight3 = 0f;
            runConfig.TierWeight4 = 0f;
            runConfig.TierWeight5 = 0f;

            // Apply drop table weights from level config
            foreach (var tw in levelConfig.DropTable)
            {
                switch (tw.TierIndex)
                {
                    case 0: runConfig.TierWeight0 = tw.Weight; break;
                    case 1: runConfig.TierWeight1 = tw.Weight; break;
                    case 2: runConfig.TierWeight2 = tw.Weight; break;
                    case 3: runConfig.TierWeight3 = tw.Weight; break;
                    case 4: runConfig.TierWeight4 = tw.Weight; break;
                    case 5: runConfig.TierWeight5 = tw.Weight; break;
                }
            }

            em.SetComponentData(runConfigEntity, runConfig);

            // Sync AsteroidSpawnTimer with RunConfigData values
            var spawnTimerQuery = em.CreateEntityQuery(typeof(AsteroidSpawnTimer));
            if (spawnTimerQuery.CalculateEntityCount() > 0)
            {
                var spawnTimerEntity = spawnTimerQuery.GetSingletonEntity();
                var spawnTimer = em.GetComponentData<AsteroidSpawnTimer>(spawnTimerEntity);
                spawnTimer.SpawnInterval = runConfig.SpawnInterval;
                spawnTimer.MaxActiveAsteroids = runConfig.MaxActiveAsteroids;
                em.SetComponentData(spawnTimerEntity, spawnTimer);
            }

            Debug.Log($"PlayingState: Applied level {runConfig.CurrentLevel} config -- HP mult: {levelConfig.AsteroidHPMultiplier}, Spawn: {runConfig.SpawnInterval}s, Max: {runConfig.MaxActiveAsteroids}");
        }

        public void Execute(GameManager manager)
        {
            if (!resolved) return;

            var data = em.GetComponentData<GameStateData>(gameStateEntity);
            data.Timer -= Time.deltaTime;

            if (data.Timer <= 0f)
            {
                data.Timer = 0f;
                em.SetComponentData(gameStateEntity, data);
                manager.TransitionTo(GamePhase.Collecting);
                return;
            }

            em.SetComponentData(gameStateEntity, data);
        }

        public void Exit(GameManager manager)
        {
            Debug.Log("Exiting Playing state");
        }
    }
}
