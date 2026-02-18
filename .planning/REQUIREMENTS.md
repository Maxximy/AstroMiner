# Requirements: Astrominer

**Defined:** 2026-02-17
**Core Value:** The mining-collecting-upgrading loop must feel satisfying — hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Infrastructure

- [ ] **INFRA-01**: ECS world bootstraps with hybrid bridge layer (ECS simulation + GameObject rendering/UI/audio)
- [ ] **INFRA-02**: WebGL build runs at 60 FPS with 100 asteroids and 1000 minerals active
- [ ] **INFRA-03**: Object pooling system for asteroids, minerals, damage popups, and particles
- [ ] **INFRA-04**: Game state machine manages Playing, Collecting, GameOver, and Upgrading states
- [ ] **INFRA-05**: InputBridge writes mouse world position to ECS singleton each frame
- [ ] **INFRA-06**: Large number formatting utility (K/M/B/T) used across all credit displays

### Mining

- [ ] **MINE-01**: Mining circle follows mouse cursor on the gameplay plane
- [ ] **MINE-02**: Mining circle deals tick-based AoE damage to all asteroids within radius
- [ ] **MINE-03**: Damage rate and damage amount are configurable and upgradeable
- [ ] **MINE-04**: Mining circle radius is upgradeable via tech tree
- [ ] **MINE-05**: Mining circle has visual feedback (cyan emissive ring with bloom glow)

### Asteroids

- [ ] **ASTR-01**: Asteroids spawn at top of screen and drift downward
- [ ] **ASTR-02**: Each asteroid has HP determined by its resource tier
- [ ] **ASTR-03**: Asteroids have 3D PBR models with per-resource-type visual appearance
- [ ] **ASTR-04**: Asteroids rotate (spin) while drifting for visual interest
- [ ] **ASTR-05**: Asteroids that reach the bottom of the screen are destroyed (missed)
- [x] **ASTR-06**: 6 resource tiers: Iron, Copper, Silver, Cobalt, Gold, Titanium with distinct visuals

### Minerals

- [x] **MINR-01**: Destroyed asteroids release mineral particles (children detach)
- [x] **MINR-02**: Detached minerals accelerate toward the player's ship
- [x] **MINR-03**: Minerals collected on contact with ship award credits based on resource tier
- [x] **MINR-04**: Mineral particles have tier-based visual appearance (color, emissive glow for rare)

### Economy

- [x] **ECON-01**: Credits are the universal currency converted from all collected minerals
- [x] **ECON-02**: Running credit total displayed during gameplay
- [x] **ECON-03**: Credits persist between runs and are spent in the tech tree
- [x] **ECON-04**: Credit values per resource tier are data-driven (ScriptableObject/config)

### Session

- [x] **SESS-01**: Timed mining runs with visible countdown timer
- [x] **SESS-02**: When timer expires, transition to Collecting state (no more damage, minerals still pulled)
- [x] **SESS-03**: When all minerals collected, transition to GameOver with results screen
- [x] **SESS-04**: Results screen shows credits earned this run
- [x] **SESS-05**: Player can proceed to Upgrade screen from results
- [x] **SESS-06**: Player can start new run from Upgrade screen

### Tech Tree

- [x] **TECH-01**: Tech tree UI displayed between runs with 5 branches visible
- [x] **TECH-02**: Mining branch: Circle Radius (I/II/III), Damage (I/II), Rate (I/II), Crit Chance, Crit Multi, DoT (I/II)
- [x] **TECH-03**: Economy branch: Resource Multiplier (I/II/III), Lucky Strike (I/II/III), Abundance (I/II)
- [x] **TECH-04**: Ship branch: Laser Burst (I/II/III), Chain Lightning (I/II/III), EMP (I/II/III), Overcharge (I/II/III), Combo Mastery
- [x] **TECH-05**: Run branch: Level Time (I/II/III)
- [x] **TECH-06**: Progression branch: Advance to Level N (one per level transition)
- [x] **TECH-07**: Upgrades have prerequisite gating (must unlock parent before child)
- [x] **TECH-08**: Upgrade costs scale per tier (1x/3x/8x base cost)
- [x] **TECH-09**: Purchasing an upgrade immediately applies its stat effect
- [x] **TECH-10**: Tech tree data is defined in ScriptableObjects for easy tuning

### Ship Skills

- [x] **SKIL-01**: Laser Burst fires a beam from ship to mining circle position, damages asteroids in path
- [x] **SKIL-02**: Chain Lightning launches projectile to circle, lightning chains between nearby asteroids
- [x] **SKIL-03**: EMP Pulse emits AoE damage centered on circle position
- [x] **SKIL-04**: Overcharge temporarily doubles mining circle damage rate
- [x] **SKIL-05**: Skills activated via keyboard (1-4) and on-screen UI buttons
- [x] **SKIL-06**: Each skill has a cooldown timer displayed on the skill bar UI
- [x] **SKIL-07**: Skills must be unlocked via Ship branch of tech tree before use
- [x] **SKIL-08**: Skills are upgradeable (damage+, cooldown-, special effects) via tech tree

### Damage System

- [x] **DMGS-01**: Critical hit system with configurable chance and multiplier (from tech tree)
- [x] **DMGS-02**: Damage-over-time (DoT) burning effect that persists after asteroid leaves mining circle
- [x] **DMGS-03**: DoT visually indicated by ember particles trailing from affected asteroid

### Levels

- [x] **LEVL-01**: Fixed set of levels (5+), each with a predefined drop table config
- [x] **LEVL-02**: Higher levels introduce rarer resource tiers and tougher asteroids (more HP)
- [x] **LEVL-03**: Level advancement requires meeting credit threshold and purchasing Advance upgrade
- [x] **LEVL-04**: Upgrades carry over between levels (no resets)
- [x] **LEVL-05**: Level configs defined in ScriptableObjects (drop rates, HP multipliers, spawn rate, thresholds)

### Feedback

- [x] **FEED-01**: Floating damage numbers on each damage tick (white, small, float up and fade)
- [x] **FEED-02**: Critical hits show yellow "CRIT!" text with scale-up pop animation
- [x] **FEED-03**: DoT ticks show orange italicized damage numbers
- [x] **FEED-04**: Skill damage shows skill-colored medium damage numbers
- [x] **FEED-05**: Particle effects on asteroid destruction (explosion into collectible particles)
- [x] **FEED-06**: Particle trail when minerals fly toward ship
- [x] **FEED-07**: Subtle screen shake on critical hits and skill impacts
- [x] **FEED-08**: Edge glow warning when timer is in last 10 seconds

### Audio

- [x] **AUDI-01**: Mining hit SFX plays on each damage tick (spatial 3D at asteroid position)
- [x] **AUDI-02**: Asteroid destruction SFX (explosion/shatter)
- [x] **AUDI-03**: Mineral collection SFX (chime, pitch varies by tier)
- [x] **AUDI-04**: Skill activation SFX (unique per skill)
- [x] **AUDI-05**: Game over fanfare
- [x] **AUDI-06**: Space ambient background loop
- [x] **AUDI-07**: UI button click SFX
- [x] **AUDI-08**: AudioMixer with separate SFX and Music volume channels

### Persistence

- [x] **SAVE-01**: Game state saves to JSON in Application.persistentDataPath
- [x] **SAVE-02**: Auto-save triggers on run end and on every tech tree purchase
- [x] **SAVE-03**: Save includes credits, tech tree unlock state, current level, player stats
- [x] **SAVE-04**: WebGL builds use .jslib plugin to flush IndexedDB after each save
- [x] **SAVE-05**: Save file includes version number for future migration support

### Visuals

- [ ] **VISL-01**: Realistic space aesthetic with starfield skybox
- [ ] **VISL-02**: URP post-processing: bloom, tonemapping
- [x] **VISL-03**: PBR materials on asteroids with per-resource-type appearance
- [x] **VISL-04**: Emissive/glow materials on rare minerals and star
- [ ] **VISL-05**: Ship visual at bottom of screen (stationary)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Special Asteroids

- **SPEC-01**: Fragile asteroids — very low HP, brief spawn window, bonus credits
- **SPEC-02**: Mega asteroids — very high HP, large size, high credit reward
- **SPEC-03**: Cluster asteroids — break into 3-5 smaller asteroids when destroyed

### Prestige

- **PRES-01**: Prestige/rebirth system resetting progress for permanent multipliers

### Idle

- **IDLE-01**: Auto-miner for idle income when away

### Platform

- **PLAT-01**: Mac desktop build
- **PLAT-02**: Mobile (iOS/Android) with touch controls

### Social

- **SOCL-01**: Achievements and milestone rewards
- **SOCL-02**: Leaderboards

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real-time difficulty scaling | Undermines upgrade satisfaction — players should feel power growth |
| Gacha/lootbox mechanics | Destroys player trust in a PC/WebGL game |
| Ship movement | Conflicts with mining circle as core interaction |
| Complex crafting system | Second resource sink competing with tech tree |
| Multiplayer | Single-player experience; would require backend + anti-cheat |
| Cloud saves | PlayerPrefs + JSON sufficient for v1 |
| Offline progression | Active-play only for v1; fundamentally different economy balance |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Pending |
| INFRA-02 | Phase 1 | Pending |
| INFRA-03 | Phase 1 | Pending |
| INFRA-04 | Phase 1 | Pending |
| INFRA-05 | Phase 1 | Pending |
| INFRA-06 | Phase 1 | Pending |
| VISL-01 | Phase 1 | Pending |
| VISL-02 | Phase 1 | Pending |
| MINE-01 | Phase 2 | Pending |
| MINE-02 | Phase 2 | Pending |
| MINE-03 | Phase 2 | Pending |
| MINE-04 | Phase 2 | Pending |
| MINE-05 | Phase 2 | Pending |
| ASTR-01 | Phase 2 | Pending |
| ASTR-02 | Phase 2 | Pending |
| ASTR-03 | Phase 2 | Pending |
| ASTR-04 | Phase 2 | Pending |
| ASTR-05 | Phase 2 | Pending |
| VISL-05 | Phase 2 | Pending |
| MINR-01 | Phase 3 | Complete |
| MINR-02 | Phase 3 | Complete |
| MINR-03 | Phase 3 | Complete |
| MINR-04 | Phase 3 | Complete |
| ECON-01 | Phase 3 | Complete |
| ECON-02 | Phase 3 | Complete |
| ECON-03 | Phase 3 | Complete |
| ECON-04 | Phase 3 | Complete |
| SESS-01 | Phase 3 | Complete |
| SESS-02 | Phase 3 | Complete |
| SESS-03 | Phase 3 | Complete |
| SESS-04 | Phase 3 | Complete |
| SESS-05 | Phase 3 | Complete |
| SESS-06 | Phase 3 | Complete |
| SAVE-01 | Phase 3 | Complete |
| SAVE-02 | Phase 3 | Complete |
| SAVE-03 | Phase 3 | Complete |
| SAVE-04 | Phase 3 | Complete |
| SAVE-05 | Phase 3 | Complete |
| FEED-01 | Phase 4 | Complete |
| FEED-02 | Phase 4 | Complete |
| FEED-03 | Phase 4 | Complete |
| FEED-04 | Phase 4 | Complete |
| FEED-05 | Phase 4 | Complete |
| FEED-06 | Phase 4 | Complete |
| FEED-07 | Phase 4 | Complete |
| FEED-08 | Phase 4 | Complete |
| AUDI-01 | Phase 4 | Complete |
| AUDI-02 | Phase 4 | Complete |
| AUDI-03 | Phase 4 | Complete |
| AUDI-04 | Phase 4 | Complete |
| AUDI-05 | Phase 4 | Complete |
| AUDI-06 | Phase 4 | Complete |
| AUDI-07 | Phase 4 | Complete |
| AUDI-08 | Phase 4 | Complete |
| VISL-03 | Phase 4 | Complete |
| VISL-04 | Phase 4 | Complete |
| SKIL-01 | Phase 5 | Complete |
| SKIL-02 | Phase 5 | Complete |
| SKIL-03 | Phase 5 | Complete |
| SKIL-04 | Phase 5 | Complete |
| SKIL-05 | Phase 5 | Complete |
| SKIL-06 | Phase 5 | Complete |
| SKIL-07 | Phase 5 | Complete |
| SKIL-08 | Phase 5 | Complete |
| DMGS-01 | Phase 5 | Complete |
| DMGS-02 | Phase 5 | Complete |
| DMGS-03 | Phase 5 | Complete |
| TECH-01 | Phase 6 | Complete |
| TECH-02 | Phase 6 | Complete |
| TECH-03 | Phase 6 | Complete |
| TECH-04 | Phase 6 | Complete |
| TECH-05 | Phase 6 | Complete |
| TECH-06 | Phase 6 | Complete |
| TECH-07 | Phase 6 | Complete |
| TECH-08 | Phase 6 | Complete |
| TECH-09 | Phase 6 | Complete |
| TECH-10 | Phase 6 | Complete |
| LEVL-01 | Phase 6 | Complete |
| LEVL-02 | Phase 6 | Complete |
| LEVL-03 | Phase 6 | Complete |
| LEVL-04 | Phase 6 | Complete |
| LEVL-05 | Phase 6 | Complete |
| ASTR-06 | Phase 6 | Complete |

**Coverage:**
- v1 requirements: 83 total
- Mapped to phases: 83
- Unmapped: 0

---
*Requirements defined: 2026-02-17*
*Last updated: 2026-02-17 after roadmap creation*
