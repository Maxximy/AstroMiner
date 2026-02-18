using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    /// <summary>
    /// Categorizes damage events for visual styling.
    /// Normal = white text, Critical = yellow "CRIT!" with scale boost,
    /// DoT = orange italic, Skill = custom color from RGB bytes.
    /// Phase 5 will add crit/DoT/skill emission; Phase 4 uses Normal only.
    /// </summary>
    public enum DamageType : byte
    {
        Normal = 0,
        Critical = 1,
        DoT = 2,
        Skill = 3
    }

    /// <summary>
    /// Buffer element emitted by MiningDamageSystem on every damage tick.
    /// Consumed by DamagePopupManager (MonoBehaviour) to spawn floating damage numbers.
    /// All fields unmanaged for Burst compatibility.
    /// </summary>
    public struct DamageEvent : IBufferElementData
    {
        /// <summary>World position of the damaged asteroid.</summary>
        public float3 Position;

        /// <summary>Damage dealt this tick.</summary>
        public float Amount;

        /// <summary>Damage category for visual styling.</summary>
        public DamageType Type;

        /// <summary>Skill color red channel (Burst-safe, no managed Color).</summary>
        public byte ColorR;

        /// <summary>Skill color green channel.</summary>
        public byte ColorG;

        /// <summary>Skill color blue channel.</summary>
        public byte ColorB;
    }

    /// <summary>
    /// Buffer element emitted by MineralSpawnSystem when an asteroid reaches 0 HP.
    /// Consumed by ExplosionManager (MonoBehaviour) to spawn debris particle effects.
    /// All fields unmanaged for Burst compatibility.
    /// </summary>
    public struct DestructionEvent : IBufferElementData
    {
        /// <summary>World position where the asteroid died.</summary>
        public float3 Position;

        /// <summary>Asteroid visual scale for particle count scaling.</summary>
        public float Scale;

        /// <summary>Resource tier for future tier-specific explosion colors.</summary>
        public int ResourceTier;
    }

    /// <summary>
    /// Buffer element emitted by skill systems for VFX bridge consumption.
    /// Carries activation data so MonoBehaviour can render beam/lightning/blast/overcharge visuals.
    /// </summary>
    public struct SkillEvent : IBufferElementData
    {
        /// <summary>Skill type: 0=Laser, 1=Chain, 2=EMP, 3=Overcharge.</summary>
        public byte SkillType;

        /// <summary>Ship position (XZ).</summary>
        public float2 OriginPos;

        /// <summary>Mouse/target position (XZ).</summary>
        public float2 TargetPos;

        // Chain lightning target positions (XZ coords, up to 4 chain targets)
        public float2 Chain1;
        public float2 Chain2;
        public float2 Chain3;
        public float2 Chain4;

        /// <summary>Number of valid chain targets (0-4).</summary>
        public int ChainCount;

        /// <summary>For EMP blast radius visual.</summary>
        public float Radius;
    }
}