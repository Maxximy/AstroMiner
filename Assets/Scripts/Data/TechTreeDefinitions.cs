using UnityEngine;

namespace Data
{
    /// <summary>
    /// Runtime data structure returned by TechTreeDefinitions.BuildTree().
    /// Contains all programmatically-created node instances and the start node index.
    /// </summary>
    public class TechTreeData
    {
        public UpgradeNodeSO[] AllNodes;
        public int StartNodeIndex;
    }

    /// <summary>
    /// Programmatic tech tree definition: constructs all ~40 UpgradeNodeSO instances at runtime
    /// using ScriptableObject.CreateInstance. Follows Option A from the plan: no .asset files needed.
    /// Graph positions use center-outward layout with 5 branches radiating from START at (0,0).
    /// </summary>
    public static class TechTreeDefinitions
    {
        // Branch angles (approximate radians from center)
        // Mining: upper-left (135 degrees)
        // Economy: upper-right (45 degrees)
        // Ship: left (180 degrees)
        // Run: lower-left (225 degrees)
        // Progression: right (0 degrees)

        /// <summary>
        /// Builds the complete tech tree with all ~40 nodes, resolving prerequisites.
        /// </summary>
        public static TechTreeData BuildTree()
        {
            // Create all node descriptors
            var descriptors = GetAllDescriptors();

            // Create UpgradeNodeSO instances
            var nodes = new UpgradeNodeSO[descriptors.Length];
            var idToIndex = new System.Collections.Generic.Dictionary<string, int>();

            for (int i = 0; i < descriptors.Length; i++)
            {
                var d = descriptors[i];
                var node = ScriptableObject.CreateInstance<UpgradeNodeSO>();
                node.NodeId = d.NodeId;
                node.DisplayName = d.DisplayName;
                node.Description = d.Description;
                node.Branch = d.Branch;
                node.BaseCost = d.BaseCost;
                node.TierLevel = d.TierLevel;
                node.Effects = d.Effects;
                node.GraphPosition = d.GraphPosition;
                node.SkillIndex = d.SkillIndex;
                node.name = d.NodeId;
                nodes[i] = node;
                idToIndex[d.NodeId] = i;
            }

            // Resolve prerequisites (second pass)
            for (int i = 0; i < descriptors.Length; i++)
            {
                var d = descriptors[i];
                if (d.PrerequisiteIds != null && d.PrerequisiteIds.Length > 0)
                {
                    var prereqs = new UpgradeNodeSO[d.PrerequisiteIds.Length];
                    for (int j = 0; j < d.PrerequisiteIds.Length; j++)
                    {
                        if (idToIndex.TryGetValue(d.PrerequisiteIds[j], out int idx))
                            prereqs[j] = nodes[idx];
                    }
                    nodes[i].Prerequisites = prereqs;
                }
                else
                {
                    nodes[i].Prerequisites = new UpgradeNodeSO[0];
                }
            }

            // Find START node index
            int startIndex = 0;
            if (idToIndex.TryGetValue("start", out int si))
                startIndex = si;

            return new TechTreeData
            {
                AllNodes = nodes,
                StartNodeIndex = startIndex
            };
        }

        private struct NodeDescriptor
        {
            public string NodeId;
            public string DisplayName;
            public string Description;
            public UpgradeBranch Branch;
            public int BaseCost;
            public int TierLevel;
            public string[] PrerequisiteIds;
            public StatEffect[] Effects;
            public Vector2 GraphPosition;
            public int SkillIndex;
        }

        private static NodeDescriptor[] GetAllDescriptors()
        {
            // Branch direction vectors (normalized then scaled by tier depth)
            // Mining: upper-left (135 deg)
            Vector2 miningDir = new Vector2(-0.707f, 0.707f);
            // Economy: upper-right (45 deg)
            Vector2 econDir = new Vector2(0.707f, 0.707f);
            // Ship: left (180 deg)
            Vector2 shipDir = new Vector2(-1f, 0f);
            // Run: lower-left (225 deg)
            Vector2 runDir = new Vector2(-0.707f, -0.707f);
            // Progression: right (0 deg)
            Vector2 progDir = new Vector2(1f, 0f);

            // Perpendicular offset for sub-branches
            Vector2 MiningPerp(float offset) => new Vector2(-miningDir.y, miningDir.x) * offset;
            Vector2 EconPerp(float offset) => new Vector2(-econDir.y, econDir.x) * offset;
            Vector2 ShipPerp(float offset) => new Vector2(-shipDir.y, shipDir.x) * offset;

            return new NodeDescriptor[]
            {
                // ============================================================
                // START NODE (index 0)
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "start",
                    DisplayName = "START",
                    Description = "The beginning of your journey.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 0, TierLevel = 1,
                    PrerequisiteIds = null,
                    Effects = new StatEffect[0],
                    GraphPosition = Vector2.zero,
                    SkillIndex = 0
                },

                // ============================================================
                // MINING BRANCH (11 nodes) -- upper-left
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "mining_radius_1",
                    DisplayName = "Circle Radius I",
                    Description = "Increases mining circle radius.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 500, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningRadius, Value = 0.5f } },
                    GraphPosition = miningDir * 1f + MiningPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_radius_2",
                    DisplayName = "Circle Radius II",
                    Description = "Further increases mining circle radius.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 800, TierLevel = 2,
                    PrerequisiteIds = new[] { "mining_radius_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningRadius, Value = 0.5f } },
                    GraphPosition = miningDir * 2f + MiningPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_radius_3",
                    DisplayName = "Circle Radius III",
                    Description = "Maximum mining circle radius upgrade.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 1200, TierLevel = 3,
                    PrerequisiteIds = new[] { "mining_radius_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningRadius, Value = 0.5f } },
                    GraphPosition = miningDir * 3f + MiningPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_damage_1",
                    DisplayName = "Mining Damage I",
                    Description = "Increases damage per mining tick.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 600, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningDamage, Value = 5f } },
                    GraphPosition = miningDir * 1f + MiningPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_damage_2",
                    DisplayName = "Mining Damage II",
                    Description = "Further increases mining tick damage.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 1000, TierLevel = 2,
                    PrerequisiteIds = new[] { "mining_damage_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningDamage, Value = 5f } },
                    GraphPosition = miningDir * 2f + MiningPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_rate_1",
                    DisplayName = "Mining Rate I",
                    Description = "Decreases time between mining ticks.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 700, TierLevel = 1,
                    PrerequisiteIds = new[] { "mining_damage_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningTickInterval, Value = 0.05f } },
                    GraphPosition = miningDir * 2f + MiningPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_rate_2",
                    DisplayName = "Mining Rate II",
                    Description = "Further decreases mining tick interval.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 1200, TierLevel = 2,
                    PrerequisiteIds = new[] { "mining_rate_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MiningTickInterval, Value = 0.05f } },
                    GraphPosition = miningDir * 3f + MiningPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_crit_chance",
                    DisplayName = "Crit Chance",
                    Description = "Increases critical hit chance by 5%.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 800, TierLevel = 1,
                    PrerequisiteIds = new[] { "mining_radius_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.CritChance, Value = 0.05f } },
                    GraphPosition = miningDir * 2f + MiningPerp(-1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_crit_multi",
                    DisplayName = "Crit Multiplier",
                    Description = "Increases critical hit damage multiplier.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 1000, TierLevel = 2,
                    PrerequisiteIds = new[] { "mining_crit_chance" },
                    Effects = new[] { new StatEffect { Target = StatTarget.CritMultiplier, Value = 0.5f } },
                    GraphPosition = miningDir * 3f + MiningPerp(-1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_dot_1",
                    DisplayName = "Burning I",
                    Description = "Increases burning damage over time.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 900, TierLevel = 1,
                    PrerequisiteIds = new[] { "mining_damage_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.DotDamage, Value = 2f } },
                    GraphPosition = miningDir * 3f + MiningPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "mining_dot_2",
                    DisplayName = "Burning II",
                    Description = "Extends burning duration.",
                    Branch = UpgradeBranch.Mining,
                    BaseCost = 1500, TierLevel = 2,
                    PrerequisiteIds = new[] { "mining_dot_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.DotDuration, Value = 1f } },
                    GraphPosition = miningDir * 4f + MiningPerp(0.5f),
                    SkillIndex = 0
                },

                // ============================================================
                // ECONOMY BRANCH (8 nodes) -- upper-right
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "econ_mult_1",
                    DisplayName = "Resource Multi I",
                    Description = "Increases resource credit value by 15%.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 500, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.ResourceMultiplier, Value = 0.15f } },
                    GraphPosition = econDir * 1f + EconPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_mult_2",
                    DisplayName = "Resource Multi II",
                    Description = "Increases resource credit value by another 15%.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 900, TierLevel = 2,
                    PrerequisiteIds = new[] { "econ_mult_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.ResourceMultiplier, Value = 0.15f } },
                    GraphPosition = econDir * 2f + EconPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_mult_3",
                    DisplayName = "Resource Multi III",
                    Description = "Increases resource credit value by 20%.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 1500, TierLevel = 3,
                    PrerequisiteIds = new[] { "econ_mult_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.ResourceMultiplier, Value = 0.20f } },
                    GraphPosition = econDir * 3f + EconPerp(-0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_lucky_1",
                    DisplayName = "Lucky Strike I",
                    Description = "8% chance for bonus minerals from asteroids.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 600, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.LuckyStrikeChance, Value = 0.08f } },
                    GraphPosition = econDir * 1f + EconPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_lucky_2",
                    DisplayName = "Lucky Strike II",
                    Description = "Additional 8% bonus mineral chance.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 1000, TierLevel = 2,
                    PrerequisiteIds = new[] { "econ_lucky_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.LuckyStrikeChance, Value = 0.08f } },
                    GraphPosition = econDir * 2f + EconPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_lucky_3",
                    DisplayName = "Lucky Strike III",
                    Description = "10% more bonus mineral chance.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 1500, TierLevel = 3,
                    PrerequisiteIds = new[] { "econ_lucky_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.LuckyStrikeChance, Value = 0.10f } },
                    GraphPosition = econDir * 3f + EconPerp(0.5f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_abundance_1",
                    DisplayName = "Abundance I",
                    Description = "Faster asteroid spawns and higher cap.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 700, TierLevel = 1,
                    PrerequisiteIds = new[] { "econ_mult_1" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.SpawnRateReduction, Value = 0.2f },
                        new StatEffect { Target = StatTarget.MaxAsteroidsBonus, Value = 10f }
                    },
                    GraphPosition = econDir * 2f + EconPerp(-1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_abundance_2",
                    DisplayName = "Abundance II",
                    Description = "Even faster spawns and higher asteroid cap.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 1200, TierLevel = 2,
                    PrerequisiteIds = new[] { "econ_abundance_1" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.SpawnRateReduction, Value = 0.2f },
                        new StatEffect { Target = StatTarget.MaxAsteroidsBonus, Value = 10f }
                    },
                    GraphPosition = econDir * 3f + EconPerp(-1.2f),
                    SkillIndex = 0
                },

                // -- Mineral Yield sub-branch (5 nodes, chained from start) --
                new NodeDescriptor
                {
                    NodeId = "econ_yield_1",
                    DisplayName = "Mineral Yield I",
                    Description = "+1 mineral drop per asteroid.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 400, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MineralDropCount, Value = 1f } },
                    GraphPosition = econDir * 1f + EconPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_yield_2",
                    DisplayName = "Mineral Yield II",
                    Description = "+1 mineral drop per asteroid.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 600, TierLevel = 1,
                    PrerequisiteIds = new[] { "econ_yield_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MineralDropCount, Value = 1f } },
                    GraphPosition = econDir * 2f + EconPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_yield_3",
                    DisplayName = "Mineral Yield III",
                    Description = "+1 mineral drop per asteroid.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 350, TierLevel = 2,
                    PrerequisiteIds = new[] { "econ_yield_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MineralDropCount, Value = 1f } },
                    GraphPosition = econDir * 3f + EconPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_yield_4",
                    DisplayName = "Mineral Yield IV",
                    Description = "+1 mineral drop per asteroid.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 500, TierLevel = 2,
                    PrerequisiteIds = new[] { "econ_yield_3" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MineralDropCount, Value = 1f } },
                    GraphPosition = econDir * 4f + EconPerp(1.2f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "econ_yield_5",
                    DisplayName = "Mineral Yield V",
                    Description = "+1 mineral drop per asteroid.",
                    Branch = UpgradeBranch.Economy,
                    BaseCost = 300, TierLevel = 3,
                    PrerequisiteIds = new[] { "econ_yield_4" },
                    Effects = new[] { new StatEffect { Target = StatTarget.MineralDropCount, Value = 1f } },
                    GraphPosition = econDir * 5f + EconPerp(1.2f),
                    SkillIndex = 0
                },

                // ============================================================
                // SHIP BRANCH (13 nodes) -- left
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "ship_laser_1",
                    DisplayName = "Laser Burst",
                    Description = "Unlocks the Laser Burst skill.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 600, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.SkillUnlock, Value = 1f } },
                    GraphPosition = shipDir * 1f + ShipPerp(0.7f),
                    SkillIndex = 1
                },
                new NodeDescriptor
                {
                    NodeId = "ship_laser_2",
                    DisplayName = "Laser Burst II",
                    Description = "Increases Laser Burst damage.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1000, TierLevel = 2,
                    PrerequisiteIds = new[] { "ship_laser_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.LaserDamage, Value = 50f } },
                    GraphPosition = shipDir * 2f + ShipPerp(0.7f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_laser_3",
                    DisplayName = "Laser Burst III",
                    Description = "Major laser damage boost and faster cooldown.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1800, TierLevel = 3,
                    PrerequisiteIds = new[] { "ship_laser_2" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.LaserDamage, Value = 75f },
                        new StatEffect { Target = StatTarget.LaserCooldown, Value = 1f }
                    },
                    GraphPosition = shipDir * 3f + ShipPerp(0.7f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_chain_1",
                    DisplayName = "Chain Lightning",
                    Description = "Unlocks the Chain Lightning skill.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 600, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.SkillUnlock, Value = 1f } },
                    GraphPosition = shipDir * 1f + ShipPerp(-0.7f),
                    SkillIndex = 2
                },
                new NodeDescriptor
                {
                    NodeId = "ship_chain_2",
                    DisplayName = "Chain Lightning II",
                    Description = "More chain damage and an extra target.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1000, TierLevel = 2,
                    PrerequisiteIds = new[] { "ship_chain_1" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.ChainDamage, Value = 20f },
                        new StatEffect { Target = StatTarget.ChainTargets, Value = 1f }
                    },
                    GraphPosition = shipDir * 2f + ShipPerp(-0.7f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_chain_3",
                    DisplayName = "Chain Lightning III",
                    Description = "More chain damage and faster cooldown.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1800, TierLevel = 3,
                    PrerequisiteIds = new[] { "ship_chain_2" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.ChainDamage, Value = 30f },
                        new StatEffect { Target = StatTarget.ChainCooldown, Value = 2f }
                    },
                    GraphPosition = shipDir * 3f + ShipPerp(-0.7f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_emp_1",
                    DisplayName = "EMP Pulse",
                    Description = "Unlocks the EMP Pulse skill.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 700, TierLevel = 1,
                    PrerequisiteIds = new[] { "ship_laser_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.SkillUnlock, Value = 1f } },
                    GraphPosition = shipDir * 2f + ShipPerp(0f),
                    SkillIndex = 3
                },
                new NodeDescriptor
                {
                    NodeId = "ship_emp_2",
                    DisplayName = "EMP Pulse II",
                    Description = "More EMP damage and wider radius.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1200, TierLevel = 2,
                    PrerequisiteIds = new[] { "ship_emp_1" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.EmpDamage, Value = 30f },
                        new StatEffect { Target = StatTarget.EmpRadius, Value = 1f }
                    },
                    GraphPosition = shipDir * 3f + ShipPerp(0f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_emp_3",
                    DisplayName = "EMP Pulse III",
                    Description = "Maximum EMP power and faster cooldown.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 2000, TierLevel = 3,
                    PrerequisiteIds = new[] { "ship_emp_2" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.EmpDamage, Value = 40f },
                        new StatEffect { Target = StatTarget.EmpCooldown, Value = 2f }
                    },
                    GraphPosition = shipDir * 4f + ShipPerp(0f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_overcharge_1",
                    DisplayName = "Overcharge",
                    Description = "Unlocks the Overcharge skill.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 800, TierLevel = 1,
                    PrerequisiteIds = new[] { "ship_emp_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.SkillUnlock, Value = 1f } },
                    GraphPosition = shipDir * 3f + ShipPerp(1.4f),
                    SkillIndex = 4
                },
                new NodeDescriptor
                {
                    NodeId = "ship_overcharge_2",
                    DisplayName = "Overcharge II",
                    Description = "Longer Overcharge buff duration.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 1500, TierLevel = 2,
                    PrerequisiteIds = new[] { "ship_overcharge_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.OverchargeDuration, Value = 2f } },
                    GraphPosition = shipDir * 4f + ShipPerp(1.4f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_overcharge_3",
                    DisplayName = "Overcharge III",
                    Description = "Stronger Overcharge and faster cooldown.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 2500, TierLevel = 3,
                    PrerequisiteIds = new[] { "ship_overcharge_2" },
                    Effects = new[]
                    {
                        new StatEffect { Target = StatTarget.OverchargeDamageMultiplier, Value = 0.5f },
                        new StatEffect { Target = StatTarget.OverchargeCooldown, Value = 2f }
                    },
                    GraphPosition = shipDir * 5f + ShipPerp(1.4f),
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "ship_combo",
                    DisplayName = "Combo Mastery",
                    Description = "Chaining skills deals bonus damage.",
                    Branch = UpgradeBranch.Ship,
                    BaseCost = 2000, TierLevel = 2,
                    PrerequisiteIds = new[] { "ship_emp_1", "ship_overcharge_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.ComboMastery, Value = 1.5f } },
                    GraphPosition = shipDir * 4f + ShipPerp(-1.0f),
                    SkillIndex = 0
                },

                // ============================================================
                // RUN BRANCH (3 nodes) -- lower-left
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "run_time_1",
                    DisplayName = "Level Time I",
                    Description = "Adds 5 seconds to each run.",
                    Branch = UpgradeBranch.Run,
                    BaseCost = 500, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.RunDuration, Value = 5f } },
                    GraphPosition = runDir * 1f,
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "run_time_2",
                    DisplayName = "Level Time II",
                    Description = "Adds 5 more seconds to each run.",
                    Branch = UpgradeBranch.Run,
                    BaseCost = 900, TierLevel = 2,
                    PrerequisiteIds = new[] { "run_time_1" },
                    Effects = new[] { new StatEffect { Target = StatTarget.RunDuration, Value = 5f } },
                    GraphPosition = runDir * 2f,
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "run_time_3",
                    DisplayName = "Level Time III",
                    Description = "Adds 10 seconds to each run.",
                    Branch = UpgradeBranch.Run,
                    BaseCost = 1500, TierLevel = 3,
                    PrerequisiteIds = new[] { "run_time_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.RunDuration, Value = 10f } },
                    GraphPosition = runDir * 3f,
                    SkillIndex = 0
                },

                // ============================================================
                // PROGRESSION BRANCH (4 nodes) -- right
                // ============================================================
                new NodeDescriptor
                {
                    NodeId = "prog_level_2",
                    DisplayName = "Advance to Level 2",
                    Description = "Unlocks Level 2: Copper minerals and tougher asteroids.",
                    Branch = UpgradeBranch.Progression,
                    BaseCost = 3000, TierLevel = 1,
                    PrerequisiteIds = new[] { "start" },
                    Effects = new[] { new StatEffect { Target = StatTarget.AdvanceLevel, Value = 1f } },
                    GraphPosition = progDir * 1.5f,
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "prog_level_3",
                    DisplayName = "Advance to Level 3",
                    Description = "Unlocks Level 3: Silver minerals and harder asteroids.",
                    Branch = UpgradeBranch.Progression,
                    BaseCost = 8000, TierLevel = 1,
                    PrerequisiteIds = new[] { "prog_level_2" },
                    Effects = new[] { new StatEffect { Target = StatTarget.AdvanceLevel, Value = 1f } },
                    GraphPosition = progDir * 3f,
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "prog_level_4",
                    DisplayName = "Advance to Level 4",
                    Description = "Unlocks Level 4: Cobalt minerals and tough asteroids.",
                    Branch = UpgradeBranch.Progression,
                    BaseCost = 20000, TierLevel = 1,
                    PrerequisiteIds = new[] { "prog_level_3" },
                    Effects = new[] { new StatEffect { Target = StatTarget.AdvanceLevel, Value = 1f } },
                    GraphPosition = progDir * 4.5f,
                    SkillIndex = 0
                },
                new NodeDescriptor
                {
                    NodeId = "prog_level_5",
                    DisplayName = "Advance to Level 5",
                    Description = "Unlocks Level 5: Gold and Titanium minerals!",
                    Branch = UpgradeBranch.Progression,
                    BaseCost = 50000, TierLevel = 1,
                    PrerequisiteIds = new[] { "prog_level_4" },
                    Effects = new[] { new StatEffect { Target = StatTarget.AdvanceLevel, Value = 1f } },
                    GraphPosition = progDir * 6f,
                    SkillIndex = 0
                },
            };
        }
    }
}
