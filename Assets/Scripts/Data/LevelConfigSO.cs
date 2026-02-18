using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Per-level configuration defining drop tables, HP scaling, and advance thresholds.
    /// One instance per level (5 levels total). Referenced by the level progression system.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "AstroMiner/Level Config")]
    public class LevelConfigSO : ScriptableObject
    {
        /// <summary>Level number (1-5).</summary>
        [Range(1, 5)]
        public int LevelNumber = 1;

        /// <summary>
        /// Credits needed to purchase the "Advance to Level N+1" tech tree node.
        /// The Advance node in the Progression branch uses this as its cost.
        /// </summary>
        public long AdvanceCreditThreshold;

        /// <summary>
        /// Multiplier applied to asteroid max HP at this level.
        /// Level 1 = 1.0, scaling up for higher levels.
        /// </summary>
        public float AsteroidHPMultiplier = 1f;

        /// <summary>
        /// Override for spawn interval at this level. -1 means use default from GameConstants.
        /// </summary>
        public float SpawnIntervalOverride = -1f;

        /// <summary>
        /// Weighted random drop table defining which resource tiers appear at this level.
        /// Higher levels introduce rarer tiers.
        /// </summary>
        public ResourceTierWeight[] DropTable;

        /// <summary>
        /// Override for max active asteroids at this level. -1 means use default from GameConstants.
        /// </summary>
        public int MaxActiveAsteroidsOverride = -1;
    }

    /// <summary>
    /// A weighted entry in a level's drop table for resource tier selection.
    /// Used by the spawn system for weighted random tier picking.
    /// </summary>
    [Serializable]
    public struct ResourceTierWeight
    {
        /// <summary>Resource tier index (0=Iron, 1=Copper, 2=Silver, 3=Cobalt, 4=Gold, 5=Titanium).</summary>
        public int TierIndex;

        /// <summary>Relative probability weight for this tier in the drop table.</summary>
        public float Weight;
    }
}
