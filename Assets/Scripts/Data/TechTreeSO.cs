using UnityEngine;

namespace Data
{
    /// <summary>
    /// Aggregation ScriptableObject holding all upgrade nodes in the tech tree.
    /// Contains the START node reference and an ordered array of all nodes.
    /// The index in AllNodes corresponds to the save array index for persistence.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTechTree", menuName = "AstroMiner/Tech Tree")]
    public class TechTreeSO : ScriptableObject
    {
        /// <summary>
        /// The center START node (free, already purchased at game start).
        /// This node has no prerequisites and serves as the root for all branches.
        /// </summary>
        public UpgradeNodeSO StartNode;

        /// <summary>
        /// All upgrade nodes in the tree, ordered for save array indexing.
        /// The index of each node in this array maps to TechTreeUnlocks[index] in SaveData.
        /// </summary>
        public UpgradeNodeSO[] AllNodes;

        /// <summary>
        /// Returns the index of a node by its NodeId, or -1 if not found.
        /// Used for save array indexing.
        /// </summary>
        public int GetNodeIndex(string nodeId)
        {
            if (AllNodes == null) return -1;

            for (int i = 0; i < AllNodes.Length; i++)
            {
                if (AllNodes[i] != null && AllNodes[i].NodeId == nodeId)
                    return i;
            }

            return -1;
        }
    }
}
