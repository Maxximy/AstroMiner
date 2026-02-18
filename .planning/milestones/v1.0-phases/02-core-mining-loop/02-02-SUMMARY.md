---
phase: 02-core-mining-loop
plan: 02
subsystem: gameplay
tags: [ecs, dots, mining, damage, aoe, linerenderer, bloom, hdr, burst, input]

# Dependency graph
requires:
  - phase: 01-foundation-webgl-validation
    provides: "ECS bootstrap, InputBridge (mouse world position), GameStateData/InputData singletons"
  - phase: 02-core-mining-loop
    plan: 01
    provides: "AsteroidTag, HealthData, DamageTickTimer components, MiningConfigData singleton, AsteroidDestructionSystem, AsteroidRenderer"
provides:
  - "MiningDamageSystem: Burst-compiled tick-based AoE damage to asteroids within configurable mining radius"
  - "MiningCircleVisual: LineRenderer circle with HDR cyan emissive bloom glow following mouse cursor"
  - "Complete mining interaction: hover over asteroids, watch HP deplete, see them destroyed"
affects: [03-collection-economy-session, 04-visual-audio-feedback, 05-ship-skills-advanced-damage]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Per-entity tick timer with fractional accumulation (subtract interval, not reset to 0)"
    - "HDR emissive material on LineRenderer for URP bloom glow (intensity 4x above threshold)"
    - "MonoBehaviour reads ECS singletons each frame for cursor-driven visual (InputData, MiningConfigData)"
    - "Coroutine-based initialization to wait one frame for ECS singleton availability"

key-files:
  created:
    - Assets/Scripts/ECS/Systems/MiningDamageSystem.cs
    - Assets/Scripts/MonoBehaviours/Rendering/MiningCircleVisual.cs
  modified:
    - Assets/Scripts/MonoBehaviours/Core/GameManager.cs

key-decisions:
  - "LineRenderer with HDR unlit material for mining circle visual (simple, bloom-compatible, no extra mesh)"
  - "Unit-circle positions in LineRenderer, parent transform scale controls actual radius from MiningConfigData"
  - "Debug key shortcuts (1-4) removed from GameManager since mining interaction is now the gameplay"

patterns-established:
  - "HDR emissive LineRenderer: unit-circle positions scaled by transform, URP/Unlit shader with intensity multiplier for bloom"
  - "Per-entity tick timer: accumulate elapsed time, subtract interval on tick, reset on exit condition"
  - "Rendering MonoBehaviours in Assets/Scripts/MonoBehaviours/Rendering/ directory"

requirements-completed: [MINE-01, MINE-02, MINE-03, MINE-04, MINE-05]

# Metrics
duration: 25min
completed: 2026-02-17
---

# Phase 2 Plan 02: Mining Circle & Damage Summary

**Tick-based AoE mining damage system with HDR cyan bloom circle visual following cursor, completing the core mining interaction loop**

## Performance

- **Duration:** ~25 min (including checkpoint verification)
- **Started:** 2026-02-17T18:58:00Z
- **Completed:** 2026-02-17T19:23:00Z
- **Tasks:** 2 (1 auto + 1 checkpoint verification)
- **Files modified:** 3

## Accomplishments
- MiningDamageSystem applies tick-based AoE damage to all asteroids within mining circle radius, using Burst-compiled ISystem with per-asteroid DamageTickTimer
- MiningCircleVisual renders a glowing cyan ring following the mouse cursor using LineRenderer with HDR emissive material for URP bloom
- Debug key shortcuts removed from GameManager -- the mining interaction is now the primary gameplay
- Complete core mining loop verified: asteroids spawn, drift, take damage when hovered, and are destroyed at 0 HP

## Task Commits

Each task was committed atomically:

1. **Task 1: MiningDamageSystem and MiningCircleVisual** - `22d8bda` (feat)
2. **Task 2: Verify complete mining interaction** - checkpoint:human-verify (user approved)

## Files Created/Modified
- `Assets/Scripts/ECS/Systems/MiningDamageSystem.cs` - Burst-compiled ISystem: reads InputData mouse position and MiningConfigData, applies tick-based damage to asteroids within radius, resets timers on exit
- `Assets/Scripts/MonoBehaviours/Rendering/MiningCircleVisual.cs` - MonoBehaviour: LineRenderer unit-circle on XZ plane with HDR cyan emissive material, reads ECS singletons for position and radius each frame
- `Assets/Scripts/MonoBehaviours/Core/GameManager.cs` - Removed debug key shortcuts (digit1-4) for state transitions; gameplay is now mining-driven

## Decisions Made
- **LineRenderer for mining circle:** Used LineRenderer with unit-circle positions and parent transform scaling rather than a mesh or sprite. Simple, bloom-compatible, and radius syncs directly from MiningConfigData via localScale.
- **HDR intensity at 4x:** Set color multiplier to 4.0 (well above bloom threshold of ~0.8) for visible glow halo around the cyan ring.
- **Debug shortcuts removed:** Keys 1-4 no longer trigger GamePhase transitions. The mining circle is the core interaction; future state transitions will be timer-driven (Phase 3).

## Deviations from Plan

### Orchestrator-Applied Fixes

**1. [Rule 3 - Blocking] Removed [BurstCompile] from OnCreate methods in Phase 02-01 systems**
- **Found during:** Pre-execution scene testing
- **Issue:** AsteroidSpawnSystem, AsteroidBoundsSystem, and AsteroidMovementSystem had [BurstCompile] on their OnCreate methods, which caused Burst compilation errors
- **Fix:** Orchestrator removed [BurstCompile] attribute from OnCreate methods (kept on OnUpdate and struct-level)
- **Files modified:** `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs`, `Assets/Scripts/ECS/Systems/AsteroidBoundsSystem.cs`, `Assets/Scripts/ECS/Systems/AsteroidMovementSystem.cs`

**2. [Rule 3 - Blocking] Scene setup via CoPlay MCP**
- **Found during:** Pre-execution scene testing
- **Issue:** AsteroidRenderer and MiningCircleVisual MonoBehaviours needed to be attached to GameObjects in the scene
- **Fix:** Orchestrator used CoPlay MCP to create AsteroidRenderer and MiningCircleVisual GameObjects in the scene hierarchy
- **Files modified:** `Assets/Scenes/Game.unity` (scene file)

---

**Total deviations:** 2 orchestrator-applied fixes (both blocking issues resolved before/during execution)
**Impact on plan:** Fixes were necessary for the plan to execute correctly. No scope creep.

## Issues Encountered
None beyond the deviations noted above.

## User Setup Required
None - scene setup was completed via CoPlay MCP during execution.

## Next Phase Readiness
- Core mining loop complete: spawn, drift, damage, destroy cycle fully functional
- Phase 3 (Collection, Economy, Session) can build on: mineral drops from destroyed asteroids, credits from collection, timed session runs
- MiningConfigData singleton provides runtime-configurable values for future upgrades (radius, damage, tick rate)
- DamageTickTimer per-asteroid pattern ready for Phase 5 advanced damage (crits, DoT stacking)
- AsteroidDestructionSystem is the natural trigger point for mineral spawning in Phase 3

## Self-Check: PASSED

- All 3 created/modified files verified on disk
- Commit 22d8bda (Task 1) verified in git log

---
*Phase: 02-core-mining-loop*
*Completed: 2026-02-17*
