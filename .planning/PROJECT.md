# Astrominer

## What This Is

Astrominer is an idle/clicker asteroid mining game built in Unity 6.3 LTS using Hybrid ECS (DOTS). Players control a mining circle with the mouse to damage asteroids drifting down the screen, collect mineral particles, earn credits, activate 4 ship skills, and invest in a 40-node tech tree across 5 branches to progress through 5 levels with 6 resource tiers. Targets Windows desktop and WebGL.

## Core Value

The mining-collecting-upgrading loop must feel satisfying — hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.

## Requirements

### Validated

- ✓ ECS world bootstraps with hybrid bridge layer — v1.0
- ✓ WebGL build runs at 60 FPS with 100 asteroids and 1000 minerals — v1.0
- ✓ Object pooling for asteroids, minerals, damage popups, and particles — v1.0
- ✓ Game state machine (Playing, Collecting, GameOver, Upgrading) — v1.0
- ✓ InputBridge writes mouse world position to ECS singleton — v1.0
- ✓ Large number formatting (K/M/B/T) — v1.0
- ✓ Mining circle follows cursor, tick-based AoE damage — v1.0
- ✓ Mining circle radius and damage upgradeable via tech tree — v1.0
- ✓ Asteroids spawn, drift, rotate, break apart with HP per tier — v1.0
- ✓ Ship visual at bottom of screen — v1.0
- ✓ Mineral collection pipeline (spawn, pull, collect, credits) — v1.0
- ✓ Credit economy with tier-based values and persistence — v1.0
- ✓ Timed sessions with HUD, results screen, run reset — v1.0
- ✓ JSON save system with WebGL IndexedDB support — v1.0
- ✓ Tech tree UI with 5 branches, 40 nodes, prerequisite gating — v1.0
- ✓ Tier-based upgrade costs (1x/3x/8x) with ScriptableObject data — v1.0
- ✓ 4 ship skills (Laser, Chain Lightning, EMP, Overcharge) with cooldowns — v1.0
- ✓ Skill unlock gating via Ship branch of tech tree — v1.0
- ✓ Critical hit system (configurable chance + multiplier from tech tree) — v1.0
- ✓ DoT burning with ember particle trails — v1.0
- ✓ 5 levels with weighted drop tables and HP scaling — v1.0
- ✓ 6 resource tiers (Iron through Titanium) with distinct visuals — v1.0
- ✓ Damage popups (white/yellow CRIT/orange DoT/skill-colored) — v1.0
- ✓ Explosion particles, mineral trails, emissive materials — v1.0
- ✓ Screen shake, timer edge glow warning — v1.0
- ✓ Full audio system (mining hits, destruction, collection chimes, skill SFX, ambient music) — v1.0
- ✓ AudioMixer with SFX/Music volume channels — v1.0
- ✓ Starfield skybox, URP bloom/tonemapping — v1.0
- ✓ PBR asteroid materials per resource type — v1.0

### Active

- [ ] Special asteroid types (Fragile, Mega, Cluster)
- [ ] Economy balance pass (spreadsheet model for upgrade costs, credit rates)
- [ ] Settings menu (volume sliders, keybindings display)
- [ ] Tutorial/onboarding flow for new players

### Out of Scope

- Prestige/rebirth system — design should accommodate it, defer to post-v1
- Auto-miner/idle income — active play only; fundamentally different economy balance
- Mobile input — targeting desktop + WebGL first; PWA approach for mobile later
- Cloud saves — local JSON + PlayerPrefs sufficient; no backend infrastructure
- Multiplayer — single-player experience; would need backend + anti-cheat
- Ship visual customization — defer to post-v1
- Real-time difficulty scaling — undermines upgrade satisfaction
- Complex crafting system — competing resource sink with tech tree
- Offline progression — active-play only for v1

## Context

Shipped v1.0 MVP with ~9,750 LOC C# across 67 files.
Tech stack: Unity 6.3 LTS, URP, Entities 1.4.4, InputSystem 1.18.0, uGUI.
Architecture: Hybrid ECS with singleton-based cross-layer communication, DynamicBuffer event pipeline, and programmatic UI creation.
All rendering via pooled GameObjects (no Entities Graphics — WebGL incompatible).
Built in 2 days (1.6 hours of AI execution time across 16 plans).

Known tech debt:
- Procedural skybox shader as temporary stand-in for cubemap texture
- Economy values are placeholders — need spreadsheet balance pass
- ASTR-03 uses programmatic materials, not actual 3D PBR models (functional but not final art)

### Key Documents

- `Docs/Astrominer_GDD.md` — Full game design document (authoritative for gameplay)
- `Docs/Unity-PRD.md` — Bevy prototype reference (historical)

## Constraints

- **Engine:** Unity 6.3 LTS with URP (Universal Render Pipeline)
- **Architecture:** Hybrid ECS — gameplay entities in DOTS; UI, audio, camera as GameObjects
- **UI Framework:** uGUI (Canvas) for all UI including tech tree
- **Platforms:** Windows desktop + WebGL (no threading in WebGL — no `System.Threading`)
- **Input:** New Input System only (`UnityEngine.InputSystem`) — mouse + keyboard with on-screen skill buttons
- **Save System:** File.IO primary + PlayerPrefs fallback (works in WebGL via IndexedDB)
- **Tooling:** CoPlay Unity MCP for editor automation where applicable

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Hybrid ECS over pure ECS | Gameplay entities benefit from DOTS performance; UI/audio stay as GameObjects | ✓ Good — 60 FPS WebGL with 1100 entities |
| uGUI over UI Toolkit | Tech tree is major UI; uGUI well-documented, handles complex layouts | ✓ Good — 40-node tree with pan/zoom works well |
| GDD scope over PRD port | Full vision from scratch vs recreating Bevy prototype | ✓ Good — complete game in 2 days |
| Top-down scrolling over orbital | Ship bottom, asteroids drift down; fits idle/clicker conventions | ✓ Good — intuitive spatial model |
| PlayerPrefs + JSON over cloud | Simple local persistence for v1 single-player | ✓ Good — works desktop + WebGL |
| Keyboard + UI buttons for skills | On-screen buttons + 1-4 keys; WebGL/mobile compatible | ✓ Good — accessible on all platforms |
| No Entities Graphics | WebGL incompatible; all rendering via pooled GameObject SpriteRenderers | ✓ Good — avoids WebGL breakage |
| XZ plane coordinate system | Aligned with InputBridge mouse projection for Phase 2+ | ✓ Good — consistent coord space |
| DynamicBuffer event pipeline | ISystem writes events, MonoBehaviour drains in Update | ✓ Good — clean ECS-to-GameObject bridge |
| Programmatic UI via UISetup | All UI hierarchy created in code; no manual scene editor work | ✓ Good — reproducible, no scene merge conflicts |
| RuntimeInitializeOnLoadMethod pattern | Managers self-instantiate; no manual scene wiring needed | ✓ Good — zero-setup architecture |
| Programmatic ScriptableObject.CreateInstance | All 40 tech tree nodes defined in code, no .asset files | ✓ Good — version-controlled, easy iteration |
| File.IO + PlayerPrefs fallback | Robust cross-platform save with IndexedDB flush for WebGL | ✓ Good — works on all targets |
| Save schema pre-includes future fields | Phase 6 placeholders in Phase 3 save to avoid migration | ⚠️ Revisit — v1->v2 migration was still needed |

---
*Last updated: 2026-02-18 after v1.0 milestone*
