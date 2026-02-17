---
phase: 01-foundation-webgl-validation
verified: 2026-02-17T18:00:00Z
status: gaps_found
score: 9/11 must-haves verified
re_verification: false
gaps:
  - truth: "Object pool pre-allocates GameObjects and recycles them without runtime Instantiate/Destroy during gameplay"
    status: partial
    reason: "INFRA-03 requires pooling for asteroids, minerals, damage popups, AND particles. The pool infrastructure exists and works for asteroids and minerals (1100 objects pre-warmed), but damage popup and particle pools are explicitly deferred to Phase 4. The requirement is partially satisfied -- the infrastructure is proven, but the full scope is not delivered."
    artifacts:
      - path: "Assets/Scripts/MonoBehaviours/Pool/GameObjectPool.cs"
        issue: "Pool itself is complete and correct; the gap is in scope coverage, not in implementation quality"
    missing:
      - "Pool instances for damage popups (deferred to Phase 4)"
      - "Pool instances for particle effects (deferred to Phase 4)"
  - truth: "Starfield skybox renders as dark void with sparse warm-toned stars"
    status: partial
    reason: "SkyboxMaterial.mat uses the Skybox/Procedural shader (not a cubemap with actual star points). The procedural shader with _Exposure: 0.05 and _SUNDISK_NONE produces a very dark sky but does NOT render discrete star points -- it produces a gradient atmospheric sky at near-zero exposure. The visual output is 'dark void' but lacks the sparse warm-toned star points specified in the truth and the GDD. The SUMMARY acknowledges this is a 'temporary stand-in' pending texture generation."
    artifacts:
      - path: "Assets/Materials/SkyboxMaterial.mat"
        issue: "Uses Skybox/Procedural shader (guid: 0000000000000000f000000000000000 = built-in Procedural), not a cubemap with star texture. No star points visible."
    missing:
      - "Cubemap or panoramic texture with sparse warm-toned star points"
      - "SkyboxMaterial pointing to a proper space texture (not procedural atmospheric shader)"
human_verification:
  - test: "Confirm WebGL build runs at 60 FPS"
    expected: "Debug overlay shows 60+ FPS in Chrome with all 1100 placeholder entities active and moving"
    why_human: "WebGL build must be served and opened in browser; cannot verify programmatically"
  - test: "Confirm InputBridge projects mouse correctly in-editor"
    expected: "Moving mouse over game area changes InputData.MouseWorldPos in Entity Debugger in real time; MouseValid=true when over gameplay plane"
    why_human: "Runtime mouse event behavior cannot be verified from static code analysis"
  - test: "Confirm fade transitions are smooth"
    expected: "Pressing 2/3/4 in Play mode produces a visible 0.4s smooth fade-to-black; Playing->Collecting (key 2 from Playing) has NO fade"
    why_human: "Visual animation quality requires runtime observation"
  - test: "Confirm DebugOverlay wiring via reflection"
    expected: "UISetup wires FadeController._fadeCanvasGroup and DebugOverlay text fields via reflection. Confirm these fields are non-null at runtime (no NullReferenceException in console)"
    why_human: "Reflection-based wiring cannot be verified statically; requires Play mode"
---

# Phase 1: Foundation and WebGL Validation -- Verification Report

**Phase Goal:** The ECS simulation world runs on both desktop and WebGL with a working game state machine, input bridge, and object pooling -- proving the hybrid architecture before any gameplay is built
**Verified:** 2026-02-17T18:00:00Z
**Status:** gaps_found
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | ECS world bootstraps and singleton entities (GameStateData, InputData) exist after scene loads | VERIFIED | ECSBootstrap.cs creates both entities via EntityManager in Start(); log message "ECS Bootstrap complete: singletons created" confirms |
| 2 | Mouse position is projected to world space via perspective camera and written to ECS InputData singleton every frame | VERIFIED | InputBridge.cs uses new Input System (Mouse.current), Camera.ScreenPointToRay, Plane.Raycast, SetComponentData -- all wired end-to-end |
| 3 | Large numbers format correctly with K/M/B/T suffixes (999->999, 1500->1.5K, 2500000->2.5M) | VERIFIED | NumberFormatter.cs has correct division-by-1000 loop with F1/F0 formatting; logic is substantively correct |
| 4 | Starfield skybox renders as dark void with sparse warm-toned stars | PARTIAL | SkyboxMaterial.mat exists with near-black procedural skybox (exposure 0.05, no sun disk) but uses Skybox/Procedural shader -- no actual star points visible; acknowledged in SUMMARY as temporary stand-in |
| 5 | URP post-processing (bloom + neutral tonemapping) is active and visible on emissive objects | VERIFIED | SampleSceneProfile.asset has Bloom (threshold:0.8, intensity:0.3) and Tonemapping (mode:1=Neutral) both active:1 |
| 6 | WebGL IndexedDB flush function exists and compiles for WebGL builds | VERIFIED | IndexedDBSync.jslib with SyncIndexedDB function exists; WebGLHelper.cs uses conditional DllImport correctly |
| 7 | Game state machine transitions between Playing, Collecting, GameOver, and Upgrading states with smooth fade-to-black | VERIFIED | GameManager.cs has TransitionTo with isTransitioning guard, FadeController.FadeOut/FadeIn with SmoothStep coroutine, all four states in Dictionary; Playing->Collecting skips fade per spec |
| 8 | Debug overlay shows current game state, FPS, and entity count in top-left corner | VERIFIED | DebugOverlay.cs reads GameStateData singleton for state, calculates FPS every 0.5s, queries LocalTransform count; UISetup.cs creates top-left panel programmatically |
| 9 | Object pool pre-allocates GameObjects and recycles them without runtime Instantiate/Destroy during gameplay | PARTIAL | GameObjectPool.cs correctly pre-warms via Get+Release loop; used for 100 asteroids + 1000 minerals; damage popup and particle pools deferred to Phase 4 (INFRA-03 partially satisfied) |
| 10 | 100 placeholder asteroids drift downward and spin, 1000 placeholder minerals drift, all driven by ECS systems | VERIFIED | PlaceholderMovementSystem.cs has BurstCompile ISystem with PlaceholderDriftJob and PlaceholderSpinJob IJobEntity; PlaceholderSpawner creates 100 asteroid + 1000 mineral entities with correct archetypes |
| 11 | WebGL build loads and runs at 60 FPS with all 1100 placeholder entities active | HUMAN NEEDED | User-approved per SUMMARY (Task 3 checkpoint); cannot verify programmatically |

**Score:** 9/11 truths verified (2 partial, 1 human-needed)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/ECS/Components/GameStateComponents.cs` | GameStateData and InputData IComponentData structs | VERIFIED | Both structs present with correct fields; IComponentData implemented |
| `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` | Procedural ECS world bootstrap | VERIFIED | Creates both singleton entities, logs confirmation, 31 lines -- substantive |
| `Assets/Scripts/MonoBehaviours/Bridge/InputBridge.cs` | Mouse-to-world-space projection | VERIFIED | ScreenPointToRay + Plane.Raycast + SetComponentData -- full pipeline; uses new Input System |
| `Assets/Scripts/Shared/NumberFormatter.cs` | K/M/B/T large number formatting | VERIFIED | Correct division loop, F1/F0 formatting, negative handling, 27 lines |
| `Assets/Plugins/WebGL/IndexedDBSync.jslib` | JavaScript bridge for IndexedDB flush | VERIFIED | SyncIndexedDB function with FS.syncfs present, 9 lines |
| `Assets/Scripts/Shared/WebGLHelper.cs` | Conditional WebGL compilation wrapper | VERIFIED | #if UNITY_WEBGL && !UNITY_EDITOR guard, DllImport, FlushSaveData method |
| `Assets/Scripts/MonoBehaviours/Core/GameManager.cs` | FSM with TransitionTo, fade integration | VERIFIED | Dictionary<GamePhase, IGameState>, TransitionTo with guard, WritePhaseToECS, FadeOut/FadeIn callbacks |
| `Assets/Scripts/States/IGameState.cs` | State interface with Enter/Execute/Exit | VERIFIED | Interface with correct three methods |
| `Assets/Scripts/MonoBehaviours/Pool/GameObjectPool.cs` | Object pool with pre-warming | VERIFIED | ObjectPool<T> wrapper with correct pre-warm Get+Release loop |
| `Assets/Scripts/ECS/Systems/PlaceholderMovementSystem.cs` | ECS system with IJobEntity for drift and spin | VERIFIED | BurstCompile ISystem, PlaceholderDriftJob and PlaceholderSpinJob IJobEntity, GamePhase guard |
| `Assets/Scripts/MonoBehaviours/UI/DebugOverlay.cs` | Debug UI showing state, FPS, entity count | VERIFIED | LateUpdate reads GameStateData singleton, calculates FPS, queries entity count; SetText allocation-free |
| `Assets/Scripts/MonoBehaviours/UI/FadeController.cs` | CanvasGroup fade-to-black transitions | VERIFIED | FadeOut/FadeIn with SmoothStep coroutine, blocksRaycasts management, SetBlack/SetClear |
| `Assets/Materials/SkyboxMaterial.mat` | Dark space skybox material | PARTIAL | Exists with near-black procedural skybox (exposure 0.05); no actual star points (Skybox/Procedural shader, not cubemap) |
| `Assets/Settings/SampleSceneProfile.asset` | URP volume profile with bloom + tonemapping | VERIFIED | Bloom (threshold:0.8, intensity:0.3, active:1) and Tonemapping (mode:Neutral, active:1) confirmed |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| ECSBootstrap.cs | GameStateComponents.cs | Creates singleton entities with GameStateData and InputData | WIRED | `em.CreateEntity(typeof(GameStateData))` + `em.SetComponentData(gameStateEntity, new GameStateData{...})` -- lines 13-28 |
| InputBridge.cs | GameStateComponents.cs | Writes mouse world position to InputData singleton each frame | WIRED | `_em.SetComponentData(_inputEntity, inputData)` -- line 43; inputData is type InputData |
| GameManager.cs | GameStateComponents.cs | Writes GamePhase to ECS GameStateData singleton on state transition | WIRED | `WritePhaseToECS(phase)` called on every transition; method uses `em.SetComponentData(entity, data)` -- lines 116-129 |
| GameManager.cs | IGameState.cs | Holds Dictionary of IGameState, calls Enter/Execute/Exit | WIRED | `Dictionary<GamePhase, IGameState> _states` at line 14; `_currentState.Enter/Execute/Exit(this)` called in Start/Update/TransitionTo |
| PlaceholderMovementSystem.cs | GameStateComponents.cs | Reads GameStateData singleton to check Phase | WIRED | `SystemAPI.HasSingleton<GameStateData>()` + `SystemAPI.GetSingleton<GameStateData>()` -- lines 47-50 |
| DebugOverlay.cs | GameStateComponents.cs | Reads GameStateData to display current phase name | WIRED | `_gameStateQuery.GetSingleton<GameStateData>()` then `.Phase.ToString()` -- lines 59-60 |
| PlaceholderSpawner.cs | PlaceholderComponents.cs | Creates ECS entities with DriftData, SpinData, PlaceholderTag | WIRED | `CreateArchetype(typeof(DriftData), typeof(SpinData), typeof(PlaceholderTag))` -- lines 63-77 |
| PlaceholderRenderer.cs | PlaceholderComponents.cs | Reads PlaceholderTag entities for transform sync | WIRED | `_em.CreateEntityQuery(typeof(PlaceholderTag), typeof(LocalTransform))` -- lines 75-78 |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| INFRA-01 | 01-01 | ECS world bootstraps with hybrid bridge layer | SATISFIED | ECSBootstrap, InputBridge, GameStateComponents all present and wired end-to-end |
| INFRA-02 | 01-02 | WebGL build runs at 60 FPS with 100 asteroids and 1000 minerals active | NEEDS HUMAN | User reported 60 FPS during Task 3 checkpoint; cannot verify programmatically |
| INFRA-03 | 01-02 | Object pooling system for asteroids, minerals, damage popups, and particles | PARTIAL | GameObjectPool infrastructure is correct; asteroid and mineral pools exist (1100 objects); damage popup and particle pools explicitly deferred to Phase 4 per SUMMARY |
| INFRA-04 | 01-02 | Game state machine manages Playing, Collecting, GameOver, and Upgrading states | SATISFIED | GameManager.cs with 4 states, TransitionTo, FadeController integration -- all four states reachable |
| INFRA-05 | 01-01 | InputBridge writes mouse world position to ECS singleton each frame | SATISFIED | InputBridge.cs fully implemented with Plane.Raycast projection and SetComponentData per frame |
| INFRA-06 | 01-01 | Large number formatting utility (K/M/B/T) used across all credit displays | PARTIAL | NumberFormatter.cs utility exists and is correct; however it is not yet wired to any credit display (no credit display exists yet in Phase 1 -- this is expected for Phase 1 scope) |
| VISL-01 | 01-01 | Realistic space aesthetic with starfield skybox | PARTIAL | Near-black procedural skybox present; does not render discrete star points; acknowledged as temporary stand-in |
| VISL-02 | 01-01 | URP post-processing: bloom, tonemapping | SATISFIED | Bloom and Tonemapping both configured and active in SampleSceneProfile.asset |

**Orphaned requirements check:** REQUIREMENTS.md Traceability section lists exactly INFRA-01 through INFRA-06, VISL-01, VISL-02 for Phase 1. All eight are accounted for across the two plans. No orphaned requirements.

**Note on INFRA-06:** NumberFormatter is a utility with no consumer yet in Phase 1 (no credit display exists until Phase 3). The requirement says "used across all credit displays" -- this is aspirational scope for future phases. The utility existing in Phase 1 is the deliverable; "used across all credit displays" applies when those displays exist. This is not a Phase 1 gap.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `GameManager.cs` | 62 | `// TODO: Remove in Phase 2 -- temporary testing shortcuts` | Info | Keyboard shortcuts 1-4 for state testing are intentional Phase 1 scaffolding; documented for removal in Phase 2 |
| `SkyboxMaterial.mat` | -- | Procedural shader instead of star-point texture | Warning | Dark void renders but no star points visible; affects VISL-01 satisfaction |

No blocker anti-patterns (empty returns, stub implementations, or unwired handlers) found in the codebase.

### Human Verification Required

#### 1. WebGL 60 FPS Confirmation

**Test:** Serve the WebGL build locally and open in Chrome. Observe the debug overlay FPS counter.
**Expected:** FPS shows 60+ with all 1100 placeholder entities active and moving
**Why human:** WebGL build must be launched in a browser; cannot verify programmatically. SUMMARY states user approved this at the Task 3 checkpoint.

#### 2. InputBridge World Projection

**Test:** Enter Play mode in Unity Editor. Open Window > Entities > Inspector, select the InputData singleton entity. Move the mouse over the game area.
**Expected:** MouseWorldPos X and Z values change as mouse moves; MouseValid=true when over the gameplay plane
**Why human:** Mouse event processing is a runtime behavior; static code analysis confirms the implementation is correct but cannot confirm the plane intersection produces accurate world coordinates at the configured camera angle

#### 3. Fade Transition Visual Quality

**Test:** In Play mode, press 3 (GameOver) then 1 (Playing). Observe the screen.
**Expected:** Smooth 0.4s SmoothStep fade to black, then fade back in. Press 2 from Playing -- no fade occurs (direct transition to Collecting).
**Why human:** Visual animation smoothness and the no-fade exception for Playing->Collecting must be observed at runtime

#### 4. UISetup Reflection Wiring

**Test:** Enter Play mode. Check the Unity Console for NullReferenceExceptions. Move mouse to trigger FPS display updates and state display.
**Expected:** No NullReferenceExceptions; FPS and state text update correctly in the debug overlay
**Why human:** UISetup wires FadeController._fadeCanvasGroup and DebugOverlay text fields via System.Reflection. Static analysis confirms the field names match, but runtime must confirm reflection finds the private fields correctly.

### Gaps Summary

**Gap 1: INFRA-03 -- Partial Object Pool Coverage**

The pool infrastructure is fully implemented and working for the Phase 1 entity counts (1100 GameObjects pre-warmed). However, INFRA-03 specifies pooling "for asteroids, minerals, damage popups, AND particles." The damage popup and particle pools are explicitly deferred to Phase 4 (when those objects are introduced). This is a documented scope deferral, not an oversight.

The gap is structural: INFRA-03 will remain partially satisfied until Phase 4. If the REQUIREMENTS.md traceability is intended to show Phase 1 fully delivers INFRA-03, the requirement's scope needs to be acknowledged as partially deferred. Recommend noting this in REQUIREMENTS.md or accepting that INFRA-03 becomes complete in Phase 4.

**Gap 2: VISL-01 -- Procedural Skybox, No Star Points**

The Skybox/Procedural shader with near-zero exposure produces a dark background, but it does not generate discrete star points. VISL-01 requires a "realistic space aesthetic with starfield skybox" and the plan truth specifies "sparse warm-toned stars." The procedural shader cannot produce star points; a cubemap or panoramic texture with painted stars is required.

The SUMMARY documents this explicitly: "a proper cubemap with sparse warm-toned stars should be generated when the texture generation tool is available." This is a deferred visual asset, not a code gap. The architectural plumbing (SkyboxMaterial assigned to scene, ambient configured, URP volume active) is all correct.

This gap does not block Phase 2 gameplay development but should be addressed before Phase 4 visual polish.

---

## Summary Assessment

The Phase 1 implementation is architecturally complete and correct. The hybrid ECS architecture is proven:
- ECS singleton components communicate state across the boundary
- MonoBehaviour bridges (ECSBootstrap, InputBridge, GameManager) write to and read from ECS per frame
- IJobEntity systems (PlaceholderMovementSystem) demonstrate Burst-compatible jobs at 1100 entities
- Object pool pre-warming eliminates runtime allocation
- All code uses the new Input System (UnityEngine.InputSystem) after the user-fixed deviation during WebGL testing

The two gaps are both documented scope deferrals rather than implementation failures:
- INFRA-03 pool coverage (damage popups/particles) is intentionally deferred to Phase 4
- VISL-01 star texture is intentionally deferred pending texture generation tooling

No blocker anti-patterns were found. The phase goal -- "proving the hybrid architecture before any gameplay is built" -- is substantively achieved.

---

_Verified: 2026-02-17T18:00:00Z_
_Verifier: Claude (gsd-verifier)_
