# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AstroMiner is an idle/clicker asteroid mining game built in **Unity 6.3 LTS (6000.3.8f1)** using a **Hybrid ECS (DOTS)** architecture. Players control a mining circle with the mouse to damage drifting asteroids, collect minerals, earn credits, and upgrade through a tech tree. Targets Windows desktop and WebGL.

## Build & Run

- Open in Unity Hub (version 6000.3.8f1)
- Main scene: `Assets/Scenes/Game.unity`
- WebGL builds require `Assets/Plugins/WebGL/IndexedDBSync.jslib` for save persistence
- CoPlay Unity MCP is available for editor automation (scene setup, asset manipulation)

## Architecture: Hybrid ECS

Three-layer architecture with strict boundaries:

**ECS Layer (DOTS)** — All gameplay simulation (asteroids, minerals, damage, economy)
- `Assets/Scripts/ECS/Components/` — Unmanaged `IComponentData` structs
- `Assets/Scripts/ECS/Systems/` — `ISystem` (Burst-compiled hot paths) and `SystemBase` (bridge logic needing managed access)
- `Assets/Scripts/ECS/Authoring/` — Baker/Authoring scripts for entity conversion

**GameObject Layer (MonoBehaviour)** — UI, audio, camera, rendering
- `Assets/Scripts/MonoBehaviours/Core/` — GameManager, Bootstrap
- `Assets/Scripts/MonoBehaviours/UI/` — uGUI Canvas controllers
- `Assets/Scripts/MonoBehaviours/Audio/` — AudioManager, SFX
- `Assets/Scripts/MonoBehaviours/Camera/` — CameraController
- `Assets/Scripts/MonoBehaviours/Save/` — SaveManager (PlayerPrefs + JSON)

**Bridge Layer** — Bidirectional ECS ↔ GameObject communication
- `Assets/Scripts/MonoBehaviours/Bridge/` — InputBridge, UIDataBridge, AudioEventBridge
- Input: MonoBehaviour writes mouse position → ECS singleton each frame
- Events: ECS writes to DynamicBuffers → MonoBehaviour drains in LateUpdate
- Stats: MonoBehaviour reads ECS singletons for UI updates

**Shared** — `Assets/Scripts/Shared/` for constants/enums used by both layers (no layer dependencies)
**Data** — `Assets/Scripts/Data/` for ScriptableObject class definitions; instances in `Assets/ScriptableObjects/`

## Key Architectural Rules

- **No Entities Graphics package** — WebGL incompatible. All rendering via GameObject SpriteRenderer/MeshRenderer synced from ECS data
- **ISystem + IJobEntity** for hot paths (movement, damage, collection at 100+ asteroids, 1000+ minerals). Must be Burst-compatible with only unmanaged IComponentData
- **SystemBase** only for bridge/presentation systems needing managed types
- **Singleton components** for cross-layer state (GameStateData, PlayerStats, InputData)
- **DynamicBuffer event pattern** for ECS→GameObject events (damage, destruction, collection)
- **No System.Threading** anywhere — WebGL is single-threaded
- **No companion GameObjects** on ECS entities — defeats ECS purpose at scale
- **UI stays in MonoBehaviours** — never create ECS components for UI state
- **EntityManager access only on main thread** in Update/LateUpdate, never from coroutines
- **Runtime spawning via ECB** (EntityCommandBuffer), not subscene streaming

## WebGL Constraints

- Burst compiles to WASM (~2-10x slower than native)
- Jobs run sequentially on main thread
- No `File.IO` — use PlayerPrefs only (maps to IndexedDB)
- Audio requires user interaction before first playback
- Target: 60 FPS with 100 asteroids + 1000 minerals on WebGL

## Naming Conventions

- **Components**: PascalCase with `Data` or `Component` suffix (`GameStateData`, `HealthComponent`)
- **Systems**: PascalCase + `System` suffix (`AsteroidMovementSystem`)
- **Private fields**: `_camelCase` prefix
- **Enums**: PascalCase (`GamePhase`)
- **Scripts**: PascalCase matching class name

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| URP | 17.3.0 | Rendering pipeline |
| com.unity.entities | 1.4.4 | ECS framework (to be installed) |
| com.unity.physics | 1.4.3 | Collision queries (to be installed) |
| InputSystem | 1.18.0 | Mouse/keyboard input |
| uGUI | 2.0.0 | UI framework |

## Planning & Documentation

- `.planning/PROJECT.md` — Project overview, requirements, key decisions
- `.planning/ROADMAP.md` — 6-phase execution plan with success criteria
- `.planning/STATE.md` — Current progress and session continuity
- `.planning/REQUIREMENTS.md` — 83 requirements mapped to phases
- `.planning/research/ARCHITECTURE.md` — Detailed architecture patterns and data flow
- `Docs/Astrominer_GDD.md` — Authoritative game design document
- `Docs/Unity-PRD.md` — Bevy prototype reference (historical)

## Development Phases

1. **Foundation & WebGL Validation** — ECS bootstrap, state machine, bridges, pooling (current)
2. **Core Mining Loop** — Asteroid spawning, mining circle, damage
3. **Collection, Economy, Session** — Minerals, credits, timed runs, save system
4. **Visual & Audio Feedback** — Damage numbers, particles, SFX, screen shake
5. **Ship Skills & Advanced Damage** — 4 active skills, crits, DoT
6. **Tech Tree & Level Progression** — Upgrades, levels, tiered resources
