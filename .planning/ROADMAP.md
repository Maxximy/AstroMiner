# Roadmap: Astrominer

## Overview

Astrominer delivers a satisfying mining-collecting-upgrading loop across six phases. The build progresses from ECS foundation and WebGL validation, through the core mining and collection loops, into feedback systems that make the game feel good, then adds combat depth with skills and damage systems, and finally delivers the meta-game layer of tech tree upgrades and level progression. Each phase delivers a playable, verifiable capability that builds on the last.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation and WebGL Validation** - ECS world, game state machine, bridge layer, object pooling, and WebGL performance confirmation _(completed 2026-02-17)_
- [x] **Phase 2: Core Mining Loop** - Asteroids spawn and drift, mining circle follows mouse and damages them, playable mining _(completed 2026-02-17)_
- [x] **Phase 3: Collection, Economy, and Session** - Minerals fly to ship, credits awarded, timed runs with results, persistent save system _(completed 2026-02-17)_
- [x] **Phase 4: Visual and Audio Feedback** - Damage numbers, particle effects, screen shake, audio SFX, space aesthetic polish _(completed 2026-02-18)_
- [x] **Phase 5: Ship Skills and Advanced Damage** - Four active skills, critical hits, damage-over-time, skill bar UI _(completed 2026-02-18)_
- [ ] **Phase 6: Tech Tree and Level Progression** - Branching upgrade tree, level system with tiered resources, full meta-game loop

## Phase Details

### Phase 1: Foundation and WebGL Validation
**Goal**: The ECS simulation world runs on both desktop and WebGL with a working game state machine, input bridge, and object pooling -- proving the hybrid architecture before any gameplay is built
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05, INFRA-06, VISL-01, VISL-02
**Success Criteria** (what must be TRUE):
  1. ECS world bootstraps and a WebGL build loads without errors, running at 60 FPS with placeholder entities at target counts (100 asteroids, 1000 minerals)
  2. Game state machine transitions between Playing, Collecting, GameOver, and Upgrading states with visible state shown in debug UI
  3. Mouse position is projected to world space and readable by ECS systems every frame (InputBridge writes to ECS singleton)
  4. Object pool pre-allocates and recycles GameObjects without runtime instantiation during gameplay
  5. Starfield skybox and URP post-processing (bloom, tonemapping) render correctly on both desktop and WebGL
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md -- ECS bootstrap, singleton components, InputBridge, NumberFormatter, skybox, and URP post-processing
- [x] 01-02-PLAN.md -- Game state machine with fade transitions, object pooling, placeholder entities with ECS movement, debug overlay, and WebGL 60 FPS validation

### Phase 2: Core Mining Loop
**Goal**: Player can hover a mining circle over asteroids that drift down the screen, watch them take damage and break apart -- the fundamental interaction that the entire game depends on
**Depends on**: Phase 1
**Requirements**: MINE-01, MINE-02, MINE-03, MINE-04, MINE-05, ASTR-01, ASTR-02, ASTR-03, ASTR-04, ASTR-05, VISL-05
**Success Criteria** (what must be TRUE):
  1. Asteroids spawn at the top of the screen, drift downward with visible rotation, and are destroyed if they reach the bottom
  2. A cyan emissive mining circle follows the mouse cursor and visually glows with bloom
  3. Asteroids within the mining circle take tick-based damage, and their HP depletes at the configured rate
  4. Asteroids with 0 HP are destroyed (removed from play) with the destruction visible on screen
  5. Ship is visible at bottom of screen as a stationary reference point
**Plans**: 2 plans

Plans:
- [ ] 02-01-PLAN.md -- Asteroid ECS systems (spawn, drift, bounds, destruction), AsteroidRenderer, ship visual, coordinate system transition to XZ plane
- [ ] 02-02-PLAN.md -- Mining circle visual (LineRenderer + HDR bloom), tick-based AoE damage system, mining interaction verification

### Phase 3: Collection, Economy, and Session
**Goal**: The core game loop closes -- destroyed asteroids release minerals that fly to the ship, credits accumulate, timed runs end with a results screen, and all progress persists between sessions
**Depends on**: Phase 2
**Requirements**: MINR-01, MINR-02, MINR-03, MINR-04, ECON-01, ECON-02, ECON-03, ECON-04, SESS-01, SESS-02, SESS-03, SESS-04, SESS-05, SESS-06, SAVE-01, SAVE-02, SAVE-03, SAVE-04, SAVE-05
**Success Criteria** (what must be TRUE):
  1. Destroyed asteroids release mineral particles that accelerate toward the ship and are collected on contact, awarding credits based on resource tier
  2. A running credit total is displayed during gameplay using large number formatting (K/M/B/T)
  3. A visible countdown timer controls the run; when it expires, mining stops but minerals still fly to the ship, then a results screen shows credits earned
  4. Player can proceed from results to an upgrade screen and start a new run from there
  5. Game state (credits, progress) persists across browser refreshes on WebGL and application restarts on desktop
**Plans**: 3 plans

Plans:
- [x] 03-01-PLAN.md -- Mineral ECS lifecycle: spawn from destroyed asteroids, pull toward ship, collect on contact, award credits, render via pooled GameObjects
- [x] 03-02-PLAN.md -- Session flow: timed runs with HUD (credits + timer), phase transitions, results screen, upgrade placeholder, run reset
- [ ] 03-03-PLAN.md -- Save system: JSON persistence with auto-save on run end, credit restoration on game start, WebGL IndexedDB compatibility

### Phase 4: Visual and Audio Feedback
**Goal**: The game transforms from functional to satisfying -- every mining hit, destruction, collection, and critical moment has visual and audio feedback that makes the player feel powerful
**Depends on**: Phase 3
**Requirements**: FEED-01, FEED-02, FEED-03, FEED-04, FEED-05, FEED-06, FEED-07, FEED-08, AUDI-01, AUDI-02, AUDI-03, AUDI-04, AUDI-05, AUDI-06, AUDI-07, AUDI-08, VISL-03, VISL-04
**Success Criteria** (what must be TRUE):
  1. Floating damage numbers appear on each damage tick (white), with distinct styles for critical hits (yellow "CRIT!"), DoT (orange italic), and skill damage (skill-colored)
  2. Asteroid destruction triggers explosion particle effects, and minerals leave a visible trail as they fly toward the ship
  3. Screen shakes subtly on critical hits and skill impacts, and the screen edge glows red in the last 10 seconds of a run
  4. Every meaningful action has audio: mining hits, destruction, mineral collection chimes (pitch varies by tier), and skill activation each play spatial SFX
  5. Ambient space music loops in the background with separate SFX and Music volume controls via AudioMixer
**Plans**: 2 plans

Plans:
- [x] 04-01-PLAN.md -- ECS event pipeline (DamageEvent/DestructionEvent/CollectionEvent buffers), damage popup manager, explosion particles, mineral trails, material improvements, credit counter pop _(completed 2026-02-18)_
- [x] 04-02-PLAN.md -- Audio system (AudioManager, AudioMixer, SFX/Music), FeedbackEventBridge, CameraShake, TimerWarningEffect, UI click SFX, game over fanfare _(completed 2026-02-18)_

### Phase 5: Ship Skills and Advanced Damage
**Goal**: Combat gains depth -- the player has four active skills to aim and fire, a critical hit system that rewards lucky moments, and damage-over-time that makes every hit count even after asteroids leave the mining circle
**Depends on**: Phase 2, Phase 4
**Requirements**: SKIL-01, SKIL-02, SKIL-03, SKIL-04, SKIL-05, SKIL-06, SKIL-07, SKIL-08, DMGS-01, DMGS-02, DMGS-03
**Success Criteria** (what must be TRUE):
  1. Player can activate Laser Burst, Chain Lightning, EMP Pulse, and Overcharge via keyboard (1-4) and on-screen UI buttons, each with distinct visual effects
  2. Each skill has a visible cooldown timer on the skill bar UI and cannot be re-used until cooldown completes
  3. Critical hits occur at a configurable chance, display yellow "CRIT!" text with a scale-up animation, and deal multiplied damage
  4. Damage-over-time (DoT) applies a burning effect to asteroids that persists after they leave the mining circle, shown by ember particle trails
  5. Skills require unlocking via the Ship branch of the tech tree before they appear in the skill bar
**Plans**: 3 plans

Plans:
- [x] 05-01-PLAN.md -- ECS skill components, four skill damage systems (Laser/Chain/EMP/Overcharge), crit integration, DoT burning system, input extension _(completed 2026-02-18)_
- [x] 05-02-PLAN.md -- Skill bar UI with radial cooldowns, skill VFX (beam/lightning/blast/glow), ember particles for burning, audio wiring, Overcharge visual feedback _(completed 2026-02-18)_
- [ ] 05-03-PLAN.md -- Gap closure: skill unlock gating scaffolding (SKIL-07) and runtime-mutable skill stats singleton (SKIL-08)

### Phase 6: Tech Tree and Level Progression
**Goal**: The meta-game delivers long-term motivation -- players spend credits on a branching tech tree to become noticeably more powerful, then advance through levels with new resource tiers and tougher asteroids
**Depends on**: Phase 3, Phase 5
**Requirements**: TECH-01, TECH-02, TECH-03, TECH-04, TECH-05, TECH-06, TECH-07, TECH-08, TECH-09, TECH-10, LEVL-01, LEVL-02, LEVL-03, LEVL-04, LEVL-05, ASTR-06
**Success Criteria** (what must be TRUE):
  1. Tech tree UI displays 5 branches (Mining, Economy, Ship, Run, Progression) with all upgrade nodes visible and prerequisite gating enforced
  2. Purchasing an upgrade deducts credits, immediately applies its stat effect (e.g., larger mining circle, more damage), and the improvement is noticeable in the next run
  3. Upgrade costs scale per tier (1x/3x/8x) and all tech tree data is defined in ScriptableObjects
  4. At least 5 levels exist with distinct drop tables; higher levels introduce rarer resource tiers (up to 6 tiers: Iron through Titanium) and tougher asteroids with more HP
  5. Level advancement requires meeting a credit threshold and purchasing an Advance upgrade; all upgrades carry over between levels
**Plans**: 3 plans

Plans:
- [ ] 06-01-PLAN.md -- Data model foundation: ScriptableObject classes, enums, ECS singletons, save system expansion, bootstrap changes
- [ ] 06-02-PLAN.md -- Tech tree UI and purchase system: center-outward node graph, pan/zoom, color-coded states, one-click purchase with stat application, tooltip, VFX/SFX, ~40 node definitions
- [ ] 06-03-PLAN.md -- Level progression and resource tiers: 5 level configs, 6 resource tier colors, asteroid HP/size scaling, economy bonus integration

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6

| Phase | Plans Complete | Status | Completed |
|-------|---------------|--------|-----------|
| 1. Foundation and WebGL Validation | 2/2 | Complete | 2026-02-17 |
| 2. Core Mining Loop | 0/2 | Complete    | 2026-02-17 |
| 3. Collection, Economy, and Session | 2/3 | Complete    | 2026-02-17 |
| 4. Visual and Audio Feedback | 2/2 | Complete | 2026-02-18 |
| 5. Ship Skills and Advanced Damage | 2/3 | Gap closure | 2026-02-18 |
| 6. Tech Tree and Level Progression | 0/3 | Planning complete | - |

---
*Roadmap created: 2026-02-17*
*Last updated: 2026-02-18 (Phase 6 planning complete)*
