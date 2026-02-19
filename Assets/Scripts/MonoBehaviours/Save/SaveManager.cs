using System;
using System.IO;
using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace MonoBehaviours.Save
{
    /// <summary>
    /// Singleton MonoBehaviour managing JSON save/load with WebGL IndexedDB flush.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// Persists across scenes via DontDestroyOnLoad.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveFileName = "astrominer_save.json";
        private const string PlayerPrefsKey = "astrominer_save";

        private SaveData _currentSave;

        /// <summary>
        /// The currently loaded save data. Always non-null after Awake.
        /// </summary>
        public SaveData CurrentSave => _currentSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SaveManager>();
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
            DontDestroyOnLoad(gameObject);
            _currentSave = Load();
            Debug.Log($"SaveManager: Initialized. Loaded credits: {_currentSave.TotalCredits}, version: {_currentSave.SaveVersion}, level: {_currentSave.CurrentLevel}");
        }

        /// <summary>
        /// Save data to JSON at Application.persistentDataPath.
        /// Falls back to PlayerPrefs if file I/O fails.
        /// Flushes IndexedDB on WebGL via WebGLHelper.
        /// </summary>
        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);

            try
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);
                File.WriteAllText(path, json);
                WebGLHelper.FlushSaveData();
                Debug.Log($"SaveManager: Saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: File.WriteAllText failed ({e.Message}). Falling back to PlayerPrefs.");
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Load save data from JSON file, with PlayerPrefs fallback.
        /// Returns a fresh SaveData if no save exists or on error.
        /// Applies version migration for old saves.
        /// </summary>
        public SaveData Load()
        {
            SaveData data = null;

            try
            {
                string path = Path.Combine(Application.persistentDataPath, SaveFileName);

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"SaveManager: Loaded from file ({path})");
                }
                else if (PlayerPrefs.HasKey(PlayerPrefsKey))
                {
                    string json = PlayerPrefs.GetString(PlayerPrefsKey);
                    data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("SaveManager: Loaded from PlayerPrefs fallback");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: Load failed ({e.Message}). Returning fresh save.");
            }

            if (data == null)
            {
                Debug.Log("SaveManager: No existing save found. Starting fresh.");
                return new SaveData();
            }

            // Apply version migration
            data = MigrateIfNeeded(data);
            return data;
        }

        /// <summary>
        /// Migrates old save versions to the current format.
        /// v1 -> v2: Initializes all new Phase 6 fields to defaults.
        /// Backward compat: pre-Phase-6 saves keep skills unlocked.
        /// </summary>
        private SaveData MigrateIfNeeded(SaveData data)
        {
            if (data.SaveVersion < 2)
            {
                Debug.Log($"SaveManager: Migrating save from v{data.SaveVersion} to v2");

                // Initialize expanded stats with defaults if they're at zero
                // (JsonUtility will deserialize missing fields as default(T))
                if (data.Stats == null)
                    data.Stats = new PlayerStatsData();

                // Fix any zero-valued stats that should have defaults
                // (v1 saves had MiningRadius=1f and DamageMultiplier=1f only)
                if (data.Stats.MiningRadius < 0.1f) data.Stats.MiningRadius = GameConstants.DefaultMiningRadius;
                if (data.Stats.DamagePerTick < 0.1f) data.Stats.DamagePerTick = GameConstants.DefaultDamagePerTick;
                if (data.Stats.TickInterval < 0.01f) data.Stats.TickInterval = GameConstants.DefaultTickInterval;
                if (data.Stats.CritChance < 0.001f) data.Stats.CritChance = GameConstants.CritChance;
                if (data.Stats.CritMultiplier < 0.1f) data.Stats.CritMultiplier = GameConstants.CritMultiplier;
                if (data.Stats.ResourceMultiplier < 0.1f) data.Stats.ResourceMultiplier = GameConstants.DefaultResourceMultiplier;
                if (data.Stats.RunDuration < 1f) data.Stats.RunDuration = GameConstants.DefaultRunDuration;
                if (data.Stats.LaserDamage < 1f) data.Stats.LaserDamage = GameConstants.LaserBurstDamage;
                if (data.Stats.LaserCooldown < 0.1f) data.Stats.LaserCooldown = GameConstants.LaserBurstCooldown;
                if (data.Stats.ChainDamage < 1f) data.Stats.ChainDamage = GameConstants.ChainLightningDamage;
                if (data.Stats.ChainCooldown < 0.1f) data.Stats.ChainCooldown = GameConstants.ChainLightningCooldown;
                if (data.Stats.ChainMaxTargets < 1) data.Stats.ChainMaxTargets = GameConstants.ChainLightningMaxTargets;
                if (data.Stats.ChainMaxDist < 0.1f) data.Stats.ChainMaxDist = GameConstants.ChainLightningMaxChainDist;
                if (data.Stats.EmpDamage < 1f) data.Stats.EmpDamage = GameConstants.EmpPulseDamage;
                if (data.Stats.EmpCooldown < 0.1f) data.Stats.EmpCooldown = GameConstants.EmpPulseCooldown;
                if (data.Stats.EmpRadius < 0.1f) data.Stats.EmpRadius = GameConstants.EmpPulseRadius;
                if (data.Stats.OverchargeCooldown < 0.1f) data.Stats.OverchargeCooldown = GameConstants.OverchargeCooldown;
                if (data.Stats.OverchargeDuration < 0.1f) data.Stats.OverchargeDuration = GameConstants.OverchargeDuration;
                if (data.Stats.OverchargeDamageMultiplier < 0.1f) data.Stats.OverchargeDamageMultiplier = GameConstants.OverchargeDamageMultiplier;
                if (data.Stats.OverchargeRadiusMultiplier < 0.1f) data.Stats.OverchargeRadiusMultiplier = GameConstants.OverchargeRadiusMultiplier;
                if (data.Stats.LaserDotDamagePerTick < 0.1f) data.Stats.LaserDotDamagePerTick = GameConstants.LaserDotDamagePerTick;
                if (data.Stats.LaserDotTickInterval < 0.01f) data.Stats.LaserDotTickInterval = GameConstants.LaserDotTickInterval;
                if (data.Stats.LaserDotDuration < 0.1f) data.Stats.LaserDotDuration = GameConstants.LaserDotDuration;
                if (data.Stats.EmpDotDamagePerTick < 0.1f) data.Stats.EmpDotDamagePerTick = GameConstants.EmpDotDamagePerTick;
                if (data.Stats.EmpDotTickInterval < 0.01f) data.Stats.EmpDotTickInterval = GameConstants.EmpDotTickInterval;
                if (data.Stats.EmpDotDuration < 0.1f) data.Stats.EmpDotDuration = GameConstants.EmpDotDuration;
                if (data.Stats.ComboMasteryMultiplier < 0.1f) data.Stats.ComboMasteryMultiplier = 1f;
                if (data.Stats.MineralDropCount < 1) data.Stats.MineralDropCount = 1;

                // Initialize SkillUnlocks array
                if (data.SkillUnlocks == null || data.SkillUnlocks.Length < 4)
                    data.SkillUnlocks = new bool[4];

                // Backward compat: pre-Phase-6 saves have no tech tree data,
                // so keep all skills unlocked (they were unlocked in Phase 5).
                // Research pitfall #1: old saves must not lose skill access.
                if (data.TechTreeUnlocks == null || data.TechTreeUnlocks.Length == 0)
                {
                    data.SkillUnlocks[0] = true;
                    data.SkillUnlocks[1] = true;
                    data.SkillUnlocks[2] = true;
                    data.SkillUnlocks[3] = true;
                    Debug.Log("SaveManager: Migration -- pre-Phase-6 save, keeping all skills unlocked");
                }

                if (data.CurrentLevel < 1) data.CurrentLevel = 1;

                data.SaveVersion = 2;
                Debug.Log("SaveManager: Migration to v2 complete");
            }

            return data;
        }

        /// <summary>
        /// Auto-save: reads current state from all ECS singletons,
        /// updates the in-memory save, and persists to disk.
        /// Called on run end (GameOverState.Enter).
        /// </summary>
        public void AutoSave()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("SaveManager: AutoSave skipped -- ECS world not available.");
                return;
            }

            var em = world.EntityManager;

            // Save credits from GameStateData
            var gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
            if (gameStateQuery.CalculateEntityCount() > 0)
            {
                var gameState = gameStateQuery.GetSingleton<GameStateData>();
                _currentSave.TotalCredits = gameState.Credits;
            }

            // Save skill unlock state
            var skillUnlockQuery = em.CreateEntityQuery(typeof(SkillUnlockData));
            if (skillUnlockQuery.CalculateEntityCount() > 0)
            {
                var unlocks = skillUnlockQuery.GetSingleton<SkillUnlockData>();
                if (_currentSave.SkillUnlocks == null || _currentSave.SkillUnlocks.Length < 4)
                    _currentSave.SkillUnlocks = new bool[4];
                _currentSave.SkillUnlocks[0] = unlocks.Skill1Unlocked;
                _currentSave.SkillUnlocks[1] = unlocks.Skill2Unlocked;
                _currentSave.SkillUnlocks[2] = unlocks.Skill3Unlocked;
                _currentSave.SkillUnlocks[3] = unlocks.Skill4Unlocked;
            }

            // Save mining config
            var miningQuery = em.CreateEntityQuery(typeof(MiningConfigData));
            if (miningQuery.CalculateEntityCount() > 0)
            {
                var mining = miningQuery.GetSingleton<MiningConfigData>();
                _currentSave.Stats.MiningRadius = mining.Radius;
                _currentSave.Stats.DamagePerTick = mining.DamagePerTick;
                _currentSave.Stats.TickInterval = mining.TickInterval;
            }

            // Save crit config
            var critQuery = em.CreateEntityQuery(typeof(CritConfigData));
            if (critQuery.CalculateEntityCount() > 0)
            {
                var crit = critQuery.GetSingleton<CritConfigData>();
                _currentSave.Stats.CritChance = crit.CritChance;
                _currentSave.Stats.CritMultiplier = crit.CritMultiplier;
            }

            // Save skill stats
            var skillStatsQuery = em.CreateEntityQuery(typeof(SkillStatsData));
            if (skillStatsQuery.CalculateEntityCount() > 0)
            {
                var skills = skillStatsQuery.GetSingleton<SkillStatsData>();
                _currentSave.Stats.LaserDamage = skills.LaserDamage;
                _currentSave.Stats.LaserCooldown = skills.LaserCooldown;
                _currentSave.Stats.ChainDamage = skills.ChainDamage;
                _currentSave.Stats.ChainCooldown = skills.ChainCooldown;
                _currentSave.Stats.ChainMaxTargets = skills.ChainMaxTargets;
                _currentSave.Stats.ChainMaxDist = skills.ChainMaxDist;
                _currentSave.Stats.EmpDamage = skills.EmpDamage;
                _currentSave.Stats.EmpCooldown = skills.EmpCooldown;
                _currentSave.Stats.EmpRadius = skills.EmpRadius;
                _currentSave.Stats.OverchargeCooldown = skills.OverchargeCooldown;
                _currentSave.Stats.OverchargeDuration = skills.OverchargeDuration;
                _currentSave.Stats.OverchargeDamageMultiplier = skills.OverchargeDamageMultiplier;
                _currentSave.Stats.OverchargeRadiusMultiplier = skills.OverchargeRadiusMultiplier;
                _currentSave.Stats.LaserDotDamagePerTick = skills.LaserDotDamagePerTick;
                _currentSave.Stats.LaserDotTickInterval = skills.LaserDotTickInterval;
                _currentSave.Stats.LaserDotDuration = skills.LaserDotDuration;
                _currentSave.Stats.EmpDotDamagePerTick = skills.EmpDotDamagePerTick;
                _currentSave.Stats.EmpDotTickInterval = skills.EmpDotTickInterval;
                _currentSave.Stats.EmpDotDuration = skills.EmpDotDuration;
            }

            // Save economy bonuses
            var bonusQuery = em.CreateEntityQuery(typeof(PlayerBonusData));
            if (bonusQuery.CalculateEntityCount() > 0)
            {
                var bonus = bonusQuery.GetSingleton<PlayerBonusData>();
                _currentSave.Stats.ResourceMultiplier = bonus.ResourceMultiplier;
                _currentSave.Stats.LuckyStrikeChance = bonus.LuckyStrikeChance;
                _currentSave.Stats.ComboMasteryMultiplier = bonus.ComboMasteryMultiplier;
                _currentSave.Stats.MineralDropCount = bonus.MineralDropCount;
            }

            // Save run config
            var runConfigQuery = em.CreateEntityQuery(typeof(RunConfigData));
            if (runConfigQuery.CalculateEntityCount() > 0)
            {
                var runConfig = runConfigQuery.GetSingleton<RunConfigData>();
                _currentSave.Stats.RunDuration = runConfig.RunDuration;
                _currentSave.CurrentLevel = runConfig.CurrentLevel;
            }

            Save(_currentSave);
            Debug.Log($"SaveManager: Auto-saved. Credits: {_currentSave.TotalCredits}, Level: {_currentSave.CurrentLevel}");
        }

        /// <summary>
        /// Load saved state into all ECS singletons.
        /// Called on first run start to restore progress from previous session.
        /// </summary>
        public void LoadIntoECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("SaveManager: LoadIntoECS skipped -- ECS world not available.");
                return;
            }

            var em = world.EntityManager;

            // Restore credits
            var gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
            if (gameStateQuery.CalculateEntityCount() > 0)
            {
                var entity = gameStateQuery.GetSingletonEntity();
                var data = em.GetComponentData<GameStateData>(entity);
                data.Credits = _currentSave.TotalCredits;
                em.SetComponentData(entity, data);
            }

            // Restore skill unlocks
            if (_currentSave.SkillUnlocks != null && _currentSave.SkillUnlocks.Length == 4)
            {
                var skillUnlockQuery = em.CreateEntityQuery(typeof(SkillUnlockData));
                if (skillUnlockQuery.CalculateEntityCount() > 0)
                {
                    var entity = skillUnlockQuery.GetSingletonEntity();
                    var unlocks = em.GetComponentData<SkillUnlockData>(entity);
                    unlocks.Skill1Unlocked = _currentSave.SkillUnlocks[0];
                    unlocks.Skill2Unlocked = _currentSave.SkillUnlocks[1];
                    unlocks.Skill3Unlocked = _currentSave.SkillUnlocks[2];
                    unlocks.Skill4Unlocked = _currentSave.SkillUnlocks[3];
                    em.SetComponentData(entity, unlocks);
                }
            }

            // Restore mining config
            var miningQuery = em.CreateEntityQuery(typeof(MiningConfigData));
            if (miningQuery.CalculateEntityCount() > 0)
            {
                var entity = miningQuery.GetSingletonEntity();
                var mining = em.GetComponentData<MiningConfigData>(entity);
                mining.Radius = _currentSave.Stats.MiningRadius;
                mining.DamagePerTick = _currentSave.Stats.DamagePerTick;
                mining.TickInterval = _currentSave.Stats.TickInterval;
                em.SetComponentData(entity, mining);
            }

            // Restore crit config
            var critQuery = em.CreateEntityQuery(typeof(CritConfigData));
            if (critQuery.CalculateEntityCount() > 0)
            {
                var entity = critQuery.GetSingletonEntity();
                var crit = em.GetComponentData<CritConfigData>(entity);
                crit.CritChance = _currentSave.Stats.CritChance;
                crit.CritMultiplier = _currentSave.Stats.CritMultiplier;
                em.SetComponentData(entity, crit);
            }

            // Restore skill stats
            var skillStatsQuery = em.CreateEntityQuery(typeof(SkillStatsData));
            if (skillStatsQuery.CalculateEntityCount() > 0)
            {
                var entity = skillStatsQuery.GetSingletonEntity();
                var skills = em.GetComponentData<SkillStatsData>(entity);
                skills.LaserDamage = _currentSave.Stats.LaserDamage;
                skills.LaserCooldown = _currentSave.Stats.LaserCooldown;
                skills.ChainDamage = _currentSave.Stats.ChainDamage;
                skills.ChainCooldown = _currentSave.Stats.ChainCooldown;
                skills.ChainMaxTargets = _currentSave.Stats.ChainMaxTargets;
                skills.ChainMaxDist = _currentSave.Stats.ChainMaxDist;
                skills.EmpDamage = _currentSave.Stats.EmpDamage;
                skills.EmpCooldown = _currentSave.Stats.EmpCooldown;
                skills.EmpRadius = _currentSave.Stats.EmpRadius;
                skills.OverchargeCooldown = _currentSave.Stats.OverchargeCooldown;
                skills.OverchargeDuration = _currentSave.Stats.OverchargeDuration;
                skills.OverchargeDamageMultiplier = _currentSave.Stats.OverchargeDamageMultiplier;
                skills.OverchargeRadiusMultiplier = _currentSave.Stats.OverchargeRadiusMultiplier;
                skills.LaserDotDamagePerTick = _currentSave.Stats.LaserDotDamagePerTick;
                skills.LaserDotTickInterval = _currentSave.Stats.LaserDotTickInterval;
                skills.LaserDotDuration = _currentSave.Stats.LaserDotDuration;
                skills.EmpDotDamagePerTick = _currentSave.Stats.EmpDotDamagePerTick;
                skills.EmpDotTickInterval = _currentSave.Stats.EmpDotTickInterval;
                skills.EmpDotDuration = _currentSave.Stats.EmpDotDuration;
                em.SetComponentData(entity, skills);
            }

            // Restore economy bonuses
            var bonusQuery = em.CreateEntityQuery(typeof(PlayerBonusData));
            if (bonusQuery.CalculateEntityCount() > 0)
            {
                var entity = bonusQuery.GetSingletonEntity();
                var bonus = em.GetComponentData<PlayerBonusData>(entity);
                bonus.ResourceMultiplier = _currentSave.Stats.ResourceMultiplier;
                bonus.LuckyStrikeChance = _currentSave.Stats.LuckyStrikeChance;
                bonus.ComboMasteryMultiplier = _currentSave.Stats.ComboMasteryMultiplier;
                bonus.MineralDropCount = _currentSave.Stats.MineralDropCount;
                bonus.ComboMasteryWindow = GameConstants.DefaultComboMasteryWindow;
                bonus.LastSkillUseTime = 0f;
                bonus.SkillsUsedInWindow = 0;
                em.SetComponentData(entity, bonus);
            }

            // Restore run config
            var runConfigQuery = em.CreateEntityQuery(typeof(RunConfigData));
            if (runConfigQuery.CalculateEntityCount() > 0)
            {
                var entity = runConfigQuery.GetSingletonEntity();
                var runConfig = em.GetComponentData<RunConfigData>(entity);
                runConfig.RunDuration = _currentSave.Stats.RunDuration;
                runConfig.CurrentLevel = _currentSave.CurrentLevel;
                // SpawnInterval, MaxActiveAsteroids, and AsteroidHPMultiplier
                // are set by the level progression system based on CurrentLevel.
                // Keep defaults here; level system overrides on run start.
                em.SetComponentData(entity, runConfig);
            }

            Debug.Log($"SaveManager: Loaded into ECS. Credits: {_currentSave.TotalCredits}, Level: {_currentSave.CurrentLevel}");
        }

        /// <summary>
        /// Persist tech tree unlock state immediately after a purchase.
        /// Called by TechTreeController after each node purchase.
        /// </summary>
        public void SaveTechTreeState(bool[] unlocks)
        {
            if (unlocks != null)
            {
                _currentSave.TechTreeUnlocks = unlocks;
            }
            Save(_currentSave);
            Debug.Log($"SaveManager: Tech tree state saved ({unlocks?.Length ?? 0} nodes)");
        }
    }
}
