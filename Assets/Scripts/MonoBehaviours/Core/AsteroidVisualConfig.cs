using UnityEngine;

namespace MonoBehaviours.Core
{
    /// <summary>
    /// Per-tier asteroid and destruction VFX prefab references.
    /// Assigned in the Inspector on a scene GameObject.
    /// </summary>
    [System.Serializable]
    public class TierVisualPrefabs
    {
        [Tooltip("Small asteroid prefab (low HP)")]
        public GameObject Small;

        [Tooltip("Medium asteroid prefab (mid HP)")]
        public GameObject Medium;

        [Tooltip("Large asteroid prefab (high HP)")]
        public GameObject Large;

        [Tooltip("Particle effect played on asteroid destruction")]
        public GameObject Destroy;

        /// <summary>
        /// Get the asteroid prefab for the given size class.
        /// Returns null if not assigned.
        /// </summary>
        public GameObject GetBySize(AsteroidSize size)
        {
            return size switch
            {
                AsteroidSize.Small => Small,
                AsteroidSize.Medium => Medium,
                AsteroidSize.Large => Large,
                _ => Small
            };
        }
    }

    /// <summary>
    /// Scene-based config holding per-tier asteroid mesh prefabs and destruction VFX prefabs.
    /// Place on a GameObject in the scene and assign prefabs in the Inspector.
    /// Array index = tier index (0=Iron, 1=Copper, 2=Silver, 3=Cobalt, 4=Gold, 5=Titanium).
    /// Accessed at runtime via AsteroidVisualConfig.Instance.
    /// </summary>
    public class AsteroidVisualConfig : MonoBehaviour
    {
        public static AsteroidVisualConfig Instance { get; private set; }

        [Header("Per-Tier Prefabs (index = tier: 0=Iron, 1=Copper, 2=Silver, 3=Cobalt, 4=Gold, 5=Titanium)")]
        [SerializeField] private TierVisualPrefabs[] Tiers = new TierVisualPrefabs[6];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("AsteroidVisualConfig: Duplicate instance destroyed.");
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Get the asteroid prefab for the given tier and size.
        /// Returns null if the tier is out of range or the prefab slot is unassigned.
        /// </summary>
        public GameObject GetAsteroidPrefab(int tierIndex, AsteroidSize size)
        {
            if (tierIndex < 0 || tierIndex >= Tiers.Length) return null;
            var tier = Tiers[tierIndex];
            if (tier == null) return null;
            return tier.GetBySize(size);
        }

        /// <summary>
        /// Get the destruction VFX prefab for the given tier.
        /// Returns null if the tier is out of range or the prefab slot is unassigned.
        /// </summary>
        public GameObject GetDestroyPrefab(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= Tiers.Length) return null;
            return Tiers[tierIndex]?.Destroy;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
