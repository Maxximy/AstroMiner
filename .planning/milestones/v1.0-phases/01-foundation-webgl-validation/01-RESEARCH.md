# Phase 1: Foundation and WebGL Validation - Research

**Researched:** 2026-02-17
**Domain:** Unity Hybrid ECS bootstrap, WebGL validation, game state machine, object pooling, URP post-processing
**Confidence:** MEDIUM-HIGH

## Summary

Phase 1 is pure technical foundation work -- no gameplay mechanics, just proving the hybrid architecture runs on both desktop and WebGL. The core challenge is bootstrapping a Unity DOTS ECS world that creates entities procedurally (no SubScenes -- broken on WebGL), bridging ECS data to GameObjects for rendering, and confirming 60 FPS at target entity counts (100 asteroids, 1000 minerals) on WebGL where Burst compilation and multithreading are unavailable.

The research confirms that Unity Entities 1.4.4 works on WebGL for pure data/logic, but with significant performance constraints: no Burst SIMD optimization, single-threaded job execution, and no SubScene streaming. All entities must be created procedurally at runtime using `EntityManager.CreateEntity` or `EntityCommandBuffer`. The bridge between ECS and GameObjects uses singleton components for shared state (game phase, mouse position, player stats) and DynamicBuffers for event communication (future damage events, audio triggers). Unity's built-in `ObjectPool<T>` from `UnityEngine.Pool` handles GameObject pooling with manual pre-warming. URP post-processing (bloom + tonemapping) works on WebGL but should use conservative settings (no High Quality Filtering, Quarter downscale for bloom). The `.jslib` IndexedDB flush for save persistence has been partially addressed by Unity's `autoSyncPersistentDataPath` option in the build template, but explicit `FS.syncfs()` calls via `.jslib` remain the reliable approach.

**Primary recommendation:** Bootstrap the ECS world with automatic system creation (no custom bootstrap needed), create all entities procedurally via EntityCommandBuffer, use ISystem + IJobEntity for all hot-path systems, and validate WebGL performance with placeholder entities before writing any gameplay code.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Camera and play area
- Perspective camera with top-down angle -- slight 3D depth, asteroids have visual presence
- Landscape orientation, 16:9 aspect ratio -- standard desktop/web layout
- Scale-to-fit with safe zone -- play area scales to fill screen, UI stays in safe margin, works on any resolution
- Ship sits at bottom 10% of screen, asteroids use ~85% of vertical space to drift down

#### Game state transitions
- Smooth fade-to-black between states (Playing, Collecting, GameOver, Upgrading)
- During Collecting state: gameplay view stays visible, player watches remaining minerals fly to ship, then fade to results -- satisfying visual payoff
- Upgrading state (tech tree): full screen takeover, not an overlay
- Debug UI: small corner overlay in top-left showing current state, FPS, entity count -- unobtrusive

#### Placeholder visuals
- Asteroids: colored Unity primitives (spheres/cubes) -- obviously placeholder programmer art
- Minerals: tiny colored cubes/spheres in various colors -- easy to see count visually
- Ship: basic triangle or arrow shape at bottom center -- establishes spatial reference
- All placeholders have simple drift + spin movement -- proves ECS systems are running, gives more realistic benchmark

#### Space visual style
- Dark void with sparse scattered stars -- clean, serious, lets gameplay elements pop
- Fewer, brighter stars with lots of empty black space
- Warm amber accents -- stars lean warm, slight golden ambient light, inviting despite the void
- Subtle bloom on emissive objects -- gentle glow that adds polish without washing out
- Static skybox texture -- zero performance cost, pre-baked star texture

### Claude's Discretion
- Exact post-processing parameter values (bloom threshold, intensity, tonemapping mode)
- Fade duration and easing curves for state transitions
- Specific primitive shapes and color assignments for placeholders
- Debug overlay font size and positioning details
- URP renderer configuration details

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFRA-01 | ECS world bootstraps with hybrid bridge layer (ECS simulation + GameObject rendering/UI/audio) | ECS world auto-bootstraps with Entities 1.4.4. Bridge uses singleton components (InputData, GameStateData) and DynamicBuffers for events. No SubScenes -- all entities created procedurally. See Architecture Patterns section. |
| INFRA-02 | WebGL build runs at 60 FPS with 100 asteroids and 1000 minerals active | WebGL runs without Burst (managed fallback) and single-threaded. Must profile early. IJobEntity still runs but sequentially. See WebGL Performance section and Common Pitfalls. |
| INFRA-03 | Object pooling system for asteroids, minerals, damage popups, and particles | Unity's built-in `ObjectPool<T>` from `UnityEngine.Pool` namespace. Pre-warm by calling Get/Release in a loop. See Object Pooling section. |
| INFRA-04 | Game state machine manages Playing, Collecting, GameOver, and Upgrading states | State pattern with IState interface (Enter/Execute/Exit). GameManager MonoBehaviour owns the FSM. Game phase written to ECS singleton for system awareness. See Game State Machine section. |
| INFRA-05 | InputBridge writes mouse world position to ECS singleton each frame | MonoBehaviour captures Input.mousePosition, uses Camera.ScreenPointToRay + Plane.Raycast to project to gameplay plane, writes float2 to ECS singleton via EntityManager.SetComponentData. See InputBridge section. |
| INFRA-06 | Large number formatting utility (K/M/B/T) used across all credit displays | Static utility class with magnitude array ["", "K", "M", "B", "T"]. Divide by 1000 repeatedly, pick suffix. Use double for range. See Large Number Formatting section. |
| VISL-01 | Realistic space aesthetic with starfield skybox | Static cubemap skybox texture. Pre-baked or generated externally (Space 3D at tyro.net or hand-painted). Applied via Lighting Settings > Skybox Material. Zero runtime cost. See Skybox section. |
| VISL-02 | URP post-processing: bloom, tonemapping | URP Volume with Bloom (threshold ~0.8, intensity ~0.3, scatter ~0.6) and Tonemapping (Neutral mode). Disable High Quality Filtering for WebGL. See Post-Processing section. |
</phase_requirements>

## Standard Stack

### Core (Phase 1 specific)

| Library | Version | Purpose | Why Standard | Confidence |
|---------|---------|---------|--------------|------------|
| com.unity.entities | 1.4.4 | ECS framework | Released for Unity 6000.3. ISystem, IJobEntity, SystemAPI.Query, singleton components, ECB. Entities.ForEach obsoleted -- use IJobEntity. | HIGH |
| com.unity.collections | 2.6.4 | Native containers | Transitive dependency of Entities. NativeArray, NativeList, NativeHashMap for ECS data. | HIGH |
| com.unity.burst | 1.8.27 | Compiler optimization | Transitive dependency. Compiles ISystem/IJobEntity to optimized native code on desktop. **Does NOT run on WebGL** -- managed fallback used automatically. | HIGH |
| com.unity.mathematics | 1.3.3 | Math library | Transitive dependency. float2, float3, quaternion, math functions. Used by all ECS component data. | HIGH |
| com.unity.render-pipelines.universal | 17.3.0 | Render pipeline | Already in project. URP post-processing (bloom, tonemapping). Forward rendering. WebGL compatible. | HIGH |
| com.unity.inputsystem | 1.18.0 | Input handling | Already in project. Mouse position capture for InputBridge. | HIGH |
| com.unity.ugui | 2.0.0 | UI framework | Already in project. Debug overlay, state display, future HUD. Canvas-based. | HIGH |
| UnityEngine.Pool | Built-in | Object pooling | Built-in since Unity 2021. ObjectPool<T> with stack-based reuse, configurable create/get/release/destroy callbacks. | HIGH |

### To Install (add to manifest.json)

```json
{
  "dependencies": {
    "com.unity.entities": "1.4.4"
  }
}
```

Collections, Burst, and Mathematics are pulled automatically as transitive dependencies. Do NOT add `com.unity.entities.graphics` (WebGL incompatible). Do NOT add `com.unity.physics` yet (not needed in Phase 1 -- simple distance checks suffice).

### To Remove from manifest.json

These default packages are unnecessary and add build size:
- `com.unity.ai.navigation` -- no NavMesh needed
- `com.unity.multiplayer.center` -- single-player game
- `com.unity.visualscripting` -- code-first project
- `com.unity.timeline` -- no timeline sequences needed

### Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| Unity built-in ObjectPool<T> | Custom pool implementation | Built-in is tested, maintained, has collectionCheck for double-release detection. No reason to hand-roll. |
| ISystem + IJobEntity | SystemBase + Entities.ForEach | Entities.ForEach obsoleted in 1.4. ISystem enables Burst on desktop. SystemBase reserved for managed bridge logic only. |
| Procedural entity creation | SubScenes with bakers | SubScene streaming is broken on WebGL. All entities must be created at runtime via code. |
| Static cubemap skybox | Procedural skybox shader | User decision: static pre-baked texture. Zero runtime cost. Procedural adds GPU cost with no benefit for a static starfield. |
| State pattern (IState classes) | Enum + switch statement | 4 states with distinct enter/exit behavior benefit from proper state objects. Enum FSM gets messy with fade transitions. |

## Architecture Patterns

### Recommended Project Structure (Phase 1)

```
Assets/
  Scripts/
    ECS/
      Components/
        GameStateComponents.cs    # GameStateData, InputData singletons
        PlaceholderComponents.cs  # DriftSpeed, SpinRate for benchmark entities
      Systems/
        PlaceholderMovementSystem.cs  # ISystem + IJobEntity: drift + spin
        GameStateSystem.cs            # ISystem: reads GameStateData, enables/disables systems
    MonoBehaviours/
      Core/
        GameManager.cs           # FSM owner, state transitions, fade controller
        ECSBootstrap.cs          # Creates singleton entities on Awake
      Bridge/
        InputBridge.cs           # Writes mouse world pos to ECS singleton
      UI/
        DebugOverlay.cs          # FPS, entity count, current state display
        FadeController.cs        # CanvasGroup alpha for fade-to-black
      Pool/
        GameObjectPool.cs        # Wrapper around ObjectPool<T> with pre-warming
    States/
      IGameState.cs              # Interface: Enter(), Execute(), Exit()
      PlayingState.cs            # Active gameplay state
      CollectingState.cs         # Post-timer mineral collection
      GameOverState.cs           # Results display
      UpgradingState.cs          # Tech tree screen (stub in Phase 1)
    Shared/
      GameEnums.cs               # GamePhase enum
      NumberFormatter.cs         # K/M/B/T formatting utility
  Plugins/
    WebGL/
      IndexedDBSync.jslib        # FS.syncfs() flush function
  Materials/
    SkyboxMaterial.mat           # Cubemap skybox
  Textures/
    Skybox/                      # Cubemap faces
  Scenes/
    GameScene.unity              # Main scene
  Settings/                      # Existing URP settings
```

### Pattern 1: ECS Bootstrap Without SubScenes

**What:** Create singleton entities and entity archetypes programmatically at runtime. No SubScenes, no bakers for data entities.

**When to use:** Always on this project (WebGL constraint).

**Why:** SubScene entity streaming is broken on WebGL. The standard Entities workflow uses SubScenes + Bakers, but that path does not work for WebGL builds. Instead, create entities in a MonoBehaviour's Awake/Start or in a system's OnCreate.

**Example:**
```csharp
// MonoBehaviours/Core/ECSBootstrap.cs
using Unity.Entities;
using Unity.Mathematics;

public class ECSBootstrap : MonoBehaviour
{
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;

        // Create GameState singleton
        var gameStateEntity = em.CreateEntity(
            typeof(GameStateData)
        );
        em.SetComponentData(gameStateEntity, new GameStateData
        {
            Phase = GamePhase.Playing,
            Timer = 0f,
            Score = 0
        });

        // Create Input singleton
        var inputEntity = em.CreateEntity(
            typeof(InputData)
        );
        em.SetComponentData(inputEntity, new InputData
        {
            MouseWorldPos = float2.zero,
            MouseValid = false
        });
    }
}
```

**Confidence:** HIGH -- EntityManager.CreateEntity is the documented API. World.DefaultGameObjectInjectionWorld is the default world created by Entities package at startup.

### Pattern 2: Singleton Components for Cross-Layer Communication

**What:** Store shared state (game phase, mouse position, player stats) as ECS singleton components. Both ISystem code and MonoBehaviours access these on the main thread.

**When to use:** Any state that both ECS systems and MonoBehaviours need: game phase, input data, score, timer.

**Example:**
```csharp
// ECS/Components/GameStateComponents.cs
using Unity.Entities;
using Unity.Mathematics;

public struct GameStateData : IComponentData
{
    public GamePhase Phase;
    public float Timer;
    public long Credits;
}

public struct InputData : IComponentData
{
    public float2 MouseWorldPos;
    public bool MouseValid;
}

// Reading in an ISystem:
[BurstCompile]
public partial struct PlaceholderMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        if (gameState.Phase != GamePhase.Playing)
            return;

        var dt = SystemAPI.Time.DeltaTime;
        // ... schedule movement job
    }
}

// Writing from MonoBehaviour:
// InputBridge.cs
void Update()
{
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    var query = em.CreateEntityQuery(typeof(InputData));
    var entity = query.GetSingletonEntity();

    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    var plane = new Plane(Vector3.up, Vector3.zero);
    if (plane.Raycast(ray, out float dist))
    {
        var worldPos = ray.GetPoint(dist);
        em.SetComponentData(entity, new InputData
        {
            MouseWorldPos = new float2(worldPos.x, worldPos.z),
            MouseValid = true
        });
    }
}
```

**Confidence:** HIGH -- Singleton components are officially documented (Entities 1.0+ manual). SystemAPI.GetSingleton<T>() is the standard read path.

### Pattern 3: ISystem + IJobEntity for Hot-Path Systems

**What:** Use unmanaged ISystem with BurstCompile attribute for all per-entity-per-frame logic. Use IJobEntity for the actual iteration.

**When to use:** Placeholder movement (drift + spin) in Phase 1. All future gameplay systems (asteroid movement, mineral pull, damage).

**Example:**
```csharp
// ECS/Systems/PlaceholderMovementSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PlaceholderDriftJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, in DriftData drift)
    {
        transform.Position.y -= drift.Speed * DeltaTime;
    }
}

[BurstCompile]
public partial struct PlaceholderSpinJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, in SpinData spin)
    {
        transform = transform.RotateY(spin.RadiansPerSecond * DeltaTime);
    }
}

[BurstCompile]
public partial struct PlaceholderMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        new PlaceholderDriftJob { DeltaTime = dt }
            .ScheduleParallel();

        new PlaceholderSpinJob { DeltaTime = dt }
            .ScheduleParallel();
    }
}
```

**Note on WebGL:** On WebGL, ScheduleParallel still works but runs single-threaded. Burst-compiled code runs as managed fallback. The code is identical for both platforms -- the performance difference is handled by the runtime.

**Confidence:** HIGH -- IJobEntity with ISystem is the documented pattern in Entities 1.4 official samples.

### Pattern 4: Game State Machine (MonoBehaviour + State Pattern)

**What:** GameManager MonoBehaviour owns a finite state machine with IGameState objects. State transitions write the current phase to an ECS singleton so systems can check game phase.

**When to use:** Managing Playing, Collecting, GameOver, Upgrading states with fade transitions.

**Example:**
```csharp
// States/IGameState.cs
public interface IGameState
{
    void Enter(GameManager manager);
    void Execute(GameManager manager);
    void Exit(GameManager manager);
}

// MonoBehaviours/Core/GameManager.cs
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private IGameState _currentState;
    private Dictionary<GamePhase, IGameState> _states;

    void Awake()
    {
        Instance = this;
        _states = new Dictionary<GamePhase, IGameState>
        {
            { GamePhase.Playing, new PlayingState() },
            { GamePhase.Collecting, new CollectingState() },
            { GamePhase.GameOver, new GameOverState() },
            { GamePhase.Upgrading, new UpgradingState() },
        };
    }

    public void TransitionTo(GamePhase newPhase)
    {
        _currentState?.Exit(this);
        _currentState = _states[newPhase];

        // Write to ECS singleton so systems can read game phase
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(GameStateData));
        var entity = query.GetSingletonEntity();
        var data = em.GetComponentData<GameStateData>(entity);
        data.Phase = newPhase;
        em.SetComponentData(entity, data);

        _currentState.Enter(this);
    }

    void Update()
    {
        _currentState?.Execute(this);
    }
}
```

**Confidence:** HIGH -- State pattern is a standard Unity design pattern. Writing to ECS singletons from MonoBehaviours is documented.

### Pattern 5: GameObject Object Pool with Pre-Warming

**What:** Use Unity's built-in ObjectPool<T> to pre-allocate GameObjects for entities that spawn/despawn frequently. Wrap in a convenience class that pre-warms on initialization.

**When to use:** Asteroids, minerals, damage popups, particle effects -- anything created and destroyed during gameplay.

**Example:**
```csharp
// MonoBehaviours/Pool/GameObjectPool.cs
using UnityEngine;
using UnityEngine.Pool;

public class GameObjectPool
{
    private readonly ObjectPool<GameObject> _pool;
    private readonly GameObject _prefab;
    private readonly Transform _parent;

    public GameObjectPool(GameObject prefab, Transform parent,
        int preWarmCount, int maxSize)
    {
        _prefab = prefab;
        _parent = parent;

        _pool = new ObjectPool<GameObject>(
            createFunc: () => Object.Instantiate(_prefab, _parent),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Object.Destroy(obj),
            collectionCheck: true,
            defaultCapacity: preWarmCount,
            maxSize: maxSize
        );

        // Pre-warm: create objects and immediately return them
        var preWarmed = new GameObject[preWarmCount];
        for (int i = 0; i < preWarmCount; i++)
            preWarmed[i] = _pool.Get();
        for (int i = 0; i < preWarmCount; i++)
            _pool.Release(preWarmed[i]);
    }

    public GameObject Get() => _pool.Get();
    public void Release(GameObject obj) => _pool.Release(obj);
    public int CountActive => _pool.CountActive;
    public int CountInactive => _pool.CountInactive;
}
```

**Key detail:** ObjectPool does NOT pre-allocate objects based on defaultCapacity. The defaultCapacity parameter sets the initial capacity of the internal stack data structure, not the number of objects created. You must manually pre-warm by calling Get then Release in a loop.

**Confidence:** HIGH -- ObjectPool<T> API is documented in Unity ScriptReference. Pre-warming pattern verified from community sources and official Unity Learn tutorials.

### Anti-Patterns to Avoid

- **Using SubScenes for entity data:** Broken on WebGL. Entity scene streaming fails to load from StreamingAssets on WebGL builds. Use procedural entity creation only.
- **Entities.ForEach / Job.WithCode:** Obsoleted in Entities 1.4. Will generate compiler warnings. Use IJobEntity and SystemAPI.Query.
- **Direct EntityManager access from non-bridge MonoBehaviours:** All ECS communication should go through dedicated Bridge scripts (InputBridge, etc.) to keep the boundary clean and debuggable.
- **Instantiate/Destroy in gameplay loops:** Never call Object.Instantiate or Object.Destroy during active gameplay. Use object pools exclusively.
- **System.Threading anywhere in codebase:** Does not work on WebGL. Use coroutines or Awaitable for async patterns.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| GameObject pooling | Custom linked-list pool | `ObjectPool<T>` from `UnityEngine.Pool` | Built-in, tested, has double-release detection, handles capacity limits |
| Entity iteration | Manual EntityManager.GetComponentData loops | IJobEntity with ISystem | Source-generated query, Burst-compatible, handles chunk iteration |
| Structural ECS changes | Direct EntityManager.DestroyEntity in system loops | EntityCommandBuffer from ECBSystem singletons | ECB batches changes, avoids sync points, thread-safe via ParallelWriter |
| Mouse-to-world projection | Custom matrix math | Camera.ScreenPointToRay + Plane.Raycast | Built-in, handles perspective correctly, accounts for camera orientation |
| Number formatting (K/M/B/T) | Ad-hoc string formatting per UI element | Centralized static NumberFormatter utility | Used everywhere, must be consistent, avoids string allocation bugs |
| Post-processing setup | Manual shader passes | URP Volume + Bloom/Tonemapping overrides | Built into URP, handles render pipeline integration, WebGL compatible |

**Key insight:** Phase 1 should use well-tested Unity APIs and patterns. The complexity is in the architecture (hybrid ECS bridge, WebGL compatibility), not in building custom tools.

## Common Pitfalls

### Pitfall 1: SubScenes Don't Work on WebGL

**What goes wrong:** The standard Entities workflow uses SubScenes with Baker components to define entities. On WebGL, entity scene files fail to load from StreamingAssets. The build may succeed but entities never appear at runtime.

**Why it happens:** WebGL's virtual filesystem cannot reliably stream entity scene binary data from StreamingAssets.

**How to avoid:** Never use SubScenes for entity data. Create all entities procedurally in MonoBehaviour Start() or ISystem.OnCreate() using EntityManager.CreateEntity with explicit archetypes, or EntityCommandBuffer for deferred creation. This is the correct pattern for the entire project lifetime, not just a workaround.

**Warning signs:** Entities exist in Editor play mode but not in WebGL builds. "Failed to load entity scene" errors in browser console.

### Pitfall 2: Burst Does Not Compile on WebGL

**What goes wrong:** All [BurstCompile] ISystem and IJobEntity code runs as managed C# on WebGL. Performance is 5-10x worse than Burst-compiled desktop for the same entity counts.

**Why it happens:** Burst compiles to native SIMD code which cannot be represented in WebAssembly. Unity's WebGL pipeline uses IL2CPP to C++ to Emscripten/WASM instead.

**How to avoid:** Write all ECS code with [BurstCompile] attributes (they are ignored on WebGL, not errors). Profile on WebGL in Phase 1 with target entity counts. If 100 asteroids + 1000 minerals exceed 16ms frame time on WebGL, reduce counts or simplify per-entity work. The code is identical for both platforms -- the runtime handles the difference.

**Warning signs:** Desktop runs at 1ms frame time, WebGL at 12ms+ for same scene. Profiler shows main thread stalls in system updates on WebGL.

### Pitfall 3: EntityManager.CreateEntity Causes Sync Points

**What goes wrong:** Calling EntityManager.CreateEntity during system execution forces a sync point -- all running jobs must complete before the structural change happens. With many simultaneous spawn/destroy operations, this tanks frame rate.

**How to avoid:** Use EntityCommandBuffer (ECB) for all structural changes. Get the ECB from BeginSimulationEntityCommandBufferSystem.Singleton or EndSimulationEntityCommandBufferSystem.Singleton. ECB records commands that are batched and executed at a defined sync point, minimizing stalls.

```csharp
// Correct: use ECB for spawning
var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
var entity = ecb.CreateEntity();
ecb.AddComponent(entity, new DriftData { Speed = 2f });

// Incorrect: direct EntityManager in system
// state.EntityManager.CreateEntity(...);  // forces sync point!
```

**Warning signs:** Frame spikes when entities spawn. Profiler shows "Sync Point" or "Structural Changes" as a top contributor.

### Pitfall 4: ObjectPool Does Not Pre-Allocate

**What goes wrong:** Developer sets defaultCapacity to 100 expecting 100 objects to exist at startup. defaultCapacity only sets internal stack size -- no objects are created until Get() is called. First gameplay frame that needs 50 pooled objects causes 50 Instantiate calls and a massive frame spike.

**How to avoid:** Manually pre-warm the pool after construction: call Get() in a loop to create objects, then Release() each back to the pool. Do this in a loading state before gameplay begins. See the GameObjectPool wrapper pattern in Architecture Patterns section.

**Warning signs:** Frame spike on the first gameplay frame. Profiler shows Instantiate calls during active gameplay.

### Pitfall 5: CanvasGroup Fade Blocks Raycasts

**What goes wrong:** Using CanvasGroup.alpha to fade UI panels to/from black. When alpha is 0, the invisible panel still blocks raycasts to UI elements behind it unless `blocksRaycasts` is explicitly disabled.

**How to avoid:** When fading out, set `canvasGroup.blocksRaycasts = false` at the start of the fade. When fading in to a new panel, set `blocksRaycasts = true` after the fade completes.

**Warning signs:** Buttons unresponsive after a state transition. Invisible UI panel intercepting clicks.

### Pitfall 6: Camera.main is Expensive Per-Frame

**What goes wrong:** `Camera.main` does a FindGameObjectWithTag("MainCamera") lookup every call. In InputBridge.Update() called every frame, this adds unnecessary overhead.

**How to avoid:** Cache the camera reference in Start() or Awake():
```csharp
private Camera _mainCamera;
void Start() { _mainCamera = Camera.main; }
void Update()
{
    var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
    // ...
}
```

**Warning signs:** Profiler shows FindGameObjectWithTag in InputBridge Update.

## Code Examples

### InputBridge: Mouse-to-World Projection

```csharp
// MonoBehaviours/Bridge/InputBridge.cs
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class InputBridge : MonoBehaviour
{
    private Camera _mainCamera;
    private EntityManager _em;
    private Entity _inputEntity;
    private Plane _gameplayPlane;

    void Start()
    {
        _mainCamera = Camera.main;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _inputEntity = _em.CreateEntityQuery(typeof(InputData))
            .GetSingletonEntity();

        // Gameplay plane at Y=0, facing up
        // For perspective top-down camera, the plane determines
        // where mouse rays intersect the game world
        _gameplayPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void Update()
    {
        var mousePos = Input.mousePosition;
        var ray = _mainCamera.ScreenPointToRay(mousePos);

        var inputData = new InputData();
        if (_gameplayPlane.Raycast(ray, out float distance))
        {
            var worldPoint = ray.GetPoint(distance);
            inputData.MouseWorldPos = new float2(worldPoint.x, worldPoint.z);
            inputData.MouseValid = true;
        }
        else
        {
            inputData.MouseValid = false;
        }

        _em.SetComponentData(_inputEntity, inputData);
    }
}
```

**Note:** For a perspective camera with top-down angle, the ray will not be perpendicular to the gameplay plane. `Plane.Raycast` correctly handles the angle and returns the intersection point at the Y=0 plane where entities live. The X and Z components map to the 2D gameplay space.

### Large Number Formatting Utility

```csharp
// Shared/NumberFormatter.cs
public static class NumberFormatter
{
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

    /// <summary>
    /// Formats a number with magnitude suffix.
    /// Examples: 999 -> "999", 1500 -> "1.5K", 2500000 -> "2.5M"
    /// </summary>
    public static string Format(double value)
    {
        if (value < 0) return "-" + Format(-value);
        if (value < 1000) return value.ToString("F0");

        int magnitude = 0;
        double display = value;
        while (display >= 1000 && magnitude < Suffixes.Length - 1)
        {
            display /= 1000;
            magnitude++;
        }

        // Show 1 decimal place for values like 1.5K, no decimal for 15K+
        if (display < 10)
            return display.ToString("F1") + Suffixes[magnitude];
        if (display < 100)
            return display.ToString("F1") + Suffixes[magnitude];
        return display.ToString("F0") + Suffixes[magnitude];
    }
}
```

**Note:** Use `double` as the backing type for credits. It provides range up to ~1.7e308 which is sufficient for any idle game. Do NOT use float (only ~7 significant digits) or int (overflow at 2.1B). Avoid string allocation where possible -- consider a StringBuilder-based variant for UI that updates every frame.

### WebGL IndexedDB Sync Plugin

```javascript
// Plugins/WebGL/IndexedDBSync.jslib
mergeInto(LibraryManager.library, {
    SyncIndexedDB: function () {
        // Flush the Emscripten virtual filesystem to IndexedDB
        FS.syncfs(false, function (err) {
            if (err) {
                console.error("IndexedDB sync failed:", err);
            }
        });
    }
});
```

```csharp
// Shared/WebGLHelper.cs
using System.Runtime.InteropServices;

public static class WebGLHelper
{
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SyncIndexedDB();
    #endif

    public static void FlushSaveData()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        SyncIndexedDB();
        #endif
    }
}
```

**Note:** Unity 6 added `autoSyncPersistentDataPath` as a build template option, but it is not enabled by default and may not be reliable in all scenarios. The explicit `.jslib` approach is more dependable. Call `WebGLHelper.FlushSaveData()` after every `File.WriteAllText` or `PlayerPrefs.Save()` call on WebGL.

### Debug Overlay

```csharp
// MonoBehaviours/UI/DebugOverlay.cs
using UnityEngine;
using Unity.Entities;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private TextMeshProUGUI _entityCountText;

    private float _fpsTimer;
    private int _frameCount;
    private EntityManager _em;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void LateUpdate()
    {
        // FPS counter (update every 0.5s to avoid jitter)
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            float fps = _frameCount / _fpsTimer;
            _fpsText.SetText("FPS: {0:0}", fps);
            _frameCount = 0;
            _fpsTimer = 0f;
        }

        // Game state
        var gameState = _em.CreateEntityQuery(typeof(GameStateData))
            .GetSingleton<GameStateData>();
        _stateText.SetText(gameState.Phase.ToString());

        // Entity count
        // Note: Getting total entity count is not cheap; update less frequently if needed
        var entityQuery = _em.CreateEntityQuery(typeof(Unity.Transforms.LocalTransform));
        _entityCountText.SetText("Entities: {0}", entityQuery.CalculateEntityCount());
    }
}
```

**Note:** Use TextMeshPro's `SetText` with numeric format overloads rather than string interpolation or concatenation. SetText with value parameters avoids string allocation.

## WebGL Performance Research

### Confirmed Constraints (Unity 6.3 / WebGL)

| Constraint | Impact on Phase 1 | Source |
|-----------|-------------------|--------|
| No Burst compilation | IJobEntity and ISystem code runs as managed C#. ~5-10x slower than Burst on desktop. | Unity Burst discussions, confirmed no WebGL support |
| Single-threaded jobs | ScheduleParallel() runs sequentially. No parallel chunk processing. | Unity WebGL technical docs: "Managed (C#) threads aren't supported" |
| No SubScene streaming | Cannot load entity scenes from StreamingAssets. | Unity discussions, issue tracker |
| GC once per frame | Garbage collector runs only once per frame on WebGL (no incremental GC). String allocations accumulate. | Unity WebGL technical docs |
| WebAssembly 2023 recommended | Enables native WASM exceptions, removes dynCall overhead. Should be enabled in Player Settings. | Unity WebGL performance docs |
| No System.Threading | System.Threading namespace non-functional on WebGL. | Unity WebGL technical docs |
| OpenGL ES graphics | WebGL API based on OpenGL ES with inherent limitations. | Unity WebGL technical docs |
| Audio requires user gesture | Web Audio API requires user interaction before playing. | Unity WebGL audio docs |

### Performance Budget for Phase 1

Target: 60 FPS = 16.67ms per frame on WebGL.

| Budget Item | Estimated Cost (WebGL) | Notes |
|-------------|----------------------|-------|
| ECS system updates (100 entities drift + spin) | 1-3ms | Without Burst, simple float math on 100 entities. Should be fast even managed. |
| ECS system updates (1000 mineral entities) | 3-8ms | 1000 entities with position updates. Critical benchmark. |
| Bridge reads/writes | <0.5ms | Singleton access is cheap. Once per frame. |
| URP rendering (primitives + skybox + post-processing) | 3-6ms | 1100 simple meshes. Forward rendering. Bloom adds ~1-2ms. |
| UI updates (debug overlay) | <0.5ms | 3 TextMeshPro SetText calls per frame. |
| **Total estimated** | **8-18ms** | Within budget if ECS managed code is efficient |

**Critical benchmark:** Phase 1 must produce a WebGL build with 100 drifting/spinning spheres + 1000 drifting cubes and measure actual frame time. If it exceeds 16ms, reduce entity counts or simplify per-entity work before proceeding.

### WebGL Player Settings Recommendations

- **Compression Format:** Brotli (best compression for WebGL)
- **WebAssembly:** Enable "WebAssembly 2023" in Player Settings for better WASM performance
- **Code Stripping:** Set to High for smallest build size
- **Exception Handling:** Set to "Explicitly Thrown Exceptions Only" for better performance (avoid Full for production)
- **Memory:** Default initial memory (32MB). Unity auto-grows. Target under 512MB total at runtime.
- **Template:** Use minimal HTML template. Enable `autoSyncPersistentDataPath` in template if available.

## Post-Processing Configuration (Claude's Discretion)

### Recommended URP Volume Settings

**Bloom:**
- Threshold: 0.8 (catches only bright emissive objects, not general scene)
- Intensity: 0.3 (subtle glow -- "gentle glow that adds polish without washing out" per user)
- Scatter: 0.6 (moderate spread)
- Tint: Very slight warm amber (RGB ~255, 240, 220) to match warm accent decision
- High Quality Filtering: **OFF** (important for WebGL performance -- bicubic vs bilinear)
- Downscale: **Quarter** (reduces bloom GPU cost; acceptable for subtle bloom)
- Max Iterations: 4 (reduced from default 6 for WebGL; sufficient for gentle glow)
- Clamp: 65472 (default; prevents extreme bloom from very bright pixels)

**Tonemapping:**
- Mode: **Neutral** (preserves original colors without heavy stylization; ACES would shift warm tones too aggressively for the "calm void" aesthetic. Neutral lets the warm amber accent colors come through naturally.)

**Rationale:** Neutral tonemapping is preferred because the user wants warm amber accents as subtle points of light. ACES applies its own color grading (compresses highlights, shifts hues) which could fight against the intended color palette. Neutral applies range mapping without color opinion, giving more control over the intended warm-toned look.

**Color Adjustments (optional):**
- Color temperature: Slight warm shift (+5 to +10) to support the warm amber atmosphere
- No vignette, no film grain, no chromatic aberration -- keep it clean

### Skybox Configuration

- Material type: Skybox/Cubemap
- Source: Pre-baked cubemap texture with sparse bright stars against deep black
- Ambient Source: Skybox (very low ambient intensity, ~0.05, to maintain "dark void" feel)
- Ambient Mode: Flat (no environment reflections needed for this aesthetic)
- Reflection intensity: 0 (no reflections needed)

The skybox texture should be generated using a tool like the Space 3D generator (tyro.net) or painted manually. Requirements: mostly black, sparse warm-toned stars (lean amber/gold), no nebulae or gas clouds, no bright central star. Resolution: 512x512 per face is sufficient for a dark skybox with point stars.

## State Transition Implementation (Claude's Discretion)

### Fade Parameters

- **Fade duration:** 0.4 seconds (fast enough to not feel sluggish, slow enough to be visible)
- **Easing curve:** Ease-in-out (smooth acceleration and deceleration)
- **Implementation:** CanvasGroup on a full-screen black panel. Animate alpha from 0 to 1 for fade-out, 1 to 0 for fade-in.
- **Transition sequence:** Current state Exit() -> fade to black (0.4s) -> new state Enter() -> fade from black (0.4s)
- **Collecting state exception:** No fade on transition from Playing to Collecting (per user decision: gameplay stays visible, minerals fly to ship, then fade to results)

### Placeholder Visual Assignments (Claude's Discretion)

| Entity | Shape | Color | Size | Notes |
|--------|-------|-------|------|-------|
| Asteroid (placeholder) | Sphere | Varied: dark gray, brown, rust | 1.0-1.5 world units | Use Unity primitive sphere. Random color per instance from a warm muted palette. |
| Mineral (placeholder) | Cube | Varied: cyan, green, gold, purple | 0.15-0.25 world units | Tiny cubes. Color indicates resource tier placeholder. |
| Ship (placeholder) | Arrow/triangle mesh or rotated cube | Light gray / white | 1.0 world units wide | Flat triangle shape pointing up. Could be a flattened cube (scale Y to 0.1) rotated 45 degrees as a diamond, or a simple triangle mesh. |

### Debug Overlay Positioning

- Position: Top-left corner, 10px margin from edges
- Font size: 14px (readable but unobtrusive)
- Background: Semi-transparent black panel (alpha 0.6) behind text
- Content: Three lines -- "State: Playing", "FPS: 60", "Entities: 1100"
- Always visible regardless of game state (debug tool)

## Open Questions

1. **Exact Entities 1.4.4 + WebGL behavior with no SubScenes**
   - What we know: Entities package works on WebGL for pure data/logic. SubScene streaming is broken. EntityManager.CreateEntity works.
   - What's unclear: Whether the default ECS world auto-creation works reliably on WebGL without any SubScene in the project. Some reports suggest the Entities package expects at least one SubScene for initialization.
   - Recommendation: Test immediately in Phase 1. If auto-world creation fails on WebGL, use the `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD` scripting define and create the world manually via `DefaultWorldInitialization.Initialize("Default World")`. LOW confidence this is needed, but have the fallback ready.

2. **WebGL managed ECS performance at 1100 entities**
   - What we know: Without Burst, ECS managed code is slower. The 5-10x estimate comes from community reports.
   - What's unclear: Actual frame time for 1100 entities with simple drift + spin on current Unity 6.3 WASM pipeline. WebAssembly 2023 mode may improve managed performance vs. older WASM targets.
   - Recommendation: This is the primary validation goal of Phase 1. Build and measure. Do not proceed to Phase 2 until this number is known. If >16ms, reduce mineral count to 500 or simplify per-entity systems.

3. **autoSyncPersistentDataPath reliability**
   - What we know: Unity added this option to the WebGL build template to automatically sync persistentDataPath to IndexedDB. It was added in a 2024 patch.
   - What's unclear: Whether it is enabled by default in Unity 6.3 build templates, and whether it is reliable across all browsers (especially Safari).
   - Recommendation: Implement the explicit `.jslib` FS.syncfs() approach regardless. It is the proven, reliable method. The autoSync option can serve as a backup.

## Sources

### Primary (HIGH confidence)
- [Unity Entities 1.4 IJobEntity Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.4//manual/iterating-data-ijobentity.html) -- IJobEntity patterns, Execute signature, scheduling
- [Unity Entities 1.0 Singleton Components](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/components-singleton.html) -- GetSingleton, SetSingleton API
- [Unity Entities 1.0 ECB Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) -- EntityCommandBuffer patterns
- [Unity ECB Samples (GitHub)](https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/EntitiesSamples/Docs/entity-command-buffers.md) -- ECB best practices, ParallelWriter, system singletons
- [Unity ECS Jobs Samples (GitHub)](https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/EntitiesSamples/Assets/ExampleCode/Jobs.cs) -- IJobEntity code examples
- [Unity WebGL Technical Limitations (6.3)](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html) -- Threading, memory, audio constraints
- [Unity WebGL Performance (6.3)](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-performance.html) -- WASM performance recommendations
- [Unity ObjectPool API](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html) -- ObjectPool<T> constructor, methods, properties
- [Unity ObjectPool Tutorial](https://unity.com/how-to/use-object-pooling-boost-performance-c-scripts-unity) -- Official pooling guide
- [URP Bloom Volume Override](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-bloom.html) -- Bloom parameters, HQ filtering, downscale
- [URP Tonemapping Volume Override](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-tonemapping.html) -- Neutral vs ACES modes
- [Unity Skybox Cubemap Shader](https://docs.unity3d.com/Manual/shader-skybox-cubemap.html) -- Cubemap skybox material setup
- [Camera.ScreenPointToRay](https://docs.unity3d.com/Manual/CameraRays.html) -- Rays from camera documentation
- [Unity State Pattern Tutorial](https://learn.unity.com/course/design-patterns-unity-6/tutorial/develop-a-modular-flexible-codebase-with-the-state-programming-pattern) -- Official state pattern guide for Unity 6

### Secondary (MEDIUM confidence)
- [Burst for WebGL Discussion](https://discussions.unity.com/t/burst-for-webgl/849368) -- Confirms no Burst WebGL support; ~10x slower managed fallback
- [WebGL and DOTS Discussion](https://discussions.unity.com/t/webgl-and-dots/913496) -- Community reports on DOTS + WebGL compatibility
- [SubScenes on WebGL Discussion](https://discussions.unity.com/t/is-there-a-way-to-load-subscenes-for-entities-on-webgl/1527759) -- SubScene streaming broken on WebGL
- [DOTS Content Without Entities Graphics](https://discussions.unity.com/t/how-to-organize-content-in-dots-netcode-for-entities-without-entities-graphics-webgl-target/1689598) -- Organizing DOTS for WebGL without Entities Graphics
- [WebGL IndexedDB Flush Discussion](https://discussions.unity.com/t/webgl-flushing-data-to-indexdb/240698) -- FS.syncfs() pattern for IndexedDB persistence
- [WebGL autoSyncPersistentDataPath](https://issuetracker.unity3d.com/issues/webgl-streamwriter-not-triggering-syncfs-when-writing-a-file-to-slash-idbfs) -- Unity Issue Tracker fix adding autoSync option
- [ObjectPool DefaultCapacity vs MaxSize](https://discussions.unity.com/t/objectpool-maxsize-vs-defaultcapacity/1698885) -- Clarification that defaultCapacity does NOT pre-allocate
- [Space 3D Skybox Generator](https://discussions.unity.com/t/space-skybox-generator-neat-resource-i-found/810216) -- Tool for generating space cubemap textures
- [Large Number Formatting for Idle Games](https://blog.innogames.com/dealing-with-huge-numbers-in-idle-games/) -- InnoGames engineering blog on idle number systems
- [GetSingleton from MonoBehaviour](https://discussions.unity.com/t/how-to-get-an-singleton-entity-from-a-monobehavior/901711) -- Accessing ECS singletons from managed code

### Tertiary (LOW confidence)
- [Eliot's WebGL Notes for 2026](https://discussions.unity.com/t/eliots-webgl-notes-for-2026/1701530) -- Community WebGL tips (could not access full content)
- [ECS Custom Bootstrap Discussion](https://discussions.unity.com/t/how-to-do-custom-bootstrap-in-unity-ecs/941643) -- Fallback if auto-bootstrap fails on WebGL

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- Entities 1.4.4 versions confirmed, ObjectPool API documented, URP post-processing parameters documented
- Architecture: MEDIUM-HIGH -- Singleton pattern, ISystem/IJobEntity patterns are officially documented. Bridge pattern is community-synthesized but widely used. Procedural entity creation without SubScenes is less documented but confirmed working.
- Pitfalls: HIGH -- WebGL constraints confirmed by official docs. SubScene issue confirmed by community and issue tracker. Burst limitation confirmed. ObjectPool pre-warming gap documented.
- WebGL performance estimates: MEDIUM -- 5-10x managed penalty is directional, not calibrated to this specific game. Phase 1 benchmark will resolve.

**Research date:** 2026-02-17
**Valid until:** 2026-04-17 (60 days -- Unity 6.3 LTS is stable; Entities 1.4 is released)
