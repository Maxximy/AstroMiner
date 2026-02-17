---
phase: 01-foundation-webgl-validation
plan: 02
subsystem: infra
tags: [unity-ecs, dots, game-state-machine, fsm, object-pool, webgl, placeholder-entities, burst, ijobentity, fade-transitions, debug-overlay]

# Dependency graph
requires:
  - phase: 01-01
    provides: "ECS world with GameStateData/InputData singletons, InputBridge, skybox, URP bloom"
provides:
  - Game state machine (FSM) with 4 states and fade-to-black transitions
  - Object pool with pre-warming (wraps Unity ObjectPool<T>)
  - PlaceholderMovementSystem (ISystem + IJobEntity) driving 1100 entities
  - PlaceholderSpawner creating ECS entities with DriftData/SpinData
  - PlaceholderRenderer syncing ECS LocalTransform to pooled GameObjects
  - DebugOverlay showing state, FPS, entity count
  - FadeController with CanvasGroup-based fade-to-black
  - WebGL build validated at 60 FPS with 1100 entities
affects: [02-core-mining-loop, 03-collection-economy, 04-visual-audio-feedback]

# Tech tracking
tech-stack:
  added: []
  patterns: [fsm-state-pattern, object-pool-prewarm, ecs-gameobject-sync, ijobentity-parallel, dynamic-ui-setup]

key-files:
  created:
    - Assets/Scripts/States/IGameState.cs
    - Assets/Scripts/States/PlayingState.cs
    - Assets/Scripts/States/CollectingState.cs
    - Assets/Scripts/States/GameOverState.cs
    - Assets/Scripts/States/UpgradingState.cs
    - Assets/Scripts/MonoBehaviours/Core/GameManager.cs
    - Assets/Scripts/MonoBehaviours/UI/FadeController.cs
    - Assets/Scripts/MonoBehaviours/UI/DebugOverlay.cs
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs
    - Assets/Scripts/MonoBehaviours/Pool/GameObjectPool.cs
    - Assets/Scripts/ECS/Components/PlaceholderComponents.cs
    - Assets/Scripts/ECS/Systems/PlaceholderMovementSystem.cs
    - Assets/Scripts/MonoBehaviours/Core/PlaceholderSpawner.cs
    - Assets/Scripts/MonoBehaviours/Core/PlaceholderRenderer.cs
  modified:
    - Assets/Scenes/Game.unity

key-decisions:
  - "UISetup.cs creates all UI hierarchy programmatically (no manual scene editor work required)"
  - "PlaceholderRenderer uses managed SystemBase-like approach in MonoBehaviour LateUpdate for ECS-to-GameObject transform sync"
  - "User mandates NEW Input System (UnityEngine.InputSystem) only -- legacy UnityEngine.Input must not be used going forward"

patterns-established:
  - "FSM pattern: IGameState interface with Enter/Execute/Exit, GameManager holds Dictionary<GamePhase, IGameState>"
  - "Object pool pre-warming: Get+Release loop after construction since Unity ObjectPool defaultCapacity does NOT pre-allocate"
  - "ECS-to-GameObject sync: MonoBehaviour reads ECS entities in LateUpdate, syncs LocalTransform to GameObject Transform"
  - "Dynamic UI setup: UISetup MonoBehaviour creates Canvas/Panel/Text hierarchy in code, eliminating manual scene configuration"

requirements-completed: [INFRA-02, INFRA-03, INFRA-04]

# Metrics
duration: 8min
completed: 2026-02-17
---

# Phase 1 Plan 2: Game State Machine, Object Pooling, and WebGL Validation Summary

**FSM with 4 states and fade transitions, object pool pre-warming 1100 GameObjects, ECS IJobEntity driving drift/spin for 100 asteroids + 1000 minerals, WebGL build validated at 60 FPS**

## Performance

- **Duration:** ~8 min (across two sessions with checkpoint)
- **Started:** 2026-02-17T16:52:00Z
- **Completed:** 2026-02-17T17:20:01Z
- **Tasks:** 3 (2 auto + 1 human-verify checkpoint)
- **Files modified:** 34 (including .meta files)

## Accomplishments
- Game state machine transitions between Playing, Collecting, GameOver, and Upgrading with smooth 0.4s fade-to-black (Playing->Collecting skips fade per user decision)
- Object pool wraps Unity ObjectPool<T> with pre-warming loop that actually pre-allocates 1100 GameObjects at startup
- ECS PlaceholderMovementSystem uses BurstCompile + IJobEntity for parallel drift and spin of 1100 entities, respects GamePhase (only moves during Playing)
- PlaceholderSpawner creates 100 asteroid entities (DriftData + SpinData + PlaceholderTag) and 1000 mineral entities (DriftData + PlaceholderTag) with randomized positions and speeds
- PlaceholderRenderer syncs ECS LocalTransform data to pooled GameObjects each frame in LateUpdate
- Debug overlay displays current state name, FPS counter (updated every 0.5s), and entity count
- UISetup creates all UI hierarchy programmatically (FadeCanvas, DebugCanvas, panels, text elements)
- WebGL build loads and runs at 60 FPS with all 1100 placeholder entities active -- Phase 1 validation gate PASSED

## Task Commits

Each task was committed atomically:

1. **Task 1: Game state machine with fade transitions and debug overlay** - `4f7ac65` (feat)
2. **Task 2: Object pool, placeholder entities with ECS movement, and renderer** - `0fef178` (feat)
3. **Task 3: WebGL build and 60 FPS validation** - checkpoint:human-verify (approved by user, no code commit)

## Files Created/Modified
- `Assets/Scripts/States/IGameState.cs` - State interface with Enter/Execute/Exit
- `Assets/Scripts/States/PlayingState.cs` - Playing state stub with logging
- `Assets/Scripts/States/CollectingState.cs` - Collecting state stub with logging
- `Assets/Scripts/States/GameOverState.cs` - GameOver state stub with logging
- `Assets/Scripts/States/UpgradingState.cs` - Upgrading state stub with logging
- `Assets/Scripts/MonoBehaviours/Core/GameManager.cs` - Singleton FSM owner with TransitionTo, fade integration, ECS singleton updates, temporary keyboard shortcuts
- `Assets/Scripts/MonoBehaviours/UI/FadeController.cs` - CanvasGroup fade-to-black with SmoothStep easing, blocksRaycasts management
- `Assets/Scripts/MonoBehaviours/UI/DebugOverlay.cs` - State/FPS/entity count display using TextMeshProUGUI.SetText (allocation-free)
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Programmatic creation of FadeCanvas, DebugCanvas, panels, and text elements
- `Assets/Scripts/MonoBehaviours/Pool/GameObjectPool.cs` - ObjectPool<T> wrapper with pre-warm Get+Release loop
- `Assets/Scripts/ECS/Components/PlaceholderComponents.cs` - DriftData, SpinData, PlaceholderTag IComponentData structs
- `Assets/Scripts/ECS/Systems/PlaceholderMovementSystem.cs` - BurstCompile ISystem with PlaceholderDriftJob and PlaceholderSpinJob IJobEntity jobs
- `Assets/Scripts/MonoBehaviours/Core/PlaceholderSpawner.cs` - Spawns 100 asteroid + 1000 mineral ECS entities with randomized properties
- `Assets/Scripts/MonoBehaviours/Core/PlaceholderRenderer.cs` - Syncs ECS entity transforms to pooled GameObjects in LateUpdate
- `Assets/Scenes/Game.unity` - Scene updated with GameManager, UISetup, PlaceholderSpawner, PlaceholderRenderer GameObjects

## Decisions Made
- UISetup.cs creates all UI hierarchy (Canvases, panels, text elements) programmatically at runtime rather than requiring manual scene editor setup. This avoids complex scene YAML serialization and makes the UI setup reproducible.
- PlaceholderRenderer uses a MonoBehaviour with LateUpdate (not a SystemBase) to sync ECS transforms to GameObjects, keeping the managed-type access pattern clear.
- **User mandates NEW Input System only:** The user encountered issues with the old Input System (UnityEngine.Input) during WebGL testing and had to fix them. Going forward, ALL input code must use the new Input System (UnityEngine.InputSystem) package. The legacy Input class must NOT be used. This affects InputBridge and any future input code.

## Deviations from Plan

### Issues Found During Checkpoint

**1. [User-reported] Old Input System incompatible with WebGL build**
- **Found during:** Task 3 (WebGL build verification by user)
- **Issue:** InputBridge and/or GameManager keyboard shortcuts used legacy `UnityEngine.Input` (old Input System), which caused issues in the WebGL build. The user had to manually fix these to get the build working.
- **Resolution:** User fixed the issues and mandated that only the NEW Input System (`UnityEngine.InputSystem`) be used going forward. This is recorded as a decision and constraint for all future plans.
- **Impact:** No code changes in this plan -- the user handled the fix. Future plans must use `UnityEngine.InputSystem` exclusively.

---

**Total deviations:** 1 user-reported issue (old Input System)
**Impact on plan:** WebGL validation still passed after user fix. Input System constraint recorded for all future work.

## Issues Encountered
- WebGL build required user intervention to fix old Input System (UnityEngine.Input) usage. The legacy Input class does not work correctly in all WebGL scenarios. User resolved this and mandated exclusive use of the new Input System package going forward.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Full hybrid ECS architecture validated end-to-end: ECS systems drive entities, GameObjects render them, bridges communicate between layers
- Game state machine ready for real state behavior in Phase 2+ (currently stub states with logging)
- Object pool infrastructure ready for asteroid/mineral GameObjects in Phase 2
- PlaceholderMovementSystem pattern established for real AsteroidMovementSystem in Phase 2
- **IMPORTANT:** All future input code must use `UnityEngine.InputSystem` (new Input System), not `UnityEngine.Input` (legacy). InputBridge needs migration to new Input System in Phase 2.
- WebGL performance validated at 60 FPS with 1100 entities -- safe to proceed with gameplay implementation
- Phase 1 Foundation and WebGL Validation is COMPLETE

---
## Self-Check: PASSED

All 14 created files verified present on disk. Both task commits (4f7ac65, 0fef178) verified in git log. Task 3 was a human-verify checkpoint with no code commit (approved by user).

---
*Phase: 01-foundation-webgl-validation*
*Completed: 2026-02-17*
