---
phase: 01-foundation-webgl-validation
plan: 01
subsystem: infra
tags: [unity-ecs, dots, entities, input-bridge, number-formatter, webgl, skybox, urp, post-processing]

# Dependency graph
requires: []
provides:
  - GameStateData and InputData ECS singleton components
  - ECSBootstrap procedural entity creation (no SubScenes)
  - InputBridge mouse-to-world projection via Plane.Raycast
  - NumberFormatter K/M/B/T/Qa/Qi suffix utility
  - WebGL IndexedDB flush jslib plugin
  - Dark procedural skybox material
  - URP bloom + neutral tonemapping post-processing
affects: [01-02, 02-core-mining-loop, 03-collection-economy]

# Tech tracking
tech-stack:
  added: [com.unity.entities 1.4.4]
  patterns: [procedural-entity-creation, singleton-components, bridge-pattern, conditional-compilation]

key-files:
  created:
    - Assets/Scripts/ECS/Components/GameStateComponents.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/Bridge/InputBridge.cs
    - Assets/Scripts/Shared/GameEnums.cs
    - Assets/Scripts/Shared/NumberFormatter.cs
    - Assets/Scripts/Shared/WebGLHelper.cs
    - Assets/Plugins/WebGL/IndexedDBSync.jslib
    - Assets/Materials/SkyboxMaterial.mat
  modified:
    - Packages/manifest.json
    - Assets/Settings/SampleSceneProfile.asset
    - Assets/Scenes/Game.unity

key-decisions:
  - "Procedural skybox shader used as temporary stand-in for cubemap (CoPlay MCP unavailable for texture generation)"
  - "Directional light reduced to 0.5 intensity with warm tint to match dark void aesthetic"
  - "Vignette disabled in volume profile per clean visual style decision"
  - "Camera positioned at Y=18 with 60-degree X rotation for perspective top-down view"

patterns-established:
  - "Procedural entity creation: EntityManager.CreateEntity in MonoBehaviour Start() instead of SubScenes"
  - "Singleton components: GameStateData and InputData as cross-layer communication mechanism"
  - "Bridge pattern: MonoBehaviour writes to ECS singleton each frame for input data flow"
  - "Conditional WebGL compilation: #if UNITY_WEBGL && !UNITY_EDITOR with DllImport for jslib interop"

requirements-completed: [INFRA-01, INFRA-05, INFRA-06, VISL-01, VISL-02]

# Metrics
duration: 4min
completed: 2026-02-17
---

# Phase 1 Plan 1: ECS Bootstrap and Visual Baseline Summary

**ECS world with GameStateData/InputData singletons, InputBridge mouse-to-world projection, NumberFormatter K/M/B/T utility, WebGL IndexedDB flush plugin, dark procedural skybox, and URP bloom + neutral tonemapping**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-17T16:41:27Z
- **Completed:** 2026-02-17T16:45:28Z
- **Tasks:** 2
- **Files modified:** 19

## Accomplishments
- ECS world bootstraps with two singleton entities (GameStateData with Phase=Playing, InputData with MouseValid=false) via procedural creation in MonoBehaviour Start()
- InputBridge projects mouse position to Y=0 gameplay plane every frame using Camera.ScreenPointToRay + Plane.Raycast, writes float2 world position to ECS InputData singleton
- NumberFormatter formats numbers with K/M/B/T/Qa/Qi suffixes with 1 decimal for values under 100 at each magnitude
- WebGL IndexedDB flush plugin (IndexedDBSync.jslib) with WebGLHelper conditional compilation wrapper
- Dark procedural skybox with near-black sky tint, no sun disk, very low exposure (0.05)
- URP bloom configured with 0.8 threshold, 0.3 intensity, warm amber tint, no HQ filtering (WebGL perf), quarter downscale
- Entities 1.4.4 installed; ai.navigation, multiplayer.center, visualscripting, timeline packages removed

## Task Commits

Each task was committed atomically:

1. **Task 1: Install Entities package, remove unnecessary packages, create ECS components and bootstrap** - `1d6b99e` (feat)
2. **Task 2: Create InputBridge, NumberFormatter, WebGL helper, and set up space visuals** - `0acd294` (feat)

## Files Created/Modified
- `Packages/manifest.json` - Added com.unity.entities 1.4.4, removed 4 unnecessary packages
- `Assets/Scripts/Shared/GameEnums.cs` - GamePhase enum (Playing, Collecting, GameOver, Upgrading)
- `Assets/Scripts/ECS/Components/GameStateComponents.cs` - GameStateData and InputData IComponentData singleton structs
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Procedural ECS world bootstrap creating singleton entities
- `Assets/Scripts/MonoBehaviours/Bridge/InputBridge.cs` - Mouse-to-world projection writing to ECS InputData singleton
- `Assets/Scripts/Shared/NumberFormatter.cs` - K/M/B/T large number formatting utility
- `Assets/Scripts/Shared/WebGLHelper.cs` - Conditional WebGL IndexedDB flush wrapper
- `Assets/Plugins/WebGL/IndexedDBSync.jslib` - JavaScript bridge for FS.syncfs() IndexedDB flush
- `Assets/Materials/SkyboxMaterial.mat` - Dark procedural skybox material (near-black, no sun)
- `Assets/Settings/SampleSceneProfile.asset` - Updated bloom/tonemapping settings, disabled vignette
- `Assets/Scenes/Game.unity` - Camera repositioned (Y=18, 60deg), skybox assigned, ambient darkened, GameCore with ECSBootstrap + InputBridge

## Decisions Made
- Used Skybox/Procedural shader as temporary stand-in since CoPlay MCP was not available for cubemap texture generation. The procedural skybox is configured to render near-black with no sun disk, matching the "dark void" aesthetic. A proper cubemap with sparse warm-toned stars should be generated when the texture generation tool is available.
- Directional light intensity reduced from 2.0 to 0.5 with a warm color tint (r:1, g:0.95, b:0.85) to fit the space environment where objects should not be brightly lit.
- Vignette volume override disabled since the context specifies "no vignette" for a clean visual style.
- Camera placed at position (0, 18, -8) with 60-degree X rotation for a perspective top-down view that gives slight 3D depth to objects while still showing the full gameplay plane.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Reduced directional light for space aesthetic**
- **Found during:** Task 2 (space visuals setup)
- **Issue:** Default directional light at intensity 2.0 with white color was too bright for a dark space void
- **Fix:** Reduced intensity to 0.5 and added warm color tint to match the amber accent aesthetic
- **Files modified:** Assets/Scenes/Game.unity
- **Verification:** Light settings updated in scene YAML
- **Committed in:** 0acd294 (Task 2 commit)

**2. [Rule 2 - Missing Critical] Disabled vignette override in volume profile**
- **Found during:** Task 2 (post-processing configuration)
- **Issue:** SampleSceneProfile had vignette enabled at 0.2 intensity, conflicting with "no vignette" visual style decision
- **Fix:** Set vignette active to 0 (disabled)
- **Files modified:** Assets/Settings/SampleSceneProfile.asset
- **Verification:** Vignette override disabled in YAML
- **Committed in:** 0acd294 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 missing critical)
**Impact on plan:** Both auto-fixes align with the user's visual style decisions. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ECS world bootstraps with singleton entities, ready for game state machine and object pooling (Plan 2)
- InputBridge writes mouse position every frame, ready for mining circle interaction (Phase 2)
- Post-processing and skybox are active, ready for placeholder entity visuals (Plan 2)
- The procedural skybox is a temporary stand-in; a proper cubemap with sparse warm-toned stars should be generated when available

---
## Self-Check: PASSED

All 11 created/modified files verified present on disk. Both task commits (1d6b99e, 0acd294) verified in git log.

---
*Phase: 01-foundation-webgl-validation*
*Completed: 2026-02-17*
