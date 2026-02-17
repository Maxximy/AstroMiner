# Project Research Summary

**Project:** AstroMiner
**Domain:** Unity Hybrid ECS idle/clicker game (2D space arcade, WebGL + Desktop)
**Researched:** 2026-02-17
**Confidence:** MEDIUM-HIGH

## Executive Summary

AstroMiner is an active-play idle/clicker set in space where players use a hovering mining circle to damage asteroids, collect minerals, earn credits, and upgrade through a branching tech tree between timed runs. The genre's core loop is immediate sensory reward (damage numbers, particles, sounds) + run-then-upgrade session structure, but AstroMiner differentiates through active ship skills with aiming, physics-based mineral collection, and special asteroid types — mechanics that are rare in the space mining idle genre. The recommended approach is Unity 6.3 LTS with Hybrid ECS: DOTS handles simulation data (asteroid health, mineral movement, damage math, skill cooldowns) while GameObjects handle everything visual (rendering, UI, audio, particles). This is the correct architecture for ~1000 active entities at 60 FPS, while keeping the codebase compatible with both WebGL and Windows desktop targets.

The single most important technical constraint is that Entities Graphics and Burst compilation do not support WebGL. This means all rendering must go through GameObject SpriteRenderers synced from ECS data, and the ECS performance advantage on WebGL is reduced (single-threaded, no Burst SIMD). The mitigation is to keep entity counts reasonable (under 500 active), test WebGL builds from Phase 1 onward, and design the bridge between ECS and the GameObject layer with a single disciplined event-buffer pattern. A well-defined bridge layer (InputBridge, AudioEventBridge, UIDataBridge) is the structural backbone that everything else depends on — it must be established before any gameplay systems are built.

The highest risks are: (1) discovering late that DOTS is too slow on WebGL and needing to rewrite simulation systems, (2) bridge layer turning into scattered spaghetti that is expensive to refactor, (3) WebGL save data silently lost due to IndexedDB flush issues, and (4) economy balance collapsing without a mathematical model built before any upgrade code is written. All four risks are preventable with front-loaded validation work in Phase 1 and Phase 2.

## Key Findings

### Recommended Stack

Unity 6000.3.8f1 (LTS through December 2027) with URP 17.3.0 is already in the project and is the correct foundation. The DOTS stack (Entities 1.4.4, Physics 1.4.3, Collections 2.6.4, Burst 1.8.27, Mathematics 1.3.3) is released and stable for this editor version and needs to be added to `Packages/manifest.json`. The critical constraint is that `com.unity.entities.graphics` must NOT be added — it does not support WebGL. All rendering uses GameObject SpriteRenderers positioned by ECS sync scripts. The entire ECS layer is data-and-logic only: no rendering, no UI, no audio.

For persistence, Newtonsoft.Json (com.unity.nuget.newtonsoft-json 3.2.2) handles complex save structures (polymorphic tech tree state, dictionaries) that JsonUtility cannot. Player settings go to PlayerPrefs; full game saves go to JSON in Application.persistentDataPath (backed by IndexedDB on WebGL, requiring an explicit `.jslib` flush plugin). For UI tweening, PrimeTween 1.3.7 is recommended over DOTween for zero-allocation performance on WebGL, though either works. uGUI (Canvas) is the required UI system per project constraints — UI Toolkit is not viable here.

**Core technologies:**
- Unity 6000.3.8f1 + URP 17.3.0: Engine and render pipeline — already in project, LTS stable
- com.unity.entities 1.4.4: ECS framework — simulation backbone for asteroids, minerals, damage
- com.unity.physics 1.4.3: Spatial queries — collision/overlap checks integrated with ECS natively
- com.unity.burst 1.8.27: Burst compiler — optimizes IJobEntity on desktop; managed fallback runs on WebGL automatically
- com.unity.nuget.newtonsoft-json 3.2.2: JSON serialization — save system and tech tree data
- com.unity.ugui 2.0.0: UI — HUD, tech tree, menus; Canvas-based, WebGL compatible
- PrimeTween 1.3.7: UI animation — zero-allocation tweening for number popups and feedback
- TextMeshPro (built-in): Rich text — damage numbers, UI labels

**What NOT to add:** com.unity.entities.graphics (WebGL incompatible), Entities.ForEach / Job.WithCode (obsoleted in Entities 1.4, use IJobEntity and SystemAPI.Query), SubScenes for entity data (broken on WebGL), LeanTween (unmaintained).

### Expected Features

The feature research provides a clear three-tier model. The core mining loop is the non-negotiable foundation — without responsive, satisfying mining the game has no reason to exist. Active ship skills are the primary competitive differentiator against passive-only idle miners. Prestige, offline income, and multiplayer are correctly deferred to v2+ to avoid scope explosion and economy rebalancing risk.

**Must have (table stakes — launch blockers):**
- Mining circle follows mouse, applies tick-based AoE damage to asteroids in radius
- Asteroids spawn, drift, have HP, destroy into mineral particles flying to ship
- Credit economy: minerals award credits, credits fund tech tree upgrades
- Timed runs with clear start/results/upgrade session structure
- Tech tree: at minimum Mining branch (Radius, Damage, Rate) and Run branch (Time)
- At least 2 resource tiers with distinct visuals, at least 2 levels with different drop tables
- Damage numbers, particle effects, audio SFX on every meaningful action
- Persistent save with auto-save on run end and every purchase
- Large number formatting (K/M/B/T) — implement early, used everywhere
- Basic settings (volume, quality preset for WebGL performance)

**Should have (competitive differentiators — v1.x post-validation):**
- Ship Skills: Laser Burst, Chain Lightning, EMP, Overcharge (4 skills, keyboard 1-4 + UI buttons)
- Special asteroid types: Fragile (quick reaction), Mega (big payout), Cluster (chain rewards)
- Critical hit system with "CRIT!" visual feedback and screen shake
- Damage-over-time (DoT) with ember particles
- Economy tech tree branch: Resource Multiplier, Lucky Strike, Abundance
- Edge-of-timer tension: last-10-second visual glow and audio urgency shift
- Combo Mastery: skill succession multiplier for skill expression ceiling

**Defer (v2+):**
- Prestige/rebirth system — requires complete, balanced base game
- Auto-miner / offline idle income — fundamentally different economy; separate balance pass
- Achievements, daily challenges, events — live-ops features
- Multiplayer/leaderboards — requires backend + anti-cheat
- Cloud saves — PlayerPrefs + JSON sufficient for v1

**Anti-features to avoid:** Real-time difficulty scaling (undermines upgrade satisfaction), gacha/lootbox mechanics (destroys player trust in a PC/WebGL game), ship movement (conflicts with mining circle core interaction), complex crafting system (second resource sink competing with tech tree).

### Architecture Approach

The project uses a strict three-layer Hybrid ECS architecture. The ECS layer owns all simulation state as unmanaged IComponentData structs (AsteroidHealth, OrbitalMotion, MiningCircle, MineralPosition, DamageEvent, SkillCooldown, GameStateData, PlayerStats). The Bridge layer — a set of dedicated MonoBehaviours in Scripts/MonoBehaviours/Bridge/ — is the only place ECS and GameObjects communicate: InputBridge writes mouse position into ECS singletons each frame; AudioEventBridge and UIDataBridge drain DynamicBuffer event queues in LateUpdate to trigger audio, particles, and HUD updates. The GameObject layer handles everything user-facing: uGUI Canvas, AudioManager, CameraRig, particle pools, and the tech tree screen. ECS systems should use ISystem + IJobEntity for hot paths (movement, damage) and SystemBase only for managed bridge logic. EntityCommandBuffers handle all structural changes (spawn, destroy, detach minerals) to avoid sync point stalls.

**Major components:**
1. ECS Layer (ISystem + IJobEntity): AsteroidSpawnSystem, AsteroidMovementSystem, MiningDamageSystem, MineralPullSystem, MineralCollectionSystem, DamageEventSystem, SkillSystems, GameStateSystem
2. Bridge Layer (MonoBehaviour): InputBridge (MB writes mouse pos to ECS singleton), UIDataBridge (reads ECS singletons for HUD), AudioEventBridge (drains DynamicBuffer for SFX triggers)
3. GameObject Layer (MonoBehaviour): GameManager (state machine), TechTreeController (uGUI + ScriptableObjects), HUDController, DamagePopupManager (pooled TextMeshPro), AudioManager, SaveManager, CameraController
4. Config Layer (ScriptableObjects): AsteroidTypeSO, LevelConfigSO, UpgradeNodeSO, SkillDefinitionSO — static data authoring baked into ECS or read at runtime by MonoBehaviours
5. Save Layer: SaveManager serializes only progression state (credits, tech tree unlocks, player stats) as plain C# class to JSON — never serialize ECS entity state directly

### Critical Pitfalls

1. **DOTS without Burst on WebGL kills performance rationale** — Profile WebGL in Phase 1 with 100 entities + 1000 minerals. If frame time is 2x+ desktop, plan fallback to MonoBehaviour + NativeArray for simulation. Design for graceful degradation from the start.

2. **Hybrid bridge becomes synchronization spaghetti** — Establish a single, documented bridge pattern in Phase 1 (NativeQueue/DynamicBuffer events written by ECS, drained by MonoBehaviours in LateUpdate). Never allow direct EntityManager access from non-bridge MonoBehaviours. Prevention is 10x cheaper than the 2-3 week refactor.

3. **WebGL save data silently lost** — Implement `.jslib` plugin calling `FS.syncfs()` after every save from Phase 1. Add save version number to JSON payload. Test persistence across builds before any public deployment. Safari iframes on itch.io do not support IndexedDB at all.

4. **Economy collapses without a mathematical model** — Build a spreadsheet model of the credit economy before writing any upgrade code. Define target pacing (e.g., Level 2 unlocked after ~10 runs). Make all values data-driven (ScriptableObjects/JSON), never hardcoded. Implement a debug panel showing credits/minute and time-to-next-upgrade.

5. **EntityCommandBuffer structural changes tank framerate** — Never call EntityManager.DestroyEntity or AddComponent inside system foreach loops. Use ECB from the appropriate ECBSystem. Pre-define EntityArchetypes for Asteroid, AttachedMineral, DetachedMineral. On WebGL (single-threaded), structural change cost is amplified.

## Implications for Roadmap

Based on the combined research, the architecture file's 7-phase build order is validated by the feature dependency graph and pitfall prevention requirements. The phase structure below reflects those dependencies with pitfall mitigations integrated.

### Phase 1: Foundation and WebGL Validation
**Rationale:** Everything depends on the ECS world, game state machine, bridge pattern, and WebGL compatibility being confirmed. The three most expensive pitfalls (Burst-on-WebGL, bridge spaghetti, ECB patterns) must all be addressed here. This is validation work, not feature work.
**Delivers:** ECS world bootstrapped, GameManager state machine (Playing/Collecting/GameOver/Upgrading), InputBridge with mouse-to-world projection, singleton components (GameStateData, PlayerStats), object pool infrastructure, `.jslib` IndexedDB flush plugin, WebGL build with 100+ entities at target framerate
**Addresses:** DOTS packages installed, bridge architecture defined, pooling infrastructure established
**Avoids:** WebGL Burst pitfall, bridge spaghetti pitfall, ECB structural change pitfall, WebGL audio autoplay pitfall

### Phase 2: Core Mining Loop
**Rationale:** This is the minimum viable game. Without responsive, satisfying mining there is no product. Asteroid spawning, damage, and destruction form the dependency root for all subsequent features.
**Delivers:** Asteroids spawn/drift/take damage/destroy, mining circle follows mouse and applies tick-based AoE damage, asteroid HP tracked as ECS component, ECB-batched destruction
**Addresses:** Mining circle (P1), Asteroid spawning + HP (P1), Responsive controls (P1)
**Avoids:** Frame-rate-dependent damage ticks (use deltaTime accumulator), per-asteroid Physics collision (use IJobEntity distance checks, not Physics.OverlapSphere)

### Phase 3: Collection, Economy, and Save System
**Rationale:** Closes the core loop (mine → collect → earn → persist). The save system is load-bearing — everything built afterward depends on persistence being reliable. Economy math must be modeled before tech tree code is written.
**Delivers:** Mineral particle pull-to-ship physics (ECS IJobEntity), credit award on collection, run timer with results screen, HUD (score + timer + credits), save/load with WebGL IndexedDB flush, large number formatting utility
**Addresses:** Mineral particle collection (P1), Credit economy (P1), Run timer (P1), Persistent save (P1), Large number formatting (P1), Results screen (P1)
**Avoids:** Economy balance collapse (build spreadsheet model before upgrade code), WebGL save data loss (implement .jslib flush here), save corruption (single monolithic JSON is acceptable MVP, add versioning before public release)

### Phase 4: Visual and Audio Feedback
**Rationale:** The feedback layer transforms a functional loop into a satisfying one. Damage numbers, particles, and audio are genre table stakes that cannot ship without. Object pooling patterns established in Phase 1 are consumed here.
**Delivers:** Damage popup manager (pooled TextMeshPro), AudioEventBridge consuming DynamicBuffer events, AudioManager with SFX/music, particle effects (mining hits, asteroid explosions, mineral collection), screen shake, UIDataBridge for HUD
**Addresses:** Visual/audio feedback on every action (P1), Particle effects (P1), Audio SFX (P1), Damage numbers (P1)
**Avoids:** Damage number string allocation GC spikes (use SetText with numeric overloads, pool instances), unbatched particle trail renderers (single ParticleSystem with sub-emitters), URP post-processing cost on WebGL (profile and cap at 2ms)

### Phase 5: Ship Skills and Advanced Damage
**Rationale:** Skills are the primary differentiator. They require the damage pipeline (Phase 2) and the VFX system (Phase 4) to exist first. Crit and DoT extend the same damage math.
**Delivers:** 4 ship skills (Laser Burst, Chain Lightning, EMP, Overcharge) with cooldown components, SkillBarController UI, critical hit system with visual "CRIT!" feedback, damage-over-time with ember particles, combo mastery timestamp tracking
**Addresses:** Active skill system (differentiator), Crit + DoT systems (P2), Screen shake on crits (P2), Edge-of-timer warning (P2)
**Avoids:** Skills must use IJobEntity for AoE/chain calculations; DoT must clean up on entity destruction (verify no null-ref on dead entities)

### Phase 6: Tech Tree, Level Progression, and Full Economy
**Rationale:** Meta-game layer that provides long-term retention. Requires Phase 3 (credits to spend) and Phase 5 (skills to unlock). Economy balance validation happens here against the spreadsheet model built in Phase 3.
**Delivers:** TechTreeController with ~30 nodes across 5 branches (Mining, Ship/Skills, Economy, Run Duration, future placeholder), prerequisite gating, UpgradeNodeSO data model, stat application to PlayerStats ECS singleton, level progression system with ScriptableObject configs per level, multiple resource tiers with distinct materials
**Addresses:** Tech tree with meaningful choices (P1), Level/zone progression (P1), Economy branch (P2), Multiple resource tiers (P1)
**Avoids:** Tech tree canvas rebuild on every purchase (dirty-flag pattern, cache connection lines), showing all 30 nodes immediately (progressive disclosure: show locked + adjacent only), no clear indication of upgrade effect (before/after comparison on purchase)

### Phase 7: Special Asteroids, Polish, and Content Completion
**Rationale:** Content and polish on top of complete gameplay systems. Special asteroid types extend the core loop but are not structural. WebGL optimization pass happens here as a final validation gate.
**Delivers:** Special asteroid types (Fragile, Mega, Cluster), all 6 resource tiers, remaining levels with drop tables, visual material differentiation per resource, WebGL memory profiling and optimization, Brotli-compressed build size validation
**Addresses:** Special asteroid types (P2), All 6 resource tiers (P1 deferred content), Additional VFX and material polish
**Avoids:** WebGL memory ceiling (profile in Chrome DevTools, target under 512MB), WebGL build size bloat (target under 30MB Brotli-compressed, under 15s load on 10Mbps)

### Phase Ordering Rationale

- Phases 1 → 2 → 3 are strictly sequential: ECS world must exist before mining systems, mining systems must exist before collection and economy
- Phase 4 can partially overlap Phase 3: AudioManager infrastructure and particle pool setup can begin while scoring is finalized
- Phase 5 depends on Phase 2 (damage pipeline) and Phase 4 (VFX for skill effects) being complete
- Phase 6 depends on Phase 3 (credits to spend) and Phase 5 (skills to unlock via tech tree)
- Phase 7 is always last: content and polish on top of all functioning systems
- WebGL validation is not a phase — it is a continuous requirement from Phase 1 onward with a final gate in Phase 7

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 1:** WebGL + DOTS compatibility is documented but sparse for the specific Entities 1.4.x / Unity 6.3 combination. The `.jslib` IndexedDB flush plugin pattern needs a concrete implementation reference. Validate the ECBSystem group selection for this project's system ordering before writing spawn code.
- **Phase 5:** Chain Lightning targeting algorithm (pick nearest unlit asteroid within N range from each struck asteroid) and EMP Pulse AoE radius on ECS entities need concrete implementation patterns. Combo timestamp tracking in ECS components is non-trivial.
- **Phase 6:** Prerequisite validation across a 30-node branching tree (not a simple linear chain) and the uGUI tech tree connection line rendering (UILineRenderer or vertex drawing) need implementation research. The save migration strategy (v1 save loading in v2 build) needs a concrete schema versioning pattern.

Phases with standard patterns (skip research):
- **Phase 2:** Asteroid ECS components, IJobEntity movement, distance-check damage, ECB destruction are all well-documented DOTS patterns with official samples.
- **Phase 3:** Mineral pull physics (acceleration toward target position), credit accumulation in a singleton component, and PlayerPrefs + JSON save are standard and well-understood.
- **Phase 4:** Object pooling with a pre-allocated pool and recycle-on-complete is a standard Unity pattern. AudioManager singleton with an AudioSource pool is textbook.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Unity 6.3 LTS + Entities 1.4.4 confirmed released and compatible. WebGL constraints confirmed by official docs and Unity staff responses. Package versions verified. |
| Features | MEDIUM | Genre analysis is solid (multiple competitor games referenced). Specific feature priority ordering is synthesized from idle game design principles, not verified against AstroMiner playtest data. Balance values unknown. |
| Architecture | MEDIUM-HIGH | Hybrid ECS patterns are officially documented. ISystem vs SystemBase split is clear. The specific bridge pattern (DynamicBuffer events drained by MonoBehaviours) is community-synthesized, not an official Unity sample. Works in principle. |
| Pitfalls | MEDIUM-HIGH | WebGL constraints confirmed by official docs. Economy balance risks verified by idle game post-mortems. ECB structural change pitfall is documented DOTS behavior. Save persistence issues on WebGL are community-verified on itch.io. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **WebGL DOTS performance at actual entity counts:** The 5-10x slowdown estimate on WebGL without Burst is directionally correct but not calibrated to this specific game's entity counts (100 asteroids + 1000 minerals). The Phase 1 WebGL profiling benchmark will resolve this. If performance is unacceptable, the fallback is MonoBehaviour + NativeArray simulation (1-2 week refactor, not a project-stopper).

- **Economy balance values:** The GDD notes "exact values to be balanced during playtesting." The research confirms this is correct but flags it as a risk without a mathematical model. The spreadsheet model must be built before Phase 3 upgrade code is written. Target pacing (runs per level unlock, session length per full tech tree completion) is a design decision not resolvable by research.

- **Entities Graphics in ARCHITECTURE.md footnote:** The architecture document references Entities Graphics in the rendering block of its system diagram and in the Required Packages table, which contradicts the STACK.md hard constraint (no Entities Graphics on WebGL). This is a documentation inconsistency in ARCHITECTURE.md — the correct implementation is pure GameObject SpriteRenderer rendering as stated in STACK.md. All development must follow the STACK.md constraint.

- **com.unity.entities version discrepancy:** ARCHITECTURE.md references version 1.3.x while STACK.md specifies 1.4.4. Use 1.4.4 as specified in STACK.md (released for Unity 6000.3, confirmed in What's New docs). ARCHITECTURE.md was written with an earlier version reference.

## Sources

### Primary (HIGH confidence)
- [Unity Entities 1.4.4 Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html) — Package API, IJobEntity, SystemAPI patterns
- [Entities Graphics 6.5.0 Overview](https://docs.unity3d.com/Packages/com.unity.entities.graphics@6.5/manual/overview.html) — Confirms no WebGL support
- [Unity WebGL Technical Limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html) — Official WebGL constraints documentation
- [What's New in Entities 1.4](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/whats-new.html) — Entities.ForEach / Job.WithCode obsoleted
- [Systems comparison (ISystem vs SystemBase)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-comparison.html) — ISystem preferred for hot paths
- [EntityCommandBuffer Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) — ECB batching patterns
- [Unity WebGL Audio Documentation](https://docs.unity3d.com/Manual/webgl-audio.html) — Audio autoplay policy on WebGL
- [Unity 6.3 LTS Release](https://unity.com/blog/unity-6-3-lts-is-now-available) — Editor version confirmation

### Secondary (MEDIUM confidence)
- [WebGL Platform Support for Entities Graphics](https://discussions.unity.com/t/webgl-platform-support-for-entities-graphics/918881) — Community confirmation + Unity staff response
- [Burst for WebGL Discussion](https://discussions.unity.com/t/burst-for-webgl/849368) — Confirms no Burst WebGL support
- [WebGL Build Fails with Entities](https://discussions.unity.com/t/unity-webgl-build-cant-load-entity-scene-files-from-streamingassets-works-in-editor-standalone/1673854) — Entity scene streaming broken on WebGL
- [ECS Development Status December 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) — Latest DOTS roadmap
- [WebGL PlayerPrefs/IndexedDB Issues](https://itch.io/t/140214/persistent-data-in-updatable-webgl-games) — Community-verified persistence issues
- [Kongregate: Math of Idle Games](https://blog.kongregate.com/the-math-of-idle-games-part-i/) — Authoritative idle game economy design reference
- [Hybrid ECS architecture advice](https://discussions.unity.com/t/need-advice-for-hybrid-architecture-with-ecs/789215) — Bridge pattern community consensus
- [Space Miner: Idle Adventures on Steam](https://store.steampowered.com/app/2679860/Space_Miner__Idle_Adventures/) — Competitor feature analysis
- [AsteroIdle on Steam](https://store.steampowered.com/app/2007080/AsteroIdle/) — Competitor feature analysis

### Tertiary (LOW confidence)
- [DOTween vs LeanTween vs PrimeTween Comparison](https://omitram.com/unity-tweening-guide-dotween-leantween-primetween/) — Tweening benchmarks (third-party blog)
- [Idle Game Design Principles - Eric Guan](https://ericguan.substack.com/p/idle-game-design-principles) — Community design reference

---
*Research completed: 2026-02-17*
*Ready for roadmap: yes*
