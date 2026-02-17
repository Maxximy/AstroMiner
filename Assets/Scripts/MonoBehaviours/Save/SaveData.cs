using System;

/// <summary>
/// Serializable save data for JSON persistence.
/// Contains all player progress that persists across sessions.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>
    /// Save file version for future migration support (SAVE-05).
    /// Increment when save format changes; add migration logic in SaveManager.Load().
    /// </summary>
    public int SaveVersion = 1;

    /// <summary>
    /// Persistent credits across all runs (ECON-03).
    /// Note: JsonUtility handles long as a number. If deserialization issues arise on
    /// certain platforms, consider switching to double and casting to/from long.
    /// </summary>
    public long TotalCredits;

    /// <summary>
    /// Current level -- Phase 6 will use for level progression.
    /// </summary>
    public int CurrentLevel = 1;

    /// <summary>
    /// PLACEHOLDER for Phase 6 tech tree state (SAVE-03).
    /// Empty array now; Phase 6 will size it to match the tech tree node count.
    /// Additive -- no migration needed when expanding.
    /// </summary>
    public bool[] TechTreeUnlocks = new bool[0];

    /// <summary>
    /// PLACEHOLDER for Phase 6 player stats persistence (SAVE-03).
    /// Defaults match base stats. Phase 6 will read/write these after tech tree purchases.
    /// </summary>
    public PlayerStatsData Stats = new PlayerStatsData();
}

/// <summary>
/// Serializable player stats for save persistence.
/// Defaults match the base (un-upgraded) values.
/// </summary>
[Serializable]
public class PlayerStatsData
{
    public float MiningRadius = 1f;
    public float DamageMultiplier = 1f;
}
