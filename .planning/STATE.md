# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-17)

**Core value:** The mining-collecting-upgrading loop must feel satisfying -- hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.
**Current focus:** Phase 5 - Ship Skills & Advanced Damage (Plan 01 complete, Plan 02 next)

## Current Position

Phase: 5 of 6 (Ship Skills & Advanced Damage)
Plan: 1 of 2 in current phase (05-01 complete)
Status: In Progress
Last activity: 2026-02-18 -- Completed 05-01 Skill Systems & Advanced Damage Backend

Progress: [##############░] 83%

## Performance Metrics

**Velocity:**
- Total plans completed: 10
- Average duration: 7 min
- Total execution time: 1.1 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation-webgl-validation | 2 | 12 min | 6 min |
| 02-core-mining-loop | 2 | 28 min | 14 min |
| 03-collection-economy-and-session | 3 | 11 min | 3.7 min |
| 04-visual-and-audio-feedback | 2 | 13 min | 6.5 min |
| 05-ship-skills-and-advanced-damage | 1 | 5 min | 5 min |

**Recent Trend:**
- Last 5 plans: 03-02 (4 min), 03-03 (4 min), 04-01 (5 min), 04-02 (8 min), 05-01 (5 min)
- Trend: consistent fast execution for well-specified plans

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
- SaveManager self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required (03-03)
- File.IO primary with PlayerPrefs fallback for save robustness across all platforms (03-03)
- Save schema pre-includes Phase 6 placeholder fields (TechTreeUnlocks, PlayerStatsData) to avoid future migration (03-03)
- Static _saveLoaded flag in PlayingState ensures credits load exactly once per session (03-03)
- DamageEvent/DestructionEvent use unmanaged byte RGB for Burst safety in ISystem (04-01)
- DamagePopupManager and ExplosionManager self-instantiate via RuntimeInitializeOnLoadMethod(AfterSceneLoad) (04-01)
- World-space Canvas at 0.02 scale with TMPro for damage popups -- billboard rotation each frame (04-01)
- Struct ActivePopup list for popup animation to minimize GC pressure (04-01)
- TrailRenderer.Clear() on pool release to prevent ghost trail artifacts (04-01)
- ECS DynamicBuffer event pipeline: ISystem adds to singleton buffer -> MonoBehaviour drains in Update -> buffer.Clear() (04-01)
- AudioMixer at Resources/Audio/GameAudioMixer with Master > SFX + Music groups, loaded via Resources.Load with graceful degradation (04-02)
- All AudioSources use spatialBlend=0 (2D) with manual distance-based volume attenuation -- WebGL spatial audio is broken (04-02)
- FeedbackEventBridge is the single central dispatcher for all ECS event buffers to all visual and audio consumer systems (04-02)
- Mining hit SFX throttled to max 4/sec, collection chimes batch within 50ms window with pitch variation (04-02)
- CameraShake attached directly to Main Camera GameObject for correct transform offset (04-02)
- URP Vignette override state must be set per-property (intensity.overrideState, color.overrideState) -- Add<T>(overrideState) param does not work (04-02)
- SkillInputResetSystem as separate system running after all skill systems ensures clean one-frame-per-press consumption (05-01)
- OR-merge pattern (|=) in InputBridge preserves UI button presses set before keyboard polling (05-01)
- Chain Lightning does NOT apply DoT per research discretion recommendation (05-01)
- DoT damage popups use DamageType.Normal (white) not DamageType.DoT per user decision (05-01)
- Each skill system RNG seeded with unique OR-mask to avoid identical crit sequences across systems (05-01)

### Pending Todos

- Migrate InputBridge from legacy UnityEngine.Input to new UnityEngine.InputSystem (before Phase 3 input work)

### Blockers/Concerns

- ~~WebGL DOTS performance at target entity counts (100 asteroids + 1000 minerals) is unvalidated -- Phase 1 must benchmark this~~ RESOLVED: WebGL validated at 60 FPS with 1100 entities
- Economy balance values unknown -- spreadsheet model needed before Phase 6 upgrade code

## Session Continuity

Last session: 2026-02-18
Stopped at: Completed 05-01-PLAN.md
Resume file: .planning/phases/05-ship-skills-and-advanced-damage/05-01-SUMMARY.md
