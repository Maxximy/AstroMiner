---
phase: 06-tech-tree-and-level-progression
plan: 03
subsystem: gameplay
tags: [level-progression, resource-tiers, economy-bonuses, drop-tables, mineral-colors]

# Dependency graph
requires:
  - phase: 06-tech-tree-and-level-progression
    provides: "RunConfigData and PlayerBonusData ECS singletons, GameConstants tier credit values, LevelConfigSO and ResourceTierSO class definitions"
provides:
  - "5 levels with distinct weighted drop tables (Iron-only to all 6 tiers)"
  - "6 resource tiers with distinct mineral colors: grey, orange, white, blue, yellow, purple"
  - "Per-level asteroid HP scaling via RunConfigData.AsteroidHPMultiplier"
  - "HP-based asteroid size scaling in AsteroidRenderer"
  - "ResourceMultiplier and LuckyStrike economy bonus application in MineralCollectionSystem"
  - "Run duration reads from tech-tree-modifiable RunConfigData singleton"
  - "PlayingState applies level config to all ECS singletons at run start"
  - "LevelConfigDefinitions and ResourceTierDefinitions static runtime data classes"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: [weighted-random-tier-selection, burst-accessible-tier-weights, hp-based-size-scaling]

key-files:
  created:
    - Assets/Scripts/Data/LevelConfigDefinitions.cs
    - Assets/Scripts/Data/ResourceTierDefinitions.cs
  modified:
    - Assets/Scripts/ECS/Components/AsteroidComponents.cs
    - Assets/Scripts/ECS/Components/EconomyComponents.cs
    - Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs
    - Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs
    - Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs
    - Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs
    - Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/States/PlayingState.cs

key-decisions:
  - "Tier weights stored as 6 float fields in RunConfigData for Burst accessibility -- no managed arrays in IComponentData"
  - "PlayingState.ApplyLevelConfig writes level config into both RunConfigData and AsteroidSpawnTimer at run start"
  - "HP-based asteroid size uses linear scaling: 30% of HP increase maps to size increase"
  - "MineralSpawnSystem uses if/else chains instead of switch for Burst-safe tier lookups"
  - "LuckyStrike doubles finalCredits (after ResourceMultiplier) not baseCredits"

patterns-established:
  - "Level config flow: LevelConfigDefinitions -> PlayingState.ApplyLevelConfig -> RunConfigData/AsteroidSpawnTimer -> AsteroidSpawnSystem reads at runtime"
  - "Tier weight pattern: 6 float fields TierWeight0-5 in RunConfigData, Burst-compiled weighted random in AsteroidSpawnSystem.PickResourceTier"
  - "Per-tier mineral visuals: MineralRenderer reads MineralData.ResourceTier on entity discovery, applies color from ResourceTierDefinitions"

requirements-completed: [LEVL-01, LEVL-02, LEVL-03, ASTR-06]

# Metrics
duration: 6min
completed: 2026-02-18
---

# Phase 6 Plan 03: Level Progression and Resource Tiers Summary

**5 levels with weighted drop tables, 6 color-coded resource tiers (Iron grey through Titanium purple), HP-based asteroid scaling, and ResourceMultiplier/LuckyStrike economy bonuses applied during mineral collection**

## Performance

- **Duration:** 6 min
- **Started:** 2026-02-18T15:36:53Z
- **Completed:** 2026-02-18T15:42:30Z
- **Tasks:** 2
- **Files created:** 2
- **Files modified:** 9

## Accomplishments
- Complete level progression: 5 levels with distinct weighted drop tables, from Iron-only (Level 1) to all 6 tiers (Level 5)
- 6 resource tiers with distinct mineral colors (grey/orange/white/blue/yellow/purple) and emissive intensities
- Asteroids scale in HP (1x to 3x) and visual size with level progression, with additional tier-based HP bonus
- Economy bonuses (ResourceMultiplier, LuckyStrike) actively modify credit earnings during mineral collection
- Run duration reads from the tech-tree-modifiable RunConfigData singleton instead of hardcoded constant
- PlayingState applies full level config (drop table, HP mult, spawn settings) to ECS singletons at every run start

## Task Commits

Each task was committed atomically:

1. **Task 1: Expand asteroid spawning with per-level HP, resource tier assignment, and size scaling** - `29de730` (feat)
2. **Task 2: Integrate economy bonuses (ResourceMultiplier, LuckyStrike) and RunConfigData** - `487862b` (feat)

## Files Created/Modified
- `Assets/Scripts/Data/LevelConfigDefinitions.cs` - Static 5-level config data with weighted drop tables, HP multipliers, spawn overrides
- `Assets/Scripts/Data/ResourceTierDefinitions.cs` - Static 6-tier resource data with colors, credit values, emissive intensities, mineral count ranges
- `Assets/Scripts/ECS/Components/AsteroidComponents.cs` - Added AsteroidResourceTier component for per-asteroid tier tracking
- `Assets/Scripts/ECS/Components/EconomyComponents.cs` - Added TierWeight0-5 fields to RunConfigData for Burst-accessible weighted random
- `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs` - Reads RunConfigData for tier weights and HP multiplier, weighted random tier selection
- `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` - Reads asteroid tier for per-tier credit values and mineral counts
- `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` - Applies ResourceMultiplier and LuckyStrike bonuses on collection
- `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` - Per-tier mineral colors and emissive from ResourceTierDefinitions
- `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` - HP-based asteroid size scaling on entity discovery
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Initialize RunConfigData with Level 1 tier weights
- `Assets/Scripts/States/PlayingState.cs` - ApplyLevelConfig method, RunDuration from RunConfigData, AsteroidSpawnTimer sync

## Decisions Made
- Tier weights stored as 6 individual float fields (TierWeight0-5) in RunConfigData rather than an array, since IComponentData must be fully unmanaged for Burst compatibility. PlayingState (managed code) writes these from LevelConfigDefinitions at run start.
- HP-based asteroid size scaling uses linear formula: baseScale * (1 + (hpRatio - 1) * 0.3), so doubling HP increases size by ~30%, keeping visuals proportional without extreme size differences.
- LuckyStrike doubles the final credit value (after ResourceMultiplier application) to stack multiplicatively, making the combination of both upgrades increasingly rewarding.
- MineralSpawnSystem uses if/else chains for tier-to-credit and tier-to-mineral-count lookups instead of switch expressions, ensuring Burst compatibility without managed type concerns.
- PlayingState.ApplyLevelConfig resets spawn settings to defaults before applying overrides, so levels without overrides (SpawnIntervalOverride = -1) correctly use GameConstants defaults.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Level progression system fully integrated end-to-end
- All 6 resource tiers with distinct visual and economic properties
- Economy bonuses (ResourceMultiplier, LuckyStrike) active during gameplay
- When 06-02 (tech tree UI and purchase logic) is complete, the full upgrade-play loop will be functional: purchase upgrades -> start run -> level config applied -> play with bonuses -> earn credits -> upgrade more
- Level 1 gameplay is backward compatible with Phase 5 (Iron-only, default HP)

## Self-Check: PASSED

- All 11 source files verified present on disk
- Commit 29de730 verified in git log (Task 1)
- Commit 487862b verified in git log (Task 2)
- SUMMARY.md verified present at .planning/phases/06-tech-tree-and-level-progression/06-03-SUMMARY.md

---
*Phase: 06-tech-tree-and-level-progression*
*Completed: 2026-02-18*
