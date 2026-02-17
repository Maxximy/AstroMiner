# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-17)

**Core value:** The mining-collecting-upgrading loop must feel satisfying -- hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.
**Current focus:** Phase 3 - Collection, Economy & Session (IN PROGRESS)

## Current Position

Phase: 3 of 6 (Collection, Economy & Session)
Plan: 2 of 3 in current phase (03-02 complete)
Status: Executing
Last activity: 2026-02-17 -- Completed 03-02 Session Timer & Economy HUD

Progress: [########░░] 50%

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 7 min
- Total execution time: 0.7 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation-webgl-validation | 2 | 12 min | 6 min |
| 02-core-mining-loop | 2 | 28 min | 14 min |
| 03-collection-economy-and-session | 2 | 7 min | 3.5 min |

**Recent Trend:**
- Last 5 plans: 01-02 (8 min), 02-01 (3 min), 02-02 (25 min), 03-01 (3 min), 03-02 (4 min)
- Trend: fast execution for component-heavy plans without checkpoints

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Hybrid ECS (DOTS simulation + GameObject rendering) -- core architecture
- No Entities Graphics package (WebGL incompatible) -- all rendering via GameObject SpriteRenderers
- WebGL .jslib IndexedDB flush required from Phase 1
- Procedural skybox shader as temporary stand-in for cubemap texture (01-01)
- Camera at Y=18, 60-degree angle for perspective top-down view (01-01)
- Directional light reduced to 0.5 intensity with warm tint for space aesthetic (01-01)
- Vignette disabled in volume profile for clean visual style (01-01)
- UISetup creates all UI hierarchy programmatically -- no manual scene editor work needed (01-02)
- PlaceholderRenderer syncs ECS transforms to pooled GameObjects in MonoBehaviour LateUpdate (01-02)
- **NEW INPUT SYSTEM ONLY:** User mandates UnityEngine.InputSystem (new) exclusively -- UnityEngine.Input (legacy) must NOT be used. InputBridge needs migration. (01-02)
- XZ plane coordinate system for all Phase 2+ entities, aligning with InputBridge mouse projection (02-01)
- Placeholder systems disabled (not deleted) to preserve DriftData/SpinData component definitions (02-01)
- AsteroidMovementSystem uses WithAll<AsteroidTag> filter to scope jobs to asteroid entities only (02-01)
- GameConstants static class with const fields for Burst-accessible tuning values (02-01)
- LineRenderer with HDR unlit material for mining circle visual -- simple, bloom-compatible, no extra mesh (02-02)
- Unit-circle positions in LineRenderer scaled by transform.localScale from MiningConfigData.Radius (02-02)
- Debug key shortcuts removed from GameManager; mining interaction is the core gameplay (02-02)
- [BurstCompile] must NOT be on OnCreate methods in ISystem structs (causes compilation errors) (02-02)
- Direct GameStateData.Credits increment in MineralCollectionSystem (DynamicBuffer events deferred to Phase 4) (03-01)
- MineralsSpawnedTag prevents double-spawn from ECB one-frame delay (03-01)
- Gold/amber mineral color to distinguish from dark asteroid palette (03-01)
- MineralRenderer pool: 200 pre-warm, 1200 max for 1000+ mineral scalability (03-01)
- Credits persistent across runs, CreditsAtRunStart snapshot tracks per-run delta for display (03-02)
- CollectingState 2-second grace period handles ECB entity destruction delay edge case (03-02)
- UISetup CreateButton helper for consistent programmatic button creation (03-02)
- HUD visibility toggled by SetActive on root GameObject based on game phase (03-02)

### Pending Todos

- Migrate InputBridge from legacy UnityEngine.Input to new UnityEngine.InputSystem (before Phase 3 input work)

### Blockers/Concerns

- ~~WebGL DOTS performance at target entity counts (100 asteroids + 1000 minerals) is unvalidated -- Phase 1 must benchmark this~~ RESOLVED: WebGL validated at 60 FPS with 1100 entities
- Economy balance values unknown -- spreadsheet model needed before Phase 6 upgrade code

## Session Continuity

Last session: 2026-02-17
Stopped at: Completed 03-02-PLAN.md (Session Timer & Economy HUD) -- Ready for 03-03 (Save System & Persistence)
Resume file: .planning/phases/03-collection-economy-and-session/03-02-SUMMARY.md
