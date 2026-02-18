namespace Data
{
    /// <summary>
    /// Static runtime level configuration data for 5 levels with drop tables and HP multipliers.
    /// Used by managed code (PlayingState, renderers) to apply level settings.
    /// Drop table weights are written into RunConfigData for Burst-compiled systems.
    /// </summary>
    public static class LevelConfigDefinitions
    {
        /// <summary>
        /// Runtime level configuration (managed struct, not Burst-compatible).
        /// </summary>
        public struct LevelConfig
        {
            public int LevelNumber;
            public float AsteroidHPMultiplier;
            public float SpawnIntervalOverride; // -1 means use default
            public int MaxActiveAsteroidsOverride; // -1 means use default
            public TierWeight[] DropTable;
        }

        /// <summary>
        /// Weighted entry for a resource tier in a level's drop table.
        /// </summary>
        public struct TierWeight
        {
            public int TierIndex;
            public float Weight;
        }

        private static readonly LevelConfig[] Levels = new LevelConfig[]
        {
            // Level 1: Iron only
            new LevelConfig
            {
                LevelNumber = 1,
                AsteroidHPMultiplier = 1.0f,
                SpawnIntervalOverride = -1f,
                MaxActiveAsteroidsOverride = -1,
                DropTable = new TierWeight[]
                {
                    new TierWeight { TierIndex = 0, Weight = 100f }
                }
            },
            // Level 2: Iron + Copper
            new LevelConfig
            {
                LevelNumber = 2,
                AsteroidHPMultiplier = 1.3f,
                SpawnIntervalOverride = -1f,
                MaxActiveAsteroidsOverride = -1,
                DropTable = new TierWeight[]
                {
                    new TierWeight { TierIndex = 0, Weight = 70f },
                    new TierWeight { TierIndex = 1, Weight = 30f }
                }
            },
            // Level 3: Iron + Copper + Silver
            new LevelConfig
            {
                LevelNumber = 3,
                AsteroidHPMultiplier = 1.7f,
                SpawnIntervalOverride = 1.3f,
                MaxActiveAsteroidsOverride = -1,
                DropTable = new TierWeight[]
                {
                    new TierWeight { TierIndex = 0, Weight = 40f },
                    new TierWeight { TierIndex = 1, Weight = 35f },
                    new TierWeight { TierIndex = 2, Weight = 25f }
                }
            },
            // Level 4: Iron + Copper + Silver + Cobalt
            new LevelConfig
            {
                LevelNumber = 4,
                AsteroidHPMultiplier = 2.2f,
                SpawnIntervalOverride = 1.1f,
                MaxActiveAsteroidsOverride = 60,
                DropTable = new TierWeight[]
                {
                    new TierWeight { TierIndex = 0, Weight = 20f },
                    new TierWeight { TierIndex = 1, Weight = 25f },
                    new TierWeight { TierIndex = 2, Weight = 25f },
                    new TierWeight { TierIndex = 3, Weight = 30f }
                }
            },
            // Level 5: All 6 tiers
            new LevelConfig
            {
                LevelNumber = 5,
                AsteroidHPMultiplier = 3.0f,
                SpawnIntervalOverride = 0.9f,
                MaxActiveAsteroidsOverride = 70,
                DropTable = new TierWeight[]
                {
                    new TierWeight { TierIndex = 0, Weight = 10f },
                    new TierWeight { TierIndex = 1, Weight = 15f },
                    new TierWeight { TierIndex = 2, Weight = 20f },
                    new TierWeight { TierIndex = 3, Weight = 20f },
                    new TierWeight { TierIndex = 4, Weight = 20f },
                    new TierWeight { TierIndex = 5, Weight = 15f }
                }
            }
        };

        /// <summary>
        /// Get level configuration for a given level (1-5). Clamps to valid range.
        /// </summary>
        public static LevelConfig GetLevelConfig(int level)
        {
            int index = level - 1;
            if (index < 0) index = 0;
            if (index >= Levels.Length) index = Levels.Length - 1;
            return Levels[index];
        }
    }
}
