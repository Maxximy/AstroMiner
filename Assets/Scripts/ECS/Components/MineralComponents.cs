using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    /// <summary>
    /// Tag component to identify mineral entities.
    /// Used by pull, collection, and rendering systems.
    /// </summary>
    public struct MineralTag : IComponentData
    {
    }

    /// <summary>
    /// Resource data for a mineral entity.
    /// Determines credit value when collected.
    /// </summary>
    public struct MineralData : IComponentData
    {
        /// <summary>Resource tier index (0 = Iron, future tiers in Phase 6).</summary>
        public int ResourceTier;

        /// <summary>Credits awarded when this mineral is collected.</summary>
        public int CreditValue;
    }

    /// <summary>
    /// Movement data for mineral pull-toward-ship behavior.
    /// Speed increases over time via acceleration for satisfying collection feel.
    /// </summary>
    public struct MineralPullData : IComponentData
    {
        /// <summary>Current movement speed toward the ship (units/sec).</summary>
        public float CurrentSpeed;

        /// <summary>Acceleration applied each frame (units/sec^2).</summary>
        public float Acceleration;
    }

    /// <summary>
    /// Tag added to asteroid entities after minerals have been spawned from them.
    /// Prevents double-spawn: the asteroid persists one extra frame while ECB
    /// destruction queues, so without this guard the same dead asteroid would
    /// trigger mineral spawning twice.
    /// </summary>
    public struct MineralsSpawnedTag : IComponentData
    {
    }

    /// <summary>
    /// Buffer element for mineral collection events.
    /// Used by MonoBehaviour bridge systems to trigger SFX/VFX in Phase 4.
    /// </summary>
    public struct CollectionEvent : IBufferElementData
    {
        /// <summary>Resource tier of the collected mineral.</summary>
        public int ResourceTier;

        /// <summary>Credit value awarded.</summary>
        public int CreditValue;

        /// <summary>World position where collection occurred (for VFX spawn).</summary>
        public float3 Position;
    }
}