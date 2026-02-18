using Unity.Entities;
using Unity.Mathematics;

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
