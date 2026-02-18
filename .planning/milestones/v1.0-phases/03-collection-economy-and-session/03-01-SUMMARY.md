---
phase: 03-collection-economy-and-session
plan: 01
subsystem: ecs
tags: [dots, minerals, collection, economy, pooling, burst, ijobentity]

# Dependency graph
requires:
  - phase: 02-core-mining-loop
    provides: "AsteroidDestructionSystem, AsteroidTag, HealthData, GameStateData, AsteroidRenderer pattern"
provides:
  - "MineralTag, MineralData, MineralPullData components for mineral ECS entities"
  - "MineralSpawnSystem spawns minerals from dead asteroids"
  - "MineralPullSystem accelerates minerals toward ship via IJobEntity"
  - "MineralCollectionSystem awards credits on collection"
  - "MineralRenderer syncs mineral ECS entities to pooled GameObjects"
  - "CollectionEvent buffer entity for Phase 4 SFX/VFX infrastructure"
  - "ResourceTierSO ScriptableObject data model for Phase 6 tiers"
  - "GameConstants mineral physics and session timing values"
affects: [03-02, 03-03, 04-visual-audio-feedback, 06-tech-tree-level-progression]

# Tech tracking
tech-stack:
  added: []
  patterns: [mineral-pull-toward-ship, ijobentity-burst-parallel, double-spawn-prevention-tag, collection-event-buffer]

key-files:
  created:
    - Assets/Scripts/ECS/Components/MineralComponents.cs
    - Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs
    - Assets/Scripts/ECS/Systems/MineralPullSystem.cs
    - Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs
    - Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs
    - Assets/Scripts/Data/ResourceTierSO.cs
  modified:
    - Assets/Scripts/Shared/GameConstants.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs

key-decisions:
  - "Direct GameStateData.Credits increment in MineralCollectionSystem instead of DynamicBuffer events (Phase 4 will add buffer events when AudioEventBridge exists)"
  - "MineralsSpawnedTag prevents double-spawn from asteroid entity persisting one extra frame during ECB destruction"
  - "Gold/amber mineral color (0.9, 0.75, 0.3) to visually distinguish from dark asteroid palette"
  - "MineralRenderer pool: 200 pre-warm, 1200 max for 1000+ mineral scalability"

patterns-established:
  - "Double-spawn prevention: Add a tag component via ECB to mark entities already processed, filter with WithNone"
  - "IJobEntity with WithAll attribute for Burst-compiled parallel entity processing"
  - "System ordering via UpdateBefore/UpdateAfter for spawn-before-destroy and collect-after-move"

requirements-completed: [MINR-01, MINR-02, MINR-03, MINR-04, ECON-01, ECON-04]

# Metrics
duration: 3min
completed: 2026-02-17
---

# Phase 3 Plan 1: Mineral Collection Pipeline Summary

**Mineral ECS lifecycle with 3 Burst-compiled systems: spawn from dead asteroids, pull toward ship via IJobEntity, collect on contact awarding credits, rendered as pooled gold spheres**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-17T20:24:53Z
- **Completed:** 2026-02-17T20:28:08Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Complete mineral ECS pipeline: spawn, pull, collect with credit awarding
- MineralPullSystem uses IJobEntity for Burst-compiled parallel processing of 1000+ minerals
- MineralRenderer syncs ECS entities to pooled GameObjects (200 pre-warm, 1200 max)
- Double-spawn prevention via MineralsSpawnedTag component
- CollectionEvent buffer entity ready for Phase 4 SFX/VFX integration
- ResourceTierSO data model ready for Phase 6 tiered resources

## Task Commits

Each task was committed atomically:

1. **Task 1: Mineral ECS components, constants, ResourceTierSO, and 3 mineral systems** - `a8bc835` (feat)
2. **Task 2: MineralRenderer with pooled GameObjects and tier-based coloring** - `8c4cf85` (feat)

**Plan metadata:** `9936733` (docs: complete plan)

## Files Created/Modified
- `Assets/Scripts/ECS/Components/MineralComponents.cs` - MineralTag, MineralData, MineralPullData, MineralsSpawnedTag, CollectionEvent
- `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` - Spawns minerals from dead asteroids with UpdateBefore ordering
- `Assets/Scripts/ECS/Systems/MineralPullSystem.cs` - IJobEntity accelerates minerals toward ship
- `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` - Awards credits on collection, destroys mineral entities
- `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` - Pooled GameObject sync for mineral ECS entities
- `Assets/Scripts/Data/ResourceTierSO.cs` - ScriptableObject for resource tier configuration (Phase 6)
- `Assets/Scripts/Shared/GameConstants.cs` - Added mineral physics, session timing, ship position constants
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Added CollectionEvent buffer entity

## Decisions Made
- Direct `GameStateData.Credits` increment in MineralCollectionSystem rather than using DynamicBuffer events, because AudioEventBridge doesn't exist yet (Phase 4)
- MineralsSpawnedTag prevents double-spawn from the one-frame delay in ECB entity destruction
- Gold/amber mineral color (0.9, 0.75, 0.3) chosen to visually distinguish from dark gray/brown/rust asteroids
- MineralRenderer pool sized at 200 pre-warm / 1200 max to handle 1000+ simultaneous minerals

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required

MineralRenderer MonoBehaviour must be added to a GameObject in the Unity scene. This can be done via:
- CoPlay MCP editor automation, or
- Manual addition: Create empty GameObject named "MineralRenderer" in Game scene, attach the `MineralRenderer` component

## Next Phase Readiness
- Mineral pipeline complete -- asteroids produce valuable minerals when destroyed
- Credits accumulate in GameStateData.Credits, ready for Plan 03-02 (session timer, HUD display)
- CollectionEvent buffer entity ready for Phase 4 audio/visual feedback
- ResourceTierSO ready for Phase 6 tiered resources

## Self-Check: PASSED

- All 8 files verified on disk (6 created, 2 modified)
- Commit `a8bc835`: 7 files (Task 1) - verified
- Commit `8c4cf85`: 1 file (Task 2) - verified
- All plan verification criteria confirmed via grep

---
*Phase: 03-collection-economy-and-session*
*Completed: 2026-02-17*
