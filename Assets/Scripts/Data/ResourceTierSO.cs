using UnityEngine;

/// <summary>
/// ScriptableObject defining a resource tier's properties.
/// Phase 3 uses GameConstants defaults for the single Iron tier.
/// Phase 6 will use instances of this SO for tiered resources.
/// </summary>
[CreateAssetMenu(fileName = "NewResourceTier", menuName = "AstroMiner/Resource Tier")]
public class ResourceTierSO : ScriptableObject
{
    /// <summary>Display name of the tier (e.g., "Iron", "Gold").</summary>
    public string TierName;

    /// <summary>Zero-based tier index for sorting and lookup.</summary>
    public int TierIndex;

    /// <summary>Credits awarded per mineral of this tier.</summary>
    public int CreditValue;

    /// <summary>Number of minerals spawned when an asteroid of this tier is destroyed.</summary>
    public int MineralsPerAsteroid;

    /// <summary>Color applied to mineral GameObjects of this tier.</summary>
    public Color MineralColor;

    /// <summary>Emissive intensity for glow effect (0 = none, 4 = bright).</summary>
    [Range(0f, 4f)]
    public float EmissiveIntensity;
}
