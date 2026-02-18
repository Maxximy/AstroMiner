# Phase 6: Tech Tree and Level Progression - Context

**Gathered:** 2026-02-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Branching tech tree with 5 branches (Mining, Economy, Ship, Run, Progression), node-and-line graph UI displayed between runs, purchasing upgrades that apply stat effects, tiered costs, and a level system with 5+ levels introducing rarer resource tiers and tougher asteroids. All tech tree and level data defined in ScriptableObjects.

</domain>

<decisions>
## Implementation Decisions

### Tree UI & navigation
- Center-outward node-and-line graph: START node in center, 5 branches radiate outward
- Pan & zoom navigation: drag to pan, scroll to zoom, full freedom to explore the tree
- Node color coding by state:
  - **Blue** = purchased/owned
  - **Green** = available to buy (connected and affordable)
  - **Red** = connected but not enough credits
  - **Hidden** = not yet connected to any purchased node (unrevealed)
- Nodes reveal progressively as the player purchases connected nodes
- Credit total always visible at top of screen
- Each visible node shows its cost directly on the node
- Hover tooltip shows: name, cost, effect description, current vs next stat values

### Upgrade purchase feel
- Quick & satisfying: fast color transition (green to blue), "cha-ching" sound, brief particle burst on the node
- One click to buy -- no confirmation dialog, instant purchase on click
- Stat changes are gradual stacking: individual upgrades are subtle, but buying several in a branch feels strong, rewarding commitment to a branch

### Economy & pacing
- First upgrade affordable in 1-2 runs: immediate hook, player sees the loop working right away
- Cost curve: increasing base cost for deeper nodes AND tier scaling (1x/3x/8x) on top -- double scaling creates a long tail of progression
- Credits scale with level: higher levels yield rarer tiers worth more credits, offsetting rising upgrade costs (classic idle game feel)
- Credits and node costs always visible on tree screen -- green nodes affordable, red nodes not

### Level progression
- Seamless progression: "Advance to Level N" node turns blue like any other purchase, next run uses new level config -- the new asteroids ARE the reward
- Gradual resource tier introduction: each level adds 1-2 new tiers to the drop table
  - Level 1: Iron only
  - Level 2: Iron + Copper
  - Level 3: Iron + Copper + Silver
  - Level 4: Iron + Copper + Silver + Cobalt
  - Level 5: All 6 tiers (Iron through Titanium)
- Each resource tier has a distinct mineral color: Iron = grey, Copper = orange, Silver = white, Cobalt = blue, Gold = yellow, Titanium = purple/magenta
- Higher-HP asteroids are larger (size scales with HP) -- no color tinting, just bigger

### Claude's Discretion
- Exact node layout algorithm and spacing for center-outward graph
- Pan/zoom implementation details and camera bounds
- Particle burst design for purchase feedback
- Exact base cost values and per-level credit targets (respecting 1-2 runs to first buy)
- Tooltip styling and positioning
- Animation timing for node state transitions

</decisions>

<specifics>
## Specific Ideas

- Nodes that aren't connected to any purchased node should be completely hidden -- tree reveals itself as you buy, creating discovery
- The color scheme (blue/green/red/hidden) keeps tree reading simple at a glance
- No ceremony on level advance -- buying the Advance node feels like any other purchase, the new content in the next run is the reward itself

</specifics>

<deferred>
## Deferred Ideas

None -- discussion stayed within phase scope

</deferred>

---

*Phase: 06-tech-tree-and-level-progression*
*Context gathered: 2026-02-18*
