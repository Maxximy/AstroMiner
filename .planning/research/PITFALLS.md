# Pitfalls Research

**Domain:** Unity Hybrid ECS idle/clicker game (WebGL + Desktop)
**Researched:** 2026-02-17
**Confidence:** MEDIUM-HIGH (mix of official docs + verified community sources)

## Critical Pitfalls

### Pitfall 1: DOTS Without Burst on WebGL Destroys the Performance Rationale

**What goes wrong:**
The entire justification for using DOTS/ECS is performance — handling 100+ asteroids and 1000+ minerals at high framerate. But Burst compiler does not support WebGL. Jobs run single-threaded. The ECS performance advantage on WebGL is dramatically reduced, potentially 5-10x slower than desktop for the same DOTS code. You pay the full complexity cost of ECS (structural changes, EntityCommandBuffers, unmanaged component restrictions) without the primary payoff on one of your two target platforms.

**Why it happens:**
Developers adopt DOTS for desktop performance, then discover at build time that WebGL strips away Burst and multithreading. The Unity Entities package works on WebGL, but the compilation pipeline that makes it fast does not.

**How to avoid:**
- Profile on WebGL early — in Phase 1, not after Phase 3. Build a WebGL test with 100 entities + 1000 child entities moving each frame. Measure actual frame time.
- Design the ECS layer so it degrades gracefully: keep entity counts reasonable for single-threaded WASM, not just desktop capabilities.
- Consider NativeArray-based systems without full ECS if WebGL profiling shows ECS overhead is too high for the entity counts needed.
- Have a fallback plan: if WebGL DOTS performance is unacceptable, the hybrid architecture lets you move asteroid/mineral logic to MonoBehaviours without rewriting UI/audio.

**Warning signs:**
- WebGL build frame time is 2x+ desktop for the same scene
- You're spending time optimizing ECS code that only helps desktop
- WebGL profiler shows main thread stalls in EntityManager operations

**Phase to address:**
Phase 1 (Foundation) — validate DOTS-on-WebGL performance before building the entire game on it. This is the single most important early validation.

---

### Pitfall 2: Hybrid ECS Bridge Becomes a Synchronization Nightmare

**What goes wrong:**
Hybrid ECS means gameplay entities (asteroids, minerals, damage) live in DOTS world while UI, audio, and camera are GameObjects. The bridge between these two worlds — reading ECS data from MonoBehaviours, triggering GameObject effects from ECS systems — becomes a growing source of bugs. Transform synchronization is one-directional (ECS overwrites companion GameObject transforms), managed component access has no Burst/Jobs support, and there's no standard pattern for "ECS entity destroyed, now play a sound and show a particle."

**Why it happens:**
Unity's Hybrid Renderer handles rendering automatically, but everything else (audio, UI updates, particles, screen shake) requires manual bridging code. Each new feature adds another bridge point. Without a disciplined pattern, bridge code scatters across the codebase.

**How to avoid:**
- Establish a single, explicit bridge pattern in Phase 1: an EventBus or NativeQueue<EventType> that ECS systems write to and MonoBehaviour systems read from each frame.
- Never let MonoBehaviours directly query the EntityManager. All communication flows through the bridge.
- Keep the bridge surface area small: ECS systems produce events (AsteroidDestroyed, MineralCollected, DamageTaken), MonoBehaviour consumers react to events (spawn particles, play SFX, update HUD).
- Document every bridge point so they don't multiply invisibly.

**Warning signs:**
- MonoBehaviour scripts importing Unity.Entities namespaces directly
- Race conditions where effects play at wrong times or on wrong entities
- "Companion object" transform jitter or position mismatches
- Increasing number of GetComponent/EntityManager calls scattered across files

**Phase to address:**
Phase 1 (Foundation) — define the bridge architecture. Every subsequent phase depends on this being clean.

---

### Pitfall 3: WebGL Save Data Silently Lost on Game Updates

**What goes wrong:**
PlayerPrefs on WebGL uses IndexedDB, which is keyed to the build and domain. When you deploy a new build to the same URL, browsers can treat it as a "new" application instance, making previously saved PlayerPrefs and persistent data inaccessible. Players lose all progress. Additionally, IndexedDB writes are asynchronous — Unity's PlayerPrefs.Save() returns before data is actually persisted. If the browser tab closes during this window, data is lost. Safari in iframes doesn't support IndexedDB at all (affects embedding on itch.io).

**Why it happens:**
Desktop developers assume PlayerPrefs is reliable because it is on desktop (writes to registry/plist). WebGL's IndexedDB has fundamentally different persistence guarantees. Unity doesn't flush IndexedDB automatically — you need explicit FS.syncfs calls via .jslib plugins.

**How to avoid:**
- Implement explicit IndexedDB flush after every save using a .jslib plugin that calls FS.syncfs().
- Add a save version number to the JSON payload. On load, detect version mismatches and migrate data rather than discarding it.
- Test save persistence across builds: deploy build A, save data, deploy build B, verify data loads.
- For itch.io embedding, test Safari iframes specifically. Consider offering a manual "Export Save" button that downloads JSON as a file backup.
- Keep saved data small (under 100KB). PlayerPrefs has a 1MB limit, and IndexedDB quotas vary by browser.

**Warning signs:**
- Players reporting lost progress after updates
- Save/load works in editor but fails in WebGL builds
- No .jslib file in the project for IndexedDB synchronization
- Save data growing large (full tech tree state + level data + upgrade history)

**Phase to address:**
Phase 2 (Save System) — but the .jslib flush mechanism should be scaffolded in Phase 1. Cross-build persistence testing must happen before any public deployment.

---

### Pitfall 4: Idle Game Economy Collapses — Either Too Fast or Brick Wall

**What goes wrong:**
The credit economy has exponential growth (upgrades multiply damage, resource multipliers stack) against exponential costs (upgrade tier costs scale 1x/3x/8x+). Small tuning errors compound: a 0.01 difference in a cost multiplier can make the 20th upgrade trivially cheap or impossibly expensive. The tech tree's 5 branches and ~30 nodes mean the balance space is enormous. Without systematic modeling, players either max everything in 30 minutes or hit a wall where no amount of runs makes progress.

**Why it happens:**
Idle game math is deceptively complex. Developers tune by feel during testing, but their testing speed (minutes) doesn't match player speed (hours). The interaction between Resource Multiplier, Lucky Strike, Abundance, and base damage creates multiplicative stacking that's hard to reason about without spreadsheets. The GDD says "exact values to be balanced during playtesting" — but unguided playtesting without a mathematical model rarely converges.

**How to avoid:**
- Build a spreadsheet model of the economy before implementing it in Unity. Model: credits per run at each upgrade level, time-to-next-upgrade for each branch, total time to complete all upgrades.
- Use the Kongregate idle game math framework: polynomial production growth vs. exponential cost growth ensures costs eventually outpace income.
- Define target session pacing: "Player should unlock Level 2 after ~10 runs, Level 3 after ~25 runs." Work backward from targets to derive costs.
- Make all economy values data-driven (ScriptableObjects or JSON config), not hardcoded. You will need to iterate on balance 10+ times.
- Implement a debug panel that shows credits/minute, time-to-next-upgrade, and effective DPS to validate during playtesting.

**Warning signs:**
- No spreadsheet/model exists for the economy
- Upgrade costs are hardcoded in C#
- Testers report "nothing to do" or "can't progress" within first hour
- Resource Multiplier III + Lucky Strike III + Abundance II creates a 20x income spike
- Later levels feel identical to earlier ones despite harder asteroids

**Phase to address:**
Phase 3 (Economy/Tech Tree) — but the mathematical model should be created before writing any upgrade code. This is a design task, not a code task.

---

### Pitfall 5: WebGL Memory Ceiling Causes Silent Freezes

**What goes wrong:**
WebGL builds operate within a browser memory sandbox — typically 2GB effective limit (Unity 6 raised the hard cap to 4GB but most browsers constrain lower). Unity creates a virtual filesystem in memory, textures stay uncompressed in RAM, the GC only runs between frames, and WASM linear memory cannot be freed once allocated. With 100 asteroids each having PBR materials + normal maps, 1000+ mineral particles with trail renderers, damage number UI elements spawning constantly, and particle effects for every destruction — memory accumulates. When the ceiling is hit, the browser freezes or crashes silently with no error message.

**Why it happens:**
Desktop testing has 16+ GB RAM available so memory issues never surface. WebGL memory usage is invisible without explicit profiling. Unity's GC on WebGL only runs once per frame (cannot pause threads for collection), so garbage accumulates faster than it's collected. String manipulation (damage numbers, UI text updates) creates garbage every frame.

**How to avoid:**
- Set a memory budget: aim for under 512MB total for comfortable cross-browser compatibility.
- Use object pooling for everything that spawns/despawns frequently: damage number popups, mineral particles, asteroid entities, particle effects. Never use Instantiate/Destroy in a loop.
- Compress textures aggressively: enable Crunch Compression for all textures, use ASTC 8x8 for WebGL.
- Profile memory in browser DevTools (Chrome Performance tab) during a 5-minute play session.
- Avoid string concatenation for HUD updates — use StringBuilder or TextMeshPro's SetText with numeric overloads.
- Stream audio: set background music Load Type to "Streaming" so it doesn't fully load into memory.

**Warning signs:**
- Browser tab memory exceeding 1GB during play
- Frame hitches every few seconds (GC pauses)
- Game works for 2-3 runs then freezes
- No object pooling in the project
- Instantiate() calls in update loops or per-damage-tick

**Phase to address:**
Phase 1 (Foundation) for object pooling architecture. Phase 4 (Visual Polish) for texture/audio optimization. WebGL memory profiling should be continuous from Phase 1 onward.

---

### Pitfall 6: EntityCommandBuffer Structural Changes Tank Frame Rate

**What goes wrong:**
Every asteroid destruction requires: destroying the asteroid entity, detaching 10 mineral entities (removing parent component, adding DetachedMineral component), potentially spawning particle effect entities. Each of these is a structural change in ECS that invalidates chunk iteration. If multiple asteroids die in the same frame (common with AoE skills like EMP Pulse or Chain Lightning), you get a cascade of structural changes. Without proper EntityCommandBuffer batching, this creates sync points that stall the main thread.

**Why it happens:**
Developers write the obvious code: destroy entity immediately when HP reaches 0. In ECS, this is a structural change that forces a sync point. With 5+ asteroids dying simultaneously (50+ structural changes per frame), performance craters. The EntityCommandBuffer API is non-obvious — you must use the correct system group's ECB, call AddJobHandleForProducer, and use EntityArchetypes for batch creation.

**How to avoid:**
- Never perform structural changes directly in system updates. Always use EntityCommandBuffer from the appropriate ECBSystem.
- Batch all destruction into a single ECB playback per frame — don't use multiple barrier systems.
- Pre-define EntityArchetypes for common entity types (Asteroid, AttachedMineral, DetachedMineral, DamagePopup) to avoid per-component structural changes.
- For skill burst damage (EMP hitting 10 asteroids), consider a two-frame approach: mark entities for destruction on frame N, process destruction ECB on frame N+1.
- On WebGL (single-threaded), structural changes are even more expensive since there's no parallel chunk processing to amortize the cost.

**Warning signs:**
- Frame spikes when multiple asteroids die simultaneously
- `EntityManager.DestroyEntity` or `AddComponent` calls inside system foreach loops
- Multiple EntityCommandBufferSystem instances (should be one or two, not per-system)
- Profiler showing "Structural Changes" as a top contributor

**Phase to address:**
Phase 1 (Foundation) — ECS architecture decisions. The entity lifecycle (spawn, live, die, convert) must be designed correctly from the start because retrofitting ECB patterns is painful.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| MonoBehaviour for asteroid logic instead of ECS | Faster to prototype, familiar patterns | Lose DOTS performance for 100+ entities; harder to migrate later | Acceptable for Phase 1 prototype IF WebGL DOTS validation fails |
| Hardcoded economy values in C# | Ship faster, no config loading code | Every balance change requires recompile + redeploy; impossible to A/B test | Never — use ScriptableObjects or JSON from day 1 |
| Instantiate/Destroy for pooled objects | Simpler code, no pool management | GC pressure on WebGL causes frame hitches; memory fragmentation | Never for frequently spawned objects (minerals, damage popups, particles) |
| Single monolithic save JSON blob | Simple serialize/deserialize | Save corruption loses everything; no partial migration on schema changes | MVP only — add versioning and modular sections before public release |
| Using managed components in ECS | Can store strings, references, class types | No Burst, no Jobs, stored in big array with index lookup overhead, GC pressure | Only for rare "bridge" components, never for per-entity data |
| Skipping WebGL testing until late | Focus on features, test platform later | Discover showstopper performance/memory issues after months of development | Never — test WebGL weekly at minimum |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| ECS-to-uGUI bridge | Querying EntityManager every frame from MonoBehaviours to update HUD text | Use a singleton component or NativeQueue that ECS writes aggregated data to; MonoBehaviour reads once per frame |
| WebGL Audio | Playing AudioSource on game start; audio silently fails due to browser autoplay policy | Require user click/tap before initializing AudioContext; use a "Click to Start" loading screen |
| PlayerPrefs on WebGL | Calling PlayerPrefs.Save() and assuming data persisted | Implement .jslib plugin calling FS.syncfs() after save; add explicit save confirmation |
| CoPlay MCP + ECS | Using CoPlay to set up GameObjects that should be ECS entities | Use CoPlay for scene setup, prefab placement, material assignment — then bake to entities. Don't expect CoPlay to manipulate ECS entities directly |
| Particle System + ECS | Spawning ParticleSystem GameObjects from ECS destruction events with Instantiate | Pre-pool ParticleSystem instances in the scene; ECS bridge activates/positions them via event |
| uGUI Tech Tree | Building 30-node tree with individual Canvas elements per node | Use a single Canvas with a single GraphicRaycaster; nodes as RectTransform children; connections as UI.Extensions UILineRenderer or raw vertex drawing |
| URP Post-Processing on WebGL | Enabling desktop-quality bloom, ACES tonemapping, and screen-space effects | Profile post-processing cost on WebGL; use simplified bloom settings; disable effects that exceed 2ms on target browsers |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Damage number string allocation every tick | GC spikes every 1-3 seconds on WebGL; frame hitches during active mining | Pool TextMeshPro instances; use SetText(float) not string concatenation | >20 simultaneous damage numbers (common with AoE skills) |
| Unbatched mineral particle trail renderers | Draw call count exceeds 500; GPU-bound on WebGL | Use GPU instancing for minerals; single ParticleSystem with sub-emitters instead of per-mineral trail | >200 detached minerals moving simultaneously |
| Per-asteroid collision detection via Physics | Physics.OverlapSphere every damage tick on 100 asteroids | Use ECS spatial query or simple distance check (XZ only) — no physics engine needed for circle-overlap | >50 asteroids in mining circle radius |
| Tech tree Canvas rebuild on every purchase | 200ms+ UI stall when purchasing upgrades | Dirty-flag pattern: only rebuild changed nodes, not entire tree; cache vertex buffers | >15 visible nodes with connection lines |
| Uncompressed audio clips in memory | 50-100MB memory for SFX library | Compress all clips to Vorbis; stream background music; limit simultaneous SFX with AudioSource pool | When total audio assets exceed 20MB uncompressed |
| Frame-rate dependent damage ticks | Mining deals more/less damage at different frame rates | Use Time.deltaTime accumulator for damage tick timer; don't tie to Update() call count | Desktop (high FPS) vs WebGL (variable FPS) divergence |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Tech tree shows all 30 nodes immediately | Overwhelming; player doesn't know where to start | Progressive disclosure: show only unlocked + adjacent nodes; reveal branches as prerequisites are met |
| No feedback when mining circle is over nothing | Player moves mouse randomly, unsure if game is working | Visual indicator on mining circle (pulse when over asteroid, dim when over empty space) |
| Run timer anxiety without progress indication | Player watches timer, doesn't know if current run is "good" | Show credits-per-second or run quality indicator during gameplay |
| Damage numbers obscure asteroids | Can't see asteroid health state under floating text | Keep damage numbers brief (0.3s not 1.0s), offset to side, fade faster; show health bar on asteroid instead |
| No clear indication of upgrade effect | "Damage I" purchased but player can't tell what changed | Before/after comparison on purchase; visual DPS indicator in next run |
| WebGL loading screen takes 30+ seconds | Player leaves before game starts | Custom lightweight HTML loading bar (not Unity's default); show download progress in KB; target <15MB initial payload |

## "Looks Done But Isn't" Checklist

- [ ] **Object Pooling:** Objects appear to spawn/despawn correctly, but Instantiate/Destroy is still called under the hood -- verify no allocation in Profiler during gameplay
- [ ] **Save System:** Saves work in Editor Play mode, but WebGL IndexedDB flush is missing -- verify save persists after browser tab close and reopen
- [ ] **Save Migration:** Save loads current version, but loading an older save version crashes or silently resets progress -- verify version migration with a v1 save file
- [ ] **Audio in WebGL:** Audio works in Editor, but WebGL build has no sound because AudioContext wasn't resumed after user gesture -- verify audio plays in fresh WebGL tab
- [ ] **Tech Tree Connections:** Nodes display correctly, but prerequisite validation is missing -- verify you can't purchase a Tier III upgrade without Tier II
- [ ] **Economy Balance:** All upgrades are purchasable, but the progression curve hasn't been validated end-to-end -- verify a full playthrough from Level 1 to final level takes the target time
- [ ] **ECS Cleanup:** Entities spawn and die correctly during a run, but leftover entities persist across runs -- verify entity count returns to zero between runs
- [ ] **WebGL Build Size:** Game runs locally, but WebGL build exceeds 50MB and players bounce during loading -- verify compressed build size with Brotli
- [ ] **Damage Over Time:** DoT effect applies damage, but doesn't clean up when asteroid is destroyed -- verify no NullReferenceException from DoT ticking on dead entities
- [ ] **Skill Cooldowns:** Skills fire correctly, but cooldown timer doesn't display or updates incorrectly in UI -- verify cooldown visual matches actual cooldown duration

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| DOTS performance unacceptable on WebGL | HIGH | Migrate asteroid/mineral systems to MonoBehaviour + NativeArray. Keep ECS concepts (data-oriented, no MonoBehaviour per-entity state) but drop the Entities package. 1-2 week refactor. |
| Economy balance broken | MEDIUM | All values should be in ScriptableObjects/JSON. Rebuild spreadsheet model, recalculate all costs/multipliers, redeploy config. No code changes needed if data-driven. 2-3 days. |
| Save data corruption on WebGL | MEDIUM | Implement emergency save export/import via clipboard or file download. Add server-side backup if scope allows. Communicate with players. 1 week. |
| Memory ceiling hit on WebGL | MEDIUM-HIGH | Audit all textures (resize to 512x512 max), pool everything, reduce simultaneous particle count, compress audio. May require visual quality reduction. 1-2 weeks. |
| Tech tree UI performance | LOW | Switch from full Canvas rebuild to incremental updates. Cache connection line meshes. If uGUI is fundamentally too slow for 30 nodes, consider migrating to UI Toolkit (larger refactor). 3-5 days. |
| Hybrid bridge spaghetti | HIGH | Refactor to centralized EventBus pattern. Requires touching every system that crosses the ECS/GameObject boundary. Prevention is 10x cheaper than recovery. 2-3 weeks. |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| DOTS without Burst on WebGL | Phase 1 (Foundation) | WebGL build with 100 entities at 60fps; profiler screenshot committed to repo |
| Hybrid bridge architecture | Phase 1 (Foundation) | Bridge pattern documented; all MonoBehaviour-ECS communication uses bridge; no direct EntityManager access in MonoBehaviours |
| Object pooling | Phase 1 (Foundation) | Profiler shows zero allocations during steady-state gameplay |
| EntityCommandBuffer patterns | Phase 1 (Foundation) | Burst of 10 simultaneous asteroid deaths causes <2ms frame spike |
| WebGL audio autoplay | Phase 1 (Foundation) | Audio plays in fresh Chrome/Firefox tab after click-to-start |
| Save data persistence on WebGL | Phase 2 (Save System) | Save persists across: tab close/reopen, new build deployment, browser restart |
| Save versioning/migration | Phase 2 (Save System) | Load a v1 save in a v2 build without data loss |
| Economy mathematical model | Phase 3 (Economy/Tech Tree) | Spreadsheet model matches actual in-game progression within 20% |
| Tech tree UI performance | Phase 3 (Economy/Tech Tree) | 30-node tree renders and responds to input within 16ms frame budget on WebGL |
| WebGL memory budget | Phase 4 (Visual Polish) | 5-minute play session stays under 512MB in Chrome DevTools |
| WebGL build size | Phase 4 (Visual Polish) | Brotli-compressed build under 30MB; loading time under 15 seconds on 10Mbps connection |
| Damage number / particle spam | Phase 4 (Visual Polish) | 20+ simultaneous damage numbers + particle effects maintain 30fps on WebGL |

## Sources

- [Unity DOTS Best Practices](https://learn.unity.com/course/dots-best-practices) — official Unity learning course
- [Unity ECS Stack Reviews (2025)](https://discussions.unity.com/t/november-2025-ecs-stack-review/1694077) — community discussion on ECS state
- [Unity WebGL Technical Limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html) — official documentation
- [Unity WebGL Audio Documentation](https://docs.unity3d.com/Manual/webgl-audio.html) — official audio limitations
- [Unity WebGL Memory](https://docs.unity3d.com/Manual/webgl-memory.html) — official memory documentation
- [Burst for WebGL Discussion](https://discussions.unity.com/t/burst-for-webgl/849368) — community confirmation of no Burst on WebGL
- [Unity ECS + Job System Gotchas (5argon)](https://medium.com/@5argon/all-of-the-unitys-ecs-job-system-gotchas-so-far-6ca80d82d19f) — verified community reference, MEDIUM confidence
- [EntityCommandBuffer Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) — official Entities docs
- [Managed Components Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/components-managed.html) — official managed component limitations
- [Kongregate: Math of Idle Games](https://blog.kongregate.com/the-math-of-idle-games-part-i/) — authoritative idle game design reference
- [Idle Game Design Principles (Eric Guan)](https://ericguan.substack.com/p/idle-game-design-principles) — community design reference, MEDIUM confidence
- [WebGL PlayerPrefs/IndexedDB Issues](https://itch.io/t/140214/persistent-data-in-updatable-webgl-games) — community-verified persistence issues
- [Unity Particle System Optimization](https://docs.unity3d.com/6000.0/Documentation/Manual/particle-system-optimization.html) — official optimization guide
- [Unity WebGL Memory and Performance (Kongregate)](https://medium.com/@kongregate/unity-webgl-memory-and-performance-optimization-3939780a7e97) — verified community reference, MEDIUM confidence
- [Idle Game Economy Balance (Gamedeveloper.com)](https://www.gamedeveloper.com/design/balancing-tips-how-we-managed-math-on-idle-idol) — industry post-mortem, MEDIUM confidence

---
*Pitfalls research for: Unity Hybrid ECS idle/clicker game (WebGL + Desktop)*
*Researched: 2026-02-17*
