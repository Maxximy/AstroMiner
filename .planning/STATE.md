# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-17)

**Core value:** The mining-collecting-upgrading loop must feel satisfying -- hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.
**Current focus:** Phase 2 - Core Mining Loop (COMPLETE)

## Current Position

Phase: 2 of 6 (Core Mining Loop) -- COMPLETE
Plan: 2 of 2 in current phase (all plans complete)
Status: Phase Complete
Last activity: 2026-02-17 -- Completed 02-02 Mining Circle & Damage (core mining interaction verified)

Progress: [######░░░░] 33%

## Performance Metrics

**Velocity:**
- Total plans completed: 4
- Average duration: 8 min
- Total execution time: 0.5 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation-webgl-validation | 2 | 12 min | 6 min |
| 02-core-mining-loop | 2 | 28 min | 14 min |

**Recent Trend:**
- Last 5 plans: 01-01 (4 min), 01-02 (8 min), 02-01 (3 min), 02-02 (25 min)
- Trend: stable (02-02 longer due to checkpoint verification)

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

### Pending Todos

- Migrate InputBridge from legacy UnityEngine.Input to new UnityEngine.InputSystem (before Phase 3 input work)

### Blockers/Concerns

- ~~WebGL DOTS performance at target entity counts (100 asteroids + 1000 minerals) is unvalidated -- Phase 1 must benchmark this~~ RESOLVED: WebGL validated at 60 FPS with 1100 entities
- Economy balance values unknown -- spreadsheet model needed before Phase 6 upgrade code

## Session Continuity

Last session: 2026-02-17
Stopped at: Completed 02-02-PLAN.md (Mining Circle & Damage) -- Phase 2 complete, ready for Phase 3 (Collection, Economy, Session)
Resume file: .planning/phases/02-core-mining-loop/02-02-SUMMARY.md
