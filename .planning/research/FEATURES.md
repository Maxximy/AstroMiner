# Feature Research

**Domain:** Idle/clicker asteroid mining game (active-play focused, Unity Hybrid ECS)
**Researched:** 2026-02-17
**Confidence:** MEDIUM -- based on genre analysis of idle/clicker/mining games cross-referenced with GDD scope; ECS-specific considerations from Unity DOTS community discussions

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete or broken. Ordered roughly by implementation priority.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Core mining loop (hover circle, damage, destroy) | Defines the entire game. Without responsive, satisfying mining the game has no reason to exist. | HIGH | Already in GDD. ECS entities for asteroids + minerals. Mouse-to-world raycast every frame. Damage tick system. |
| Visual/audio feedback on every action | Genre standard since Cookie Clicker. Every click/hover must produce immediate sensory reward. Damage numbers, particles, sounds. | MEDIUM | Damage popups are a known perf bottleneck at scale -- pool aggressively. Screen shake, particle bursts, collection chimes all table stakes. |
| Clear resource display and running totals | Players need to see numbers going up at all times. This IS the dopamine loop. | LOW | Credits display during runs, results screen after. Consider per-second rate display too. |
| Upgrade/tech tree with meaningful choices | Core retention mechanic. Players need to spend currency on upgrades that produce *noticeable* power increases. Must feel like real decisions, not linear purchases. | HIGH | GDD specifies 5 branches, ~30 nodes. uGUI implementation. Needs clear cost/effect display, locked/unlocked states, prerequisite visualization. |
| Run-upgrade-run loop with clear progression | Session structure is genre-defining. Timed runs create urgency, upgrade screens create planning, next run validates decisions. | MEDIUM | GDD specifies timed runs + results screen + tech tree screen. Transition polish between phases matters. |
| Level/zone progression | Players expect new content unlocks. Same asteroids forever = abandonment. Higher levels with new resources, higher HP, different drop tables. | MEDIUM | GDD has 6 resource tiers across multiple levels. Each level needs a distinct visual identity or resource mix to feel like progression. |
| Persistent save system | Losing progress in an idle/clicker is unacceptable. Auto-save is expected, not manual save. | MEDIUM | PlayerPrefs + JSON per GDD. Auto-save on every purchase and run completion. WebGL uses IndexedDB backing. Must be corruption-resistant. |
| Responsive controls (low-latency mining circle) | Mouse tracking must be frame-perfect. Any perceivable lag between mouse movement and circle position = game feels broken. | LOW | Straightforward raycast but must run every frame, not on tick. ECS system should update position in LateUpdate equivalent. |
| Results/summary screen after each run | Players need to see what they earned and feel the payoff of a run. Credits earned, minerals collected, time spent. | LOW | Standard post-run screen. Show totals, possibly per-resource breakdown. |
| Large number formatting | Idle games produce big numbers fast. Displaying "1,234,567,890" is unreadable. Players expect K/M/B/T notation or scientific notation. | LOW | Standard patterns exist. Implement early since it touches all UI. Support at minimum K, M, B, T suffixes. |
| Settings (volume, quality) | Basic audio and graphics controls are minimum viable polish. | LOW | Audio mixer with SFX/Music sliders. Quality preset toggle for WebGL performance. |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but create competitive advantage in the space mining idle genre.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Active skill system (Laser, Chain Lightning, EMP, Overcharge) | Most idle miners are passive-only (tap or auto). Active skills with cooldowns, aiming, and combo potential add a skill ceiling that rewards engaged play. This is Astrominer's primary differentiator. | HIGH | 4 skills in GDD, each with unique VFX, cooldown, targeting. ECS job-friendly for AoE/chain calculations. Keyboard (1-4) + UI buttons. |
| Special asteroid types (Fragile, Mega, Cluster) | Breaks monotony of identical targets. Creates micro-decisions during runs (prioritize Fragile before it disappears? Focus Mega for big payout? Pop Cluster for chain rewards?). | MEDIUM | Each type needs distinct visual language, behavioral rules, and reward tuning. Cluster requires child-entity spawning on destruction. |
| Physics-based mineral collection (particles fly to ship) | Most idle games just increment a counter. Watching particles physically stream toward the ship after a big kill is viscerally satisfying. Key "juice" moment. | MEDIUM | Already in GDD. ECS mineral entities with acceleration toward ship. Visual trails. The volume of particles (10 per asteroid, 100+ asteroids) is where Hybrid ECS shines. |
| Hybrid active/idle design space | v1 is active-only, but the architecture should accommodate future auto-miners. The tension between active play (skills, aiming) and automation (auto-miners, drones) is a strong design axis competitors like Space Miner: Idle Adventures use effectively. | LOW (for v1) | Do not build auto-miners in v1. But design upgrade system and credit economy to be extensible. Flag in tech tree as "locked -- future update." |
| Combo/synergy mechanics (Combo Mastery upgrade) | Rewarding rapid skill usage with damage bonuses adds depth. Creates a skill expression ceiling rare in idle games. | MEDIUM | GDD includes Combo Mastery as a Ship branch capstone. Track skill timestamps, apply multiplier for quick succession. |
| Damage-over-time (DoT) and critical hit systems | Adds invisible depth. Crits create excitement spikes. DoT means leaving an asteroid still deals damage, rewarding circle movement strategy. | MEDIUM | GDD includes Crit Chance, Crit Multiplier, and DoT in Mining branch. Visual differentiation (yellow CRIT! text, ember particles for DoT) is critical for feel. |
| Edge-of-timer tension | Last 10 seconds with visual warning (edge glow, audio shift). Creates memorable "just barely" moments. | LOW | GDD mentions edge glow. Add audio urgency cue. Low cost, high impact on run feel. |
| Multiple visual asteroid variants per resource | Distinct PBR materials per resource tier (orange copper, shiny gold, blue cobalt). Players develop resource recognition and prioritization instincts. | MEDIUM | 6 resource types need distinct materials. 2-3 mesh variants per tier ideal but not required for v1. At minimum, color + material differentiation. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems for this specific game.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Offline/idle income in v1 | Genre expectation for "idle" games. Players ask "what happens when I close the game?" | Fundamentally changes economy balance. Active-only design means upgrade costs are tuned to active earning rates. Adding offline income retroactively requires rebalancing the entire economy. Also adds complexity: offline calculation simulation, catch-up display, abuse prevention. | Ship v1 as active-only. Design economy with offline income as a future multiplier (not base rate). Add "Auto-Miner" branch to tech tree in v2 with its own balance pass. |
| Prestige/rebirth system in v1 | Standard in mature idle games. Adds infinite progression. Players on forums ask for it. | Premature prestige kills retention. You need a complete, balanced base game before you can reset it meaningfully. Prestige currencies, permanent bonuses, and reset logic add massive scope. Building it early means balancing two economies simultaneously. | Design tech tree to be "prestige-aware" (leave space for a prestige branch). Ensure save system can handle a reset. Do not build the actual prestige loop until base game is validated. |
| Gacha/lootbox mechanics | Mobile idle games monetize this way. High revenue potential. | Destroys trust in a premium/free PC+WebGL game. Pay-to-win perception alienates the core audience. Randomized rewards feel unfair when players can see the tech tree. | Deterministic rewards. Tech tree choices ARE the progression depth. Special asteroids provide variance without gacha. |
| Multiplayer/leaderboards in v1 | "Would be cool to compare with friends." | Requires server infrastructure, anti-cheat, network code. Massive scope for a single-player idle game. Leaderboards in WebGL require a backend. | Add local stats tracking (personal bests per level). Defer leaderboards to v2 if demand exists. |
| Real-time difficulty scaling | "Game should get harder as you get stronger." | Undermines the core idle satisfaction: feeling powerful. If enemies scale with the player, upgrades feel meaningless. The whole point is that upgrades make you noticeably stronger. | Fixed difficulty per level. Players feel stronger within a level as they upgrade. New levels provide difficulty steps. |
| Complex crafting system | Games like Idle Cave Miner have crafting. Players may expect it. | Adds a second resource sink competing with the tech tree. Two upgrade systems create decision paralysis and dilute the core loop. Astrominer's strength is the clean mine-upgrade-repeat loop. | Keep single currency (credits). Tech tree IS the crafting equivalent. If crafting is desired later, add it as a tech tree branch (e.g., "Forge" branch that converts excess resources). |
| Ship movement/flying | "Let me fly the ship around." Players expect space game = flying. | Changes the game from idle/clicker to action game. Mining circle mechanic is the core interaction -- ship movement would make it redundant or conflicting. Controller input complexity explodes. | Ship is stationary (per GDD). Skills fire FROM ship TO circle position. This creates the "ship as base" fantasy without movement complexity. |
| Too many simultaneous systems at launch | "Add pets, events, daily quests, achievements, collection books..." | Feature bloat dilutes the core loop. Each system needs balancing, UI, persistence, bug fixing. Idle games that launch with too many systems feel overwhelming and shallow. | Launch with core loop + tech tree + skills + levels. Add secondary systems (achievements, daily challenges, events) in post-launch updates when the base is solid. |

## Feature Dependencies

```
[Mining Circle + Damage System]
    |-- requires --> [Asteroid Spawning + Health]
    |                   |-- requires --> [Resource Tier System]
    |                   |-- enables --> [Special Asteroid Types]
    |
    |-- enables --> [Mineral Particle Collection]
    |                   |-- requires --> [Credit Economy]
    |                                       |-- enables --> [Tech Tree + Upgrades]
    |                                       |                   |-- enables --> [Level Progression]
    |                                       |                   |-- enables --> [Ship Skills]
    |                                       |                   |                   |-- enhances --> [Combo Mastery]
    |                                       |                   |-- enables --> [Mining Upgrades (Radius, Damage, Rate)]
    |                                       |                   |-- enables --> [Economy Upgrades (Multiplier, Lucky Strike)]
    |                                       |                   |-- enables --> [Run Duration Upgrades]
    |                                       |
    |                                       |-- requires --> [Large Number Formatting]
    |
    |-- enhances --> [Visual Feedback (damage numbers, particles, screen shake)]
    |-- enhances --> [Audio Feedback (SFX per action)]

[Save System]
    |-- independent, but must exist before --> [Tech Tree Persistence]
    |-- independent, but must exist before --> [Level Progression Persistence]

[Run Timer]
    |-- requires --> [Game State Machine (Playing/Collecting/GameOver/Restarting)]
    |-- enables --> [Results Screen]
    |-- enhances --> [Edge-of-timer Tension (last 10s warning)]

[Crit System] -- requires --> [Base Damage System]
[DoT System] -- requires --> [Base Damage System]
[Crit System] -- conflicts with (perf, if both frequent) --> [Excessive Damage Number Spawning]
```

### Dependency Notes

- **Tech Tree requires Credit Economy:** Cannot purchase upgrades without a functioning currency. Economy must be in place and roughly balanced before tech tree is meaningful.
- **Ship Skills require Tech Tree:** Skills are unlocked through the Ship branch. Tech tree UI and unlock logic must exist first.
- **Level Progression requires Tech Tree:** "Advance" upgrade is a tech tree node. Level unlocks are gated by credit thresholds AND tech tree purchases.
- **Special Asteroids enhance Core Loop:** They are optional complexity layered on top of the base asteroid system. Can be added after standard asteroids work.
- **Combo Mastery requires Ship Skills:** Cannot track skill succession without skills being functional first.
- **Visual Feedback enhances Mining Loop:** Damage numbers, particles, screen shake can be added incrementally. But basic damage feedback should ship with the first playable build.
- **Save System is load-bearing:** Everything built on top of it. Build early, build robust. Corruption = player quits forever.
- **Large Number Formatting conflicts with late addition:** Touching every UI text element. Add the formatting utility early and use it everywhere from the start.

## MVP Definition

### Launch With (v1)

Minimum viable product -- what's needed to validate the core loop is satisfying.

- [ ] Mining circle follows mouse, damages asteroids within radius -- core interaction
- [ ] Asteroids spawn, drift downward, have HP, are destroyed when HP = 0 -- targets
- [ ] Destroyed asteroids release mineral particles that fly to ship and award credits -- reward
- [ ] Timed runs with clear start/end, results screen showing credits earned -- session structure
- [ ] Tech tree with at minimum Mining branch (Radius, Damage, Rate) and Run branch (Time) -- upgrade path
- [ ] At least 2 resource tiers with distinct visuals (Iron/Copper, Silver/Cobalt) -- variety
- [ ] At least 2 levels with different drop tables -- progression carrot
- [ ] Damage numbers on hit (basic floating text) -- feedback
- [ ] Particle effects on asteroid destruction and mineral collection -- juice
- [ ] Audio: mining hit SFX, destruction SFX, collection chime, ambient music -- immersion
- [ ] Persistent save (auto-save on run end and upgrade purchase) -- retention
- [ ] Large number formatting (K/M/B) -- UI readability
- [ ] Basic settings (volume control) -- minimum polish

### Add After Validation (v1.x)

Features to add once core loop is confirmed satisfying.

- [ ] Ship Skills (Laser Burst first, then Chain Lightning, EMP, Overcharge) -- unlocked sequentially to validate each
- [ ] Economy branch of tech tree (Resource Multiplier, Lucky Strike, Abundance) -- deepens upgrade choices
- [ ] Special asteroid types (Fragile first for quick reactions, then Mega, then Cluster) -- adds variety
- [ ] Critical hit system with visual "CRIT!" feedback -- excitement spikes
- [ ] Damage-over-time (DoT) with ember particles -- strategic depth
- [ ] Screen shake on crits and skill impacts -- polish
- [ ] Edge-of-timer warning (glow + audio shift in last 10s) -- tension
- [ ] All 6 resource tiers and remaining levels -- full content
- [ ] Combo Mastery upgrade -- skill expression ceiling
- [ ] Overcharge skill with visual mining circle enhancement -- power fantasy

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] Prestige/rebirth system -- requires complete, balanced base game to reset meaningfully
- [ ] Auto-miner / idle income -- fundamentally different economy balance; add as separate tech tree branch
- [ ] Achievements system -- nice retention layer but not core; add when base content is stable
- [ ] Additional special asteroids (Radioactive risk/reward, Golden rare bonus) -- content expansion
- [ ] Ship visual customization -- cosmetic layer; low priority vs gameplay systems
- [ ] Daily challenges / events -- live-ops features; require stable base game and possibly server support
- [ ] Cloud saves -- requires backend; PlayerPrefs + JSON sufficient for v1-v1.x
- [ ] Leaderboards -- requires backend + anti-cheat; defer unless strong community demand

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority | ECS Relevance |
|---------|------------|---------------------|----------|---------------|
| Mining circle + damage | HIGH | HIGH | P1 | Core ECS system: entity queries, spatial checks |
| Asteroid spawning + HP | HIGH | MEDIUM | P1 | Entity archetype, burst-compiled spawn |
| Mineral particle collection | HIGH | MEDIUM | P1 | 1000+ entities, acceleration system, collection threshold -- ECS sweet spot |
| Credit economy | HIGH | LOW | P1 | Singleton component or managed state |
| Run timer + game states | HIGH | LOW | P1 | State machine, system group enable/disable |
| Tech tree (Mining + Run branches) | HIGH | HIGH | P1 | Managed/MonoBehaviour -- uGUI driven, not ECS |
| Save system | HIGH | MEDIUM | P1 | Serialize ECS state to JSON; handle WebGL IndexedDB |
| Damage numbers | HIGH | MEDIUM | P1 | Perf-sensitive: pool aggressively. Consider world-space canvas or ECS-driven text. |
| Particle effects (destruction, collection) | HIGH | MEDIUM | P1 | ParticleSystem is GameObject-based; trigger from ECS events via hybrid bridge |
| Audio SFX | MEDIUM | LOW | P1 | AudioSource is GameObject-based; trigger from ECS events |
| Large number formatting | MEDIUM | LOW | P1 | Utility class, used by all UI |
| Level progression (2+ levels) | HIGH | MEDIUM | P1 | ScriptableObject-driven level configs |
| Multiple resource tiers | MEDIUM | LOW | P1 | Component data on asteroid entities |
| Ship Skills (4 skills) | HIGH | HIGH | P2 | Projectile/AoE entities in ECS; cooldown components; VFX as GameObjects |
| Special asteroids (3 types) | MEDIUM | MEDIUM | P2 | Variant components on asteroid archetype; Cluster needs child spawning |
| Economy tech tree branch | MEDIUM | MEDIUM | P2 | uGUI extension; modifier system on credit calculation |
| Crit + DoT systems | MEDIUM | MEDIUM | P2 | Additional damage calculation in ECS jobs; visual differentiation |
| Screen shake | MEDIUM | LOW | P2 | Camera shake MonoBehaviour; trigger from ECS events |
| Edge-of-timer warning | MEDIUM | LOW | P2 | Timer threshold check + UI/audio feedback |
| Combo Mastery | LOW | MEDIUM | P3 | Track skill timestamps; apply multiplier |
| Prestige system | MEDIUM | HIGH | P3 | Full economy reset + permanent bonus layer |
| Auto-miner / idle income | MEDIUM | HIGH | P3 | Offline calculation; economy rebalance |
| Achievements | LOW | MEDIUM | P3 | Event tracking + UI |

**Priority key:**
- P1: Must have for launch (validates core loop)
- P2: Should have, add in v1.x (deepens and differentiates)
- P3: Nice to have, future consideration (expansion content)

## Competitor Feature Analysis

| Feature | Idle Cave Miner | Space Miner: Idle Adventures | AsteroIdle | Clicker Heroes | Astrominer (Our Approach) |
|---------|----------------|------------------------------|------------|----------------|--------------------------|
| Core interaction | Click to mine depth layers | Fly ship + drill/laser asteroids | Click/idle mining in space | Click to kill monsters | Hover circle for AoE damage (no clicking required) |
| Active skills | None | Lasers, drones | Technologies | 9 active skills with cooldowns | 4 aimed skills from ship to circle position |
| Upgrade system | Crafting + miners | Research tree | 500+ upgrades, 20+ technologies | Hero leveling + ancients | Branching tech tree (~30 nodes, 5 branches) |
| Idle/offline income | Yes (miners work offline) | Yes (AutoMiners) | Yes (limitless offline) | Yes (idle DPS mode) | No (active-only v1, designed for future addition) |
| Prestige | None visible | Unknown | Dimensions (prestige-like) | Ascension + Transcendence (2 layers) | Not in v1 (architecture accommodates it) |
| Visual feedback | Minimal | Moderate (particles) | Minimal | Damage numbers, gold rain | Heavy emphasis: damage numbers, particles, screen shake, skill VFX |
| Special targets | Environment variety | Asteroid types + sizes | Space objects (15 types) | Boss monsters | Fragile, Mega, Cluster asteroids |
| Differentiator | Crafting + team building | Active flight + automation mix | Sheer quantity of upgrades | Deep prestige meta-game | Active skill aiming + satisfying physics-based collection |

## Unity ECS-Specific Considerations

These are not features per se, but technical requirements the Hybrid ECS architecture must handle well for features to work.

| Concern | Impact | Recommendation |
|---------|--------|----------------|
| **Entity count scaling** | 100 asteroids + 1000 minerals + damage entities + skill projectiles. Could reach 2000+ active entities during intense moments. | ECS handles this well (designed for 10K+). Use IJobEntity for spatial queries. Burst-compile damage and movement systems. |
| **Damage number pooling** | Hundreds of floating texts per second at peak. uGUI text elements are expensive to create/destroy. | Pool damage number GameObjects aggressively. Pre-allocate 50-100. Consider world-space TextMeshPro on a separate canvas with lower update frequency. |
| **Hybrid bridge (ECS to GameObject)** | ParticleSystem, AudioSource, UI are all GameObject-based. Need clean event flow from ECS to managed world. | Use a managed singleton (MonoBehaviour) that reads ECS events each frame. NativeQueue<DamageEvent> written by ECS systems, consumed by MonoBehaviour for VFX/SFX/UI. |
| **WebGL + DOTS compatibility** | DOTS on WebGL has historically been problematic. Burst compiler may not fully optimize on WASM. No multithreading in WebGL. | Test WebGL builds early and often. Jobs will run single-threaded on WebGL (still benefit from Burst SIMD). If DOTS WebGL is unstable, have fallback plan: pure MonoBehaviour for WebGL, ECS for desktop. |
| **Spatial queries (mining circle overlap)** | Every damage tick must check all asteroids against circle radius. O(n) per tick is fine for 100 asteroids but becomes relevant with special asteroids and skills doing additional checks. | Use IJobEntity with BurstCompile for distance checks. Consider spatial hashing if entity count grows past 500. |
| **Save/load with ECS state** | ECS entity data is not trivially serializable. Tech tree and economy state live in managed world (easier). | Keep persistent state in managed classes (PlayerData, TechTreeState). ECS entities are runtime-only and rebuilt on run start from config. Do not try to serialize the EntityManager. |
| **Frame-rate independence** | Damage ticks, mineral movement, asteroid drift all must be deltaTime-based. Fixed vs variable timestep matters for determinism. | Use SystemAPI.Time.DeltaTime in ECS systems. Consider fixed timestep for damage ticks (SystemGroup with fixed rate) to prevent frame-rate-dependent DPS. |

## Sources

- [Top Idle Mining Clicker Games to Play in 2026 - Mr. Mine Blog](https://blog.mrmine.com/top-idle-mining-clicker-games-to-play-in-2025/)
- [Best Idle Games 2026: Why Clicker Heroes Still Tops the Charts](https://blog.clickerheroes.com/best-idle-games-2025-why-clicker-heroes-still-tops-the-charts/)
- [Incremental game - Wikipedia](https://en.wikipedia.org/wiki/Incremental_game)
- [Idle Game Design Principles - Eric Guan](https://ericguan.substack.com/p/idle-game-design-principles)
- [How to design idle games - Machinations.io](https://machinations.io/articles/idle-games-and-how-to-design-them)
- [The Math of Idle Games, Part III - Kongregate](https://blog.kongregate.com/the-math-of-idle-games-part-iii/)
- [Lessons of my first incremental game - Game Developer](https://www.gamedeveloper.com/design/lessons-of-my-first-incremental-game)
- [Idle Clicker Game - Unity Docs](https://docs.unity.com/en-us/services/solutions/idle-clicker-game)
- [Dealing with huge numbers in idle games - InnoGames](https://blog.innogames.com/dealing-with-huge-numbers-in-idle-games/)
- [Names of Large Numbers for Idle Games - Game Developer](https://www.gamedeveloper.com/design/names-of-large-numbers-for-idle-games)
- [AsteroIdle on Steam](https://store.steampowered.com/app/2007080/AsteroIdle/)
- [Space Miner - Idle Adventures on Steam](https://store.steampowered.com/app/2679860/Space_Miner__Idle_Adventures/)
- [Idle Cave Miner on Steam](https://store.steampowered.com/app/2289450/Idle_Cave_Miner/)
- [OreBreaker Idle by Aman Ahuja](https://trcxr.itch.io/orebreaker-idle)
- [Unity DOTS / ECS Performance - Anton Antich](https://medium.com/superstringtheory/unity-dots-ecs-performance-amazing-5a62fece23d4)
- [WebGL and DOTS - Unity Discussions](https://discussions.unity.com/t/webgl-and-dots/913496)
- [ECS Pool in 2025 - Unity Discussions](https://discussions.unity.com/t/ecs-pool-in-2025-worth-it-especially-for-componentobject-or-linkedentitygroup/1663788)
- [UI Optimization - Hundreds of floating damage text - Unity Discussions](https://discussions.unity.com/t/ui-optimization-hundreds-of-floating-damage-text/250517)
- [Offline Progression in Clicker Heroes](https://blog.clickerheroes.com/offline-progression-in-clicker-heroes/)
- [Top 7 Idle Game Mechanics - Mobile Free to Play](https://mobilefreetoplay.com/top-7-idle-game-mechanics/)

---
*Feature research for: Astrominer -- idle/clicker asteroid mining game*
*Researched: 2026-02-17*
