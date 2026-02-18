---
phase: 06-tech-tree-and-level-progression
plan: 01
subsystem: data-model
tags: [scriptableobject, ecs-singleton, save-system, tech-tree, level-progression]

# Dependency graph
requires:
  - phase: 05-ship-skills-and-advanced-damage
    provides: "SkillUnlockData, SkillStatsData singletons and ECSBootstrap foundation"
  - phase: 03-collection-economy-and-session
    provides: "SaveManager, SaveData with credits and session persistence"
provides:
  - "UpgradeNodeSO, TechTreeSO, LevelConfigSO ScriptableObject class definitions"
  - "UpgradeBranch and StatTarget enums for all modifiable game stats"
  - "PlayerBonusData and RunConfigData ECS singletons"
  - "Expanded SaveData v2 with full stat persistence and tech tree unlock array"
  - "SaveManager round-trips all ECS singletons (mining, crit, skills, economy, run config)"
  - "Save version migration v1->v2 with backward compat for pre-Phase-6 saves"
  - "SaveTechTreeState method for immediate persistence after purchases"
affects: [06-02-PLAN, 06-03-PLAN]

# Tech tracking
tech-stack:
  added: []
  patterns: [tiered-cost-multipliers, save-version-migration, locked-by-default-skills]

key-files:
  created:
    - Assets/Scripts/Data/UpgradeNodeSO.cs
    - Assets/Scripts/Data/TechTreeSO.cs
    - Assets/Scripts/Data/LevelConfigSO.cs
    - Assets/Scripts/ECS/Components/EconomyComponents.cs
  modified:
    - Assets/Scripts/Shared/GameEnums.cs
    - Assets/Scripts/Shared/GameConstants.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/Save/SaveData.cs
    - Assets/Scripts/MonoBehaviours/Save/SaveManager.cs

key-decisions:
  - "Skills default to locked in ECSBootstrap; save migration keeps them unlocked for pre-Phase-6 saves"
  - "Save v1->v2 migration uses threshold checks (< 0.1f) to detect zero-initialized fields from old saves"
  - "RunConfigData SpawnInterval/MaxActiveAsteroids/HPMultiplier left at defaults in LoadIntoECS -- level system overrides on run start"
  - "ComboMasteryMultiplier defaults to 1.0 (no bonus) in save, 1.5 in GameConstants -- distinguishes purchased from unpurchased"

patterns-established:
  - "Tiered cost pattern: BaseCost * TierMultipliers[tier-1] with static readonly array {1, 3, 8}"
  - "Save version migration: MigrateIfNeeded checks SaveVersion and fixes zero-valued fields to GameConstants defaults"
  - "Full singleton round-trip: AutoSave reads all ECS singletons into SaveData, LoadIntoECS restores them"

requirements-completed: [TECH-02, TECH-03, TECH-04, TECH-05, TECH-06, TECH-08, TECH-10, LEVL-04, LEVL-05]

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 6 Plan 01: Data Foundation Summary

**ScriptableObject data model for 5-branch tech tree with tiered costs, level configs with drop tables, new ECS economy singletons, and full save round-trip for all upgradeable stats**

## Performance

- **Duration:** 8 min
- **Tasks:** 2
- **Files created:** 4
- **Files modified:** 5

## Accomplishments
- Complete data model for tech tree: UpgradeNodeSO with prerequisites, tiered costs (1x/3x/8x), and StatEffect arrays targeting all 27 modifiable stats
- Level progression configs: LevelConfigSO with drop tables, HP multipliers, and advance thresholds for 5 levels
- New ECS singletons PlayerBonusData and RunConfigData bootstrapped with GameConstants defaults
- Full save system expansion: SaveData v2 persists all mining, skill, economy, and DoT stats with v1 backward compat migration
- Skills locked by default in ECSBootstrap; migration path keeps old saves functional

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ScriptableObject data model and enum definitions** - `8028a00` (feat)
2. **Task 2: Expand ECSBootstrap, SaveData, and SaveManager** - `fc2e3e5` (feat)

## Files Created/Modified
- `Assets/Scripts/Shared/GameEnums.cs` - Added UpgradeBranch (5 branches) and StatTarget (27 stats) enums
- `Assets/Scripts/Shared/GameConstants.cs` - Added economy defaults and 6 resource tier credit values
- `Assets/Scripts/Data/UpgradeNodeSO.cs` - Upgrade node SO with tiered cost computation, prerequisites, stat effects
- `Assets/Scripts/Data/TechTreeSO.cs` - Tech tree aggregation SO with StartNode and indexed AllNodes array
- `Assets/Scripts/Data/LevelConfigSO.cs` - Per-level config SO with drop tables, HP multipliers, spawn overrides
- `Assets/Scripts/ECS/Components/EconomyComponents.cs` - PlayerBonusData and RunConfigData IComponentData singletons
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Skills locked by default, creates PlayerBonusData and RunConfigData
- `Assets/Scripts/MonoBehaviours/Save/SaveData.cs` - v2 schema with full PlayerStatsData, SkillUnlocks, expanded fields
- `Assets/Scripts/MonoBehaviours/Save/SaveManager.cs` - Full singleton round-trip, v1->v2 migration, SaveTechTreeState method

## Decisions Made
- Skills default to locked (false) in ECSBootstrap -- Phase 6 tech tree Ship branch purchases flip to true. Save migration ensures backward compat for existing saves by keeping skills unlocked when no tech tree data exists.
- Save version migration uses threshold comparison (< 0.1f etc.) to detect zero-initialized fields from old v1 saves, applying GameConstants defaults for any that appear uninitialized.
- RunConfigData spawn/HP fields left at defaults in LoadIntoECS -- the level progression system will override these based on CurrentLevel at run start, avoiding stale values.
- ComboMasteryMultiplier defaults to 1.0 in SaveData (no bonus) vs 1.5 in GameConstants, distinguishing "never purchased" from "purchased the combo mastery upgrade".

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Data model complete: 06-02 (tech tree UI and purchase logic) and 06-03 (level progression and resource tiers) can now build on this foundation
- ScriptableObject instances (actual upgrade nodes, tech tree asset, level config assets) will be created by 06-02 and 06-03
- All ECS singletons ready for runtime modification by purchase and level systems

## Self-Check: PASSED

- All 9 files verified present on disk
- Commit 8028a00 verified in git log (Task 1)
- Commit fc2e3e5 verified in git log (Task 2)

---
*Phase: 06-tech-tree-and-level-progression*
*Completed: 2026-02-18*
