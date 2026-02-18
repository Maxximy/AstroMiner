# Phase 5: Ship Skills and Advanced Damage - Context

**Gathered:** 2026-02-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Four active combat skills (Laser Burst, Chain Lightning, EMP Pulse, Overcharge) with keyboard and UI activation, a critical hit system, and damage-over-time burning. Skills fire from the ship toward the mouse. All skills unlocked by default in this phase; Phase 6 tech tree adds gating later.

</domain>

<decisions>
## Implementation Decisions

### Skill behavior & targeting
- All skills fire from the ship toward the mouse position (not centered on mining circle)
- **Laser Burst**: Single powerful beam from ship to mouse — damages everything in the line (sniper feel)
- **Chain Lightning**: Hits asteroid nearest to mouse, then auto-arcs to 3-4 nearby asteroids (satisfying chain reaction)
- **EMP Pulse**: Big AoE damage blast at target area hitting many asteroids (crowd damage)
- **Overcharge**: Temporarily buffs mining circle damage and size for a few seconds (self-buff, not a projectile)

### Skill bar & cooldown UI
- Horizontal row of 4 skill slots, bottom center of screen
- Cooldown display: radial sweep (dark overlay sweeps clockwise) + remaining seconds as number
- Keybind hints (1, 2, 3, 4) always visible as small badges on each slot corner
- All 4 skills unlocked and available by default — Phase 6 adds lock/unlock gating via tech tree

### Critical hit system
- Noticeable but not disruptive: yellow "CRIT!" text larger than normal popups + distinct crit sound, no extra screen flash or shake
- Base crit rate: 5-10% (rare — each one feels like a lucky event)
- Both skills and normal mining damage can crit
- Crit damage multiplier: 2x
- Upgradeable crit rate deferred to Phase 6 tech tree

### DoT burning
- Only specific skills apply DoT (not automatic from mining circle) — DoT is a skill bonus
- Ember particle effect only on burning asteroids — no color tint to the asteroid sprite
- Short burn duration: 2-3 seconds after asteroid leaves affected area
- DoT damage popups use same white style as normal damage (no distinct DoT popup color)

### Claude's Discretion
- Individual skill cooldown durations and damage values (balance tuning)
- Which specific skills apply the DoT burning effect
- Skill visual effects (beam width, lightning arc style, EMP blast radius visual, Overcharge glow)
- Slot icon design and ready-state flash animation
- DoT tick frequency and damage per tick
- Overcharge buff duration and multiplier values

</decisions>

<specifics>
## Specific Ideas

- Skills should feel like shooting from the ship — directional, aimed toward the mouse cursor
- Chain Lightning's auto-chain to nearby asteroids should feel like a satisfying chain reaction, not just multi-target
- Overcharge is the odd one out (self-buff vs projectile) — should have a clear visual indicator that mining circle is empowered (glow, size increase, color shift)
- Crit rate is deliberately low so each crit feels like an event, not routine — this creates space for Phase 6 upgrades to meaningfully increase it

</specifics>

<deferred>
## Deferred Ideas

- Skill unlock gating via Ship branch of tech tree — Phase 6
- Crit rate upgrade nodes — Phase 6 tech tree
- DoT damage upgrade nodes — Phase 6 tech tree
- Skill level/rank progression — Phase 6 if scoped

</deferred>

---

*Phase: 05-ship-skills-and-advanced-damage*
*Context gathered: 2026-02-18*
