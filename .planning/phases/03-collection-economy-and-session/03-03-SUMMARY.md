---
phase: 03-collection-economy-and-session
plan: 03
subsystem: save
tags: [json, persistence, webgl, indexeddb, playerprefs, singleton]

# Dependency graph
requires:
  - phase: 03-02
    provides: "GameOverState, PlayingState, GameStateData with Credits, WebGLHelper"
provides:
  - "SaveData serializable class with versioned schema"
  - "SaveManager singleton with JSON persistence and WebGL IndexedDB flush"
  - "Auto-save on run end (GameOverState.Enter)"
  - "Credit restoration from save on first session run (PlayingState.Enter)"
  - "PlayerPrefs fallback for platforms where File.IO fails"
affects: [phase-6-tech-tree, phase-6-level-progression]

# Tech tracking
tech-stack:
  added: [JsonUtility, RuntimeInitializeOnLoadMethod]
  patterns: [self-instantiating-singleton, file-io-with-fallback, versioned-save-schema]

key-files:
  created:
    - Assets/Scripts/MonoBehaviours/Save/SaveData.cs
    - Assets/Scripts/MonoBehaviours/Save/SaveManager.cs
  modified:
    - Assets/Scripts/States/GameOverState.cs
    - Assets/Scripts/States/PlayingState.cs

key-decisions:
  - "SaveManager self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required"
  - "File.IO primary with PlayerPrefs fallback for robustness across all platforms"
  - "Save schema includes placeholder fields for Phase 6 (TechTreeUnlocks, PlayerStatsData) to avoid future migration"
  - "Static _saveLoaded flag in PlayingState ensures credits load exactly once per session"

patterns-established:
  - "Self-instantiating singleton: [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] creates GameObject + component + DontDestroyOnLoad"
  - "Versioned save schema: SaveVersion field enables future migration logic in Load()"
  - "Placeholder-forward design: empty arrays and default-valued nested classes for future phase data"

requirements-completed: [SAVE-01, SAVE-02, SAVE-03, SAVE-04, SAVE-05]

# Metrics
duration: 4min
completed: 2026-02-17
---

# Phase 3 Plan 3: Save System Summary

**JSON save persistence with auto-save on run end, credit restoration on session start, WebGL IndexedDB flush, and PlayerPrefs fallback**

## Performance

- **Duration:** 4 min
- **Tasks:** 1
- **Files modified:** 4

## Accomplishments
- SaveData class with versioned schema (SaveVersion=1), credits, level, and Phase 6 placeholder fields
- SaveManager singleton that self-instantiates before scene load -- zero manual setup required
- JSON persistence to Application.persistentDataPath with WebGLHelper.FlushSaveData() for IndexedDB sync
- PlayerPrefs fallback if File.IO fails on any platform
- Auto-save fires in GameOverState.Enter() capturing current credits from ECS singleton
- PlayingState loads saved credits into ECS on first run of session, before snapshotting CreditsAtRunStart

## Task Commits

Each task was committed atomically:

1. **Task 1: SaveData class, SaveManager with JSON persistence and WebGL support, auto-save and load integration** - `2d7e319` (feat)

**Plan metadata:** (pending final commit)

## Files Created/Modified
- `Assets/Scripts/MonoBehaviours/Save/SaveData.cs` - Serializable save data with versioned schema, credits, level, Phase 6 placeholders
- `Assets/Scripts/MonoBehaviours/Save/SaveManager.cs` - Singleton JSON save/load manager with WebGL flush and PlayerPrefs fallback
- `Assets/Scripts/States/GameOverState.cs` - Added SaveManager.Instance?.AutoSave() call in Enter()
- `Assets/Scripts/States/PlayingState.cs` - Added static _saveLoaded flag and LoadIntoECS() call on first session run

## Decisions Made
- SaveManager self-instantiates via [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] -- eliminates need for manual scene setup or CoPlay MCP
- Used File.IO as primary persistence with PlayerPrefs as fallback, matching research recommendation
- Save schema pre-includes Phase 6 placeholder fields (TechTreeUnlocks bool[], PlayerStatsData) with safe defaults to avoid future migration
- Static _saveLoaded flag in PlayingState ensures credits load exactly once per session, preventing double-load on subsequent runs

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - SaveManager self-instantiates via [RuntimeInitializeOnLoadMethod]. No scene objects, no manual configuration.

## Next Phase Readiness
- Save system complete for Phase 3 scope (credits persistence)
- Phase 6 will populate TechTreeUnlocks and PlayerStatsData fields after tech tree implementation
- Phase 6 will add tech-tree-purchase save trigger (SAVE-02 partial deferred)
- All Phase 3 plans complete -- ready for Phase 4 (Visual & Audio Feedback)

## Self-Check: PASSED

- [x] SaveData.cs exists at Assets/Scripts/MonoBehaviours/Save/SaveData.cs
- [x] SaveManager.cs exists at Assets/Scripts/MonoBehaviours/Save/SaveManager.cs
- [x] GameOverState.cs modified with AutoSave call
- [x] PlayingState.cs modified with LoadIntoECS call
- [x] 03-03-SUMMARY.md exists
- [x] Commit 2d7e319 verified in git log

---
*Phase: 03-collection-economy-and-session*
*Plan: 03*
*Completed: 2026-02-17*
