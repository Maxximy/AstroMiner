# Phase 3: Collection, Economy, and Session - Research

**Researched:** 2026-02-17
**Domain:** ECS mineral lifecycle, economy data model, session state machine, JSON save persistence (desktop + WebGL)
**Confidence:** HIGH

## Summary

Phase 3 closes the core game loop by connecting asteroid destruction to mineral collection, credit accumulation, timed session flow, and persistent save data. The phase builds directly on Phase 2's AsteroidDestructionSystem (the trigger point for mineral spawning) and the established Hybrid ECS patterns: Burst-compiled ISystem for hot-path mineral movement, singleton components for session/economy state, DynamicBuffer events for ECS-to-MonoBehaviour communication, and pooled GameObjects for mineral rendering.

The codebase already has all the infrastructure needed: GameObjectPool for mineral visuals, GamePhase state machine with Playing/Collecting/GameOver/Upgrading transitions, NumberFormatter for K/M/B/T credit display, WebGLHelper with IndexedDB sync for save flushing, and UISetup's programmatic UI creation pattern. The primary new work is: (1) mineral ECS entities with pull-toward-ship physics, (2) resource tier data model via ScriptableObject, (3) session timer driving phase transitions, (4) results/upgrade UI screens, and (5) a JSON save system using File.IO on both desktop and WebGL with IndexedDB flush.

**Primary recommendation:** Build mineral entities as a new ECS archetype with MineralTag + pull physics components, use the existing AsteroidDestructionSystem as the spawn trigger (detect HP <= 0 before destroying, spawn minerals via ECB), add ResourceTierSO ScriptableObject for data-driven credit values, extend GameStateData singleton with timer/credits-this-run, and implement SaveManager as a MonoBehaviour serializing a plain C# SaveData class to JSON via File.WriteAllText + WebGLHelper.FlushSaveData().

## Standard Stack

### Core (Already Installed)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.entities | 1.4.4 | ECS framework for mineral entities and systems | Already in project; all gameplay entities use DOTS |
| com.unity.mathematics | (entities dep) | float2/float3 math for pull physics, distance checks | Burst-compatible math types |
| com.unity.burst | (entities dep) | Compile mineral movement/collection to native WASM | Required for 60 FPS at 1000 minerals on WebGL |
| UnityEngine.JsonUtility | Built-in | Serialize SaveData to JSON | No external dependency needed; supports all required types |
| TMPro | Built-in (URP dep) | UI text for credits, timer, results | Already used by DebugOverlay/UISetup |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| GameObjectPool | Custom (exists) | Pool mineral visuals like asteroid spheres | Mineral rendering -- reuse existing pool class |
| WebGLHelper | Custom (exists) | Flush IndexedDB after save via .jslib | Every save operation on WebGL |
| NumberFormatter | Custom (exists) | Format credit display as K/M/B/T | HUD credit display, results screen |
| FadeController | Custom (exists) | Fade transitions between game phases | Collecting->GameOver and GameOver->Upgrading transitions |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| JsonUtility | Newtonsoft JSON | More features (dictionaries, LINQ), but adds dependency and WebGL bundle size. JsonUtility sufficient for flat SaveData class |
| PlayerPrefs for save | File.WriteAllText to persistentDataPath | PlayerPrefs has 1MB limit on WebGL. File.IO works on WebGL via Emscripten virtual FS + IndexedDB sync. File.IO chosen per CLAUDE.md/SAVE-01 requirement |
| ScriptableObject for tiers | GameConstants const fields | GameConstants is Burst-accessible but static; ScriptableObjects allow in-editor tuning and future level-specific drop tables (Phase 6). Use SO for tier config. |
| Unity Physics for mineral pull | Custom ISystem pull math | Unity Physics (com.unity.physics) adds heavyweight collision detection. Mineral collection only needs simple distance check + acceleration math. Custom is correct. |

**No new packages to install.** All dependencies are already in the project.

## Architecture Patterns

### Recommended File Structure (New Files for Phase 3)

```
Assets/Scripts/
├── ECS/
│   ├── Components/
│   │   └── MineralComponents.cs         # MineralTag, MineralData, CollectionEvent
│   └── Systems/
│       ├── MineralSpawnSystem.cs         # Detects HP<=0 asteroids, spawns minerals via ECB
│       ├── MineralPullSystem.cs          # Accelerates minerals toward ship (IJobEntity)
│       ├── MineralCollectionSystem.cs    # Distance check, awards credits, destroys mineral
│       └── SessionTimerSystem.cs         # Countdown timer, triggers phase transitions
├── MonoBehaviours/
│   ├── Core/
│   │   └── MineralRenderer.cs           # ECS-to-pooled-GameObject sync (mirrors AsteroidRenderer)
│   ├── UI/
│   │   ├── HUDController.cs             # Credits + timer display during Playing/Collecting
│   │   ├── ResultsScreen.cs             # Credits earned this run, Continue button
│   │   └── UpgradeScreen.cs             # Placeholder for Phase 6 tech tree, Start Run button
│   └── Save/
│       ├── SaveManager.cs               # JSON save/load, auto-save triggers
│       └── SaveData.cs                  # [Serializable] class with version, credits, etc.
├── Data/
│   └── ResourceTierSO.cs               # ScriptableObject: tier name, credit value, color
└── Shared/
    └── GameConstants.cs                 # (extend) Add session timer, mineral pull constants
```

### Pattern 1: Mineral Spawn from Asteroid Destruction

**What:** When an asteroid reaches 0 HP, a system detects this BEFORE the entity is destroyed, reads the asteroid's position, and spawns 3-8 mineral entities at that position via ECB. The mineral count and resource tier come from the asteroid's data.

**When to use:** Every time an asteroid is destroyed by mining damage.

**Implementation approach:** Create a new MineralSpawnSystem that runs BEFORE AsteroidDestructionSystem (use `[UpdateBefore(typeof(AsteroidDestructionSystem))]`). It queries asteroids with `CurrentHP <= 0` and `AsteroidTag`, spawns mineral entities via ECB, then lets the existing AsteroidDestructionSystem handle the actual entity destruction.

```csharp
// ECS/Systems/MineralSpawnSystem.cs
[BurstCompile]
[UpdateBefore(typeof(AsteroidDestructionSystem))]
public partial struct MineralSpawnSystem : ISystem
{
    private Unity.Mathematics.Random _rng;

    public void OnCreate(ref SystemState state)
    {
        _rng = new Unity.Mathematics.Random((uint)System.Environment.TickCount | 1u);
        state.RequireForUpdate<GameStateData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        // Spawn minerals during Playing AND Collecting (timer expired but still collecting)
        if (gameState.Phase != GamePhase.Playing && gameState.Phase != GamePhase.Collecting)
            return;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, transform, entity) in
            SystemAPI.Query<RefRO<HealthData>, RefRO<LocalTransform>>()
                .WithAll<AsteroidTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.CurrentHP <= 0f)
            {
                // Spawn N minerals at asteroid's position with slight random offsets
                int mineralCount = _rng.NextInt(GameConstants.MinMineralsPerAsteroid,
                    GameConstants.MaxMineralsPerAsteroid + 1);

                for (int i = 0; i < mineralCount; i++)
                {
                    var mineralEntity = ecb.CreateEntity();
                    ecb.AddComponent(mineralEntity, new MineralTag());
                    ecb.AddComponent(mineralEntity, new MineralData
                    {
                        ResourceTier = 0, // Iron default; tier from asteroid data in Phase 6
                        CreditValue = GameConstants.DefaultCreditValuePerMineral
                    });
                    ecb.AddComponent(mineralEntity, new MineralPullData
                    {
                        CurrentSpeed = GameConstants.MineralInitialSpeed,
                        Acceleration = GameConstants.MineralAcceleration
                    });

                    // Random offset from asteroid center
                    float2 offset = _rng.NextFloat2(new float2(-0.5f), new float2(0.5f));
                    float3 spawnPos = transform.ValueRO.Position + new float3(offset.x, 0f, offset.y);
                    ecb.AddComponent(mineralEntity, LocalTransform.FromPosition(spawnPos));
                    ecb.AddComponent(mineralEntity, new LocalToWorld());
                }
            }
        }
    }
}
```

**Confidence:** HIGH -- Uses the exact same ECB entity creation pattern as AsteroidSpawnSystem (already working). UpdateBefore ordering is standard DOTS.

### Pattern 2: Mineral Pull-Toward-Ship Physics (IJobEntity)

**What:** Minerals accelerate toward the ship position each frame. Speed increases over time (pull effect). When within collection radius, a separate system awards credits and destroys the mineral.

**When to use:** Every frame while mineral entities exist (Playing and Collecting phases).

**Implementation approach:** Use IJobEntity for Burst-compiled parallel processing of 1000+ minerals. The ship position is a known constant (`GameConstants.ShipPositionZ`), so no singleton lookup needed inside the job.

```csharp
// ECS/Systems/MineralPullSystem.cs
[BurstCompile]
[WithAll(typeof(MineralTag))]
public partial struct MineralPullJob : IJobEntity
{
    public float DeltaTime;
    public float3 ShipPosition;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref MineralPullData pull)
    {
        // Direction toward ship on XZ plane
        float3 dir = ShipPosition - transform.Position;
        float dist = math.length(dir);
        if (dist > 0.01f)
        {
            float3 normalizedDir = dir / dist;
            // Accelerate over time for satisfying "pull" feel
            pull.CurrentSpeed += pull.Acceleration * DeltaTime;
            transform.Position += normalizedDir * pull.CurrentSpeed * DeltaTime;
        }
    }
}
```

**Confidence:** HIGH -- Mirrors the AsteroidDriftJob pattern with WithAll tag filter.

### Pattern 3: DynamicBuffer Collection Events for UI Bridge

**What:** When a mineral is collected (reaches ship), the collection system writes a CollectionEvent to a DynamicBuffer on a singleton entity. The MonoBehaviour HUD drains this buffer in LateUpdate to update credit display and play future SFX.

**When to use:** Any time ECS needs to communicate discrete events to the MonoBehaviour layer (collection, destruction, damage).

**Implementation approach:** Create a singleton entity with a DynamicBuffer<CollectionEvent> in ECSBootstrap. MineralCollectionSystem appends events. HUDController drains in LateUpdate.

```csharp
// ECS/Components/MineralComponents.cs
public struct CollectionEvent : IBufferElementData
{
    public int ResourceTier;
    public int CreditValue;
    public float3 Position;
}

// MonoBehaviours/UI/HUDController.cs (LateUpdate)
var buffer = _em.GetBuffer<CollectionEvent>(_eventEntity);
for (int i = 0; i < buffer.Length; i++)
{
    _creditsThisRun += buffer[i].CreditValue;
}
buffer.Clear();
```

**Confidence:** HIGH -- This is the established event buffer pattern from the ARCHITECTURE.md research, and the same approach used for AudioEventBridge in the architecture plan.

### Pattern 4: Session Timer via ECS Singleton + MonoBehaviour State Machine

**What:** The run timer counts down in GameStateData.Timer. When it reaches 0, the GameManager transitions Playing -> Collecting. When all minerals are collected, transitions Collecting -> GameOver. The timer could be managed by either an ECS system or the MonoBehaviour state machine.

**Recommendation:** Use the MonoBehaviour PlayingState.Execute() to decrement GameStateData.Timer and trigger the transition. This keeps phase transition logic in the state machine (where it belongs) rather than in ECS systems.

```csharp
// States/PlayingState.cs
public void Execute(GameManager manager)
{
    var world = World.DefaultGameObjectInjectionWorld;
    var em = world.EntityManager;
    var query = em.CreateEntityQuery(typeof(GameStateData));
    var entity = query.GetSingletonEntity();
    var data = em.GetComponentData<GameStateData>(entity);

    data.Timer -= Time.deltaTime;
    em.SetComponentData(entity, data);

    if (data.Timer <= 0f)
    {
        data.Timer = 0f;
        em.SetComponentData(entity, data);
        manager.TransitionTo(GamePhase.Collecting);
    }
}
```

**Confidence:** HIGH -- Timer management in state machine keeps phase transitions co-located. ECS systems only need to check `gameState.Phase` (already established pattern).

### Pattern 5: Save System with WebGL Compatibility

**What:** SaveData is a plain [Serializable] C# class containing all persistent state. SaveManager serializes to JSON via JsonUtility, writes to a file path, and calls WebGLHelper.FlushSaveData() on WebGL.

**Critical design decisions:**

1. **File path strategy:** Use `Application.persistentDataPath` for desktop. For WebGL, File.IO does work via Emscripten's virtual filesystem mapped to IndexedDB. The existing WebGLHelper.FlushSaveData() calls `FS.syncfs()` which is the standard pattern. However, SAVE-01 says "JSON in Application.persistentDataPath" -- this works on both platforms.

2. **SaveData schema:** Use a flat [Serializable] class. JsonUtility supports int, float, double, bool, string, and arrays/lists of [Serializable] types. For `long` credits: GameStateData already uses `long Credits` in ECS. JsonUtility's handling of `long` is ambiguous in docs. **Recommendation:** Use `long` in SaveData and test. If it fails, store as two ints or use `double` (which is explicitly supported and can represent integers up to 2^53 exactly -- more than enough for game credits).

3. **Auto-save triggers:** On run end (Collecting -> GameOver transition) and on tech tree purchase (Phase 6). SaveManager subscribes to GameManager phase changes.

```csharp
// MonoBehaviours/Save/SaveData.cs
[System.Serializable]
public class SaveData
{
    public int SaveVersion = 1;
    public long TotalCredits;
    public int CurrentLevel = 1;
    // Phase 6 will add: public bool[] TechTreeUnlocks;
    // Phase 6 will add: public float[] PlayerStats;
}

// MonoBehaviours/Save/SaveManager.cs
public class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "astrominer_save.json";

    public void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = System.IO.Path.Combine(Application.persistentDataPath, SaveFileName);
        System.IO.File.WriteAllText(path, json);
        WebGLHelper.FlushSaveData(); // no-op on desktop
    }

    public SaveData Load()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, SaveFileName);
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return new SaveData(); // fresh save
    }
}
```

**Confidence:** MEDIUM -- File.IO on WebGL works via Emscripten VFS but the CLAUDE.md says "No File.IO -- use PlayerPrefs only." This contradicts multiple verified sources showing File.WriteAllText works on WebGL when followed by FS.syncfs(). The project already has WebGLHelper with SyncIndexedDB jslib. **If File.IO fails on WebGL, fallback to PlayerPrefs.SetString("save", json).** Both approaches ultimately use IndexedDB under the hood.

### Pattern 6: Mineral Renderer (Mirrors AsteroidRenderer)

**What:** A MonoBehaviour that syncs ECS mineral entities to pooled GameObjects, exactly like AsteroidRenderer does for asteroids.

**When to use:** Every frame while mineral entities exist.

**Implementation approach:** Copy the AsteroidRenderer pattern: Dictionary<Entity, GameObject>, EntityQuery for MineralTag + LocalTransform, pool Get/Release lifecycle. Use smaller sphere primitives with tier-based colors via MaterialPropertyBlock.

**Confidence:** HIGH -- Direct replication of a proven pattern in the codebase.

### Anti-Patterns to Avoid

- **Do NOT put mineral entities and asteroid entities in the same archetype.** Use separate MineralTag vs AsteroidTag. Minerals have completely different movement (pull vs drift) and lifecycle (collection vs bounds-destroy). Separate archetypes allow Burst-compiled jobs to operate on only the relevant entity set.

- **Do NOT use coroutines for phase transitions.** The state machine pattern is already established. Phase transitions go through GameManager.TransitionTo() which handles fading. CollectingState should poll for "all minerals collected" in its Execute() method.

- **Do NOT store UI state in ECS components.** Results screen visibility, button states, and upgrade selection are pure MonoBehaviour concerns. Only credits/timer/phase live in ECS singletons.

- **Do NOT use Unity Physics for mineral collection.** The pull-toward-ship math is trivial (direction + speed + acceleration). Adding com.unity.physics would bring collision detection overhead for a simple distance check.

- **Do NOT save ECS entity state.** Only save progression data (credits, unlocks, level). Entities are ephemeral and regenerated each run.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Number formatting (K/M/B/T) | Custom formatter | `NumberFormatter.Format()` (exists) | Already built in Phase 1, tested, handles edge cases |
| Object pooling for mineral visuals | Custom pool | `GameObjectPool` (exists) | Already built in Phase 1, pre-warming, proper lifecycle |
| Fade transitions between phases | Custom alpha lerp | `FadeController` (exists) | Already built in Phase 1, coroutine-based with SmoothStep |
| IndexedDB flush on WebGL | Custom jslib | `WebGLHelper.FlushSaveData()` (exists) | Already built in Phase 1 with SyncIndexedDB.jslib |
| Game phase state machine | Custom FSM | `IGameState` + `GameManager` (exists) | Already built in Phase 1, dictionary-based with transitions |
| UI hierarchy creation | Manual scene setup | `UISetup` pattern (exists) | Programmatic UI creation avoids scene editor dependency |

**Key insight:** Phase 3 heavily leverages Phase 1 infrastructure. The majority of supporting systems are already built -- the new work is the gameplay logic connecting them.

## Common Pitfalls

### Pitfall 1: MineralSpawnSystem Races AsteroidDestructionSystem

**What goes wrong:** If MineralSpawnSystem runs AFTER AsteroidDestructionSystem, the asteroid entity is already destroyed when the spawn system tries to read its position. Minerals spawn at origin or not at all.

**Why it happens:** ECS system execution order is not guaranteed unless explicitly specified.

**How to avoid:** Use `[UpdateBefore(typeof(AsteroidDestructionSystem))]` on MineralSpawnSystem. This ensures the spawn system reads asteroid data before destruction.

**Warning signs:** Minerals appearing at position (0,0,0) or not appearing at all when asteroids are destroyed.

### Pitfall 2: ECB Playback Timing for Mineral Spawn + Destroy in Same Frame

**What goes wrong:** Using the same ECB (EndSimulationEntityCommandBufferSystem) for both creating minerals and destroying asteroids in the same frame could cause issues if both systems create their own ECBs and one runs before the other.

**Why it happens:** Both systems get ECBs from EndSimulationEntityCommandBufferSystem.Singleton. ECB commands are played back in the order they were recorded across all systems that used that ECB system.

**How to avoid:** This actually works correctly with the UpdateBefore ordering. MineralSpawnSystem records mineral creation commands first, then AsteroidDestructionSystem records destroy commands. During playback, creates happen before destroys. Both using EndSimulation ECB is fine.

**Warning signs:** None expected if ordering is correct.

### Pitfall 3: Collecting State Never Transitions to GameOver

**What goes wrong:** The Collecting state waits for "all minerals collected" but the mineral entity count never reaches zero because: (a) minerals are still being spawned from asteroids dying during collection, or (b) the entity query counts stale entities from pending ECB commands.

**Why it happens:** During Collecting, the MiningDamageSystem should stop dealing damage (already guarded by `Phase != GamePhase.Playing`), but asteroids that were damaged below 0 HP in the last Playing frame still trigger MineralSpawnSystem.

**How to avoid:** In CollectingState.Execute(), check for zero mineral entities AND wait a grace period (1-2 seconds after last mineral collected) or check that both asteroid and mineral counts are stable. Simplest: also stop asteroid spawning (already guarded by Phase check in AsteroidSpawnSystem), let existing asteroids drift off-screen (AsteroidBoundsSystem still runs), and wait for mineral count to hit zero.

**Warning signs:** Game stuck in Collecting state forever.

### Pitfall 4: Credits Display Overflow or Precision Loss

**What goes wrong:** Credits stored as `long` in ECS but displayed via NumberFormatter which takes `double`. Conversion from `long` to `double` loses precision above 2^53 (~9 quadrillion). For an idle game, credits can grow very large.

**Why it happens:** IEEE 754 double has 53 bits of mantissa.

**How to avoid:** For v1, this is acceptable. Credits reaching 9 quadrillion would require extensive play. NumberFormatter already handles the Qa/Qi suffixes. If precision matters later, switch to custom BigNumber class. For now, `long` in ECS (max 9.2 * 10^18) with `double` conversion for display is fine.

**Warning signs:** Credit display showing incorrect values at very high amounts (post-v1 concern).

### Pitfall 5: WebGL Save File Path Changes Between Builds

**What goes wrong:** Application.persistentDataPath on WebGL includes a hash of the URL. If the game is redeployed to a different URL, saves are lost.

**Why it happens:** Emscripten IDBFS maps persistentDataPath to `/idbfs/<MD5(URL)>`.

**How to avoid:** For v1, accept this limitation. If deploying to itch.io or similar, use a fixed custom path like `"/idbfs/astrominer"` instead of `Application.persistentDataPath` on WebGL. This can be a conditional compile:
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    string dir = "/idbfs/astrominer";
#else
    string dir = Application.persistentDataPath;
#endif
```

**Warning signs:** Save data disappears after redeployment.

### Pitfall 6: Long Serialization with JsonUtility

**What goes wrong:** GameStateData uses `long Credits`. The SaveData class needs to store credits as `long`. Unity's serialization documentation does not explicitly list `long` as a supported type for JsonUtility, though `SerializedPropertyNumericType.Int64` exists.

**Why it happens:** Ambiguous documentation.

**How to avoid:** Test JsonUtility with `long` fields early. If it fails, use `double` for credits in SaveData (exact for integers up to 2^53, which is 9,007,199,254,740,992 -- more than enough). Alternatively, store as two `int` fields (high/low).

**Warning signs:** Credits value of 0 after load despite having saved a non-zero value.

## Code Examples

### ResourceTierSO -- Data-Driven Credit Values

```csharp
// Data/ResourceTierSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceTier", menuName = "AstroMiner/Resource Tier")]
public class ResourceTierSO : ScriptableObject
{
    public string TierName;
    public int TierIndex; // 0=Iron, 1=Copper, 2=Silver, 3=Cobalt, 4=Gold, 5=Titanium
    public int CreditValue; // Credits per mineral of this tier
    public int MineralsPerAsteroid; // How many minerals spawn from this tier
    public Color MineralColor; // Visual color for mineral particles
    [Range(0f, 4f)]
    public float EmissiveIntensity; // HDR glow for rare tiers
}
```

For Phase 3, only Tier 0 (Iron) needs to exist. Higher tiers are Phase 6 content. The ScriptableObject structure is defined now to establish the pattern, but Burst-compiled systems cannot read ScriptableObjects directly. The tier data must be copied into an ECS component (MineralData.CreditValue) at spawn time.

### Mineral ECS Components

```csharp
// ECS/Components/MineralComponents.cs
using Unity.Entities;
using Unity.Mathematics;

public struct MineralTag : IComponentData { }

public struct MineralData : IComponentData
{
    public int ResourceTier;
    public int CreditValue;
}

public struct MineralPullData : IComponentData
{
    public float CurrentSpeed;
    public float Acceleration;
}

// Event buffer element for collection events
public struct CollectionEvent : IBufferElementData
{
    public int ResourceTier;
    public int CreditValue;
    public float3 Position;
}
```

### Session Timer Constants

```csharp
// Add to GameConstants.cs
public const float DefaultRunDuration = 60f; // seconds per run
public const float CollectingGracePeriod = 2f; // seconds after last mineral before GameOver

// Mineral physics
public const int MinMineralsPerAsteroid = 3;
public const int MaxMineralsPerAsteroid = 8;
public const int DefaultCreditValuePerMineral = 10;
public const float MineralInitialSpeed = 1f;
public const float MineralAcceleration = 3f;
public const float MineralCollectionRadius = 0.8f; // distance from ship to collect
public const float MineralScale = 0.3f; // visual size of mineral spheres
```

### HUD Creation in UISetup Pattern

```csharp
// Extend UISetup.Awake() or create new HUDSetup
// Following existing pattern: programmatic UI creation
private void CreateHUDCanvas()
{
    var hudCanvasGO = new GameObject("HUDCanvas");
    hudCanvasGO.transform.SetParent(transform);

    var canvas = hudCanvasGO.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 10;

    // Credits display (top-right)
    var creditsText = CreateTMPText("CreditsText", "0", hudCanvasGO.transform);
    // Anchor top-right, fontSize 24

    // Timer display (top-center)
    var timerText = CreateTMPText("TimerText", "60", hudCanvasGO.transform);
    // Anchor top-center, fontSize 32
}
```

### Save/Load Flow

```csharp
// Called by GameManager when transitioning to GameOver
public void AutoSave()
{
    var data = new SaveData();

    // Read from ECS singleton
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    var query = em.CreateEntityQuery(typeof(GameStateData));
    var gameState = query.GetSingleton<GameStateData>();

    data.TotalCredits = gameState.Credits;
    data.CurrentLevel = 1; // Phase 6 adds level tracking
    data.SaveVersion = 1;

    Save(data);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| PlayerPrefs for all save data | File.WriteAllText + IndexedDB sync | Unity 2020+ WebGL | File.IO works on WebGL via Emscripten VFS. PlayerPrefs has 1MB limit. File.IO is the modern approach for larger save data. |
| Companion GameObjects on every entity | Pooled GameObject renderers synced from ECS | Entities 1.0+ (no Entities Graphics on WebGL) | AsteroidRenderer pattern is established; MineralRenderer follows same pattern |
| SystemBase for all systems | ISystem + Burst for hot paths | Entities 1.0+ recommendation | MineralPullSystem and MineralCollectionSystem must be ISystem for 1000+ mineral performance |

**Deprecated/outdated:**
- `Application.ExternalEval("FS.syncfs()")` -- replaced by .jslib DllImport pattern (already in WebGLHelper)
- `EntityManager.CreateEntity()` in systems -- use ECB instead for structural changes

## Phase Requirements

<phase_requirements>

| ID | Description | Research Support |
|----|-------------|-----------------|
| MINR-01 | Destroyed asteroids release mineral particles (children detach) | MineralSpawnSystem pattern: runs before AsteroidDestructionSystem, detects HP<=0, spawns N mineral entities at asteroid position via ECB. Pattern 1 code example. |
| MINR-02 | Detached minerals accelerate toward the player's ship | MineralPullSystem with IJobEntity: direction toward ship constant, speed increases via acceleration each frame. Pattern 2 code example. |
| MINR-03 | Minerals collected on contact with ship award credits based on resource tier | MineralCollectionSystem: distance check against ship position (GameConstants.ShipPositionZ), reads MineralData.CreditValue, increments GameStateData.Credits, writes CollectionEvent to buffer. |
| MINR-04 | Mineral particles have tier-based visual appearance (color, emissive glow for rare) | MineralRenderer mirrors AsteroidRenderer: MaterialPropertyBlock sets _BaseColor from tier color. ResourceTierSO defines color and emissive intensity per tier. Phase 3 uses single tier (Iron); full tier visuals in Phase 6. |
| ECON-01 | Credits are the universal currency converted from all collected minerals | GameStateData.Credits (long) stores running total. MineralCollectionSystem adds MineralData.CreditValue on collection. Single currency type. |
| ECON-02 | Running credit total displayed during gameplay | HUDController reads GameStateData.Credits in LateUpdate, formats with NumberFormatter.Format(), displays via TMPro text. |
| ECON-03 | Credits persist between runs and are spent in the tech tree | SaveData.TotalCredits persisted via SaveManager. On run start, credits loaded from save. Tech tree spending deferred to Phase 6. |
| ECON-04 | Credit values per resource tier are data-driven (ScriptableObject/config) | ResourceTierSO ScriptableObject with CreditValue field. Values copied into MineralData.CreditValue at spawn time. For Phase 3, single tier; Phase 6 adds full tier config. Alternative: use GameConstants for Phase 3 simplicity, migrate to SO in Phase 6. |
| SESS-01 | Timed mining runs with visible countdown timer | GameStateData.Timer initialized to DefaultRunDuration on run start. PlayingState.Execute() decrements each frame. HUDController displays formatted time. |
| SESS-02 | When timer expires, transition to Collecting state (no more damage, minerals still pulled) | PlayingState.Execute() calls TransitionTo(Collecting) when Timer <= 0. MiningDamageSystem already guards on Phase==Playing. MineralPullSystem runs during Collecting. AsteroidSpawnSystem stops (Phase guard). |
| SESS-03 | When all minerals collected, transition to GameOver with results screen | CollectingState.Execute() counts MineralTag entities. When count == 0 (and asteroids also gone or grace period elapsed), calls TransitionTo(GameOver). |
| SESS-04 | Results screen shows credits earned this run | GameOverState.Enter() shows ResultsScreen UI. Credits-this-run tracked by HUDController (accumulated from CollectionEvents). Displayed via NumberFormatter. |
| SESS-05 | Player can proceed to Upgrade screen from results | ResultsScreen has Continue button. OnClick calls TransitionTo(Upgrading). |
| SESS-06 | Player can start new run from Upgrade screen | UpgradeScreen has Start Run button. OnClick resets GameStateData (timer, credits-this-run), destroys leftover entities, calls TransitionTo(Playing). |
| SAVE-01 | Game state saves to JSON in Application.persistentDataPath | SaveManager.Save() uses JsonUtility.ToJson() + File.WriteAllText() to persistentDataPath. WebGL uses Emscripten VFS mapped to IndexedDB. |
| SAVE-02 | Auto-save triggers on run end and on every tech tree purchase | SaveManager.AutoSave() called in GameOverState.Enter(). Tech tree purchase trigger deferred to Phase 6. |
| SAVE-03 | Save includes credits, tech tree unlock state, current level, player stats | SaveData class: TotalCredits, CurrentLevel, SaveVersion. TechTreeUnlocks and PlayerStats fields added in Phase 6 (stub empty arrays now). |
| SAVE-04 | WebGL builds use .jslib plugin to flush IndexedDB after each save | WebGLHelper.FlushSaveData() already exists and calls SyncIndexedDB from IndexedDBSync.jslib. Called after every File.WriteAllText(). |
| SAVE-05 | Save file includes version number for future migration support | SaveData.SaveVersion = 1. On load, check version and apply migration if needed (no migrations for v1). |

</phase_requirements>

## Open Questions

1. **File.IO on WebGL: Does it actually work in Unity 6000.3?**
   - What we know: Multiple community sources confirm File.WriteAllText works on WebGL via Emscripten virtual filesystem. The project's CLAUDE.md says "No File.IO -- use PlayerPrefs only." However, the project already has a .jslib for IndexedDB sync, suggesting the original plan anticipated file-based saves.
   - What's unclear: Whether Unity 6000.3 has changed WebGL filesystem behavior.
   - Recommendation: Implement with File.IO + WebGLHelper.FlushSaveData(). If it fails in WebGL testing, fall back to PlayerPrefs.SetString("save", json) which also maps to IndexedDB. Both approaches use the same underlying storage.

2. **Long (int64) serialization with JsonUtility**
   - What we know: Unity docs list "int, float, double, bool, string" as supported primitives. `long` is not explicitly listed but SerializedPropertyNumericType.Int64 exists. Community reports are mixed.
   - What's unclear: Whether JsonUtility.ToJson() handles `long` fields correctly in the current Unity version.
   - Recommendation: Test early. If it fails, use `double TotalCredits` in SaveData (exact up to 2^53) and cast to/from `long` in GameStateData. Or use two `int` fields.

3. **Mineral count target for Collecting -> GameOver transition**
   - What we know: SESS-03 says "when all minerals collected." But minerals might still be in-flight, or new ones might spawn from asteroids that were damaged below 0 HP in the final frame.
   - What's unclear: Exact transition condition robustness.
   - Recommendation: In Collecting state, wait for BOTH mineral entity count == 0 AND asteroid entity count == 0 (or use a grace timer of 1-2 seconds after last mineral collected). This handles edge cases cleanly.

4. **Resource tier data source for Phase 3 vs Phase 6**
   - What we know: ECON-04 requires data-driven credit values via ScriptableObject. Phase 3 only uses one tier (Iron). Phase 6 introduces the full 6-tier system with per-level drop tables.
   - What's unclear: Whether to build the full ScriptableObject system now or use GameConstants for Phase 3.
   - Recommendation: Define ResourceTierSO now to establish the pattern, but use GameConstants default values in MineralSpawnSystem for Phase 3. The SO infrastructure will be consumed by Phase 6's AsteroidSpawnSystem when tiers are introduced.

5. **Run reset mechanics: What happens to existing entities on new run start?**
   - What we know: When transitioning from Upgrading -> Playing (new run), any leftover asteroid/mineral entities from the previous run should be cleaned up.
   - What's unclear: Best approach -- destroy all tagged entities via ECB, or let them naturally expire?
   - Recommendation: In the PlayingState.Enter() or a dedicated RunResetSystem, query and destroy all entities with AsteroidTag or MineralTag via ECB. This ensures a clean slate for each run.

## Sources

### Primary (HIGH confidence)
- **Codebase analysis** -- All existing systems (AsteroidSpawnSystem, AsteroidDestructionSystem, AsteroidRenderer, GameObjectPool, GameManager, ECSBootstrap, WebGLHelper) read and analyzed directly
- [Entity command buffer overview | Entities 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) -- ECB patterns for entity creation/destruction
- [Unity Serialization rules](https://docs.unity3d.com/6000.2/Documentation/Manual/script-serialization-rules.html) -- Supported field types for JsonUtility

### Secondary (MEDIUM confidence)
- [Wayline: persistentDataPath Explained](https://www.wayline.io/blog/persistentdatapath-explained-how-to-store-data-in-unity) -- WebGL File.IO approach with custom IDBFS path and JS_FileSystem_Sync
- [Unity Discussions: How to save on WebGL](https://discussions.unity.com/t/how-to-save-and-load-on-webgl/919830) -- Community patterns for WebGL persistence
- [Unity Discussions: Designing event system for ECS](https://discussions.unity.com/t/designing-an-event-system-for-ecs/824936) -- DynamicBuffer event pattern validation

### Tertiary (LOW confidence)
- JsonUtility `long` support -- Not explicitly confirmed in official docs. Community reports suggest it works but official docs only list "int, float, double, bool, string." Needs validation by testing.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- All packages already installed; no new dependencies needed
- Architecture: HIGH -- All patterns are direct extensions of proven codebase patterns (AsteroidRenderer -> MineralRenderer, AsteroidSpawnSystem -> MineralSpawnSystem, etc.)
- Pitfalls: HIGH -- Identified from direct codebase analysis (system ordering, phase transitions, save limitations)
- Save system: MEDIUM -- WebGL File.IO works per community sources but contradicts CLAUDE.md guidance; `long` serialization needs testing

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (stable -- no fast-moving dependencies)
