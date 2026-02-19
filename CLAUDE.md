# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AstroMiner is an idle/clicker asteroid mining game built in **Unity 6.3 LTS (6000.3.8f1)** using a **Hybrid ECS (DOTS)** architecture. Players control a mining circle with the mouse to damage drifting asteroids, collect minerals, earn credits, and upgrade through a tech tree. Targets Windows desktop and WebGL. v1.0 MVP is complete.

## Build & Run

- Open in Unity Hub (version 6000.3.8f1)
- Main scene: `Assets/Scenes/Game.unity`
- No CLI build commands — build via Unity Editor (File → Build Settings)
- WebGL builds require `Assets/Plugins/WebGL/IndexedDBSync.jslib` for save persistence
- CoPlay Unity MCP (`com.coplaydev.coplay` package) is available for editor automation

## Architecture: Hybrid ECS

Three-layer architecture with strict boundaries:

**ECS Layer (DOTS)** — All gameplay simulation
- `Assets/Scripts/ECS/Components/` — Unmanaged `IComponentData` structs
- `Assets/Scripts/ECS/Systems/` — `ISystem` (Burst-compiled) and `SystemBase` (bridge logic)
- No Authoring/Baker at runtime — all singletons created imperatively in `ECSBootstrap.Start()`

**GameObject Layer (MonoBehaviour)** — UI, audio, camera, rendering
- `Assets/Scripts/MonoBehaviours/Core/` — GameManager, ECSBootstrap, UISetup, renderers
- `Assets/Scripts/MonoBehaviours/UI/` — HUD, results, tech tree, skill bar
- `Assets/Scripts/MonoBehaviours/Rendering/` — VFX, damage popups, camera shake
- `Assets/Scripts/MonoBehaviours/Save/` — SaveManager (JSON + PlayerPrefs)
- `Assets/Scripts/MonoBehaviours/Pool/` — GameObjectPool for entity rendering

**Bridge Layer** — Bidirectional ECS ↔ GameObject communication
- `Assets/Scripts/MonoBehaviours/Bridge/` — InputBridge, FeedbackEventBridge
- Input: `InputBridge.Update()` writes mouse world pos + skill key presses → ECS singletons
- Events: `FeedbackEventBridge.LateUpdate()` drains `DynamicBuffer` singletons → dispatches to VFX/audio managers
- Stats: MonoBehaviours read ECS singletons directly for UI updates

**Shared** — `Assets/Scripts/Shared/` for constants/enums (no layer dependencies)
**Data** — `Assets/Scripts/Data/` for ScriptableObject definitions + static `*Definitions` classes
**States** — `Assets/Scripts/States/` for game state machine (GoF State pattern)

## Game Loop & State Machine

`GameManager` holds a `Dictionary<GamePhase, IGameState>`. Phase flow:

**Playing** (timed run, default 20s) → **Collecting** (minerals auto-pull to center) → **GameOver** (results + auto-save) → **Upgrading** (tech tree spend) → **Playing** (new run)

- All transitions except Playing→Collecting use fade-to-black via `FadeController`
- Playing→Collecting is immediate (gameplay stays visible during mineral collection)
- Credits persist across runs; timer/asteroids/minerals reset each run
- `PlayingState.Enter()` calls `ApplyLevelConfig()` to set tier weights and HP scaling from `LevelConfigDefinitions`

## Key Architectural Rules

- **New Input System only** — Use `UnityEngine.InputSystem` (`Mouse.current`, `Keyboard.current`), NEVER the old `UnityEngine.Input` API
- **No Entities Graphics package** — WebGL incompatible. All rendering via SpriteRenderer/MeshRenderer synced from ECS data through `Dictionary<Entity, GameObject>` mapping with object pooling
- **ISystem + Burst** for hot paths (movement, damage, collection). Must use only unmanaged `IComponentData`
- **SystemBase** only for bridge/presentation systems needing managed types
- **Singleton components** for cross-layer state (`GameStateData`, `InputData`, `MiningConfigData`, etc.)
- **DynamicBuffer event pattern** for ECS→GameObject events. Each buffer type lives on its own dedicated empty entity (created in `ECSBootstrap`), accessed via `SystemAPI.GetSingletonBuffer<T>()`
- **ECB for structural changes** — Always use `EndSimulationEntityCommandBufferSystem.Singleton` in Burst-compiled `OnUpdate`, never direct `em.CreateEntity/DestroyEntity`
- **Phase gating** — Every ECS system checks `GameStateData.Phase` and early-returns if not relevant
- **XZ plane** — All gameplay on Y=0. Mouse world pos is `float2(x, z)`. Play area: X ±12, Z -8 to +12
- **No System.Threading** — WebGL is single-threaded
- **No companion GameObjects** on ECS entities
- **UI stays in MonoBehaviours** — never create ECS components for UI state
- **EntityManager access only on main thread** in Update/LateUpdate, never from coroutines

## Critical Patterns

### ECSBootstrap Singleton Creation
All singletons created imperatively in `ECSBootstrap.Start()` via `em.CreateEntity(typeof(T))` + `em.SetComponentData`. Buffer carrier entities created separately. No subscenes or Authoring/Baker for singletons.

### UISetup Reflection Wiring
`UISetup.Awake()` creates the entire UI hierarchy programmatically (no prefabs). Private `[SerializeField]` fields on MonoBehaviours are set via reflection:
```csharp
typeof(FadeController).GetField("FadeCanvasGroup", NonPublic | Instance).SetValue(fade, canvasGroup);
```
Field name strings must **exactly match** the `[SerializeField] private` field names (PascalCase, no underscore prefix).

### SkillInputData OR Pattern
`InputBridge` reads the current `SkillInputData` singleton first, then OR-masks keyboard presses on top (`|=`). Never overwrite with a fresh struct — `SkillBarController` (UI buttons) may have already set flags this frame.

### MineralsSpawnedTag Guard
Prevents double mineral spawns from the same asteroid across ECB playback frames. A dead asteroid persists one extra frame while ECB queues its destruction.

### Static Definitions (Not ScriptableObject Assets)
Runtime data comes from static `*Definitions` classes, not `.asset` files:
- `TechTreeDefinitions.BuildTree()` — creates ~40 `UpgradeNodeSO` instances at runtime via `ScriptableObject.CreateInstance`
- `LevelConfigDefinitions.GetLevelConfig(int)` — 5 levels with tier weights and HP scaling
- `ResourceTierDefinitions` — 6 tiers: Iron→Copper→Silver→Cobalt→Gold→Titanium (10→1000 credits)

### Save System Migration
`SaveManager` uses JSON via `JsonUtility`. When adding new `SaveData` fields, increment `SaveVersion` and add migration in `MigrateIfNeeded()`. Missing fields deserialize as `default(T)` — use `< 0.1f` guards (not `== 0f`) for float defaults. WebGL calls `WebGLHelper.FlushSaveData()` to sync IndexedDB.

### Random in Burst Systems
`Unity.Mathematics.Random` stored as field on `ISystem` struct. Seeded with `(uint)System.Environment.TickCount | Xu` (OR with different constants per system to avoid identical seeds).

### Rendering Bridge Pattern
`AsteroidRenderer`/`MineralRenderer` maintain `Dictionary<Entity, GameObject>`. In `LateUpdate`: query ECS entities, assign pooled GameObjects to new entities, return destroyed entities to pool, sync transforms. Visual randomness applied once at assignment via `MaterialPropertyBlock`.

## WebGL Constraints

- Burst compiles to WASM (~2-10x slower than native)
- Jobs run sequentially on main thread
- No `File.IO` — save via PlayerPrefs (maps to IndexedDB)
- Audio requires user interaction before first playback
- Target: 60 FPS with 100 asteroids + 1000 minerals
- No `com.unity.physics` — collision done with `math.distancesq` checks

## Naming Conventions

- **Components**: PascalCase with `Data` or `Component` suffix (`GameStateData`, `HealthData`)
- **Systems**: PascalCase + `System` suffix (`AsteroidMovementSystem`)
- **Private fields**: camelCase (`activeCoroutine`)
- **`[SerializeField] private`**: PascalCase like public fields (`FadeCanvasGroup`) — no underscore prefix
- **Enums**: PascalCase (`GamePhase`), byte-backed for Burst compatibility where needed
- **Scripts**: PascalCase matching class name

## Key Dependencies

| Package | Purpose |
|---------|---------|
| com.unity.entities 1.4.4 | ECS/DOTS framework |
| com.unity.render-pipelines.universal 17.3.0 | URP rendering |
| com.unity.inputsystem 1.18.0 | Mouse/keyboard input (new Input System) |
| com.unity.ugui 2.0.0 | UI framework |
| com.coplaydev.coplay (git #beta) | Editor automation MCP |

## Planning & Documentation

- `.planning/PROJECT.md` — Project overview, requirements, key decisions
- `.planning/ROADMAP.md` — Phase plan with success criteria
- `.planning/STATE.md` — Current progress and session continuity
- `.planning/REQUIREMENTS.md` — 83 requirements mapped to phases
- `Docs/Astrominer_GDD.md` — Authoritative game design document
