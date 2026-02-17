---
phase: 03-collection-economy-and-session
verified: 2026-02-17T00:00:00Z
status: gaps_found
score: 11/12 must-haves verified
gaps:
  - truth: "Mineral particles are visible as small colored spheres synced from ECS"
    status: failed
    reason: "MineralRenderer MonoBehaviour exists in code but is NOT attached to any GameObject in Game.unity scene. AsteroidRenderer is present in the scene but MineralRenderer has no scene entry."
    artifacts:
      - path: "Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs"
        issue: "Script exists and is substantive, but is orphaned -- not wired into the scene"
    missing:
      - "Attach MineralRenderer component to a GameObject in Assets/Scenes/Game.unity (e.g. create empty GameObject named 'MineralRenderer' and attach the script)"
human_verification:
  - test: "Play the game for one full 60-second run"
    expected: "Asteroids release gold spheres on death, spheres fly to ship, credits increment, HUD shows formatted credits and MM:SS timer, Collecting phase activates after timer, results screen appears with credits earned, Continue leads to Upgrade screen, Start Run resets and begins a new run"
    why_human: "Full session flow requires runtime Unity editor execution; visual elements and timing cannot be verified from static code analysis"
  - test: "Complete two runs and refresh/restart the application"
    expected: "Credits from prior session are restored on next play; credits do not reset to 0"
    why_human: "Save persistence requires actual file I/O to Application.persistentDataPath which only runs in Unity Player or build"
---

# Phase 3: Collection, Economy, and Session Verification Report

**Phase Goal:** The core game loop closes -- destroyed asteroids release minerals that fly to the ship, credits accumulate, timed runs end with a results screen, and all progress persists between sessions
**Verified:** 2026-02-17
**Status:** gaps_found
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Destroyed asteroids release mineral particles at their position | VERIFIED | `MineralSpawnSystem.cs` iterates asteroids with `HealthData.CurrentHP <= 0f`, spawns 3-8 mineral entities via ECB at asteroid position with XZ scatter |
| 2 | Mineral particles accelerate toward the ship and are collected on contact | VERIFIED | `MineralPullSystem.cs` uses `IJobEntity` with `MineralPullJob` accelerating toward `ShipPosition`; `MineralCollectionSystem.cs` destroys entities within `MineralCollectionRadius` |
| 3 | Collected minerals award credits to GameStateData.Credits based on tier | VERIFIED | `MineralCollectionSystem.cs` line 41: `gameStateRW.ValueRW.Credits += mineralData.ValueRO.CreditValue` |
| 4 | Mineral particles are visible as small colored spheres synced from ECS | FAILED | `MineralRenderer.cs` script is substantive and correct, but the component is NOT added to any GameObject in `Game.unity`. `grep "MineralRenderer" Assets/Scenes/Game.unity` returns no matches. Without scene attachment, the ECS-to-GameObject sync never runs and no minerals are visible. |
| 5 | Running credit total is displayed formatted with K/M/B/T suffixes | VERIFIED | `HUDController.cs` line 58: `_creditsText.text = NumberFormatter.Format((double)gameState.Credits)` |
| 6 | Visible countdown timer counts down from 60 seconds during Playing phase | VERIFIED | `PlayingState.cs` line 59: `data.Timer -= Time.deltaTime`; `HUDController.cs` formats as MM:SS |
| 7 | When timer hits 0, mining stops but minerals still fly to ship (Collecting phase) | VERIFIED | `PlayingState.Execute()` calls `TransitionTo(GamePhase.Collecting)` at 0; both `MineralPullSystem` and `MineralCollectionSystem` guard on `Playing \|\| Collecting` |
| 8 | When all minerals and asteroids are gone, results screen shows credits earned this run | VERIFIED | `CollectingState.cs` counts mineral+asteroid entities, transitions to GameOver after grace period; `GameOverState.Enter()` calls `resultsScreen.Show()` which computes `Credits - CreditsAtRunStart` |
| 9 | Player can proceed from results to upgrade screen and start a new run | VERIFIED | `ResultsScreen.OnContinueClicked()` calls `TransitionTo(Upgrading)`; `UpgradeScreen.OnStartRunClicked()` calls `ResetRun()` then `TransitionTo(Playing)` |
| 10 | Credits accumulate across multiple runs within a session | VERIFIED | `GameManager.ResetRun()` resets only Timer, not Credits; `PlayingState.Enter()` snapshots `CreditsAtRunStart` without resetting `Credits` |
| 11 | Game state saves to JSON on run end; saved credits restore on game start | VERIFIED | `GameOverState.Enter()` calls `SaveManager.Instance?.AutoSave()`; `PlayingState.Enter()` calls `LoadIntoECS()` on first run via `_saveLoaded` flag; `SaveManager.Save()` writes to `Application.persistentDataPath` |
| 12 | WebGL builds flush IndexedDB after each save | VERIFIED | `SaveManager.Save()` calls `WebGLHelper.FlushSaveData()` which calls `SyncIndexedDB()` in `#if UNITY_WEBGL && !UNITY_EDITOR` |

**Score:** 11/12 truths verified

---

### Required Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| `Assets/Scripts/ECS/Components/MineralComponents.cs` | VERIFIED | Contains `MineralTag`, `MineralData`, `MineralPullData`, `MineralsSpawnedTag`, `CollectionEvent` as `IComponentData`/`IBufferElementData` |
| `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` | VERIFIED | Burst-compiled ISystem, `[UpdateBefore(typeof(AsteroidDestructionSystem))]`, uses `WithNone<MineralsSpawnedTag>`, adds `MineralsSpawnedTag` via ECB |
| `Assets/Scripts/ECS/Systems/MineralPullSystem.cs` | VERIFIED | Burst-compiled ISystem with `MineralPullJob : IJobEntity`, `[WithAll(typeof(MineralTag))]`, distance guard, speed acceleration |
| `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` | VERIFIED | `[UpdateAfter(typeof(MineralPullSystem))]`, increments `Credits`, destroys entities via ECB |
| `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` | ORPHANED | Script is substantive (pool, LateUpdate sync, MaterialPropertyBlock) but NOT in Game.unity scene -- zero scene references |
| `Assets/Scripts/Data/ResourceTierSO.cs` | VERIFIED | `[CreateAssetMenu]`, contains all specified fields including `TierName`, `TierIndex`, `CreditValue`, `MineralsPerAsteroid`, `MineralColor`, `EmissiveIntensity` |
| `Assets/Scripts/Shared/GameConstants.cs` | VERIFIED | Contains `MineralInitialSpeed`, `MineralAcceleration`, `MineralCollectionRadius`, `MineralScale`, `DefaultRunDuration`, `CollectingGracePeriod`, `ShipPositionX` |
| `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` | VERIFIED | Timer initialized to `DefaultRunDuration`, `AddBuffer<CollectionEvent>` present |
| `Assets/Scripts/MonoBehaviours/UI/HUDController.cs` | VERIFIED | `NumberFormatter.Format`, `GameStateData` query, phase-based visibility, MM:SS timer format |
| `Assets/Scripts/MonoBehaviours/UI/ResultsScreen.cs` | VERIFIED | `CreditsAtRunStart` delta calculation, `TransitionTo(GamePhase.Upgrading)`, starts hidden |
| `Assets/Scripts/MonoBehaviours/UI/UpgradeScreen.cs` | VERIFIED | `ResetRun()` + `TransitionTo(GamePhase.Playing)` on button click, starts hidden |
| `Assets/Scripts/States/PlayingState.cs` | VERIFIED | `data.Timer -= Time.deltaTime`, `TransitionTo(GamePhase.Collecting)` at zero, `LoadIntoECS()` on first run |
| `Assets/Scripts/States/CollectingState.cs` | VERIFIED | Counts `MineralTag` + `AsteroidTag` entities, grace period timer, `TransitionTo(GamePhase.GameOver)` |
| `Assets/Scripts/States/GameOverState.cs` | VERIFIED | `SaveManager.Instance?.AutoSave()` in `Enter()`, `ResultsScreen.Show()` / `Hide()` |
| `Assets/Scripts/States/UpgradingState.cs` | VERIFIED | `UpgradeScreen.Show()` / `Hide()` via `FindAnyObjectByType` |
| `Assets/Scripts/MonoBehaviours/Core/GameManager.cs` | VERIFIED | `ResetRun()` destroys asteroid+mineral entities, `CreditsAtRunStart` property |
| `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` | VERIFIED | `CreateHUDCanvas`, `CreateResultsCanvas`, `CreateUpgradeCanvas`, `CreateButton` helper -- all called in `Awake()` |
| `Assets/Scripts/MonoBehaviours/Save/SaveData.cs` | VERIFIED | `[Serializable]`, `SaveVersion = 1`, `TotalCredits` (long), `CurrentLevel`, `TechTreeUnlocks`, `PlayerStatsData` |
| `Assets/Scripts/MonoBehaviours/Save/SaveManager.cs` | VERIFIED | `[RuntimeInitializeOnLoadMethod]` self-instantiation, `Save()` with `WebGLHelper.FlushSaveData()` + PlayerPrefs fallback, `Load()` with file+PlayerPrefs+fresh-save chain, `AutoSave()`, `LoadIntoECS()` |

---

### Key Link Verification

| From | To | Via | Status | Evidence |
|------|----|-----|--------|---------|
| `MineralSpawnSystem.cs` | `AsteroidDestructionSystem.cs` | `[UpdateBefore(typeof(AsteroidDestructionSystem))]` ordering | WIRED | Line 12: attribute present and confirmed |
| `MineralCollectionSystem.cs` | `GameStateComponents.cs` | `Credits += mineralData.ValueRO.CreditValue` | WIRED | Line 41: direct singleton RW write confirmed |
| `MineralRenderer.cs` | `MineralComponents.cs` | `EntityQuery` on `MineralTag + LocalTransform`, syncs to pooled GOs | NOT_WIRED | Script exists and queries MineralTag correctly, BUT component is not in scene -- query never executes at runtime |
| `PlayingState.cs` | `GameStateComponents.cs` | `data.Timer -= Time.deltaTime` + `TransitionTo(Collecting)` at 0 | WIRED | Lines 59-65 confirmed |
| `HUDController.cs` | `NumberFormatter.cs` | `NumberFormatter.Format((double)gameState.Credits)` | WIRED | Line 58 confirmed |
| `UpgradingState.cs` | `GameManager.cs` | Start Run calls `ResetRun()` then `TransitionTo(Playing)` | WIRED | `UpgradeScreen.cs` lines 89-90 confirmed |
| `SaveManager.cs` | `WebGLHelper.cs` | `WebGLHelper.FlushSaveData()` after every `File.WriteAllText` | WIRED | Line 63 confirmed |
| `GameOverState.cs` | `SaveManager.cs` | `SaveManager.Instance?.AutoSave()` in `Enter()` | WIRED | Line 10 confirmed |
| `PlayingState.cs` | `SaveManager.cs` | `SaveManager.Instance?.LoadIntoECS()` on first session run | WIRED | Lines 41-42 confirmed |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| MINR-01 | 03-01 | Destroyed asteroids release mineral particles | SATISFIED | `MineralSpawnSystem` spawns 3-8 minerals on HP<=0 |
| MINR-02 | 03-01 | Minerals accelerate toward player's ship | SATISFIED | `MineralPullSystem` IJobEntity, `MineralPullJob.Execute()` accelerates toward `ShipPosition` |
| MINR-03 | 03-01 | Minerals collected on contact award credits | SATISFIED | `MineralCollectionSystem` awards `CreditValue` on distance <= `MineralCollectionRadius` |
| MINR-04 | 03-01 | Mineral particles have tier-based visual appearance | PARTIAL | Phase 3 only implements a single Iron tier with hardcoded gold/amber color. `ResourceTierSO` data model and renderer are ready for Phase 6 multi-tier colors. This is in-scope for Phase 3 (single tier only) per plan. |
| ECON-01 | 03-01 | Credits are universal currency from all collected minerals | SATISFIED | `GameStateData.Credits` is the single accumulator, incremented by `MineralCollectionSystem` |
| ECON-02 | 03-02 | Running credit total displayed during gameplay | SATISFIED | `HUDController.LateUpdate()` reads `Credits` and formats with `NumberFormatter` |
| ECON-03 | 03-02 | Credits persist between runs within session | SATISFIED | `GameManager.ResetRun()` only resets Timer; credits preserved. `CreditsAtRunStart` tracks per-run delta. |
| ECON-04 | 03-01 | Credit values per tier are data-driven | SATISFIED | `ResourceTierSO.CreditValue` field; `GameConstants.DefaultCreditValuePerMineral` for Iron tier |
| SESS-01 | 03-02 | Timed mining runs with visible countdown timer | SATISFIED | `PlayingState` decrements `GameStateData.Timer`; `HUDController` shows MM:SS |
| SESS-02 | 03-02 | Timer expiry transitions to Collecting state | SATISFIED | `PlayingState.Execute()` calls `TransitionTo(GamePhase.Collecting)` at `Timer <= 0f` |
| SESS-03 | 03-02 | All minerals collected transitions to GameOver | SATISFIED | `CollectingState` counts mineral+asteroid entities, transitions after grace period |
| SESS-04 | 03-02 | Results screen shows credits earned this run | SATISFIED | `ResultsScreen.Show()` computes `Credits - GameManager.CreditsAtRunStart` |
| SESS-05 | 03-02 | Player can proceed to Upgrade screen from results | SATISFIED | `ResultsScreen.OnContinueClicked()` calls `TransitionTo(GamePhase.Upgrading)` |
| SESS-06 | 03-02 | Player can start new run from Upgrade screen | SATISFIED | `UpgradeScreen.OnStartRunClicked()` calls `ResetRun()` then `TransitionTo(GamePhase.Playing)` |
| SAVE-01 | 03-03 | Game state saves to JSON in Application.persistentDataPath | SATISFIED | `SaveManager.Save()` writes to `Path.Combine(Application.persistentDataPath, "astrominer_save.json")` |
| SAVE-02 | 03-03 | Auto-save on run end (partial -- tech tree deferred to Phase 6) | PARTIAL | Run-end auto-save implemented in `GameOverState.Enter()`. Tech-tree-purchase trigger explicitly deferred to Phase 6 per plan frontmatter. |
| SAVE-03 | 03-03 | Save includes credits, tech tree state, level, player stats (partial) | PARTIAL | `SaveData` contains `TotalCredits`, `CurrentLevel`, `TechTreeUnlocks` (empty placeholder), `PlayerStatsData` (default placeholder). Placeholder fields explicitly noted for Phase 6 population per plan. |
| SAVE-04 | 03-03 | WebGL builds flush IndexedDB after each save | SATISFIED | `WebGLHelper.FlushSaveData()` called in `SaveManager.Save()` with correct `#if UNITY_WEBGL && !UNITY_EDITOR` guard |
| SAVE-05 | 03-03 | Save file includes version number for future migration | SATISFIED | `SaveData.SaveVersion = 1` field present |

**Orphaned requirements check:** All 19 requirement IDs listed in phase plans (MINR-01 through MINR-04, ECON-01 through ECON-04, SESS-01 through SESS-06, SAVE-01 through SAVE-05) are accounted for in REQUIREMENTS.md and all are mapped to Phase 3. No orphaned requirements.

---

### Anti-Patterns Found

| File | Pattern | Severity | Assessment |
|------|---------|----------|-----------|
| `SaveData.cs` lines 29, 36 | "PLACEHOLDER" in doc comments | Info | Intentional -- documents fields that are correctly pre-included for Phase 6 extensibility. Not a code stub; fields are serialized and functional. |
| `UISetup.cs` line 307 | "Tech tree coming in Phase 6" | Info | Intentional placeholder UI text on UpgradeScreen -- by design for Phase 3. |
| `MineralRenderer.cs` (scene) | Component exists in code but NOT in scene | Blocker | `Game.unity` has `AsteroidRenderer` but no `MineralRenderer`. Mineral ECS entities will be updated by the pull/collection systems, but no visual sync occurs. The game loop partially closes but minerals are invisible. |

---

### Human Verification Required

#### 1. Full Session Flow

**Test:** Open `Assets/Scenes/Game.unity` in the Unity Editor and Press Play. After adding MineralRenderer to the scene, mine asteroids until they die.
**Expected:** Gold/amber spheres spawn at asteroid death position, accelerate toward ship, disappear on contact. HUD credits increment. Timer counts down from 1:00. At 0:00 the phase changes to Collecting. Once all entities clear (2s grace), results screen appears with "credits earned" this run. Click Continue to see Upgrade screen. Click Start Run for a fresh run with clean entities and accumulated credits.
**Why human:** Runtime Unity execution required for all visual/interaction verification.

#### 2. Cross-Session Save Persistence

**Test:** Complete one run, note credit total. Close Play mode (or build and close app). Reopen and press Play.
**Expected:** Credit total from prior session is restored to ECS (visible in HUD) before the first run starts.
**Why human:** Requires `Application.persistentDataPath` file I/O which only executes in Player context.

#### 3. WebGL IndexedDB Flush

**Test:** Build for WebGL and play in browser. Complete a run. Refresh the page.
**Expected:** Credits persist across page refresh.
**Why human:** `WebGLHelper.FlushSaveData()` calls `SyncIndexedDB()` which requires a WebGL build with the `Assets/Plugins/WebGL/IndexedDBSync.jslib` plugin.

---

### Gaps Summary

**One gap blocks full goal achievement:**

The `MineralRenderer` MonoBehaviour was created correctly (substantive implementation, correct pool size, LateUpdate sync to ECS entities, MaterialPropertyBlock coloring) but was never added to a GameObject in `Game.unity`. The summary for plan 03-01 explicitly documented this as "User Setup Required -- MineralRenderer MonoBehaviour must be added to a GameObject in the Unity scene" but neither the executor nor a follow-up step completed it.

Without `MineralRenderer` in the scene, mineral ECS entities exist and are processed by `MineralSpawnSystem`, `MineralPullSystem`, and `MineralCollectionSystem` -- credits will still accumulate invisibly -- but the player sees no visual feedback of minerals flying to the ship. This breaks the "satisfying feel" of the core loop (minerals are supposed to visually fly to ship).

**Fix required:** Create an empty GameObject named "MineralRenderer" in Game.unity and attach the `MineralRenderer` MonoBehaviour script component to it.

**Root cause:** The executor correctly identified that MonoBehaviour scene attachment cannot be done from CLI, documented it as "User Setup Required", but the checkpoint was never completed. This is a scene wiring gap, not a code gap.

---

*Verified: 2026-02-17*
*Verifier: Claude (gsd-verifier)*
