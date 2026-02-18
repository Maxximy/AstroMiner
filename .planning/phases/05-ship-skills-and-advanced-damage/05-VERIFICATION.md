---
phase: 05-ship-skills-and-advanced-damage
verified: 2026-02-18T00:00:00Z
status: gaps_found
score: 9/11 must-haves verified
gaps:
  - truth: "Skills require unlocking via the Ship branch of the tech tree before they appear in the skill bar"
    status: failed
    reason: "SKIL-07 — No unlock gating exists. All 4 skills are always available in the skill bar regardless of tech tree state. No tech tree data or SkillUnlocked flags exist anywhere in the codebase. This was ROADMAP Success Criterion #5 for Phase 5."
    artifacts:
      - path: "Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs"
        issue: "No unlock check before showing or enabling skill slots. Slots always render and are always clickable."
      - path: "Assets/Scripts/ECS/Systems/LaserBurstSystem.cs"
        issue: "No guard checking whether skill is unlocked before activating."
    missing:
      - "SkillUnlockData component or flags in SkillComponents.cs tracking which skills are unlocked"
      - "SkillBarController must check unlock state per slot and hide/gray out locked slots"
      - "Skill systems must guard against activation if skill not unlocked (or rely on UI hiding)"
      - "ECSBootstrap must seed all skills as locked by default"
  - truth: "Skills are upgradeable (damage+, cooldown-, special effects) via tech tree"
    status: failed
    reason: "SKIL-08 — Skills have fixed constants from GameConstants.cs. No upgrade scaffolding exists. No tech tree node for Ship branch upgrades is implemented. This requirement is listed for Phase 5 but logically depends on Phase 6 tech tree infrastructure."
    artifacts:
      - path: "Assets/Scripts/Shared/GameConstants.cs"
        issue: "Skill parameters (LaserBurstDamage, ChainLightningCooldown, etc.) are compile-time constants, not runtime-upgradeable values."
    missing:
      - "Runtime-mutable skill stat data (ScriptableObject or ECS component) to replace const values"
      - "Ship branch tech tree nodes wired to skill stat multipliers"
human_verification:
  - test: "Verify all four skill VFX render correctly in Play Mode"
    expected: "Pressing 1 shows cyan beam, pressing 2 shows jagged blue-white arc, pressing 3 shows purple particle burst, pressing 4 shows gold mining circle with larger radius"
    why_human: "LineRenderer and ParticleSystem visual output cannot be verified from source code alone"
  - test: "Verify skill audio plays on activation"
    expected: "Each skill plays a distinct sound when activated; crit hits play an elevated-pitch sound. Gracefully silent if audio clips not in Resources."
    why_human: "Audio clip loading and AudioSource playback cannot be verified programmatically — audio files may not exist yet"
  - test: "Verify ember particle trails appear on burning asteroids"
    expected: "After hitting an asteroid with Laser Burst or EMP Pulse, small orange particles trail from it for 2-3 seconds then disappear"
    why_human: "BurningEffectManager tracking of live ECS entities requires runtime observation"
---

# Phase 5: Ship Skills and Advanced Damage — Verification Report

**Phase Goal:** Combat gains depth — the player has four active skills to aim and fire, a critical hit system that rewards lucky moments, and damage-over-time that makes every hit count even after asteroids leave the mining circle
**Verified:** 2026-02-18
**Status:** gaps_found
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Pressing keyboard keys 1-4 during Playing phase activates the corresponding skill | VERIFIED | `InputBridge.cs:59-63` — `skillInput.Skill1Pressed |= keyboard.digit1Key.wasPressedThisFrame` OR-merge pattern for all 4 keys |
| 2 | Laser Burst damages all asteroids in a line from ship to mouse position | VERIFIED | `LaserBurstSystem.cs:75` — `PointToSegmentDistSq(asteroidPos, shipPos, mousePos)` with correct beam half-width collision |
| 3 | Chain Lightning hits the nearest asteroid to mouse then chains to 3-4 nearby asteroids | VERIFIED | `ChainLightningSystem.cs:77-136` — NativeList visited tracking with nearest-first search and maxChainDist guard |
| 4 | EMP Pulse deals AoE damage to all asteroids within blast radius centered on mouse position | VERIFIED | `EmpPulseSystem.cs:67-69` — `math.distancesq(asteroidPos, mousePos) <= radiusSq` |
| 5 | Overcharge temporarily doubles mining circle damage and increases radius by 1.5x | VERIFIED | `OverchargeSystem.cs:40-55` activates buff; `MiningDamageSystem.cs:50-52` applies `overcharge.DamageMultiplier` and `overcharge.RadiusMultiplier` |
| 6 | Each skill has a cooldown period during which it cannot be reactivated | VERIFIED | `SkillCooldownSystem.cs:39-42` decrements each frame; all skill systems guard `cooldown.ValueRO.Skill1Remaining > 0f` before activating |
| 7 | Normal mining damage and skill damage can randomly crit at ~8% chance for 2x multiplied damage | VERIFIED | `CritConfigData` seeded at `CritChance=0.08f, CritMultiplier=2f`; `MiningDamageSystem.cs:78`, `LaserBurstSystem.cs:79`, `ChainLightningSystem.cs:148`, `EmpPulseSystem.cs:72`, `BurningDamageSystem.cs:58` all roll crits |
| 8 | Laser Burst and EMP Pulse apply a burning DoT effect that ticks damage over 2-3 seconds | VERIFIED | `LaserBurstSystem.cs:96-111` and `EmpPulseSystem.cs:89-104` — ECB add/set BurningData; `BurningDamageSystem.cs:47-72` ticks damage |
| 9 | Asteroid stops taking periodic damage ticks 2-3 seconds after being hit, ember particles cease | VERIFIED | `BurningDamageSystem.cs:76-79` — `ecb.RemoveComponent<BurningData>(entity)` when `RemainingDuration <= 0`; `BurningEffectManager.cs:182-200` returns particles to pool on component removal |
| 10 | Four skill slots visible in skill bar with keybind badges and radial cooldown sweep | VERIFIED (code) | `UISetup.cs:356-506` — `CreateSkillBarCanvas()` creates 4 slots with badges, radial fill, countdown text; visual confirmation needs human |
| 11 | Skills require unlocking via Ship branch of tech tree before appearing in skill bar | FAILED | No unlock gating exists. `SkillBarController.cs` shows/hides based on game phase only. No `SkillUnlockData` or tech tree in codebase. ROADMAP Success Criterion #5. |

**Score:** 9/11 truths verified (10/11 code-verified, 11/11 human-pending items aside)

### Required Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| `Assets/Scripts/ECS/Components/SkillComponents.cs` | VERIFIED | 5 unmanaged IComponentData structs: SkillInputData, SkillCooldownData, CritConfigData, OverchargeBuffData, BurningData — all substantive |
| `Assets/Scripts/ECS/Systems/SkillCooldownSystem.cs` | VERIFIED | 77 lines. Two systems: SkillCooldownSystem (decrement) + SkillInputResetSystem (flag clear after all skill systems). UpdateBefore/UpdateAfter ordering correct. |
| `Assets/Scripts/ECS/Systems/LaserBurstSystem.cs` | VERIFIED | 141 lines. Point-to-segment collision, crit rolls, DamageEvent + SkillEvent emission, BurningData add/refresh via ECB. |
| `Assets/Scripts/ECS/Systems/ChainLightningSystem.cs` | VERIFIED | 197 lines. NativeList-based nearest-first chaining, visited index tracking, per-target DamageEvent, SkillEvent with Chain1-4 positions. |
| `Assets/Scripts/ECS/Systems/EmpPulseSystem.cs` | VERIFIED | 119 lines. Circle AoE, crit roll, BurningData add/refresh, SkillEvent with Radius field. |
| `Assets/Scripts/ECS/Systems/OverchargeSystem.cs` | VERIFIED | 64 lines. Buff activation on Skill4Pressed, duration decrement, SkillEvent on activation only. |
| `Assets/Scripts/ECS/Systems/BurningDamageSystem.cs` | VERIFIED | 83 lines. TickAccumulator-based damage, crit roll on DoT, ECB RemoveComponent on expiry. |
| `Assets/Scripts/ECS/Components/FeedbackComponents.cs` | VERIFIED | SkillEvent IBufferElementData added (lines 67-89) with SkillType, OriginPos, TargetPos, Chain1-4, ChainCount, Radius. |
| `Assets/Scripts/MonoBehaviours/UI/SkillBarController.cs` | VERIFIED | 148 lines. 4-slot skill bar with radial fill, countdown text, click activation via ECS singleton writes, ready-flash animation, game-phase visibility. |
| `Assets/Scripts/MonoBehaviours/Rendering/SkillVFXManager.cs` | VERIFIED | 242 lines. Self-instantiating singleton. Pooled LineRenderer (laser, chain) + ParticleSystem (EMP). PlayLaserBurst/PlayChainLightning/PlayEmpPulse/PlayOverchargeActivation public API. |
| `Assets/Scripts/MonoBehaviours/Rendering/BurningEffectManager.cs` | VERIFIED | 208 lines. Self-instantiating singleton. Pool of 20 ember ParticleSystems. Dictionary<Entity, GameObject> tracking with acquire/release on BurningData presence. |

### Key Link Verification

| From | To | Via | Status | Evidence |
|------|----|-----|--------|---------|
| `InputBridge.cs` | SkillInputData singleton | `skillInput.Skill1Pressed \|= keyboard.digit1Key.wasPressedThisFrame` | WIRED | Lines 55-63: reads current data, OR-merges keys 1-4, writes back |
| `LaserBurstSystem.cs` | DamageEvent buffer + SkillEvent buffer | System writes both events on activation | WIRED | Lines 87-93 (DamageEvent.Add), lines 116-123 (SkillEvent.Add) |
| `MiningDamageSystem.cs` | CritConfigData singleton | `rng.NextFloat() < critConfig.CritChance` on each tick | WIRED | Lines 55, 78-79: GetSingleton + crit roll |
| `BurningDamageSystem.cs` | BurningData per-entity component | TickAccumulator, RemainingDuration, ECB RemoveComponent | WIRED | Lines 47-79: full tick loop + expiry removal |
| `FeedbackEventBridge.cs` | SkillVFXManager + AudioManager | DrainSkillEvents dispatches per SkillType | WIRED | Lines 132-173: switch on SkillType 0-3, calls PlayLaserBurst, PlayChainLightning, PlayEmpPulse, PlayOverchargeActivation + PlaySkillSfx |
| `SkillBarController.cs` | SkillInputData + SkillCooldownData singletons | Button click writes SkillNPressed; LateUpdate reads cooldown for fillAmount | WIRED | OnSkillButtonClicked (lines 54-67), UpdateSlot (lines 115-146) |
| `MiningCircleVisual.cs` | OverchargeBuffData singleton | Reads RemainingDuration, swaps color/scale | WIRED | Lines 112-133: gold color + scaled radius when RemainingDuration > 0 |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| SKIL-01 | 05-01 | Laser Burst fires beam, damages asteroids in path | SATISFIED | LaserBurstSystem.cs — line collision and DamageEvent emission |
| SKIL-02 | 05-01 | Chain Lightning chains between nearby asteroids | SATISFIED | ChainLightningSystem.cs — nearest-first chaining with maxChainDist |
| SKIL-03 | 05-01 | EMP Pulse emits AoE damage | SATISFIED | EmpPulseSystem.cs — radius AoE |
| SKIL-04 | 05-01 | Overcharge temporarily doubles mining circle damage rate | SATISFIED | OverchargeSystem.cs + MiningDamageSystem reads DamageMultiplier/RadiusMultiplier |
| SKIL-05 | 05-01 + 05-02 | Skills activated via keyboard (1-4) and UI buttons | SATISFIED | InputBridge.cs OR-merge + SkillBarController.OnSkillButtonClicked |
| SKIL-06 | 05-02 | Each skill has cooldown timer displayed on skill bar | SATISFIED | SkillBarController.UpdateSlot sets fillAmount + countdown text from SkillCooldownData |
| SKIL-07 | Neither plan | Skills must be unlocked via Ship branch of tech tree | NOT SATISFIED | No unlock gating anywhere in codebase. ORPHANED from plans — no plan claimed this requirement. ROADMAP Success Criterion #5. |
| SKIL-08 | Neither plan | Skills upgradeable via tech tree (damage+, cooldown-) | NOT SATISFIED | Skill stats are compile-time constants in GameConstants.cs. ORPHANED from plans. Logically depends on Phase 6 tech tree. |
| DMGS-01 | 05-01 | Critical hit system with configurable chance and multiplier | SATISFIED | CritConfigData singleton (8% chance, 2x multiplier) + crit roll in all 5 damage systems |
| DMGS-02 | 05-01 | DoT burning effect persists after asteroid leaves mining circle | SATISFIED | BurningDamageSystem ticks BurningData per-entity — independent of mining circle position |
| DMGS-03 | 05-02 | DoT visually indicated by ember particles on affected asteroid | SATISFIED | BurningEffectManager tracks entities with BurningData and attaches pooled ember ParticleSystems |

**Orphaned requirements (Phase 5 traceability but unclaimed by any plan):**
- SKIL-07 — Not in 05-01 or 05-02 requirements field; no implementation
- SKIL-08 — Not in 05-01 or 05-02 requirements field; no implementation

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `UISetup.cs:325` | `"Tech tree coming in Phase 6"` placeholder text in Upgrade screen | Info | Intentional Phase 6 scope marker, not a Phase 5 concern |
| `SkillVFXManager.cs:226-227` | `PlayOverchargeActivation()` is a no-op | Info | Intentional design — Overcharge visual handled by MiningCircleVisual reading ECS singleton. Comment documents the decision. |
| `FeedbackEventBridge.cs:90-91` | CameraShake removed from critical hit path | Info | Explicit user decision: "no extra screen flash or shake" on crits. FEED-07 (Phase 4) required shake on crits — this is a deliberate user override. Noted for transparency. |

No blocker anti-patterns found in the 10 new Phase 5 files.

### Human Verification Required

#### 1. Skill VFX Visual Confirmation

**Test:** Enter Play Mode. Press 1 (Laser Burst), 2 (Chain Lightning), 3 (EMP Pulse), 4 (Overcharge) in sequence with asteroids visible.
**Expected:** Cyan beam flash (0.15s), jagged blue-white arcs through targets (0.2s), expanding purple particle burst at mouse, mining circle turns gold/larger for 5 seconds.
**Why human:** LineRenderer positions and ParticleSystem visual output cannot be verified from source. The code is wired correctly but rendering depends on URP Unlit shader availability and scene setup.

#### 2. Skill Audio Confirmation

**Test:** Activate each skill and observe Unity Console + audio output.
**Expected:** Each skill plays a distinct sound on activation. Critical hits play an elevated-pitch tick. Console may log "null clip" warnings if audio files have not been placed in `Resources/Audio/SFX/` — this is graceful degradation and acceptable.
**Why human:** `Resources.Load<AudioClip>()` returning null is silent in code but results in no audio. Cannot verify audio file presence programmatically without filesystem checks. Audio mix and pitch cannot be verified without playback.

#### 3. Burning Ember Particle Trail Confirmation

**Test:** Hit an asteroid with Laser Burst or EMP Pulse. Watch the asteroid for 3 seconds.
**Expected:** Small orange-red ember particles trail upward from the asteroid. Particles stop (pool returned) approximately 2-3 seconds later. A second hit refreshes the duration.
**Why human:** BurningEffectManager LateUpdate polling of live ECS entities requires a running simulation. The Dictionary<Entity, GameObject> tracking pattern is correct in code but particle visibility depends on URP particle material resolution at runtime.

### Gaps Summary

Two requirements (SKIL-07, SKIL-08) were listed in the Phase 5 ROADMAP and traceability table but were not claimed by either 05-01-PLAN.md or 05-02-PLAN.md. No implementation exists for either.

**SKIL-07** (skills unlocked via tech tree) is also ROADMAP Success Criterion #5 — meaning it is explicitly part of the phase gate. Currently all 4 skills are always available to the player from the start of any run, with no unlock requirement. The SkillBarController shows all slots unconditionally during the Playing phase.

**SKIL-08** (skills upgradeable) depends on Phase 6's tech tree infrastructure and is more clearly deferred, but it was still mapped to Phase 5 in the traceability table.

The gap for SKIL-07 is small in scope: the SkillBarController needs an unlock check per slot, and ECSBootstrap or a save system needs to track which skills are purchased. The skill systems themselves do not need modification if the UI enforces the gating (locked skills simply never set their SkillInputData flags).

---

_Verified: 2026-02-18_
_Verifier: Claude (gsd-verifier)_
