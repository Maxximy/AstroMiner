namespace Data
{
    /// <summary>
    /// Static utility for asteroid visual classification.
    /// Prefab references are held by AsteroidVisualConfig (scene MonoBehaviour).
    /// </summary>
    public static class AsteroidVisualDefinitions
    {
        /// <summary>
        /// Classify asteroid MaxHP into a size class using GameConstants thresholds.
        /// </summary>
        public static AsteroidSize ClassifySize(float maxHP)
        {
            if (maxHP < GameConstants.AsteroidSizeSmallMaxHP) return AsteroidSize.Small;
            if (maxHP >= GameConstants.AsteroidSizeLargeMinHP) return AsteroidSize.Large;
            return AsteroidSize.Medium;
        }
    }
}
