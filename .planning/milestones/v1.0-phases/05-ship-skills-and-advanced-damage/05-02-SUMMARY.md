---
phase: 05-ship-skills-and-advanced-damage
plan: 02
subsystem: gameplay
tags: [ui, vfx, particles, audio, skills, cooldowns, unity-ugui, linerenderer, particlesystem]

# Dependency graph
requires:
  - phase: 05-ship-skills-and-advanced-damage
    provides: SkillEvent buffer, SkillCooldownData/OverchargeBuffData singletons, BurningData component
  - phase: 04-visual-and-audio-feedback
    provides: FeedbackEventBridge dispatcher, AudioManager, DamagePopupManager, ExplosionManager patterns
provides:
  - Skill bar UI with 4 slots, radial cooldown overlays, keybind badges, and click activation
  - Skill VFX (laser beam, chain lightning arcs, EMP particle blast, Overcharge mining circle glow)
  - Burning ember particle trails on DoT-affected asteroids
  - FeedbackEventBridge SkillEvent dispatch to VFX and audio consumers
  - AudioManager crit hit and skill activation SFX methods
  - MiningCircleVisual Overcharge buff color/scale feedback
affects: [06-tech-tree-and-level-progression]

# Tech tracking
tech-stack:
  added: []
  patterns: [pooled-linerenderer-vfx, pooled-particlesystem-tracking, radial-fill-cooldown-ui, ecs-singleton-ui-read]

key-files:
  created:
    - Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs
    - Assets/Scripts/MonoBehaviours/Rendering/SkillVFXManager.cs
    - Assets/Scripts/MonoBehaviours/Rendering/BurningEffectManager.cs
  modified:
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs
    - Assets/Scripts/MonoBehaviours/Bridge/FeedbackEventBridge.cs
    - Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs
    - Assets/Scripts/MonoBehaviours/Rendering/MiningCircleVisual.cs

key-decisions:
  - "Removed CameraShake on critical hits per user decision -- crits have sound only, no screen shake"
  - "SkillVFXManager and BurningEffectManager self-instantiate via RuntimeInitializeOnLoadMethod (same pattern as ExplosionManager)"
  - "Skill bar created programmatically by UISetup with radial fill overlays and keybind badges"
  - "BurningEffectManager tracks entities via Dictionary<Entity, GameObject> matching AsteroidRenderer pattern"

patterns-established:
  - "Radial cooldown UI: Image.Type.Filled + Radial360 + fillAmount driven by ECS singleton reads in LateUpdate"
  - "Skill VFX pooling: LineRenderer for beams/arcs, ParticleSystem for AoE blasts, coroutine disable timers"
  - "Entity-tracking particle effects: Dictionary<Entity, GameObject> with pool acquire/release on component presence"

requirements-completed: [SKIL-05, SKIL-06, DMGS-03]

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 5 Plan 02: Skill UI, VFX, and Audio Summary

**Skill bar UI with 4-slot radial cooldowns, laser/lightning/EMP/overcharge VFX, burning ember particles, and crit/skill audio wiring**

## Performance

- **Duration:** ~8 min (including user verification)
- **Started:** 2026-02-18T07:36:00Z
- **Completed:** 2026-02-18T07:53:00Z
- **Tasks:** 2 (1 auto + 1 checkpoint)
- **Files modified:** 7 (3 created, 4 modified)

## Accomplishments
- Skill bar UI with 4 interactive slots: colored skill icons, radial cooldown fill overlays, countdown text, keybind badges (1-4), and click activation via ECS singleton writes
- Skill VFX system: pooled LineRenderer beams for Laser Burst (cyan) and Chain Lightning (blue-white jagged), ParticleSystem burst for EMP Pulse (purple-blue), Overcharge no-op (handled by MiningCircleVisual)
- BurningEffectManager tracks entities with BurningData component, attaches pooled ember ParticleSystems that follow asteroid positions
- FeedbackEventBridge extended with DrainSkillEvents dispatching to SkillVFXManager and AudioManager per skill type
- AudioManager extended with PlaySkillSfx (per-skill clips) and PlayCritHit (higher-pitched distinct sound)
- MiningCircleVisual reads OverchargeBuffData to swap between cyan (normal) and gold (buffed) with radius scaling
- Removed CameraShake on critical hits per user decision -- crits now play sound only

## Task Commits

Each task was committed atomically:

1. **Task 1: Create SkillBarController UI, SkillVFXManager, BurningEffectManager, extend FeedbackEventBridge and AudioManager, modify MiningCircleVisual** - `3c77cb3` (feat)
2. **Task 2: Verify complete Phase 5 skill and damage system** - checkpoint:human-verify (user approved)

## Files Created/Modified

**Created:**
- `Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs` - Skill bar UI with 4 slots, radial cooldown fill, keybind badges, click-to-activate via ECS singleton
- `Assets/Scripts/MonoBehaviours/Rendering/SkillVFXManager.cs` - Pooled VFX: LineRenderer beams (laser, chain lightning), ParticleSystem (EMP blast), self-instantiated
- `Assets/Scripts/MonoBehaviours/Rendering/BurningEffectManager.cs` - Ember particle pool tracking burning asteroid entities via Dictionary<Entity, GameObject>

**Modified:**
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Added CreateSkillBarCanvas() with 4 programmatic skill slots and SkillBarController initialization
- `Assets/Scripts/MonoBehaviours/Bridge/FeedbackEventBridge.cs` - Added DrainSkillEvents() dispatching SkillEvent buffer to VFX and audio; removed CameraShake on crits, added crit sound
- `Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs` - Added PlaySkillSfx(int) with per-skill clip loading, PlayCritHit(Vector3) with elevated pitch
- `Assets/Scripts/MonoBehaviours/Rendering/MiningCircleVisual.cs` - Added OverchargeBuffData singleton read for gold color and radius scaling during buff

## Decisions Made
- Removed CameraShake on critical hits per user decision: "no extra screen flash or shake" on crits. Crits now have sound-only feedback.
- SkillVFXManager and BurningEffectManager self-instantiate via RuntimeInitializeOnLoadMethod (consistent with ExplosionManager pattern from Phase 4)
- Skill bar created entirely programmatically by UISetup -- no manual scene editor setup required
- BurningEffectManager uses Dictionary<Entity, GameObject> entity-tracking pattern matching AsteroidRenderer

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 5 complete: all 4 skills have full ECS backend + presentation layer (UI, VFX, audio)
- Skill bar UI ready for Phase 6 tech tree gating (show/hide slots based on unlock state)
- Audio clips loaded via Resources.Load with graceful null degradation -- actual audio asset creation is optional polish
- All skill, crit, and DoT systems are functional and verified by user in Unity Play Mode

## Self-Check: PASSED

- All 3 created files verified on disk (SkillBarController.cs, SkillVFXManager.cs, BurningEffectManager.cs)
- All 4 modified files verified on disk (UISetup.cs, FeedbackEventBridge.cs, AudioManager.cs, MiningCircleVisual.cs)
- Task 1 commit verified in git log (3c77cb3)
- Task 2 checkpoint approved by user

---
*Phase: 05-ship-skills-and-advanced-damage*
*Completed: 2026-02-18*
