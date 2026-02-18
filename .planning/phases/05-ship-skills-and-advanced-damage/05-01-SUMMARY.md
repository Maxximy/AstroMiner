---
phase: 05-ship-skills-and-advanced-damage
plan: 01
subsystem: gameplay
tags: [ecs, dots, burst, skills, crit, dot, cooldowns, unity-entities]

# Dependency graph
requires:
  - phase: 04-visual-and-audio-feedback
    provides: DamageEvent/DestructionEvent buffer pipeline, FeedbackEventBridge dispatcher
  - phase: 02-core-mining-loop
    provides: MiningDamageSystem, AsteroidTag, HealthData, MiningConfigData, InputData
provides:
  - Four skill ECS systems (LaserBurst, ChainLightning, EmpPulse, Overcharge)
  - SkillCooldownSystem with input flag reset pattern
  - BurningDamageSystem for per-entity DoT
  - Critical hit integration in MiningDamageSystem and all skill systems
  - SkillEvent IBufferElementData for VFX bridge consumption
  - SkillInputData singleton for keyboard and UI skill activation
affects: [05-02-skill-ui-and-vfx, 06-tech-tree-and-level-progression]

# Tech tracking
tech-stack:
  added: []
  patterns: [skill-cooldown-singleton, input-flag-or-merge, ecb-structural-burning, crit-roll-random]

key-files:
  created:
    - Assets/Scripts/ECS/Components/SkillComponents.cs
    - Assets/Scripts/ECS/Systems/SkillCooldownSystem.cs
    - Assets/Scripts/ECS/Systems/LaserBurstSystem.cs
    - Assets/Scripts/ECS/Systems/ChainLightningSystem.cs
    - Assets/Scripts/ECS/Systems/EmpPulseSystem.cs
    - Assets/Scripts/ECS/Systems/OverchargeSystem.cs
    - Assets/Scripts/ECS/Systems/BurningDamageSystem.cs
  modified:
    - Assets/Scripts/ECS/Components/FeedbackComponents.cs
    - Assets/Scripts/ECS/Systems/MiningDamageSystem.cs
    - Assets/Scripts/MonoBehaviours/Bridge/InputBridge.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/Shared/GameConstants.cs

key-decisions:
  - "SkillInputResetSystem as separate system running after all skill systems ensures clean one-frame-per-press consumption"
  - "OR-merge pattern (|=) in InputBridge preserves UI button presses set before keyboard polling"
  - "Chain Lightning does NOT apply DoT per research discretion recommendation"
  - "DoT damage popups use DamageType.Normal (white) not DamageType.DoT per user decision"
  - "Each RNG seeded with unique OR-mask to avoid identical crit sequences across systems"

patterns-established:
  - "Skill activation pattern: SkillInputData flags OR-merged by InputBridge, consumed by skill systems, reset by SkillInputResetSystem"
  - "Cooldown singleton pattern: SkillCooldownData tracks remaining/max per skill, decremented by SkillCooldownSystem"
  - "ECB structural changes for BurningData: add if missing, set if present, remove on expiry"
  - "Crit roll pattern: Unity.Mathematics.Random per system, seeded in OnCreate, roll < CritConfigData.CritChance"

requirements-completed: [SKIL-01, SKIL-02, SKIL-03, SKIL-04, SKIL-05, DMGS-01, DMGS-02]

# Metrics
duration: 5min
completed: 2026-02-18
---

# Phase 5 Plan 01: Skill Systems & Advanced Damage Summary

**Four ECS skill systems (Laser/Chain/EMP/Overcharge) with cooldowns, crit rolls on all damage sources, and per-entity DoT burning**

## Performance

- **Duration:** 5 min
- **Started:** 2026-02-18T08:24:37Z
- **Completed:** 2026-02-18T08:29:00Z
- **Tasks:** 3
- **Files modified:** 12 (7 created, 5 modified)

## Accomplishments
- Complete skill damage backend: Laser Burst (line), Chain Lightning (chain), EMP Pulse (AoE), Overcharge (buff)
- Critical hit system integrated into mining damage and all skill systems at 8% chance, 2x multiplier
- Burning DoT system with per-entity tick accumulation and automatic component removal on expiry
- SkillEvent buffer populated for Plan 02 VFX bridge consumption
- Input bridge extended with OR-merge pattern supporting both keyboard keys 1-4 and future UI buttons

## Task Commits

Each task was committed atomically:

1. **Task 1: Create skill ECS components, extend InputBridge, bootstrap singletons** - `a356c7e` (feat)
2. **Task 2a: SkillCooldownSystem, OverchargeSystem, crit in MiningDamageSystem** - `f6ba467` (feat)
3. **Task 2b: LaserBurst, ChainLightning, EmpPulse, BurningDamage systems** - `44f4196` (feat)

## Files Created/Modified

**Created:**
- `Assets/Scripts/ECS/Components/SkillComponents.cs` - SkillInputData, SkillCooldownData, CritConfigData, OverchargeBuffData, BurningData
- `Assets/Scripts/ECS/Systems/SkillCooldownSystem.cs` - Cooldown decrement + SkillInputResetSystem for flag clearing
- `Assets/Scripts/ECS/Systems/LaserBurstSystem.cs` - Line damage from ship to mouse with point-to-segment collision
- `Assets/Scripts/ECS/Systems/ChainLightningSystem.cs` - Nearest-to-mouse primary target + up to 3 chain targets
- `Assets/Scripts/ECS/Systems/EmpPulseSystem.cs` - AoE damage within blast radius at mouse position
- `Assets/Scripts/ECS/Systems/OverchargeSystem.cs` - Buff activation and duration management
- `Assets/Scripts/ECS/Systems/BurningDamageSystem.cs` - Per-entity DoT ticking with ECB removal on expiry

**Modified:**
- `Assets/Scripts/ECS/Components/FeedbackComponents.cs` - Added SkillEvent IBufferElementData
- `Assets/Scripts/ECS/Systems/MiningDamageSystem.cs` - Integrated Overcharge buff multipliers and crit rolls
- `Assets/Scripts/MonoBehaviours/Bridge/InputBridge.cs` - Added keyboard 1-4 skill input via OR-merge pattern
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Bootstrap Phase 5 singletons and SkillEvent buffer
- `Assets/Scripts/Shared/GameConstants.cs` - Added skill, crit, and DoT tuning constants

## Decisions Made
- SkillInputResetSystem as a dedicated system (not embedded in SkillCooldownSystem) for clean UpdateAfter ordering
- OR-merge pattern (`|=`) in InputBridge preserves UI button presses that may be set before keyboard polling
- Chain Lightning does NOT apply DoT per research discretion recommendation
- DoT damage popups use DamageType.Normal (white style) not DamageType.DoT per user decision in planning
- Each system's RNG seeded with unique bit-OR mask to avoid identical crit sequences

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All skill damage systems functional, ready for Plan 02 VFX and UI
- SkillEvent buffer populated each activation for VFX bridge to consume
- SkillCooldownData singleton available for UI cooldown display
- OverchargeBuffData singleton available for visual buff indicator

## Self-Check: PASSED

- All 7 created files verified on disk
- All 3 task commits verified in git log (a356c7e, f6ba467, 44f4196)
- All 5 modified files confirmed present

---
*Phase: 05-ship-skills-and-advanced-damage*
*Completed: 2026-02-18*
