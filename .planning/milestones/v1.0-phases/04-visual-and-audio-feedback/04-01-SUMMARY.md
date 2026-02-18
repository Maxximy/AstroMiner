---
phase: 04-visual-and-audio-feedback
plan: 01
subsystem: rendering
tags: [ecs-events, damage-popups, particle-explosions, trail-renderer, tmPro, material-property-block, hdr-emissive, object-pooling]

# Dependency graph
requires:
  - phase: 02-core-mining-loop
    provides: MiningDamageSystem, AsteroidDestructionSystem, AsteroidRenderer
  - phase: 03-collection-economy-and-session
    provides: MineralCollectionSystem, MineralRenderer, HUDController, CollectionEvent buffer, GameObjectPool
provides:
  - DamageEvent and DestructionEvent IBufferElementData ECS event types
  - DamageType enum (Normal, Critical, DoT, Skill) for visual styling infrastructure
  - DamagePopupManager singleton with pooled world-space TMPro damage numbers
  - ExplosionManager singleton with pooled ParticleSystem debris effects
  - ECS-to-MonoBehaviour event pipeline pattern (buffer emit + drain per frame)
  - Mineral TrailRenderer with HDR emissive material
  - Asteroid and mineral emissive material enhancements
  - Credit counter pop animation in HUD
affects: [04-02-audio-feedback, 05-ship-skills-advanced-damage]

# Tech tracking
tech-stack:
  added: [TextMeshPro world-space, ParticleSystem, TrailRenderer, CanvasGroup]
  patterns: [DynamicBuffer event pipeline, singleton self-instantiation, struct ActivePopup list for GC-free animation]

key-files:
  created:
    - Assets/Scripts/ECS/Components/FeedbackComponents.cs
    - Assets/Scripts/MonoBehaviours/Rendering/DamagePopupManager.cs
    - Assets/Scripts/MonoBehaviours/Rendering/ExplosionManager.cs
  modified:
    - Assets/Scripts/ECS/Systems/MiningDamageSystem.cs
    - Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs
    - Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs
    - Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs
    - Assets/Scripts/MonoBehaviours/UI/HUDController.cs
    - Assets/Scripts/Shared/GameConstants.cs

key-decisions:
  - "DamageEvent/DestructionEvent use unmanaged byte RGB instead of Color for Burst safety in ISystem"
  - "DamagePopupManager and ExplosionManager self-instantiate via RuntimeInitializeOnLoadMethod(AfterSceneLoad)"
  - "World-space Canvas with TMPro scaled to 0.02 for damage popups -- billboard rotation each frame"
  - "Struct ActivePopup list for popup animation to minimize GC pressure"
  - "TrailRenderer Clear() on pool release to prevent ghost trails"

patterns-established:
  - "ECS DynamicBuffer event pipeline: Burst ISystem adds to singleton buffer -> MonoBehaviour drains in Update -> buffer.Clear()"
  - "Singleton self-instantiation: RuntimeInitializeOnLoadMethod + DontDestroyOnLoad for managers without scene setup"
  - "Lazy ECS init in MonoBehaviour: _ecsInitialized flag + query creation deferred until World is ready"

requirements-completed: [FEED-01, FEED-02, FEED-03, FEED-04, FEED-05, FEED-06, VISL-03, VISL-04]

# Metrics
duration: 5min
completed: 2026-02-18
---

# Phase 4 Plan 01: Visual Feedback Pipeline Summary

**ECS-to-MonoBehaviour event pipeline with pooled damage popups, debris explosions, mineral trails, emissive materials, and credit counter pop animation**

## Performance

- **Duration:** 5 min
- **Started:** 2026-02-18T04:28:00Z
- **Completed:** 2026-02-18T04:33:00Z
- **Tasks:** 2
- **Files modified:** 11

## Accomplishments
- Built the ECS DynamicBuffer event pipeline (DamageEvent, DestructionEvent, CollectionEvent) that all three gameplay systems now emit into on every damage tick, asteroid kill, and mineral collection
- Created DamagePopupManager with pooled world-space TMPro damage numbers supporting four DamageType visual styles (white normal, yellow CRIT, orange italic DoT, skill-colored)
- Created ExplosionManager with pooled ParticleSystem chunky debris explosions on asteroid destruction
- Added TrailRenderer with HDR emissive material to minerals for visible flight trails
- Enhanced asteroid and mineral materials with emissive color for visual richness
- Added credit counter scale-up and gold flash animation in HUD

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ECS event components and modify systems to emit events** - `c8137f7` (feat)
2. **Task 2: Create visual feedback managers (popups, explosions, trails, materials, credit pop)** - `d1d32e4` (feat)

## Files Created/Modified
- `Assets/Scripts/ECS/Components/FeedbackComponents.cs` - DamageEvent, DestructionEvent IBufferElementData and DamageType enum
- `Assets/Scripts/ECS/Systems/MiningDamageSystem.cs` - Emits DamageEvent on every damage tick
- `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` - Emits DestructionEvent on asteroid death
- `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` - Emits CollectionEvent on mineral collection
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Creates DamageEvent and DestructionEvent buffer entities
- `Assets/Scripts/Shared/GameConstants.cs` - 16 new feedback/VFX tuning constants
- `Assets/Scripts/MonoBehaviours/Rendering/DamagePopupManager.cs` - Pooled TMPro damage popup system
- `Assets/Scripts/MonoBehaviours/Rendering/ExplosionManager.cs` - Pooled ParticleSystem debris explosions
- `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` - TrailRenderer + HDR emissive material
- `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` - Emissive color tint per-tier
- `Assets/Scripts/MonoBehaviours/UI/HUDController.cs` - Credit counter pop animation

## Decisions Made
- Used unmanaged byte RGB (ColorR/G/B) instead of managed Color struct in DamageEvent for Burst safety
- Self-instantiation pattern (RuntimeInitializeOnLoadMethod) for both managers to avoid manual scene setup
- World-space Canvas at 0.02 scale factor with TMPro for crisp damage numbers at any camera distance
- Struct-based ActivePopup list instead of class to minimize GC allocations during popup animation
- TrailRenderer.Clear() called on pool release to prevent ghost trail artifacts when minerals are recycled
- DamageType enum with 4 values provides infrastructure for Phase 5 crit/DoT/skill without needing changes to the event pipeline

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - DamagePopupManager and ExplosionManager self-instantiate. No manual scene configuration required.

## Next Phase Readiness
- Event pipeline is ready for Plan 02 (audio feedback) to consume the same DamageEvent, DestructionEvent, and CollectionEvent buffers
- Phase 5 (ship skills, crits, DoT) can emit Critical/DoT/Skill DamageType events without any pipeline changes
- Visual feedback tuning constants in GameConstants can be adjusted without code changes

## Self-Check: PASSED

- All 3 created files verified on disk (FeedbackComponents.cs, DamagePopupManager.cs, ExplosionManager.cs)
- All 8 modified files verified on disk
- Commit c8137f7 (Task 1) verified in git log
- Commit d1d32e4 (Task 2) verified in git log

---
*Phase: 04-visual-and-audio-feedback*
*Completed: 2026-02-18*
