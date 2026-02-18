---
phase: 06-tech-tree-and-level-progression
plan: 02
subsystem: ui
tags: [tech-tree, upgrade-system, pan-zoom, tooltip, ecs-singleton, save-system, input-system]

# Dependency graph
requires:
  - phase: 06-tech-tree-and-level-progression
    provides: "UpgradeNodeSO, TechTreeSO, StatEffect, StatTarget, ECS singletons, SaveData v2"
provides:
  - "TechTreeController: full node graph with pan/zoom, one-click purchase, ECS stat application"
  - "TechTreeNode: 4-state visual UI element (blue/green/red/hidden) with purchase animation"
  - "TechTreeTooltip: hover tooltip with name, cost, description, current-vs-next stat preview"
  - "TechTreeDefinitions: all 40 nodes defined programmatically across 5 branches"
  - "UISetup.CreateUpgradeCanvas: viewport-masked pannable/zoomable tech tree surface"
  - "AudioManager.PlayPurchaseSFX: cha-ching purchase feedback"
  - "UpgradeScreen integrated with TechTreeController refresh on show"
affects: [06-03-PLAN]

# Tech tracking
tech-stack:
  added: []
  patterns: [center-outward-graph-layout, viewport-mask-pan-zoom, lazy-ecs-init, programmatic-so-construction]

key-files:
  created:
    - Assets/Scripts/MonoBehaviours/UI/TechTreeController.cs
    - Assets/Scripts/MonoBehaviours/UI/TechTreeNode.cs
    - Assets/Scripts/MonoBehaviours/UI/TechTreeTooltip.cs
    - Assets/Scripts/Data/TechTreeDefinitions.cs
  modified:
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs
    - Assets/Scripts/MonoBehaviours/UI/UpgradeScreen.cs
    - Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs

key-decisions:
  - "Programmatic node construction via ScriptableObject.CreateInstance (Option A) -- no .asset files needed, fully code-driven"
  - "Right-click and middle-mouse drag for pan to avoid conflict with node left-click purchases"
  - "Tooltip on background panel (not viewport) to avoid clipping and zoom scaling issues"
  - "EMP prereq simplified to ship_laser_1 only (not OR logic) for clean prerequisite enforcement"
  - "Purchase SFX uses collection chime at 1.5x pitch as satisfying placeholder"
  - "Connection lines sent to first sibling to render behind nodes"

patterns-established:
  - "Center-outward graph layout: branch direction vectors with perpendicular offsets for sub-branches"
  - "Viewport mask + content panel pattern: RectMask2D on viewport, large child for pan/zoom surface"
  - "Programmatic UpgradeNodeSO construction with two-pass build (create all, then resolve prerequisites)"
  - "Stat preview in tooltips: read current ECS singleton value, compute next with effect formula"

requirements-completed: [TECH-01, TECH-07, TECH-09]

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 6 Plan 02: Tech Tree UI and Purchase System Summary

**Center-outward tech tree UI with 40 nodes across 5 branches, pan/zoom navigation, 4-state color coding, one-click purchasing with immediate ECS stat application, hover tooltips with stat preview, and progressive node revelation**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-18T15:36:51Z
- **Completed:** 2026-02-18T15:44:52Z
- **Tasks:** 2
- **Files created:** 4
- **Files modified:** 3

## Accomplishments
- Complete tech tree UI controller with center-outward node-and-line graph rendering from 40 programmatic node definitions
- Pan (right/middle mouse drag) and zoom (scroll wheel with mouse pivot) via New Input System (Mouse.current)
- 4-state node color coding: blue purchased, green affordable, red too expensive, hidden unrevealed
- One-click purchase flow: deduct credits, apply all stat effects to ECS singletons, play VFX/SFX, save immediately, reveal connected nodes
- ApplyStatEffect covers all 27 StatTarget values including mining, crit, economy, skills, DoT, run duration, and level advancement
- Hover tooltip with node name, cost, description, and current-vs-next stat value preview
- Progressive revelation: nodes only appear when a connected prerequisite is purchased
- UISetup creates viewport-masked pannable/zoomable surface for the tech tree

## Task Commits

Each task was committed atomically:

1. **Task 1: Build TechTreeController with node rendering, pan/zoom, purchase logic, and stat application** - `b63b587` (feat)
2. **Task 2: Create TechTreeTooltip and wire into UISetup** - `1edd629` (feat)

## Files Created/Modified
- `Assets/Scripts/MonoBehaviours/UI/TechTreeController.cs` - Main controller: node graph, pan/zoom, purchase, stat application, save integration
- `Assets/Scripts/MonoBehaviours/UI/TechTreeNode.cs` - Individual node UI: 4-state color, purchase animation, hover event handlers
- `Assets/Scripts/MonoBehaviours/UI/TechTreeTooltip.cs` - Hover tooltip: stat preview, screen-clamped positioning, formatted display
- `Assets/Scripts/Data/TechTreeDefinitions.cs` - All 40 node definitions across 5 branches with graph positions and effects
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Rewritten CreateUpgradeCanvas with viewport mask, content panel, tooltip wiring
- `Assets/Scripts/MonoBehaviours/UI/UpgradeScreen.cs` - Integrated TechTreeController with refresh on show
- `Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs` - PlayPurchaseSFX method (collection chime at 1.5x pitch)

## Decisions Made
- Used programmatic ScriptableObject.CreateInstance construction (Option A from plan) to keep the project fully code-driven -- no .asset files needed on disk, consistent with the UISetup programmatic pattern.
- Right-click and middle-mouse button for pan to avoid conflicting with left-click node purchases.
- Tooltip placed on background panel (not viewport) so it renders above the viewport mask and is not affected by zoom scaling.
- Simplified EMP prerequisite to require ship_laser_1 only (not OR logic with chain_1) for clean prerequisite enforcement consistent with the rest of the tree.
- Purchase SFX reuses the collection chime at 1.5x pitch for a satisfying "cha-ching" feel until a dedicated audio clip is added.
- Connection lines use SetAsFirstSibling to render behind node elements.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Created TechTreeDefinitions in Task 1 instead of Task 2**
- **Found during:** Task 1 (UISetup.CreateUpgradeCanvas)
- **Issue:** UISetup references TechTreeDefinitions.BuildTree() which was planned for Task 2, but Task 1 code cannot compile without it
- **Fix:** Created the full TechTreeDefinitions.cs with all 40 nodes as part of Task 1 to resolve the compilation dependency
- **Files modified:** Assets/Scripts/Data/TechTreeDefinitions.cs
- **Verification:** All code compiles and Task 2 can focus on tooltip creation
- **Committed in:** b63b587 (Task 1 commit)

**2. [Rule 3 - Blocking] Added AudioManager.PlayPurchaseSFX in Task 1 instead of Task 2**
- **Found during:** Task 1 (TechTreeController.OnNodeClicked)
- **Issue:** TechTreeController calls AudioManager.Instance?.PlayPurchaseSFX() which doesn't exist yet
- **Fix:** Added PlayPurchaseSFX() method to AudioManager using collection chime at 1.5x pitch
- **Files modified:** Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs
- **Verification:** Method exists and compiles correctly
- **Committed in:** b63b587 (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (2 blocking dependencies)
**Impact on plan:** Both auto-fixes were task ordering adjustments (moving work from Task 2 to Task 1 for compilation). No scope creep -- all planned functionality delivered.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Tech tree UI and purchase system complete -- 06-03 (level progression and resource tiers) can now build on this
- All ECS singletons are modified at runtime by purchases and persisted via SaveManager
- Level advancement via Progression branch nodes increments RunConfigData.CurrentLevel
- 06-03 needs to implement: level config application on run start, resource tier drop tables, mineral coloring, HP scaling

## Self-Check: PASSED

- All 7 files verified present on disk
- Commit b63b587 verified in git log (Task 1)
- Commit 1edd629 verified in git log (Task 2)

---
*Phase: 06-tech-tree-and-level-progression*
*Completed: 2026-02-18*
