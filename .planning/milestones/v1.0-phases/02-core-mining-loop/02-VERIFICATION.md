---
phase: 02-core-mining-loop
verified: 2026-02-17T21:00:00Z
status: human_needed
score: 11/11 must-haves verified
re_verification:
  previous_status: gaps_found
  previous_score: 7/11
  gaps_closed:
    - "Asteroids render as 3D spheres with color tinting -- AsteroidRenderer now in scene (GameObject 'AsteroidSystem', m_Enabled: 1)"
    - "A ship placeholder is visible at the bottom of the screen -- AsteroidRenderer.CreateShipPlaceholder() now runs since component is in scene"
    - "A cyan glowing circle follows the mouse cursor on the gameplay plane -- MiningCircleVisual now in scene (GameObject 'MiningCircle', m_Enabled: 1)"
    - "Asteroids reaching 0 HP are destroyed and visually removed from play -- AsteroidRenderer pool cleanup pass is now active"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Confirm asteroid stream is visible in Play mode"
    expected: "Colored 3D spheres (gray/brown/rust) spawn at top of screen, drift downward with visible Y-axis spin, disappear at bottom boundary"
    why_human: "Cannot verify visual rendering programmatically"
  - test: "Confirm ship placeholder is visible at bottom of screen"
    expected: "A small gray diamond-like shape is visible near the bottom of the screen at approximately Z = -6"
    why_human: "Ship GameObject is created at runtime by AsteroidRenderer coroutine; requires Play mode to verify"
  - test: "Confirm mining circle bloom glow is visible"
    expected: "A cyan ring with visible HDR bloom halo follows the mouse cursor in Play mode. Bloom must be enabled in the URP Global Volume post-processing profile."
    why_human: "Bloom is a post-processing effect requiring visual inspection; HDR LineRenderer material alone does not guarantee visible glow without a configured post-processing volume"
  - test: "Confirm end-to-end mining destroys asteroids in approximately 2.5 seconds"
    expected: "Hovering mining circle over an asteroid destroys it after approximately 2.5 seconds (100 HP / 10 dmg per 0.25s tick). Pooled asteroid GameObject disappears from screen."
    why_human: "Cannot test real-time gameplay interactions programmatically"
---

# Phase 2: Core Mining Loop Verification Report

**Phase Goal:** Player can hover a mining circle over asteroids that drift down the screen, watch them take damage and break apart -- the fundamental interaction that the entire game depends on
**Verified:** 2026-02-17T21:00:00Z
**Status:** human_needed
**Re-verification:** Yes -- after gap closure (commit d28dcaf added AsteroidSystem and MiningCircle GameObjects to Game.unity)

## Goal Achievement

### Observable Truths

**From Plan 02-01 (must_haves):**

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Asteroids spawn at the top of the screen and drift downward continuously | VERIFIED | AsteroidSpawnSystem.cs: ECB creates entity at `(x, 0, PlayAreaZMax)`. AsteroidMovementSystem AsteroidDriftJob subtracts along -Z. GamePhase.Playing guard confirmed. |
| 2 | Asteroids visually rotate (spin) while drifting | VERIFIED | AsteroidSpinJob with `[WithAll(typeof(AsteroidTag))]` calls `transform.RotateY(spin.RadiansPerSecond * DeltaTime)` in AsteroidMovementSystem. |
| 3 | Asteroids that reach the bottom of the screen disappear (destroyed) | VERIFIED | AsteroidBoundsSystem.cs: `if (transform.ValueRO.Position.z < GameConstants.PlayAreaZMin)` then `ecb.DestroyEntity(entity)`. |
| 4 | Each asteroid has HP that can be read by other systems | VERIFIED | AsteroidComponents.cs defines `HealthData { MaxHP; CurrentHP; }` and `DamageTickTimer { Elapsed; }`. Spawn sets `CurrentHP = MaxHP = 100f`. |
| 5 | Asteroids render as 3D spheres with color tinting | VERIFIED | AsteroidRenderer.cs (192 lines) -- full lifecycle, Dictionary<Entity,GameObject>, pool. NOW WIRED: Scene has "AsteroidSystem" GameObject with AsteroidRenderer component, m_Enabled: 1, GUID 8a7f5fa1782e8804bb5798764a901703 confirmed at Game.unity line 679. Commit d28dcaf added 272 lines to scene file. |
| 6 | A ship placeholder is visible at the bottom of the screen | VERIFIED (code) | AsteroidRenderer.CreateShipPlaceholder() called from coroutine at line 93. Creates primitive at (0, 0, GameConstants.ShipPositionZ), scale (1.0, 0.1, 0.5). AsteroidRenderer IS in scene. Requires Play mode to confirm visual. |

**From Plan 02-02 (must_haves):**

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 7 | A cyan glowing circle follows the mouse cursor on the gameplay plane | VERIFIED | MiningCircleVisual.cs (116 lines) NOW WIRED: Scene has "MiningCircle" GameObject with MiningCircleVisual component, m_Enabled: 1, GUID 371b181cf8e420d429603c82630d4045 confirmed at Game.unity line 725. Fields `_segments: 64`, `_lineWidth: 0.08`, `_hdrIntensity: 4` serialized correctly. |
| 8 | Asteroids within the mining circle take damage over time at a configurable tick rate | VERIFIED | MiningDamageSystem.cs: Reads `InputData.MouseWorldPos`, computes `distSq <= radiusSq` on XZ plane, accumulates `DamageTickTimer.Elapsed`, applies `health.ValueRW.CurrentHP -= config.DamagePerTick` per tick. ISystem auto-registered. |
| 9 | Asteroids reaching 0 HP are destroyed and visually removed from play | VERIFIED | Entity destruction: AsteroidDestructionSystem checks `CurrentHP <= 0f` and calls ECB.DestroyEntity. Visual cleanup: AsteroidRenderer LateUpdate cleanup pass at line 172 releases pool objects for entities not in activeEntities HashSet. AsteroidRenderer is now in scene. |
| 10 | Mining circle radius and damage values are configurable via MiningConfigData singleton | VERIFIED | ECSBootstrap creates MiningConfigData singleton with `Radius=2.5f`, `DamagePerTick=10f`, `TickInterval=0.25f` from GameConstants. MiningDamageSystem and MiningCircleVisual both read it. |
| 11 | The mining circle visually glows with bloom effect | VERIFIED (code) | HDR cyan at 4x intensity (`new Color(0f, 1f, 1f) * _hdrIntensity`) via URP Unlit shader, set on LineRenderer. MiningCircleVisual IS in scene. Bloom requires URP Global Volume post-processing -- requires human verification. |

**Score: 11/11 truths verified (all code-level checks pass; 4 items need human Play mode confirmation)**

### Required Artifacts

**Plan 02-01 artifacts:**

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/ECS/Components/AsteroidComponents.cs` | AsteroidTag, HealthData, DamageTickTimer, AsteroidSpawnTimer, MiningConfigData | VERIFIED | All 5 components present, Burst-compatible unmanaged structs |
| `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs` | Periodic asteroid creation via ECB | VERIFIED | 76 lines, EntityQuery count cap, RNG seeded in OnCreate, full archetype creation |
| `Assets/Scripts/ECS/Systems/AsteroidMovementSystem.cs` | Drift and spin for asteroid entities | VERIFIED | 67 lines, AsteroidDriftJob + AsteroidSpinJob, both `[WithAll(typeof(AsteroidTag))]` |
| `Assets/Scripts/ECS/Systems/AsteroidBoundsSystem.cs` | Destroys asteroids below play area via ECB | VERIFIED | 42 lines, Z check against `GameConstants.PlayAreaZMin`, ECB.DestroyEntity |
| `Assets/Scripts/ECS/Systems/AsteroidDestructionSystem.cs` | Destroys asteroids with 0 HP via ECB | VERIFIED | 31 lines, `CurrentHP <= 0f` check, WithAll<AsteroidTag> |
| `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` | ECS-to-GameObject sync for dynamic asteroid lifecycle | VERIFIED + WIRED | 192 lines, Dictionary<Entity,GameObject>, pool lifecycle. Attached to "AsteroidSystem" GameObject in scene (m_Enabled: 1). |
| `Assets/Scripts/Shared/GameConstants.cs` | Play area bounds, spawn defaults, mining defaults | VERIFIED | 17 const fields covering all required tuning values |

**Plan 02-02 artifacts:**

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/ECS/Systems/MiningDamageSystem.cs` | Tick-based AoE damage to asteroids within mining radius | VERIFIED | 65 lines, Burst-compiled ISystem, XZ distance check, per-entity tick accumulation |
| `Assets/Scripts/MonoBehaviours/Rendering/MiningCircleVisual.cs` | LineRenderer circle following mouse with HDR emissive bloom glow | VERIFIED + WIRED | 116 lines, unit-circle LineRenderer, HDR material, ECS singleton reads. Attached to "MiningCircle" GameObject in scene (m_Enabled: 1). |

### Key Link Verification

**Plan 02-01 key links:**

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| AsteroidSpawnSystem | AsteroidTag + HealthData + DriftData + SpinData + DamageTickTimer | ECB.CreateEntity with full component archetype | WIRED | AsteroidSpawnSystem.cs lines 64-74: `ecb.AddComponent(entity, new AsteroidTag())` plus all required components |
| AsteroidRenderer | AsteroidTag entities | EntityQuery in LateUpdate, Dictionary<Entity, GameObject> lifecycle | WIRED | `_asteroidQuery` created on line 71 (coroutine), LateUpdate sync loop at line 156. MonoBehaviour in scene -- wiring complete. |
| AsteroidBoundsSystem | ECB.DestroyEntity | Position Z check against lower bound | WIRED | `ecb.DestroyEntity(entity)` called when `Position.z < GameConstants.PlayAreaZMin` |

**Plan 02-02 key links:**

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| MiningDamageSystem | InputData.MouseWorldPos | SystemAPI.GetSingleton<InputData>() | WIRED | `var input = SystemAPI.GetSingleton<InputData>(); if (!input.MouseValid) return;` then `math.distancesq(asteroidPos, input.MouseWorldPos)` |
| MiningDamageSystem | HealthData.CurrentHP | Subtracts DamagePerTick when DamageTickTimer fires | WIRED | `health.ValueRW.CurrentHP -= config.DamagePerTick` when `tickTimer.ValueRO.Elapsed >= config.TickInterval` |
| MiningDamageSystem | MiningConfigData | Reads Radius, DamagePerTick, TickInterval from singleton | WIRED | `var config = SystemAPI.GetSingleton<MiningConfigData>(); float radiusSq = config.Radius * config.Radius;` |
| MiningCircleVisual | InputData.MouseWorldPos | Reads mouse position each frame and positions circle GameObject | WIRED | `var input = _em.GetComponentData<InputData>(_inputEntity);` then `transform.position = new Vector3(input.MouseWorldPos.x, 0.05f, input.MouseWorldPos.y)`. MonoBehaviour in scene. |
| AsteroidDestructionSystem | HealthData.CurrentHP <= 0 | Destroys entity when HP depleted | WIRED | `if (health.ValueRO.CurrentHP <= 0f) ecb.DestroyEntity(entity)` |

### Requirements Coverage

All 11 requirement IDs declared across Plan 02-01 and Plan 02-02 frontmatter, cross-referenced against REQUIREMENTS.md:

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| ASTR-01 | 02-01 | Asteroids spawn at top of screen and drift downward | SATISFIED | AsteroidSpawnSystem + AsteroidMovementSystem verified |
| ASTR-02 | 02-01 | Each asteroid has HP determined by its resource tier | PARTIAL | HealthData exists with flat `DefaultAsteroidHP = 100f`. No tier system -- phase 2 scoped to single tier. Full tier-based HP deferred to Phase 6. Acceptable partial for this phase. |
| ASTR-03 | 02-01 | Asteroids have 3D PBR models with per-resource-type visual appearance | PARTIAL | AsteroidRenderer creates primitive spheres with 3 color options (gray/brown/rust). No PBR materials, no per-resource-type differentiation. PBR and resource-type visuals deferred to Phase 4/6. Placeholder rendering is now wired to scene. |
| ASTR-04 | 02-01 | Asteroids rotate (spin) while drifting for visual interest | SATISFIED | AsteroidSpinJob verified |
| ASTR-05 | 02-01 | Asteroids that reach the bottom of the screen are destroyed (missed) | SATISFIED | AsteroidBoundsSystem verified |
| VISL-05 | 02-01 | Ship visual at bottom of screen (stationary) | SATISFIED (code) | AsteroidRenderer.CreateShipPlaceholder() called from coroutine when AsteroidRenderer initializes. AsteroidRenderer is in scene. Requires Play mode to visually confirm. |
| MINE-01 | 02-02 | Mining circle follows mouse cursor on the gameplay plane | SATISFIED (code) | MiningCircleVisual.Update() reads InputData.MouseWorldPos and positions circle. MiningCircleVisual is in scene. Requires Play mode to visually confirm. |
| MINE-02 | 02-02 | Mining circle deals tick-based AoE damage to all asteroids within radius | SATISFIED | MiningDamageSystem is ISystem (auto-registered), XZ distance check verified functional |
| MINE-03 | 02-02 | Damage rate and damage amount are configurable and upgradeable | SATISFIED | MiningConfigData singleton with DamagePerTick and TickInterval, readable by any system |
| MINE-04 | 02-02 | Mining circle radius is upgradeable via tech tree | PARTIAL | Radius stored in MiningConfigData singleton and readable at runtime. Tech tree upgrade mechanism (Phase 6) not yet built -- expected. |
| MINE-05 | 02-02 | Mining circle has visual feedback (cyan emissive ring with bloom glow) | SATISFIED (code) | MiningCircleVisual uses HDR emissive URP Unlit material at 4x intensity. Component is in scene. Bloom requires configured URP post-processing volume -- human verification needed. |

**Orphaned requirements:** None. All 11 Phase 2 requirements from REQUIREMENTS.md traceability table are claimed by Plan 02-01 or Plan 02-02.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Assets/Scripts/MonoBehaviours/Core/PlaceholderSpawner.cs` | 47 | `enabled = false;` early return in Start() | Info | PlaceholderSpawner is not in the scene (grep found no scene reference). Script self-disable is a no-op safety net. Not a blocker. |
| `Assets/Scripts/MonoBehaviours/Core/PlaceholderRenderer.cs` | 60 | `enabled = false;` early return in Start() | Info | PlaceholderRenderer is not in the scene. Same as above. |

No blocker anti-patterns found. All previously-flagged blocker (AsteroidRenderer and MiningCircleVisual absent from scene) is resolved.

### Human Verification Required

### 1. Asteroid Stream and Ship Placeholder

**Test:** Enter Play mode in Unity Editor. Observe the game viewport.
**Expected:** Colored 3D spheres (gray, brown, or rust tones) spawn at the top of the screen and drift downward with visible Y-axis spin rotation. A small gray diamond/cube shape is visible near the bottom of the screen (ship placeholder at Z=-6). Asteroids disappear when they reach the bottom boundary.
**Why human:** Visual rendering requires Unity Play mode; cannot verify with static analysis.

### 2. Mining Circle Bloom Glow

**Test:** Enter Play mode, move the mouse over the game viewport.
**Expected:** A cyan ring follows the mouse cursor. The ring should have a visible bloom halo (soft glowing edge). If bloom is not visible, check that the URP Global Volume in the scene has Bloom enabled in its post-processing profile (Intensity > 0, Threshold <= 0.8).
**Why human:** Bloom is a post-processing effect requiring visual inspection. HDR LineRenderer material alone does not guarantee visible glow without a properly configured URP post-processing volume.

### 3. End-to-End Mining Interaction

**Test:** Enter Play mode. Hover the mining circle over a drifting asteroid and hold it there.
**Expected:** After approximately 2.5 seconds of sustained hover (100 HP / 10 dmg per 0.25s tick), the asteroid disappears. The pooled sphere GameObject is returned to the pool and vanishes from screen. Multiple asteroids within the circle should take damage simultaneously.
**Why human:** Cannot test real-time gameplay interactions programmatically.

---

## Re-Verification Summary

**Previous status:** gaps_found (7/11 truths verified)
**Current status:** human_needed (11/11 truths verified at code level)

**All 4 gaps closed by commit d28dcaf:**

The previous VERIFICATION.md identified a single root cause: AsteroidRenderer and MiningCircleVisual scripts were not attached to any GameObject in the scene. Commit `d28dcaf` (2026-02-17) modified `Assets/Scenes/Game.unity` with 272 insertions, adding:

- `AsteroidSystem` GameObject with AsteroidRenderer component attached and enabled (`m_Enabled: 1`). GUID `8a7f5fa1782e8804bb5798764a901703` confirmed present at scene line 679. Serialized fields: `_asteroidScaleMin: 0.8`, `_asteroidScaleMax: 1.5`.
- `MiningCircle` GameObject with MiningCircleVisual component attached and enabled (`m_Enabled: 1`). GUID `371b181cf8e420d429603c82630d4045` confirmed present at scene line 725. Serialized fields: `_segments: 64`, `_lineWidth: 0.08`, `_hdrIntensity: 4`.

**No regressions detected:**
- All 7 previously-verified truths still pass (same file sizes, same key method signatures)
- PlaceholderMovementSystem still has `[DisableAutoCreation]`
- PlaceholderSpawner and PlaceholderRenderer are not in the scene and self-disable in Start() as safety

**Remaining work:** Play mode human verification only. All code and scene wiring is correct.

---

_Verified: 2026-02-17T21:00:00Z_
_Verifier: Claude (gsd-verifier)_
_Re-verification: Yes -- after commit d28dcaf_
