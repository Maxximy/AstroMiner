using Unity.Entities;

namespace ECS.Components
{
    /// <summary>
    /// Singleton: economy bonus modifiers applied by tech tree purchases.
    /// Created by ECSBootstrap with default (no bonus) values.
    /// Modified at runtime when Economy and Ship branch upgrades are purchased.
    /// </summary>
    public struct PlayerBonusData : IComponentData
    {
        /// <summary>Multiplier for all resource credit values. 1.0 = no bonus.</summary>
        public float ResourceMultiplier;

        /// <summary>Chance for a lucky strike (bonus minerals). 0.0 = no chance.</summary>
        public float LuckyStrikeChance;

        /// <summary>Combo mastery damage multiplier when chaining skills. 1.0 = no bonus.</summary>
        public float ComboMasteryMultiplier;

        /// <summary>Time window in seconds for combo skill chaining.</summary>
        public float ComboMasteryWindow;

        /// <summary>Timestamp of the last skill use for combo tracking.</summary>
        public float LastSkillUseTime;

        /// <summary>Number of skills used within the current combo window.</summary>
        public int SkillsUsedInWindow;

        /// <summary>Flat mineral drop count per asteroid. Starts at 1, +1 per upgrade.</summary>
        public int MineralDropCount;
    }

    /// <summary>
    /// Singleton: per-run configuration that can be modified by tech tree and level progression.
    /// Created by ECSBootstrap with GameConstants defaults.
    /// Modified when Run branch upgrades are purchased or level advances.
    /// </summary>
    public struct RunConfigData : IComponentData
    {
        /// <summary>Duration of a timed run in seconds.</summary>
        public float RunDuration;

        /// <summary>Seconds between asteroid spawns.</summary>
        public float SpawnInterval;

        /// <summary>Maximum simultaneous asteroid entities.</summary>
        public int MaxActiveAsteroids;

        /// <summary>HP multiplier for asteroids at the current level.</summary>
        public float AsteroidHPMultiplier;

        /// <summary>Current level number (1-5).</summary>
        public int CurrentLevel;

        // Per-tier drop weights for Burst-accessible weighted random in AsteroidSpawnSystem.
        // Set by PlayingState.Enter() from LevelConfigDefinitions based on CurrentLevel.

        /// <summary>Weight for Iron (tier 0) in the current level's drop table.</summary>
        public float TierWeight0;

        /// <summary>Weight for Copper (tier 1) in the current level's drop table.</summary>
        public float TierWeight1;

        /// <summary>Weight for Silver (tier 2) in the current level's drop table.</summary>
        public float TierWeight2;

        /// <summary>Weight for Cobalt (tier 3) in the current level's drop table.</summary>
        public float TierWeight3;

        /// <summary>Weight for Gold (tier 4) in the current level's drop table.</summary>
        public float TierWeight4;

        /// <summary>Weight for Titanium (tier 5) in the current level's drop table.</summary>
        public float TierWeight5;
    }
}
