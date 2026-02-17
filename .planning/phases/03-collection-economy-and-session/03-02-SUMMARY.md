---
phase: 03-collection-economy-and-session
plan: 02
subsystem: ui
tags: [unity, ugui, tmpro, hud, timer, state-machine, session-flow, credits]

# Dependency graph
requires:
  - phase: 03-collection-economy-and-session
    provides: "MineralTag, MineralData, MineralCollectionSystem, GameStateData.Credits, GameConstants session timing"
  - phase: 02-core-mining-loop
    provides: "AsteroidTag, GameManager state machine, FadeController, UISetup, ECSBootstrap"
provides:
  - "PlayingState with 60-second countdown timer decrementing GameStateData.Timer"
  - "CollectingState with mineral+asteroid entity count check and grace period"
  - "GameOverState showing ResultsScreen with credits-this-run display"
  - "UpgradingState showing UpgradeScreen placeholder with Start Run button"
  - "HUDController with formatted credits (K/M/B/T) and MM:SS timer display"
  - "ResultsScreen with credits earned and Continue button"
  - "UpgradeScreen placeholder with total credits and Start Run button"
  - "GameManager.ResetRun() for clean entity cleanup between runs"
  - "GameManager.CreditsAtRunStart for per-run credit delta tracking"
affects: [03-03, 04-visual-audio-feedback, 06-tech-tree-level-progression]

# Tech tracking
tech-stack:
  added: []
  patterns: [timer-countdown-state, grace-period-entity-check, credits-run-delta-tracking, programmatic-button-creation]

key-files:
  created:
    - Assets/Scripts/MonoBehaviours/UI/HUDController.cs
    - Assets/Scripts/MonoBehaviours/UI/ResultsScreen.cs
    - Assets/Scripts/MonoBehaviours/UI/UpgradeScreen.cs
  modified:
    - Assets/Scripts/States/PlayingState.cs
    - Assets/Scripts/States/CollectingState.cs
    - Assets/Scripts/States/GameOverState.cs
    - Assets/Scripts/States/UpgradingState.cs
    - Assets/Scripts/MonoBehaviours/Core/GameManager.cs
    - Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs

key-decisions:
  - "Credits persistent across runs (only timer resets) -- CreditsAtRunStart snapshot tracks per-run delta for display"
  - "CollectingState uses 2-second grace period after all entities gone to handle ECB destruction delay edge cases"
  - "UISetup CreateButton helper for reusable programmatic button creation with Image + TMPro text"
  - "HUD hidden during GameOver/Upgrading phases via root GameObject SetActive toggle"

patterns-established:
  - "Run delta tracking: Snapshot a value at state entry, compute delta at display time"
  - "Grace period pattern: Wait N seconds after condition met before transitioning, reset if condition breaks"
  - "Programmatic UI button creation: Image background + TMPro child + Button component in CreateButton helper"

requirements-completed: [ECON-02, ECON-03, SESS-01, SESS-02, SESS-03, SESS-04, SESS-05, SESS-06]

# Metrics
duration: 4min
completed: 2026-02-17
---

# Phase 3 Plan 2: Session Timer & Economy HUD Summary

**60-second timed runs with HUD (credits + timer), phase transitions (Playing -> Collecting -> GameOver -> Upgrading -> Playing), results screen showing run earnings, and upgrade placeholder with run reset**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-17T21:31:57Z
- **Completed:** 2026-02-17T21:35:40Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- Complete session flow: 60-second countdown, timer expiry transitions to Collecting, grace period waits for entities cleared, GameOver shows results, Upgrading offers Start Run
- HUD displays formatted credits (K/M/B/T suffixes via NumberFormatter) and MM:SS countdown timer during Playing and Collecting phases
- Credits persist across runs within a session -- only the timer resets on new run
- GameManager.ResetRun destroys all leftover asteroid and mineral entities for clean run starts

## Task Commits

Each task was committed atomically:

1. **Task 1: State machine updates and GameManager run reset** - `5aad9f9` (feat)
2. **Task 2: HUD, ResultsScreen, UpgradeScreen UI and UISetup integration** - `158ca16` (feat)

## Files Created/Modified
- `Assets/Scripts/States/PlayingState.cs` - Countdown timer decrementing GameStateData.Timer, transitions to Collecting at 0
- `Assets/Scripts/States/CollectingState.cs` - Waits for all minerals + asteroids cleared with 2s grace period, transitions to GameOver
- `Assets/Scripts/States/GameOverState.cs` - Shows ResultsScreen on enter, hides on exit
- `Assets/Scripts/States/UpgradingState.cs` - Shows UpgradeScreen on enter, hides on exit
- `Assets/Scripts/MonoBehaviours/Core/GameManager.cs` - Added ResetRun() and CreditsAtRunStart property
- `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` - Timer initialized to DefaultRunDuration (60s)
- `Assets/Scripts/MonoBehaviours/UI/HUDController.cs` - Credits display with K/M/B/T formatting, MM:SS timer, phase-based visibility
- `Assets/Scripts/MonoBehaviours/UI/ResultsScreen.cs` - Run results with credits earned delta and Continue button
- `Assets/Scripts/MonoBehaviours/UI/UpgradeScreen.cs` - Placeholder upgrade screen with total credits and Start Run button
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Added CreateHUDCanvas, CreateResultsCanvas, CreateUpgradeCanvas, CreateButton helper

## Decisions Made
- Credits are persistent across runs (ECON-03) -- CreditsAtRunStart snapshot at PlayingState.Enter() tracks per-run delta for ResultsScreen display
- CollectingState uses 2-second grace period (GameConstants.CollectingGracePeriod) after all entities gone to handle ECB destruction delay edge cases
- Created reusable CreateButton helper in UISetup for consistent programmatic button creation
- HUD root GameObject toggled via SetActive for clean phase-based visibility (no per-element toggling)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - all UI is created programmatically by UISetup (no manual scene editor work needed).

## Next Phase Readiness
- Session flow complete -- timed runs with HUD, results, and upgrade placeholder
- Credits accumulate across runs, ready for Plan 03-03 (SaveManager for cross-session persistence)
- UpgradeScreen placeholder ready for Phase 6 tech tree population
- ResultsScreen ready for Phase 4 visual enhancements (animations, SFX)

## Self-Check: PASSED

- All 10 files verified on disk (3 created, 7 modified)
- Commit `5aad9f9`: 6 files (Task 1 - state machine + GameManager + ECSBootstrap) - verified
- Commit `158ca16`: 4 files (Task 2 - HUD + ResultsScreen + UpgradeScreen + UISetup) - verified
- All plan verification criteria confirmed via grep

---
*Phase: 03-collection-economy-and-session*
*Completed: 2026-02-17*
