# Astrominer

## What This Is

Astrominer is an idle/clicker asteroid mining game built in Unity using Hybrid ECS (DOTS). Players control a mining circle with the mouse to damage asteroids that drift down the screen. Destroyed asteroids release mineral particles collected by the player's ship, converting to credits spent in a branching tech tree to unlock upgrades, ship skills, and level progression. The game targets Windows desktop and WebGL, with a realistic space visual style.

## Core Value

The mining-collecting-upgrading loop must feel satisfying — hovering the circle over asteroids, watching them break apart, collecting minerals, and spending credits on meaningful upgrades that make the next run noticeably better.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Timed mining runs with asteroids drifting downward
- [ ] Mining circle follows mouse cursor, damages asteroids within radius
- [ ] Asteroids break into collectible mineral particles on destruction
- [ ] Credits earned from minerals, displayed during gameplay
- [ ] Tech tree with 5 branches (Mining, Economy, Ship, Run, Progression)
- [ ] Ship skills (Laser Burst, Chain Lightning, EMP, Overcharge) with keyboard + UI buttons
- [ ] Level system with tiered resources and drop tables
- [ ] Special asteroid types (Fragile, Mega, Cluster)
- [ ] Persistent progression via PlayerPrefs + JSON
- [ ] Realistic space visuals (PBR asteroids, starfield skybox, bloom, emissive star)
- [ ] Audio system (SFX for mining/destruction/collection, ambient space music)
- [ ] Visual feedback (damage numbers, particle effects, screen shake)

### Out of Scope

- Prestige/rebirth system — future consideration, but design should accommodate it
- Auto-miner/idle income — active play only for v1
- Mobile input — targeting desktop + WebGL first; mobile later
- Cloud saves — local PlayerPrefs + JSON sufficient for v1
- Multiplayer — single-player experience
- Ship visual customization — defer to post-v1

## Context

Astrominer was originally built in Rust/Bevy with a simple orbital mining mechanic (asteroids orbiting a central star, 20-second timer, basic scoring). This Unity rebuild is a fresh start targeting the full game design vision described in the GDD, not a port of the Bevy prototype.

The GDD defines the complete game: 6 tiers of resources, tech tree with ~30 upgrade nodes across 5 branches, 4 active ship skills, level progression with per-level drop tables, and special asteroid types. The PRD documented the Bevy prototype's mechanics for reference but the scope is now the full GDD.

CoPlay Unity MCP is available for scene setup, asset manipulation, and Unity editor automation during development.

### Key Documents

- `Docs/Astrominer_GDD.md` — Full game design document (authoritative for gameplay)
- `Docs/Unity-PRD.md` — Bevy prototype recreation spec (reference for base mechanics)

## Constraints

- **Engine:** Unity with URP (Universal Render Pipeline)
- **Architecture:** Hybrid ECS — gameplay entities (asteroids, minerals, damage) in DOTS; UI, audio, camera as GameObjects
- **UI Framework:** uGUI (Canvas) for all UI including tech tree
- **Platforms:** Windows desktop + WebGL (no threading in WebGL — no `System.Threading`)
- **Input:** Mouse + keyboard with on-screen skill buttons (platform-flexible)
- **Save System:** PlayerPrefs + JSON (works in WebGL via IndexedDB)
- **Tooling:** CoPlay Unity MCP for editor automation where applicable

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Hybrid ECS over pure ECS | Gameplay entities benefit from DOTS performance (100+ asteroids, 1000+ minerals). UI/audio/camera stay as GameObjects for simplicity and uGUI compatibility. | — Pending |
| uGUI over UI Toolkit | Tech tree is a major UI feature. uGUI is well-documented, team-familiar, and handles complex layouts with anchoring/layout groups. | — Pending |
| GDD scope over PRD port | Building the full vision from scratch rather than recreating the Bevy prototype first. More ambitious but avoids throwaway work from a limited port. | — Pending |
| Top-down scrolling over orbital | GDD spatial model (ship bottom, asteroids drift down) chosen over PRD orbital model. Better fits the idle/clicker genre conventions. | — Pending |
| PlayerPrefs + JSON over cloud | Simple local persistence sufficient for v1 single-player. Works across desktop and WebGL without backend infrastructure. | — Pending |
| Keyboard + UI buttons for skills | On-screen skill buttons alongside keyboard shortcuts (1-4). Ensures playability in WebGL and future mobile without input redesign. | — Pending |

---
*Last updated: 2026-02-17 after initialization*
