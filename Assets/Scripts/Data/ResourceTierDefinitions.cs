using UnityEngine;

namespace Data
{
    /// <summary>
    /// Static runtime resource tier data for 6 tiers.
    /// Provides credit values, mineral colors, emissive intensities, and mineral count ranges.
    /// Used by MineralRenderer for per-tier visual styling and by MineralSpawnSystem for mineral counts.
    /// </summary>
    public static class ResourceTierDefinitions
    {
        /// <summary>
        /// Runtime data for a single resource tier (managed, not Burst-compatible).
        /// </summary>
        public struct ResourceTierInfo
        {
            public string Name;
            public int CreditValue;
            public int MineralsMin;
            public int MineralsMax;
            public Color MineralColor;
            public float EmissiveIntensity;
        }

        private static readonly ResourceTierInfo[] Tiers = new ResourceTierInfo[]
        {
            // Tier 0: Iron - grey
            new ResourceTierInfo
            {
                Name = "Iron",
                CreditValue = GameConstants.IronCreditValue,
                MineralsMin = 3, MineralsMax = 8,
                MineralColor = new Color(0.6f, 0.6f, 0.6f),
                EmissiveIntensity = 2.0f
            },
            // Tier 1: Copper - orange
            new ResourceTierInfo
            {
                Name = "Copper",
                CreditValue = GameConstants.CopperCreditValue,
                MineralsMin = 3, MineralsMax = 7,
                MineralColor = new Color(0.9f, 0.5f, 0.2f),
                EmissiveIntensity = 2.0f
            },
            // Tier 2: Silver - white
            new ResourceTierInfo
            {
                Name = "Silver",
                CreditValue = GameConstants.SilverCreditValue,
                MineralsMin = 2, MineralsMax = 6,
                MineralColor = new Color(0.9f, 0.9f, 0.95f),
                EmissiveIntensity = 2.5f
            },
            // Tier 3: Cobalt - blue
            new ResourceTierInfo
            {
                Name = "Cobalt",
                CreditValue = GameConstants.CobaltCreditValue,
                MineralsMin = 2, MineralsMax = 5,
                MineralColor = new Color(0.3f, 0.5f, 0.95f),
                EmissiveIntensity = 2.5f
            },
            // Tier 4: Gold - yellow
            new ResourceTierInfo
            {
                Name = "Gold",
                CreditValue = GameConstants.GoldCreditValue,
                MineralsMin = 1, MineralsMax = 4,
                MineralColor = new Color(1.0f, 0.85f, 0.2f),
                EmissiveIntensity = 3.0f
            },
            // Tier 5: Titanium - purple/magenta
            new ResourceTierInfo
            {
                Name = "Titanium",
                CreditValue = GameConstants.TitaniumCreditValue,
                MineralsMin = 1, MineralsMax = 3,
                MineralColor = new Color(0.8f, 0.3f, 0.9f),
                EmissiveIntensity = 3.5f
            }
        };

        /// <summary>
        /// Get tier info for a given tier index (0-5). Clamps to valid range.
        /// </summary>
        public static ResourceTierInfo GetTier(int tierIndex)
        {
            if (tierIndex < 0) tierIndex = 0;
            if (tierIndex >= Tiers.Length) tierIndex = Tiers.Length - 1;
            return Tiers[tierIndex];
        }

        /// <summary>Total number of defined resource tiers.</summary>
        public static int TierCount => Tiers.Length;
    }
}
