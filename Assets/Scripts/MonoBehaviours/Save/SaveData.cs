using System;

namespace MonoBehaviours.Save
{
    /// <summary>
    /// Serializable save data for JSON persistence.
    /// Contains all player progress that persists across sessions.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Save file version for migration support.
        /// v1 = Phase 3-5 (credits + placeholder fields)
        /// v2 = Phase 6 (full tech tree state, expanded stats, skill unlocks)
        /// </summary>
        public int SaveVersion = 2;

        /// <summary>
        /// Persistent credits across all runs (ECON-03).
        /// </summary>
        public long TotalCredits;

        /// <summary>
        /// Current level (1-5). Used by level progression system.
        /// </summary>
        public int CurrentLevel = 1;

        /// <summary>
        /// Tech tree unlock state. Array index maps to TechTreeSO.AllNodes index.
        /// true = purchased, false = not purchased.
        /// Empty array for fresh saves; sized to match tech tree node count on first purchase.
        /// </summary>
        public bool[] TechTreeUnlocks = new bool[0];

        /// <summary>
        /// Skill unlock state for quick access (separate from TechTreeUnlocks).
        /// Index 0 = Laser Burst, 1 = Chain Lightning, 2 = EMP Pulse, 3 = Overcharge.
        /// </summary>
        public bool[] SkillUnlocks = new bool[4];

        /// <summary>
        /// All upgradeable player stats. Defaults match base (un-upgraded) values.
        /// Modified by tech tree purchases and restored into ECS singletons on load.
        /// </summary>
        public PlayerStatsData Stats = new PlayerStatsData();
    }

    /// <summary>
    /// Serializable player stats for save persistence.
    /// Defaults match the base (un-upgraded) GameConstants values.
    /// Phase 6 tech tree modifies these when upgrades are purchased.
    /// </summary>
    [Serializable]
    public class PlayerStatsData
    {
        // -- Mining --
        public float MiningRadius = 2.5f;
        public float DamagePerTick = 10f;
        public float TickInterval = 0.25f;
        public float CritChance = 0.08f;
        public float CritMultiplier = 2f;

        // -- Economy --
        public float ResourceMultiplier = 1f;
        public float LuckyStrikeChance = 0f;

        // -- Run --
        public float RunDuration = 20f;

        // -- Laser Burst --
        public float LaserDamage = 150f;
        public float LaserCooldown = 8f;

        // -- Chain Lightning --
        public float ChainDamage = 60f;
        public float ChainCooldown = 10f;
        public int ChainMaxTargets = 4;
        public float ChainMaxDist = 5f;

        // -- EMP Pulse --
        public float EmpDamage = 80f;
        public float EmpCooldown = 12f;
        public float EmpRadius = 4f;

        // -- Overcharge --
        public float OverchargeCooldown = 15f;
        public float OverchargeDuration = 5f;
        public float OverchargeDamageMultiplier = 2f;
        public float OverchargeRadiusMultiplier = 1.5f;

        // -- DoT (Laser) --
        public float LaserDotDamagePerTick = 5f;
        public float LaserDotTickInterval = 0.5f;
        public float LaserDotDuration = 3f;

        // -- DoT (EMP) --
        public float EmpDotDamagePerTick = 3f;
        public float EmpDotTickInterval = 0.5f;
        public float EmpDotDuration = 2f;

        // -- Combo Mastery --
        public float ComboMasteryMultiplier = 1f;
    }
}
