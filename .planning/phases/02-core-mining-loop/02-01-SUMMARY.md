---
phase: 02-core-mining-loop
plan: 01
subsystem: gameplay
tags: [ecs, dots, asteroid, spawning, movement, pooling, lifecycle, burst]

# Dependency graph
requires:
  - phase: 01-foundation-webgl-validation
    provides: "ECS bootstrap, GameObjectPool, PlaceholderRenderer pattern, InputBridge, GameStateData/InputData singletons"
provides:
  - "AsteroidTag, HealthData, DamageTickTimer ECS components for asteroid entities"
  - "AsteroidSpawnTimer, MiningConfigData ECS singletons"
  - "AsteroidSpawnSystem: periodic entity creation via ECB on XZ plane"
  - "AsteroidMovementSystem: drift along -Z and spin around Y"
  - "AsteroidBoundsSystem: destroy entities below PlayAreaZMin"
  - "AsteroidDestructionSystem: destroy entities with 0 HP"
  - "AsteroidRenderer: dynamic ECS-to-pooled-GameObject sync with lifecycle management"
  - "GameConstants: shared play area bounds and tuning values"
  - "Ship placeholder visual on XZ plane"
affects: [02-core-mining-loop, 03-collection-economy-session, 04-visual-audio-feedback]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ECB entity lifecycle: spawn via CreateEntity, destroy via DestroyEntity in EndSimulationECB"
    - "Dynamic ECS-to-GameObject sync with Dictionary<Entity, GameObject> and pool release on entity destruction"
    - "WithAll<Tag> filter on IJobEntity to scope jobs to specific entity types"
    - "XZ plane coordinate system for all gameplay entities (Y=0 ground plane)"
    - "GameConstants static class with const fields for Burst-accessible tuning values"

key-files:
  created:
    - Assets/Scripts/Shared/GameConstants.cs
    - Assets/Scripts/ECS/Components/AsteroidComponents.cs
    - Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs
    - Assets/Scripts/ECS/Systems/AsteroidMovementSystem.cs
    - Assets/Scripts/ECS/Systems/AsteroidBoundsSystem.cs
    - Assets/Scripts/ECS/Systems/AsteroidDestructionSystem.cs
    - Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs
  modified:
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/Core/PlaceholderSpawner.cs
    - Assets/Scripts/MonoBehaviours/Core/PlaceholderRenderer.cs
    - Assets/Scripts/ECS/Systems/PlaceholderMovementSystem.cs

key-decisions:
  - "XZ plane coordinate system for all Phase 2+ entities, aligning with InputBridge mouse projection"
  - "Per-asteroid DamageTickTimer component for responsive tick-based damage (vs global timer)"
  - "Placeholder systems disabled rather than deleted to preserve DriftData/SpinData component definitions"
  - "AsteroidMovementSystem uses WithAll<AsteroidTag> filter to prevent processing non-asteroid entities"
  - "Ship placeholder is a pure GameObject, not an ECS entity"

patterns-established:
  - "ECB lifecycle pattern: ISystem creates/destroys entities via EndSimulationEntityCommandBufferSystem"
  - "Tag-filtered IJobEntity: WithAll<AsteroidTag> scopes Burst jobs to specific entity archetypes"
  - "Dynamic renderer sync: Dictionary<Entity, GameObject> tracks active entities, HashSet detects removals"
  - "GameConstants for Burst-accessible const values shared across ECS and MonoBehaviour layers"

requirements-completed: [ASTR-01, ASTR-02, ASTR-03, ASTR-04, ASTR-05, VISL-05]

# Metrics
duration: 3min
completed: 2026-02-17
---

# Phase 2 Plan 01: Asteroid Lifecycle Summary

**ECS asteroid spawn-drift-destroy lifecycle on XZ plane with pooled GameObject rendering, 5 new systems, and shared GameConstants**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-17T17:53:48Z
- **Completed:** 2026-02-17T17:56:21Z
- **Tasks:** 2
- **Files modified:** 11

## Accomplishments
- Full asteroid entity lifecycle: spawn at top, drift downward on -Z, spin around Y, destroy at bottom boundary or 0 HP
- Dynamic ECS-to-pooled-GameObject sync with proper lifecycle management (new entities get spheres, destroyed entities release them)
- Transitioned coordinate system from XY (Phase 1) to XZ plane, aligning entities with InputBridge mouse projection
- All placeholder Phase 1 systems cleanly disabled without deletion

## Task Commits

Each task was committed atomically:

1. **Task 1: ECS components, constants, singletons, and asteroid ECS systems** - `73ed6af` (feat)
2. **Task 2: AsteroidRenderer, ship visual, and placeholder cleanup** - `216ceab` (feat)

## Files Created/Modified
- `Assets/Scripts/Shared/GameConstants.cs` - Static constants for play area bounds and tuning values (Burst-accessible)
- `Assets/Scripts/ECS/Components/AsteroidComponents.cs` - AsteroidTag, HealthData, DamageTickTimer, AsteroidSpawnTimer, MiningConfigData
- `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs` - Periodic asteroid creation via ECB with random position/speed
- `Assets/Scripts/ECS/Systems/AsteroidMovementSystem.cs` - Drift along -Z and spin around Y with AsteroidTag filter
- `Assets/Scripts/ECS/Systems/AsteroidBoundsSystem.cs` - Destroy entities below PlayAreaZMin via ECB
- `Assets/Scripts/ECS/Systems/AsteroidDestructionSystem.cs` - Destroy entities with CurrentHP <= 0 via ECB
- `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` - Dynamic entity-to-GameObject sync with pool lifecycle
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Added AsteroidSpawnTimer and MiningConfigData singleton creation
- `Assets/Scripts/MonoBehaviours/Core/PlaceholderSpawner.cs` - Disabled in Start() with log message
- `Assets/Scripts/MonoBehaviours/Core/PlaceholderRenderer.cs` - Disabled in Start() with log message
- `Assets/Scripts/ECS/Systems/PlaceholderMovementSystem.cs` - Added [DisableAutoCreation] to prevent running

## Decisions Made
- **XZ plane coordinate system:** All Phase 2 entities use XZ plane (Y=0 ground), aligning with InputBridge which writes mouse as float2(worldPoint.x, worldPoint.z). Camera at Y=18 looking down at 60 degrees sees this plane naturally.
- **Placeholder disable strategy:** Added [DisableAutoCreation] to PlaceholderMovementSystem and disabled PlaceholderSpawner/PlaceholderRenderer in Start() rather than deleting files. This preserves DriftData/SpinData component definitions in PlaceholderComponents.cs for reuse by asteroid systems.
- **AsteroidTag job filtering:** AsteroidDriftJob and AsteroidSpinJob use [WithAll(typeof(AsteroidTag))] to ensure they only process asteroid entities, unlike the placeholder jobs which had no tag filter.

## Deviations from Plan

None - plan executed exactly as written.

## User Setup Required

The user must add the AsteroidRenderer MonoBehaviour to a GameObject in the Unity scene before testing:
- Add an AsteroidRenderer component to the existing GameManager GameObject (or create a new empty "Rendering" GameObject)
- Ensure PlaceholderSpawner and PlaceholderRenderer are either removed from the scene or left as-is (their disabled-in-code logic handles it)

## Issues Encountered
None

## Next Phase Readiness
- Asteroid entities spawn with HealthData and DamageTickTimer, ready for MiningDamageSystem (Plan 02-02)
- MiningConfigData singleton initialized with default values, ready for mining circle (Plan 02-02)
- InputBridge already provides mouse world position on XZ plane, aligning with asteroid positions
- GameConstants provides all tuning values consumed by Plan 02-02

## Self-Check: PASSED

- All 8 created/key files verified on disk
- Commit 73ed6af (Task 1) verified in git log
- Commit 216ceab (Task 2) verified in git log

---
*Phase: 02-core-mining-loop*
*Completed: 2026-02-17*
