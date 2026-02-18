# Phase 4: Visual and Audio Feedback - Context

**Gathered:** 2026-02-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Transform the game from functional to satisfying. Every mining hit, asteroid destruction, mineral collection, and critical moment gets visual and audio feedback that makes the player feel powerful. No new gameplay mechanics — this phase layers juice onto Phase 1-3's existing systems.

</domain>

<decisions>
## Implementation Decisions

### Damage number presentation
- Float upward + fade animation (classic RPG style, ~1 second duration)
- Small & subtle size — visible but not distracting, focus stays on asteroids and mining circle
- Show every number even when overlapping — no aggregation or batching of rapid ticks
- Critical hits get scale boost (noticeably larger than normal) + brief white flash/glow at the hit point
- Color coding per roadmap: white (normal), yellow "CRIT!" (critical), orange italic (DoT), skill-colored (skills)

### Particle & VFX style
- Asteroid explosions: chunky debris — rock fragments fly outward, physical and weighty feel
- Mineral trails: simple color-matched glowing streak behind each mineral as it flies to ship
- Overall visual density: moderate — noticeable effects that build up but don't overwhelm, screen stays readable in peak chaos
- Mining circle: no additional VFX when actively damaging — the existing glow is enough, damage numbers and asteroid reactions provide feedback

### Screen effects & juice
- Screen shake: light intensity, clearly noticeable but brief (2-3 frames) on critical hits and skill impacts
- Timer warning: steady red vignette that fades in at 10 seconds remaining and stays — constant pressure, no pulsing
- Mineral collection feedback: credit counter briefly scales up or changes color when credits are added (no ship flash)
- No HP indicators on asteroids — no health bars, no color tinting, damage numbers are the only HP feedback

### Audio direction
- Soundscape: mix of electronic + weighty — electronic bleeps/tones for UI sounds, heavy thuds and crunchy impacts for mining/destruction
- Background music: ambient/chill spacey synths — meditative mining mood, doesn't compete with SFX for attention
- Separate SFX and Music volume controls via AudioMixer (per roadmap)
- Audio assets: free CC0/royalty-free sound libraries and music packs — no procedural generation

### Claude's Discretion
- Mineral collection chime handling at scale (individual per-mineral vs batched for nearby collections) — optimize for what sounds good at 1000+ minerals
- Exact particle counts and lifetimes for debris explosions
- Damage number font choice and exact fade timing
- Music loop length and crossfade behavior
- AudioMixer group structure and default volume levels

</decisions>

<specifics>
## Specific Ideas

- Damage numbers: classic RPG float-up-and-fade, not modern pop/scatter styles
- Explosions should feel physical (chunky rock debris), not energy-based
- Crit flash should be a quick white glow at the impact point — brief enough to not obscure gameplay
- Timer warning is about sustained pressure (steady vignette), not pulsing urgency
- Audio reference: FTL / Into The Breach electronic tones mixed with physical mining weight
- Credit counter pop on collection creates a visual rhythm as minerals arrive in waves

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-visual-and-audio-feedback*
*Context gathered: 2026-02-18*
