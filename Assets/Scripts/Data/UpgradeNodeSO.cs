using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Individual upgrade node in the tech tree.
    /// Each node belongs to a branch, has prerequisites, a tiered cost, and stat effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgradeNode", menuName = "AstroMiner/Upgrade Node")]
    public class UpgradeNodeSO : ScriptableObject
    {
        /// <summary>Unique string ID for save persistence and lookup.</summary>
        public string NodeId;

        /// <summary>Display name shown in the UI (e.g., "Circle Radius I").</summary>
        public string DisplayName;

        /// <summary>Effect description shown in the tooltip.</summary>
        [TextArea]
        public string Description;

        /// <summary>Which branch this node belongs to.</summary>
        public UpgradeBranch Branch;

        /// <summary>Base credit cost before tier multiplier is applied.</summary>
        public int BaseCost;

        /// <summary>Tier level (1, 2, or 3). Maps to cost multipliers 1x, 3x, 8x (TECH-08).</summary>
        [Range(1, 3)]
        public int TierLevel = 1;

        /// <summary>Nodes that must be purchased before this node becomes available (TECH-07).</summary>
        public UpgradeNodeSO[] Prerequisites;

        /// <summary>Array of stat modifications this node applies when purchased.</summary>
        public StatEffect[] Effects;

        /// <summary>XY position in the center-outward graph layout.</summary>
        public Vector2 GraphPosition;

        /// <summary>
        /// Only used when one of the Effects targets StatTarget.SkillUnlock.
        /// 1-4 maps to skill slots (1=Laser, 2=Chain, 3=EMP, 4=Overcharge).
        /// </summary>
        public int SkillIndex;

        /// <summary>Tier cost multipliers: Tier 1 = 1x, Tier 2 = 3x, Tier 3 = 8x.</summary>
        private static readonly int[] TierMultipliers = { 1, 3, 8 };

        /// <summary>
        /// The actual credit cost after applying the tier multiplier.
        /// BaseCost * TierMultiplier for the node's tier level.
        /// </summary>
        public int ActualCost => BaseCost * TierMultipliers[Mathf.Clamp(TierLevel - 1, 0, 2)];
    }

    /// <summary>
    /// A single stat modification applied by an upgrade node.
    /// Value interpretation (additive or multiplicative) depends on the target stat.
    /// </summary>
    [Serializable]
    public struct StatEffect
    {
        /// <summary>Which stat this effect modifies.</summary>
        public StatTarget Target;

        /// <summary>The modification value (additive or multiplicative depending on target).</summary>
        public float Value;
    }
}
