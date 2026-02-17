# Stack Research

**Domain:** Unity Hybrid ECS idle/clicker game (2D space arcade)
**Researched:** 2026-02-17
**Confidence:** MEDIUM-HIGH

## Executive Summary

AstroMiner targets Unity 6.3 LTS (6000.3.8f1) with URP and a Hybrid ECS architecture. The DOTS packages (Entities 1.4.x, Physics 1.4.x, Collections 2.6.x, Burst 1.8.x) are released and stable for this editor version. However, **Entities Graphics does NOT support WebGL**, which is a hard constraint that shapes the entire rendering strategy. The project must use GameObjects for all visual representation and limit ECS to pure data/logic (asteroids, minerals, damage, economy). This is a true "Hybrid ECS" approach where DOTS handles simulation and GameObjects handle rendering, UI, audio, and camera.

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| Unity Editor | 6000.3.8f1 (6.3 LTS) | Game engine | Already in use. LTS supported until Dec 2027. Stable DOTS packages available. | HIGH |
| URP | 17.3.0 | Render pipeline | Already in project. Standard for 2D/lightweight 3D. WebGL compatible. Forward rendering required (Entities Graphics deferred not supported anyway). | HIGH |
| com.unity.entities | 1.4.4 | ECS framework | Released for 6000.3. Provides SystemAPI.Query, IJobEntity, Archetypes. Entities.ForEach obsoleted in 1.4 -- use IJobEntity instead. | HIGH |
| com.unity.physics | 1.4.3 | ECS collision detection | Released for 6000.3. Provides collision queries (OverlapAabb, raycasts) for mining circle vs asteroids. Use for spatial queries only, not full physics sim. | MEDIUM-HIGH |
| com.unity.collections | 2.6.4 | Native containers | Released for 6000.3. NativeArray, NativeHashMap, etc. Required by Entities. | HIGH |
| com.unity.burst | 1.8.27 | Compiler optimization | Released for 6000.3. Compiles IJobEntity/ISystem to SIMD-optimized native code. **Does NOT support WebGL** -- see Platform Notes. | HIGH |
| com.unity.mathematics | 1.3.3 | Math library | SIMD-friendly math types (float3, quaternion). Used by all DOTS code. Shader-like syntax. | HIGH |
| com.unity.inputsystem | 1.18.0 | Input handling | Already in project. Standard for mouse input. Handles both desktop and web input paths. | HIGH |

### Rendering Strategy (Critical Decision)

| Technology | Purpose | Why Recommended | Confidence |
|------------|---------|-----------------|------------|
| GameObjects + SpriteRenderer/MeshRenderer | Visual representation of ALL entities | **Entities Graphics (com.unity.entities.graphics) does NOT support WebGL.** Since AstroMiner targets WebGL, all rendering MUST go through GameObjects. ECS systems write transform/state data; MonoBehaviour sync scripts read it to position GameObjects. | HIGH |
| URP Particle System | Mineral particles, explosions, effects | Built-in, WebGL compatible, URP integrated. Sufficient for the particle counts in an idle/clicker (hundreds, not millions). | HIGH |
| URP Shader Graph | Custom asteroid/space shaders | Visual shader authoring, URP compatible, WebGL compatible. Use for asteroid damage visualization, space backgrounds. | MEDIUM-HIGH |

### UI Stack

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| com.unity.ugui | 2.0.0 | All UI (tech tree, HUD, menus) | Already in project. Canvas-based. Mature, well-documented. WebGL compatible. Project constraint specifies uGUI. | HIGH |
| DOTween or PrimeTween | 1.2.x / 1.3.7 | UI animation & tweening | Number popups, progress bars, button feedback. PrimeTween is zero-allocation and newer; DOTween has larger community. Either works. | MEDIUM |

### Data & Persistence

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| com.unity.nuget.newtonsoft-json | 3.2.2 | JSON serialization | Polymorphism support, custom converters for tech tree/upgrade data. More capable than JsonUtility for complex save structures. Unity's official Newtonsoft wrapper avoids assembly conflicts. | HIGH |
| PlayerPrefs | Built-in | Settings storage | Simple key-value for audio volume, quality settings, last-played timestamp. NOT for game saves (too limited, uses registry on Windows). | HIGH |
| JSON files (Application.persistentDataPath) | N/A | Game save data | Serialize full game state (credits, unlocks, asteroid progress, tech tree). Human-readable for debugging. Works on both Windows and WebGL (IndexedDB). | HIGH |

### Supporting Libraries

| Library | Version | Purpose | When to Use | Confidence |
|---------|---------|---------|-------------|------------|
| com.unity.nuget.newtonsoft-json | 3.2.2 | Advanced JSON serialization | Save/load system, tech tree data, level definitions | HIGH |
| TextMeshPro | Built-in with Unity 6 | Rich text rendering | Damage numbers, UI text, tech tree labels | HIGH |
| Unity Addressables | (evaluate) | Asset loading | Only if asset bundle sizes become a concern for WebGL. Skip initially. | LOW |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| CoPlay Unity MCP | Editor automation | Already in project. Git package from beta branch. |
| Unity Profiler | Performance analysis | Built-in. Use Entity Debugger window for ECS inspection. |
| Burst Inspector | Verify Burst compilation | Built-in with Burst package. Validates IJobEntity compilation. |
| Frame Debugger | Render call debugging | Built-in. Verify draw call batching for asteroid rendering. |

## Platform-Specific Notes

### WebGL Constraints (CRITICAL)

This section addresses the most important architectural constraint for the project.

| Constraint | Impact | Mitigation |
|------------|--------|------------|
| **Entities Graphics unsupported on WebGL** | Cannot use ECS rendering pipeline for WebGL builds | Use GameObject rendering exclusively. ECS is data/logic only. Sync ECS state to GameObjects each frame. |
| **Burst compiler unsupported on WebGL** | ECS jobs run ~5-10x slower on WebGL than desktop | Keep entity counts reasonable (<500 active). Profile WebGL builds early and often. Managed (non-Burst) fallback code runs on WebGL automatically. |
| **Entity Scene streaming broken on WebGL** | SubScenes with entity data fail to load from StreamingAssets on WebGL | Do NOT use SubScenes for entity data. Create entities procedurally at runtime via EntityManager.CreateEntity or baker-free approaches. |
| **Single-threaded JavaScript** | Job system parallelism unavailable on WebGL | IJobEntity still runs, but sequentially. Keep per-frame work budget small. |
| **Memory limits** | WebGL has tighter memory constraints | Reuse NativeArrays. Pool GameObjects. Limit texture sizes. |

### Windows Desktop

No constraints. Full Burst compilation, multi-threaded jobs, Entities Graphics would work (but don't use it -- maintain one rendering path for both platforms).

## Version Compatibility Matrix

| Package | Version | Required By | Unity Editor | Notes |
|---------|---------|-------------|--------------|-------|
| com.unity.entities | 1.4.4 | Project core | 6000.3.x | Released (verified) for 6000.3 |
| com.unity.physics | 1.4.3 | Collision queries | 6000.3.x | Depends on Entities 1.4.x |
| com.unity.collections | 2.6.4 | Entities, Physics | 6000.3.x | NativeContainers for DOTS |
| com.unity.burst | 1.8.27 | Entities, Physics | 6000.3.x | Desktop only for perf; WebGL runs managed |
| com.unity.mathematics | 1.3.3 | Entities, Physics, Burst | 6000.3.x | SIMD math types |
| com.unity.render-pipelines.universal | 17.3.0 | Rendering | 6000.3.x | Already in project |
| com.unity.inputsystem | 1.18.0 | Input | 6000.3.x | Already in project |
| com.unity.ugui | 2.0.0 | UI | 6000.3.x | Already in project |
| com.unity.nuget.newtonsoft-json | 3.2.2 | Save system | 6000.3.x | Add to manifest |

## Installation

Adding DOTS packages to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.entities": "1.4.4",
    "com.unity.physics": "1.4.3",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  }
}
```

**Note:** `com.unity.collections`, `com.unity.burst`, and `com.unity.mathematics` are pulled in automatically as transitive dependencies of `com.unity.entities`. You do not need to add them explicitly, though you may pin versions if desired.

**Do NOT add:**
- `com.unity.entities.graphics` -- WebGL incompatible
- `com.unity.netcode` -- Single-player game, not needed
- `com.unity.2d.entities` -- Preview-only, abandoned

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| com.unity.entities 1.4.4 | Pure GameObject MonoBehaviour architecture | ECS gives better data layout for hundreds of asteroids/minerals. Burst compilation on desktop. Clean separation of data and logic. Project explicitly targets Hybrid ECS. |
| com.unity.physics (collision queries only) | Built-in Physics2D (Box2D) | Unity Physics integrates with ECS natively. Can query entity collision data without syncing to GameObjects for physics. However, if simpler overlap checks suffice, Physics2D on GameObjects is also viable. |
| com.unity.physics 1.4.3 | Built-in Physics2D | Unity 6.3 added Box2D v3 integration for 2D physics with multi-threaded perf. For a 2D game this is a genuine alternative. Decision: start with Unity Physics for ECS integration; fall back to Physics2D if complexity not warranted. |
| uGUI (Canvas) | UI Toolkit (UI Elements) | Project constraint specifies uGUI. UI Toolkit still maturing for runtime game UI. uGUI has better WebGL support and more community resources for game HUDs. |
| Newtonsoft.Json | JsonUtility | JsonUtility cannot serialize dictionaries, polymorphic types, or nullable fields. Tech tree and upgrade data structures need these features. |
| PrimeTween 1.3.7 | DOTween 1.2.x | PrimeTween is zero-allocation and faster in benchmarks. DOTween has more tutorials and a paid Pro editor. Either works for UI tweening. PrimeTween recommended for new projects. |
| PrimeTween | LeanTween | LeanTween is unmaintained and has O(n^2) sequence creation. Not recommended for new projects. |
| PlayerPrefs + JSON files | SQLite / ScriptableObjects | Overkill for an idle game. JSON is human-readable, debuggable, and works on both Windows and WebGL (IndexedDB). ScriptableObjects are design-time only, not suitable for runtime saves. |
| GameObject rendering (SpriteRenderer) | Entities Graphics | Entities Graphics does not support WebGL. Not an option. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| com.unity.entities.graphics | **Does not support WebGL platform.** Using it would create two completely different rendering paths or abandon WebGL. | GameObject SpriteRenderer/MeshRenderer synced from ECS data |
| Entities.ForEach | Obsoleted in Entities 1.4. Will generate compiler warnings/errors. | IJobEntity for jobs, SystemAPI.Query for main-thread iteration |
| Job.WithCode | Obsoleted in Entities 1.4. | IJob for simple single-threaded jobs |
| SubScenes for entity data | Entity scene streaming is broken on WebGL (cannot load from StreamingAssets). | Procedural entity creation at runtime via EntityManager or custom bakers that run on scene load |
| com.unity.rendering.hybrid | Legacy package, superseded by Entities Graphics (which itself is WebGL-incompatible). | GameObject rendering |
| LeanTween | Unmaintained, O(n^2) sequence scaling. | PrimeTween or DOTween |
| com.unity.visualscripting | Adds unnecessary overhead. Code-first project. | Remove from manifest (currently included as default). |
| com.unity.ai.navigation | NavMesh not needed for space game with drifting asteroids. | Remove from manifest (currently included). |
| com.unity.timeline | Cutscene/timeline tool. Not needed for idle/clicker gameplay. | Remove from manifest unless used for intro sequences. |
| com.unity.multiplayer.center | Single-player game. | Remove from manifest. |

## Stack Patterns by Variant

**For Desktop-only builds (no WebGL):**
- Could use Entities Graphics for GPU-instanced rendering of thousands of asteroids
- Full Burst compilation for all jobs
- Multi-threaded job scheduling
- But: maintaining a single code path for both platforms is strongly recommended

**For WebGL-first development:**
- Test in WebGL early and often (weekly at minimum)
- Keep active entity count under 500
- Profile managed (non-Burst) job performance as baseline
- Use object pooling aggressively for GameObjects
- Limit texture atlas sizes to reduce WASM memory

**For the Hybrid ECS pattern specifically:**
- ECS World owns all gameplay state (asteroid HP, position, velocity, mineral type)
- MonoBehaviours are "presentation" layer -- read-only consumers of ECS data
- One-way data flow: ECS systems write state -> sync system copies to GameObjects -> Unity renders GameObjects
- UI reads from a "GameState" singleton component or managed class bridge
- Input is captured by MonoBehaviour (InputSystem) and written to ECS via singleton components

## Future Considerations

**Unity 6.4+ (expected 2026):** ECS becomes a core engine package. Unified transforms will allow ECS components to attach directly to GameObjects. This could simplify the hybrid sync layer significantly. Monitor the December 2025 ECS roadmap updates.

**WebGPU:** When browser support matures, Entities Graphics may gain WebGL/WebGPU support. This would allow switching to ECS rendering. Do not architect around this possibility -- it is speculative.

**Entities 1.4 deprecations:** `Entities.ForEach` and `Job.WithCode` are obsolete. All new code must use `IJobEntity` and `SystemAPI.Query`. The project should adopt these patterns from day one to avoid migration debt.

## Sources

- [Unity Entities 1.4.4 Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html) -- Package overview and API (HIGH confidence)
- [Unity Physics 1.3.14+ Changelog](https://docs.unity3d.com/Packages/com.unity.physics@1.3/changelog/CHANGELOG.html) -- Version history and features (HIGH confidence)
- [Entities Graphics 6.5.0 Overview](https://docs.unity3d.com/Packages/com.unity.entities.graphics@6.5/manual/overview.html) -- Confirms URP support, no WebGL (HIGH confidence)
- [WebGL Platform Support for Entities Graphics](https://discussions.unity.com/t/webgl-platform-support-for-entities-graphics/918881) -- Community confirmation of no WebGL support (MEDIUM confidence)
- [Plans Regarding Graphics for ECS on WebGL](https://discussions.unity.com/t/plans-regarding-graphics-for-ecs-on-the-webgl-web-platform/927308) -- Unity staff responses on WebGL graphics limitation (MEDIUM confidence)
- [WebGL Build Fails with Entities](https://discussions.unity.com/t/unity-webgl-build-cant-load-entity-scene-files-from-streamingassets-works-in-editor-standalone/1673854) -- Entity scene streaming broken on WebGL (MEDIUM confidence)
- [Burst for WebGL Discussion](https://discussions.unity.com/t/burst-for-webgl/849368) -- Confirms no Burst WebGL support (MEDIUM confidence)
- [ECS Development Status December 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) -- Latest DOTS roadmap (MEDIUM confidence)
- [Unity 6.3 LTS Release](https://unity.com/blog/unity-6-3-lts-is-now-available) -- Editor version confirmation (HIGH confidence)
- [Unity 2026 Roadmap: CoreCLR, ECS as core package](https://digitalproduction.com/2025/11/26/unitys-2026-roadmap-coreclr-verified-packages-fewer-surprises/) -- ECS becomes core in 6.4 (MEDIUM confidence)
- [What's New in Entities 1.4](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/whats-new.html) -- Entities.ForEach/Job.WithCode obsoleted (HIGH confidence)
- [Newtonsoft JSON Unity Package](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) -- Package docs (HIGH confidence)
- [PrimeTween GitHub](https://github.com/KyryloKuzyk/PrimeTween) -- Zero-allocation tweening library (MEDIUM confidence)
- [DOTween vs LeanTween vs PrimeTween Comparison](https://omitram.com/unity-tweening-guide-dotween-leantween-primetween/) -- Tweening library benchmarks (LOW confidence -- third-party blog)
- [Unity Collections 2.6.4](https://docs.unity3d.com/Packages/com.unity.collections@6.4/changelog/CHANGELOG.html) -- Collections changelog (HIGH confidence)
- [Unity Mathematics 1.3.3](https://docs.unity3d.com/Packages/com.unity.mathematics@1.3/manual/index.html) -- Math library docs (HIGH confidence)

---
*Stack research for: Unity Hybrid ECS idle/clicker game (AstroMiner)*
*Researched: 2026-02-17*
