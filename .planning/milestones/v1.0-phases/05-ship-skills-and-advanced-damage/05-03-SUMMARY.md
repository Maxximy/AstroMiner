---
phase: 05-ship-skills-and-advanced-damage
plan: 03
subsystem: ecs, skills
tags: [unity-ecs, dots, skill-system, singleton, runtime-mutable, tech-tree-scaffolding]

# Dependency graph
requires:
  - phase: 05-01
    provides: "4 skill systems (Laser, Chain, EMP, Overcharge) with cooldowns, crits, DoT"
  - phase: 05-02
    provides: "Skill bar UI, VFX, and audio integration"
provides:
  - "SkillUnlockData singleton for per-skill unlock gating"
  - "SkillStatsData singleton with all runtime-mutable skill parameters"
  - "Skill systems read damage/cooldown/radius/DoT from SkillStatsData instead of GameConstants"
  - "SkillBarController hides locked skill slots and reads cooldowns from SkillStatsData"
affects: [06-tech-tree-and-level-progression]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Runtime-mutable ECS singleton pattern: compile-time constants seed singleton, systems read singleton at runtime"
    - "Unlock gating pattern: UI and systems both guard on SkillUnlockData for defense-in-depth"

key-files:
  created: []
  modified:
    - Assets/Scripts/ECS/Components/SkillComponents.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs
    - Assets/Scripts/ECS/Systems/LaserBurstSystem.cs
    - Assets/Scripts/ECS/Systems/ChainLightningSystem.cs
    - Assets/Scripts/ECS/Systems/EmpPulseSystem.cs
    - Assets/Scripts/ECS/Systems/OverchargeSystem.cs

key-decisions:
  - "All skills default to unlocked -- Phase 6 tech tree flips to locked-by-default"
  - "SkillCooldownData.MaxCooldown fields left in place but unused -- cleanup deferred to Phase 6"
  - "OverchargeSystem unlock guard inside activation conditional (not top-level) to allow buff tick to continue for active buffs"

patterns-established:
  - "Runtime-mutable singleton: GameConstants seeds ECSBootstrap, systems read singleton, Phase 6 modifies singleton"
  - "Defense-in-depth unlock gating: UI hides slots + systems guard activation on same SkillUnlockData"

requirements-completed: [SKIL-07, SKIL-08, SKIL-01, SKIL-02, SKIL-03, SKIL-04, SKIL-05, SKIL-06, DMGS-01, DMGS-02, DMGS-03]

# Metrics
duration: 5min
completed: 2026-02-18
---

# Phase 5 Plan 3: Gap Closure Summary

**SkillUnlockData and SkillStatsData ECS singletons enabling runtime skill unlock gating and stat modification for Phase 6 tech tree**

## Performance

- **Duration:** 5 min
- **Started:** 2026-02-18
- **Completed:** 2026-02-18
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Added SkillUnlockData and SkillStatsData IComponentData structs with all skill parameters
- Bootstrapped both singletons from GameConstants defaults with all skills unlocked
- SkillBarController shows/hides slots based on unlock state and reads cooldowns from SkillStatsData
- All 4 skill systems read damage, cooldown, radius, and DoT from SkillStatsData instead of GameConstants
- All 4 skill systems guard activation on SkillUnlockData for defense-in-depth

## Task Commits

Each task was committed atomically:

1. **Task 1: Add SkillUnlockData and SkillStatsData singletons with unlock gating** - `a8d37f5` (feat)
2. **Task 2: Migrate skill systems to read from SkillStatsData singleton** - `787c226` (feat)

## Files Created/Modified
- `Assets/Scripts/ECS/Components/SkillComponents.cs` - Added SkillUnlockData and SkillStatsData structs
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Bootstrap both new singletons from GameConstants
- `Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs` - Unlock gating, slot visibility, stats-based cooldown display
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Pass slot root GameObjects to SkillBarController
- `Assets/Scripts/ECS/Systems/LaserBurstSystem.cs` - Read damage/beam/DoT from SkillStatsData, guard on unlock
- `Assets/Scripts/ECS/Systems/ChainLightningSystem.cs` - Read damage/targets/dist from SkillStatsData, guard on unlock
- `Assets/Scripts/ECS/Systems/EmpPulseSystem.cs` - Read damage/radius/DoT from SkillStatsData, guard on unlock
- `Assets/Scripts/ECS/Systems/OverchargeSystem.cs` - Read cooldown/duration from SkillStatsData, guard on unlock

## Decisions Made
- All skills default to unlocked so gameplay is unchanged before Phase 6 tech tree
- SkillCooldownData.MaxCooldown fields left in place (no harm, cleanup trivial for Phase 6)
- OverchargeSystem unlock guard placed inside the activation conditional rather than at the top of OnUpdate, so active buff duration still ticks down even if the skill becomes locked mid-buff

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 5 fully complete with all verification gaps closed
- Phase 6 tech tree can set SkillUnlockData.SkillNUnlocked to false for locked-by-default
- Phase 6 tech tree can modify SkillStatsData values at runtime without touching any skill system code
- All requirements SKIL-01 through SKIL-08, DMGS-01 through DMGS-03 satisfied

## Self-Check: PASSED

- All 8 modified files exist on disk
- Commit a8d37f5 (Task 1) verified in git log
- Commit 787c226 (Task 2) verified in git log
- SkillUnlockData and SkillStatsData structs present in SkillComponents.cs
- SkillUnlockData bootstrap present in ECSBootstrap.cs

---
*Phase: 05-ship-skills-and-advanced-damage*
*Completed: 2026-02-18*
