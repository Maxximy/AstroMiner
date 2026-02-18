# Phase 1: Foundation and WebGL Validation - Context

**Gathered:** 2026-02-17
**Status:** Ready for planning

<domain>
## Phase Boundary

ECS simulation world running on both desktop and WebGL with a working game state machine, input bridge, object pooling, and visual baseline (starfield + post-processing). Proves the hybrid architecture (DOTS simulation + GameObject rendering) before any gameplay is built. No gameplay mechanics — just the technical foundation and visual environment.

</domain>

<decisions>
## Implementation Decisions

### Camera and play area
- Perspective camera with top-down angle — slight 3D depth, asteroids have visual presence
- Landscape orientation, 16:9 aspect ratio — standard desktop/web layout
- Scale-to-fit with safe zone — play area scales to fill screen, UI stays in safe margin, works on any resolution
- Ship sits at bottom 10% of screen, asteroids use ~85% of vertical space to drift down

### Game state transitions
- Smooth fade-to-black between states (Playing, Collecting, GameOver, Upgrading)
- During Collecting state: gameplay view stays visible, player watches remaining minerals fly to ship, then fade to results — satisfying visual payoff
- Upgrading state (tech tree): full screen takeover, not an overlay
- Debug UI: small corner overlay in top-left showing current state, FPS, entity count — unobtrusive

### Placeholder visuals
- Asteroids: colored Unity primitives (spheres/cubes) — obviously placeholder programmer art
- Minerals: tiny colored cubes/spheres in various colors — easy to see count visually
- Ship: basic triangle or arrow shape at bottom center — establishes spatial reference
- All placeholders have simple drift + spin movement — proves ECS systems are running, gives more realistic benchmark

### Space visual style
- Dark void with sparse scattered stars — clean, serious, lets gameplay elements pop
- Fewer, brighter stars with lots of empty black space
- Warm amber accents — stars lean warm, slight golden ambient light, inviting despite the void
- Subtle bloom on emissive objects — gentle glow that adds polish without washing out
- Static skybox texture — zero performance cost, pre-baked star texture

### Claude's Discretion
- Exact post-processing parameter values (bloom threshold, intensity, tonemapping mode)
- Fade duration and easing curves for state transitions
- Specific primitive shapes and color assignments for placeholders
- Debug overlay font size and positioning details
- URP renderer configuration details

</decisions>

<specifics>
## Specific Ideas

- The space should feel like a calm void — dark, minimal, warm-toned stars as subtle points of light
- Gameplay elements (mining circle, asteroids, minerals) should be the visual focus against the dark backdrop
- Phase 1 placeholders should be obviously "programmer art" — no pretense of final visuals

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation-webgl-validation*
*Context gathered: 2026-02-17*
