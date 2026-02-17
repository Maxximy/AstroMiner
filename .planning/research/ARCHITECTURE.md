# Architecture Research

**Domain:** Unity Hybrid ECS (DOTS) idle/clicker game
**Researched:** 2026-02-17
**Confidence:** MEDIUM — patterns verified across multiple Unity community sources and official docs, but Hybrid ECS for idle/clicker is not a well-documented niche; some recommendations are synthesized from general DOTS best practices applied to this genre.

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                     GAMEOBJECT LAYER (MonoBehaviour)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │  uGUI    │  │  Audio   │  │  Camera  │  │  Game Manager    │   │
│  │  Canvas  │  │  System  │  │  Rig     │  │  (State Machine) │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
│       │              │             │                  │             │
├───────┴──────────────┴─────────────┴──────────────────┴─────────────┤
│                      BRIDGE LAYER (read ECS ← → write ECS)         │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  MonoBehaviour "Readers" query EntityManager on main thread  │   │
│  │  MonoBehaviour "Writers" set singleton components for input  │   │
│  │  SystemBase "Presenters" push events to managed queues       │   │
│  └──────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│                        ECS LAYER (DOTS)                             │
│  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────────────┐   │
│  │ Asteroid  │ │  Mining   │ │  Mineral  │ │  Damage / Skills  │   │
│  │ Systems   │ │  Systems  │ │  Systems  │ │  Systems          │   │
│  └─────┬─────┘ └─────┬─────┘ └─────┬─────┘ └─────────┬─────────┘   │
│        │              │             │                  │             │
│  ┌─────┴──────────────┴─────────────┴──────────────────┴─────────┐   │
│  │              ECS Components (IComponentData structs)           │   │
│  │  Asteroid, Health, OrbitalMotion, MiningCircle, Mineral,      │   │
│  │  DetachedMineral, DamageEvent, SkillCooldown, GameState...    │   │
│  └───────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│                     RENDERING (URP + Entities Graphics)             │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  Entities Graphics renders ECS entities via SRP Batcher       │   │
│  │  Particles + VFX stay as GameObjects (VFX Graph or legacy)    │   │
│  └───────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **GameManager** (MB) | Game state machine (Playing/Collecting/GameOver/Upgrading), round lifecycle, scene transitions | MonoBehaviour singleton; reads/writes ECS singleton for game state |
| **Asteroid Systems** (ECS) | Spawning, orbital/drift movement, destruction, health tracking | ISystem + IJobEntity for movement; ECB for spawn/destroy |
| **Mining Systems** (ECS) | Cursor-to-world projection, damage radius calculation, tick-based damage application | ISystem reads mouse singleton, queries asteroids in radius |
| **Mineral Systems** (ECS) | Mineral detachment from destroyed asteroids, pull-toward-ship physics, collection detection | ISystem + IJobEntity for movement; ECB for detach/collect |
| **Damage/Skills Systems** (ECS) | Damage events, DoT tracking, skill activation (Laser, Chain Lightning, EMP, Overcharge), cooldowns | ISystem processes DamageEvent components; skill systems read input singletons |
| **UI Layer** (MB) | HUD (score, timer), tech tree screen, damage popups, game over overlay | MonoBehaviour + uGUI Canvas; reads ECS data via EntityManager on main thread |
| **Audio Layer** (MB) | SFX playback (mining hits, destruction, collection), ambient music, AudioMixer routing | MonoBehaviour AudioManager; triggered by events from Bridge layer |
| **Camera Rig** (MB) | Fixed perspective camera, screen shake, post-processing volume | MonoBehaviour; reads shake events from ECS |
| **Save/Load** (MB) | Persist progression (credits, tech tree, level) to PlayerPrefs + JSON | MonoBehaviour; reads ECS singleton data, serializes to JSON |
| **Tech Tree** (MB) | Node graph of ~30 upgrades, prerequisites, purchase logic, stat application | MonoBehaviour + ScriptableObjects; writes upgraded stats back to ECS singletons |
| **Config/Data** (SO) | Level definitions, drop tables, asteroid type configs, upgrade definitions | ScriptableObjects loaded at runtime; injected into ECS as blob assets or read by bakers |

*MB = MonoBehaviour, ECS = Entity Component System, SO = ScriptableObject*

## Recommended Project Structure

```
Assets/
├── Scripts/
│   ├── ECS/
│   │   ├── Components/          # IComponentData structs
│   │   │   ├── AsteroidComponents.cs
│   │   │   ├── MiningComponents.cs
│   │   │   ├── MineralComponents.cs
│   │   │   ├── DamageComponents.cs
│   │   │   ├── SkillComponents.cs
│   │   │   └── GameStateComponents.cs
│   │   ├── Systems/             # ISystem and SystemBase implementations
│   │   │   ├── AsteroidSpawnSystem.cs
│   │   │   ├── AsteroidMovementSystem.cs
│   │   │   ├── MiningDamageSystem.cs
│   │   │   ├── MineralDetachSystem.cs
│   │   │   ├── MineralPullSystem.cs
│   │   │   ├── MineralCollectionSystem.cs
│   │   │   ├── DamageEventSystem.cs
│   │   │   ├── SkillSystems.cs
│   │   │   └── GameStateSystem.cs
│   │   ├── Authoring/           # Baker/Authoring MonoBehaviours
│   │   │   ├── AsteroidAuthoring.cs
│   │   │   └── MineralAuthoring.cs
│   │   └── Aspects/             # IAspect wrappers (optional)
│   │       └── AsteroidAspect.cs
│   ├── MonoBehaviours/
│   │   ├── Core/                # GameManager, bootstrapping
│   │   │   ├── GameManager.cs
│   │   │   └── Bootstrap.cs
│   │   ├── Bridge/              # ECS ↔ GameObject communication
│   │   │   ├── InputBridge.cs
│   │   │   ├── UIDataBridge.cs
│   │   │   └── AudioEventBridge.cs
│   │   ├── UI/                  # uGUI controllers
│   │   │   ├── HUDController.cs
│   │   │   ├── DamagePopupManager.cs
│   │   │   ├── TechTreeController.cs
│   │   │   ├── GameOverScreen.cs
│   │   │   └── SkillBarController.cs
│   │   ├── Audio/               # Audio management
│   │   │   ├── AudioManager.cs
│   │   │   └── SFXLibrary.cs
│   │   ├── Camera/              # Camera control
│   │   │   └── CameraController.cs
│   │   └── Save/                # Persistence
│   │       ├── SaveManager.cs
│   │       └── SaveData.cs
│   ├── Data/                    # ScriptableObjects definitions
│   │   ├── AsteroidTypeSO.cs
│   │   ├── LevelConfigSO.cs
│   │   ├── UpgradeNodeSO.cs
│   │   └── SkillDefinitionSO.cs
│   └── Shared/                  # Constants, enums, utilities
│       ├── GameConstants.cs
│       ├── GameEnums.cs
│       └── MathUtilities.cs
├── ScriptableObjects/           # Asset instances of SOs
│   ├── Levels/
│   ├── AsteroidTypes/
│   ├── Upgrades/
│   └── Skills/
├── Prefabs/
│   ├── ECS/                     # Entity prefabs (baked)
│   │   ├── AsteroidPrefab.prefab
│   │   └── MineralPrefab.prefab
│   └── UI/
│       ├── DamagePopup.prefab
│       └── SkillButton.prefab
├── Materials/
├── Models/
├── Audio/
│   ├── SFX/
│   └── Music/
├── VFX/                         # Particle systems, VFX Graph
├── Scenes/
│   └── GameScene.unity
└── Settings/                    # URP asset, post-processing
    ├── URPSettings.asset
    └── PostProcessVolume.asset
```

### Structure Rationale

- **Scripts/ECS/ vs Scripts/MonoBehaviours/:** Hard separation enforces the architectural boundary. ECS code never imports UnityEngine.UI; MonoBehaviour code accesses ECS only through the Bridge layer. This separation could be enforced with Assembly Definitions if desired.
- **Scripts/Bridge/:** The most critical folder. Contains all code that crosses the ECS-GameObject boundary. When debugging data flow issues, this is the first place to look.
- **Scripts/Data/:** ScriptableObject class definitions separate from instances. SO instances live in ScriptableObjects/ folder as assets.
- **Scripts/Shared/:** Constants and enums used by both ECS and MonoBehaviour layers. Must contain no dependencies on either layer.

## Architectural Patterns

### Pattern 1: Singleton Component for Cross-Layer State

**What:** Store shared game state (score, timer, game phase, mouse position) as ECS singleton components. Both ECS systems and MonoBehaviours read/write these via EntityManager on the main thread.

**When to use:** Any state that both layers need access to: game phase, current score, mouse world position, player stats.

**Trade-offs:** Simple and performant for single values. Requires main-thread access from MonoBehaviours (not a problem since UI updates are main-thread anyway). Can become unwieldy if too many singletons proliferate.

**Example:**
```csharp
// Shared/GameEnums.cs
public enum GamePhase { Playing, Collecting, GameOver, Upgrading }

// ECS/Components/GameStateComponents.cs
public struct GameStateData : IComponentData
{
    public GamePhase Phase;
    public float Timer;
    public int Score;
    public float2 MouseWorldPos;
}

// ECS/Components/PlayerStatsData.cs
public struct PlayerStats : IComponentData
{
    public float MiningRadius;
    public float DamagePerTick;
    public float DamageInterval;
    public float CritChance;
    public float CritMultiplier;
}

// MonoBehaviours/Bridge/InputBridge.cs
public class InputBridge : MonoBehaviour
{
    private EntityManager _em;
    private Entity _stateEntity;

    void Update()
    {
        // Write mouse position into ECS singleton
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
        {
            var worldPos = ray.GetPoint(dist);
            var state = _em.GetComponentData<GameStateData>(_stateEntity);
            state.MouseWorldPos = new float2(worldPos.x, worldPos.z);
            _em.SetComponentData(_stateEntity, state);
        }
    }
}

// MonoBehaviours/UI/HUDController.cs
public class HUDController : MonoBehaviour
{
    void LateUpdate()
    {
        // Read score and timer from ECS singleton
        var state = _em.GetComponentData<GameStateData>(_stateEntity);
        _scoreText.text = $"Score: {state.Score}";
        _timerText.text = $"Time: {Mathf.CeilToInt(state.Timer)}";
    }
}
```

**Confidence:** HIGH — Singleton components are officially documented and widely used for this purpose.

### Pattern 2: Event Buffer for ECS-to-GameObject Communication

**What:** ECS systems write event data (damage dealt, asteroid destroyed, mineral collected) into a managed buffer. MonoBehaviours drain the buffer each frame to trigger UI popups, audio, and VFX.

**When to use:** When ECS produces many discrete events per frame that the GameObject layer must react to (damage numbers, explosion effects, collection sounds).

**Trade-offs:** Adds a managed allocation path (the buffer), but these are UI/audio events that are already on the managed side. The alternative (companion components on every entity) is worse for performance when you have 1000+ minerals.

**Example:**
```csharp
// MonoBehaviours/Bridge/AudioEventBridge.cs
// A managed singleton that ECS systems can reference
public struct AudioEventBuffer : IComponentData, IEnableableComponent
{
    // This is a tag; actual data in DynamicBuffer
}

public struct AudioEvent : IBufferElementData
{
    public int EventType;     // enum as int for unmanaged
    public float3 Position;
    public float Intensity;
}

// ECS system writes events:
// In MineralCollectionSystem when mineral reaches ship:
//   ecb.AppendToBuffer(singletonEntity, new AudioEvent { ... });

// MonoBehaviours/Bridge/AudioEventBridge.cs (MonoBehaviour side)
public class AudioEventBridge : MonoBehaviour
{
    void LateUpdate()
    {
        var buffer = _em.GetBuffer<AudioEvent>(_bufferEntity);
        for (int i = 0; i < buffer.Length; i++)
        {
            var evt = buffer[i];
            AudioManager.Instance.PlayEvent(evt);
        }
        buffer.Clear();
    }
}
```

**Confidence:** MEDIUM — This is a synthesized pattern from official DynamicBuffer docs and community hybrid architecture discussions. The approach is sound but not a copy-paste from an official sample.

### Pattern 3: ISystem for Hot Paths, SystemBase for Bridge Logic

**What:** Use unmanaged `ISystem` (Burst-compatible) for all performance-critical gameplay systems (asteroid movement, damage calculation, mineral physics). Use managed `SystemBase` for systems that must interact with managed objects (pushing events to MonoBehaviour queues, reading ScriptableObject config).

**When to use:** Always. This is the core architectural split for Hybrid ECS.

**Trade-offs:** ISystem gives Burst compilation and better performance but cannot reference managed types. SystemBase is slower but can hold references to GameObjects, ScriptableObjects, and managed collections. The performance difference matters for movement/physics of 1000+ entities; it does not matter for once-per-frame bridge logic.

**Confidence:** HIGH — Official Unity documentation explicitly recommends ISystem over SystemBase for performance, with SystemBase retained for managed data needs.

### Pattern 4: ScriptableObject Config Injected via Baker or SystemBase

**What:** Define game data (asteroid types, level configs, upgrade trees) as ScriptableObjects. At bake time, Bakers convert relevant SO data into ECS components or BlobAssets. For runtime-changing config (upgraded stats from tech tree), a SystemBase reads updated values from a managed reference.

**When to use:** All static game configuration. ScriptableObjects are the standard Unity data authoring pattern and work well with Hybrid ECS.

**Trade-offs:** BlobAssets give best ECS-side performance but are read-only after creation. For data that changes at runtime (player stats after upgrades), use singleton components written by MonoBehaviours instead.

**Confidence:** HIGH — Baker + ScriptableObject is the documented Entities workflow.

## Data Flow

### Core Gameplay Loop (Per Frame)

```
[Mouse Input]
    │
    ▼
[InputBridge.cs (MB)] ──writes──► [GameStateData singleton (ECS)]
    │                                       │
    │                               ┌───────┴────────────────┐
    │                               ▼                        ▼
    │                    [AsteroidMovementSystem]   [MiningDamageSystem]
    │                    (ISystem + IJobEntity)     (ISystem, reads mouse pos)
    │                               │                        │
    │                               │              ┌─────────┴──────────┐
    │                               │              ▼                    ▼
    │                               │    [DamageEvent buffer]   [Health -= dmg]
    │                               │              │                    │
    │                               │              │           (if HP <= 0)
    │                               │              │                    ▼
    │                               │              │       [MineralDetachSystem]
    │                               │              │       (ECB: destroy asteroid,
    │                               │              │        spawn detached minerals)
    │                               │              │                    │
    │                               │              │                    ▼
    │                               │              │       [MineralPullSystem]
    │                               │              │       (IJobEntity: accelerate
    │                               │              │        toward ship)
    │                               │              │                    │
    │                               │              │                    ▼
    │                               │              │       [MineralCollectionSystem]
    │                               │              │       (distance check → score++)
    │                               │              │                    │
    │                               ▼              ▼                    ▼
    │                    ┌──────────────────────────────────────────────────┐
    │                    │          Event Buffers (DynamicBuffer)           │
    │                    │  DamageEvents, DestructionEvents, CollectEvents  │
    │                    └──────────────────────┬───────────────────────────┘
    │                                           │
    ▼                                           ▼
[UIDataBridge.cs (MB)]              [AudioEventBridge.cs (MB)]
    │                                           │
    ▼                                           ▼
[HUDController]                     [AudioManager]
[DamagePopupManager]                (plays SFX at positions)
(reads score/timer,
 spawns floating text)
```

### Tech Tree / Upgrade Flow (Between Runs)

```
[GameManager sets Phase = Upgrading]
    │
    ▼
[TechTreeController.cs (MB)]
    │  displays node graph from UpgradeNodeSOs
    │  player clicks node → checks prerequisites, deducts credits
    │
    ▼
[TechTreeController writes updated stats]
    │
    ├──► [PlayerStats singleton (ECS)]     ← mining radius, damage, etc.
    ├──► [SkillCooldowns singleton (ECS)]  ← skill unlocks, cooldown values
    └──► [SaveManager.cs (MB)]             ← persist to JSON/PlayerPrefs
```

### Save/Load Flow

```
[SaveManager.cs (MB)]
    │
    │  SAVE: reads ECS singletons (PlayerStats, GameStateData)
    │        + MonoBehaviour state (TechTree unlocks, current level)
    │        → serializes to SaveData class → JsonUtility.ToJson()
    │        → PlayerPrefs.SetString("save", json)
    │
    │  LOAD: PlayerPrefs.GetString("save") → JsonUtility.FromJson<SaveData>()
    │        → writes to ECS singletons via EntityManager
    │        → restores TechTree state in MonoBehaviour
    │
    ▼
[PlayerPrefs (disk / IndexedDB on WebGL)]
```

### Key Data Flows

1. **Input → ECS:** InputBridge MonoBehaviour writes mouse world position and skill activation flags into ECS singleton components every frame. ECS systems read these — no callbacks, no events, just data.

2. **ECS → UI/Audio:** ECS systems append events to DynamicBuffers. Bridge MonoBehaviours drain these buffers in LateUpdate to trigger popups, sounds, and VFX. This is a pull model: the GameObject layer decides when to read.

3. **Config → ECS:** ScriptableObjects define static data. Bakers convert prefab config at bake time. Runtime stat changes (from tech tree upgrades) are written into ECS singletons by MonoBehaviours.

4. **ECS → Rendering:** Entities Graphics package handles rendering ECS entities automatically via URP's SRP Batcher. No manual bridge needed for entity rendering — attach RenderMesh components and Entities Graphics does the rest.

## Scaling Considerations

This is a single-player idle game, not a server. "Scaling" means entity count and frame budget.

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 100 asteroids + 1000 minerals (v1) | Base architecture handles this comfortably. IJobEntity parallelizes movement. Burst compiles damage checks. 60 FPS on mid-range hardware. |
| 500 asteroids + 5000 minerals (future) | Spatial partitioning for damage radius checks (NativeMultiHashMap grid). Damage popups need object pooling (reuse UI elements, do not Instantiate/Destroy). |
| Performance-critical path | Asteroid movement, mineral pull, and damage radius checks are the hot paths. These must be ISystem + IJobEntity + Burst. Everything else (UI, audio, state transitions) is fine as MonoBehaviour. |

### Scaling Priorities

1. **First bottleneck — damage radius checks:** Iterating all asteroids to check distance to mining circle is O(n). At 100 asteroids this is trivial. At 500+ with AoE skills, implement a spatial hash grid (NativeMultiHashMap keyed by cell) to reduce to O(k) where k = nearby asteroids.

2. **Second bottleneck — UI popups:** Instantiating damage number GameObjects per hit is expensive. Pool them from the start — allocate 50-100 popup objects and recycle. This is a MonoBehaviour-side optimization, not an ECS concern.

3. **Third bottleneck — mineral rendering:** 1000+ small meshes. Entities Graphics handles instanced rendering well, but verify draw calls. If minerals share mesh+material, SRP Batcher will batch them automatically.

## Anti-Patterns

### Anti-Pattern 1: Putting UI Logic in ECS Systems

**What people do:** Create ECS components for UI state (button pressed, panel open) and systems to manage UI transitions, trying to keep "everything in ECS."

**Why it's wrong:** uGUI is inherently managed (Canvas, Text, Image are all MonoBehaviours). Wrapping them in ECS adds complexity with no performance benefit. Managed components in ECS skip Burst, skip Jobs, and add GC pressure from the managed wrapper.

**Do this instead:** Keep UI entirely in MonoBehaviours. UI reads ECS data through the Bridge layer. UI writes player actions back to ECS singletons. UI never becomes an entity.

### Anti-Pattern 2: Companion GameObjects for Every Entity

**What people do:** Attach MonoBehaviour components to ECS entities using companion GameObjects so they can use traditional Unity features (particles, audio sources) on each entity.

**Why it's wrong:** Companion GameObjects defeat the purpose of ECS. Each companion is a full GameObject with Transform sync overhead. At 1000 minerals, that is 1000 companion GameObjects — worse performance than pure GameObjects.

**Do this instead:** Use Entities Graphics for rendering (no companion needed). For VFX on specific events (asteroid explosion), the Bridge layer spawns/pools a GameObject particle at the event position. The particle is a temporary effect, not attached to the entity.

### Anti-Pattern 3: Mixing Managed and Unmanaged in Hot-Path Systems

**What people do:** Use SystemBase for asteroid movement because it is more familiar, or store managed references (List<>, string, GameObject) in components used by movement systems.

**Why it's wrong:** SystemBase cannot be Burst-compiled. Managed components prevent job scheduling. For 1000+ entities processed every frame, this leaves significant performance on the table.

**Do this instead:** All per-entity-per-frame systems (movement, damage, collection) must use ISystem + IJobEntity with only unmanaged IComponentData. Reserve SystemBase exclusively for bridge/presentation systems that run once per frame.

### Anti-Pattern 4: Reading EntityManager from Multiple Threads

**What people do:** Try to read ECS data from coroutines, async methods, or background threads.

**Why it's wrong:** EntityManager is main-thread only. Accessing it off the main thread causes race conditions and crashes. This is especially important given the WebGL constraint (single-threaded anyway), but the pattern must be correct for desktop too.

**Do this instead:** All MonoBehaviour ↔ ECS communication happens in Update/LateUpdate on the main thread. Never access EntityManager from coroutines that might resume on a different context.

### Anti-Pattern 5: Saving ECS Entity State Directly

**What people do:** Try to serialize entire ECS worlds or individual entity component data using Unity's SerializeUtility for save files.

**Why it's wrong:** ECS entity serialization is designed for scene streaming, not save games. Entity references are unstable across sessions. Runtime-spawned entities (asteroids, minerals) are ephemeral and should not be persisted — they are regenerated each run.

**Do this instead:** Save only progression state (credits, unlocked upgrades, current level, player stats) as a plain C# class serialized to JSON. On load, write values back to ECS singletons and let the game systems regenerate entities.

## Integration Points

### ECS ↔ GameObject Boundary Rules

| Direction | Mechanism | When |
|-----------|-----------|------|
| **MB → ECS write** | `EntityManager.SetComponentData` on singleton | Input, stat changes, game phase transitions |
| **MB → ECS read** | `EntityManager.GetComponentData` on singleton | UI updates (score, timer) in LateUpdate |
| **ECS → MB** | DynamicBuffer events drained by MB in LateUpdate | Damage popups, audio triggers, VFX spawning |
| **ECS → Rendering** | Entities Graphics (automatic) | Every frame, no manual code |
| **Config → ECS** | Baker at bake time; MB writes singletons at runtime | Level start, tech tree upgrades |

### WebGL Compatibility Boundary

| Feature | Desktop | WebGL | Mitigation |
|---------|---------|-------|------------|
| Burst compilation | Full support | Compiles to WASM but slower (~2-10x) | Acceptable for entity counts in this game |
| Job System | Multi-threaded | Single-threaded (jobs run on main thread) | Still correct, just sequential. Performance budget must account for this. |
| Entities package | Full support | Works but scene loading has reported issues | Use runtime spawning (ECB), not subscene streaming |
| System.Threading | Available | Not available | Do not use anywhere in codebase |
| Audio autoplay | Works | Requires user interaction first | Gate audio initialization behind first click |
| File I/O | File.WriteAllText | Not available | Use PlayerPrefs only (maps to IndexedDB in WebGL) |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| ECS Systems ↔ ECS Systems | Shared components + system ordering | Use `[UpdateAfter]`/`[UpdateBefore]` attributes for deterministic ordering |
| ECS ↔ Bridge | EntityManager + DynamicBuffer | All on main thread; Bridge scripts in dedicated folder |
| Bridge ↔ UI | Direct C# method calls / UnityEvents | Same MonoBehaviour layer; standard Unity patterns |
| Bridge ↔ Audio | AudioManager singleton | Bridge calls `AudioManager.Play(eventType, position)` |
| GameManager ↔ Everything | Game phase singleton + C# events | Phase changes written to ECS singleton AND broadcast as C# event for MB listeners |
| Tech Tree ↔ ECS | Writes PlayerStats singleton | One-way: tech tree pushes updated stats into ECS after purchase |

## Build Order Implications

The architecture has clear dependency chains that dictate build order.

### Phase 1: Foundation (must be first)
**Build:** ECS bootstrap, GameManager state machine, singleton components, InputBridge, camera rig, basic URP scene setup.
**Why first:** Everything else depends on the ECS world existing, game state flowing, and mouse input reaching ECS. Without this, no other system can function.
**Packages to install:** `com.unity.entities`, `com.unity.entities.graphics`, `com.unity.mathematics`

### Phase 2: Core Mining Loop
**Build:** Asteroid spawning system, asteroid movement system, mining damage system, health component, asteroid destruction → mineral detach.
**Why second:** This is the minimum viable game loop. Depends on Phase 1 (input, state). Everything after this decorates or extends the core loop.
**Dependencies:** Phase 1 complete.

### Phase 3: Collection + Scoring
**Build:** Mineral pull system, mineral collection system, score tracking, HUD (score + timer), basic UIDataBridge.
**Why third:** Closes the core loop (mine → collect → score). Requires Phase 2 (minerals exist only after asteroid destruction).
**Dependencies:** Phase 2 complete.

### Phase 4: Visual + Audio Feedback
**Build:** Damage popups, AudioEventBridge, AudioManager, particle effects (mining hits, explosions, collection), screen shake.
**Why fourth:** Feedback makes the loop feel good but is not structurally required. Can be stubbed/skipped during earlier phases.
**Dependencies:** Phase 2-3 (needs events to react to).

### Phase 5: Skills + Advanced Damage
**Build:** Skill components, skill systems (Laser Burst, Chain Lightning, EMP, Overcharge), cooldown management, SkillBarController UI.
**Why fifth:** Extends the core damage system. Requires Phase 2 (damage pipeline) and Phase 4 (visual effects for skills).
**Dependencies:** Phase 2, Phase 4.

### Phase 6: Tech Tree + Progression
**Build:** UpgradeNodeSO data model, TechTreeController, tech tree UI, stat application to PlayerStats singleton, level system, save/load.
**Why sixth:** Meta-game layer on top of the complete gameplay loop. Requires Phase 3 (score/credits to spend) and Phase 5 (skills to unlock).
**Dependencies:** Phase 3, Phase 5.

### Phase 7: Polish + Content
**Build:** Special asteroid types (Fragile, Mega, Cluster), level configs/drop tables, balancing, additional VFX, WebGL testing/optimization.
**Why last:** Content and polish. Requires all systems operational.
**Dependencies:** All previous phases.

### Critical Path
```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7
                                  ↑
                                  └── Phase 4 can start partially in parallel with Phase 3
                                      (audio/VFX infrastructure while scoring is built)
```

## Required DOTS Packages

The project (Unity 6000.3.8f1) currently has URP 17.3.0 and uGUI 2.0.0 but is missing DOTS packages. The following must be added to `Packages/manifest.json`:

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.entities` | 1.3.x (latest for 6000.3) | Core ECS framework |
| `com.unity.entities.graphics` | 1.3.x (matches entities) | Renders ECS entities via URP |
| `com.unity.mathematics` | Already a dependency of entities | Math types (float2, float3) for Burst |
| `com.unity.burst` | Already installed (entities dependency) | Burst compiler for ISystem |
| `com.unity.collections` | Already installed (entities dependency) | NativeArray, NativeList, NativeMultiHashMap |

**Note:** Verify the exact compatible versions for Unity 6000.3 before installing. Use the Package Manager UI to add `com.unity.entities` and let it pull compatible dependencies.

## Sources

- [Unity ECS for Unity overview](https://unity.com/ecs) — Official ECS landing page
- [Entities overview 1.3.14](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/index.html) — Official package docs
- [Systems comparison (ISystem vs SystemBase)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-comparison.html) — Official recommendation to prefer ISystem
- [Singleton components](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/components-singleton.html) — Official singleton pattern docs
- [Entity command buffer overview](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) — ECB patterns
- [Companion components (Entities Graphics)](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.4/manual/companion-components.html) — When and how to use companion objects
- [ECS Development Status December 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) — Current state of Entities development
- [Hybrid ECS architecture advice](https://discussions.unity.com/t/need-advice-for-hybrid-architecture-with-ecs/789215) — Community patterns for hybrid architecture
- [WebGL technical limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html) — Official WebGL constraints (no threading, audio interaction)
- [WebGL and DOTS discussion](https://discussions.unity.com/t/webgl-and-dots/913496) — Community reports on DOTS WebGL compatibility
- [ECS project structure discussion](https://discussions.unity.com/t/ecs-project-structure-and-planning/861973) — Community folder organization patterns
- [Hybrid ECS modifying components from MonoBehaviour](https://discussions.unity.com/t/hybrid-ecs-modifying-component-from-monobehaviour/888334) — EntityManager access patterns from MB

---
*Architecture research for: Unity Hybrid ECS idle/clicker game (Astrominer)*
*Researched: 2026-02-17*
