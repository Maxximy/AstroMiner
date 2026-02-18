---
phase: 06-tech-tree-and-level-progression
verified: 2026-02-18T16:30:00Z
status: passed
score: 13/13 must-haves verified
re_verification: false
human_verification:
  - test: "Open Upgrade screen and verify tech tree renders with 5 branches radiating from a center START node"
    expected: "START node visible at center (0,0), Mining upper-left, Economy upper-right, Ship left, Run lower-left, Progression right. Nodes adjacent to START are green (affordable) or red (too expensive)."
    why_human: "UI layout and visual correctness cannot be verified by code inspection alone"
  - test: "Click a green node and verify purchase flow"
    expected: "Credits deduct immediately, node turns blue with scale-punch animation, adjacent hidden nodes reveal, credit display at top updates"
    why_human: "Runtime interaction and visual feedback requires a running game"
  - test: "Right-click drag and scroll wheel navigation"
    expected: "Pan follows drag delta; scroll zooms toward mouse cursor pivot point; both use Mouse.current from New Input System"
    why_human: "Input behavior requires runtime testing"
  - test: "Hover a visible node and verify tooltip"
    expected: "Tooltip appears near cursor showing node name, cost in gold text, description, and current->next stat preview in green. Tooltip does not clip off-screen edges."
    why_human: "Tooltip positioning and visual correctness requires runtime testing"
  - test: "Purchase an upgrade, quit game, reopen -- verify state persists"
    expected: "Purchased nodes remain blue; credits match post-purchase total; level persists"
    why_human: "Cross-session persistence requires runtime save/load cycle"
  - test: "Purchase 'Advance to Level 2' and start a new run -- verify Level 2 behavior"
    expected: "Some asteroids spawn as Copper (orange minerals worth 25 credits); asteroids have 1.3x HP; default spawn settings unchanged"
    why_human: "Level config application and visual mineral colors require runtime testing"
  - test: "Purchase 'Laser Burst' in the Ship branch -- verify skill unlocks"
    expected: "Laser Burst skill appears in the skill bar and can be activated with key 1"
    why_human: "SkillBarController visibility update requires runtime testing"
  - test: "Purchase Resource Multi I and verify economy bonus applies"
    expected: "Mineral collection earns more credits per mineral (1.15x base value visible in collection popups)"
    why_human: "Economy bonus effect on credit earnings requires runtime play"
---

# Phase 6: Tech Tree and Level Progression Verification Report

**Phase Goal:** The meta-game delivers long-term motivation -- players spend credits on a branching tech tree to become noticeably more powerful, then advance through levels with new resource tiers and tougher asteroids
**Verified:** 2026-02-18T16:30:00Z
**Status:** passed
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | All tech tree data (5 branches, ~40 nodes, costs, effects, prerequisites) is defined in code | VERIFIED | `TechTreeDefinitions.cs` builds 41 nodes at runtime via `ScriptableObject.CreateInstance`: 1 START + 11 Mining + 8 Economy + 13 Ship + 3 Run + 4 Progression = 40 upgrade nodes |
| 2 | New ECS singletons (PlayerBonusData, RunConfigData) exist for economy bonuses and run configuration | VERIFIED | `EconomyComponents.cs` defines both structs implementing `IComponentData`; `ECSBootstrap.cs` creates both with GameConstants defaults including Level 1 tier weights |
| 3 | SaveData persists tech tree unlock state and all upgradeable stat values across sessions | VERIFIED | `SaveData.cs` v2 has `bool[] TechTreeUnlocks`, `bool[] SkillUnlocks[4]`, and `PlayerStatsData` with all 25 upgradeable fields. `SaveManager` round-trips all ECS singletons in `AutoSave()` and `LoadIntoECS()` |
| 4 | Skills default to locked in ECSBootstrap ready for tech tree purchases | VERIFIED | `ECSBootstrap.cs` sets `Skill1Unlocked = false`, `Skill2Unlocked = false`, `Skill3Unlocked = false`, `Skill4Unlocked = false` |
| 5 | Level configs for 5 levels with drop tables and HP multipliers are defined | VERIFIED | `LevelConfigDefinitions.cs` defines 5 levels: L1 Iron-only (1.0x HP), L2 Iron+Copper (1.3x), L3 +Silver (1.7x, 1.3s spawn), L4 +Cobalt (2.2x, max 60), L5 all 6 tiers (3.0x, max 70) |
| 6 | Tech tree UI displays between runs with 5-branch center-outward graph | VERIFIED | `TechTreeController.cs` has full `Initialize`, `BuildNodeGraph`, `BuildConnectionLines`, `RefreshAllNodeStates`. `UISetup.CreateUpgradeCanvas` creates viewport-masked canvas and calls `TechTreeController.Initialize`. `UpgradeScreen.Show()` calls `TechTreeController.RefreshAllNodeStates()` |
| 7 | 4-state node color coding (blue/green/red/hidden) with prerequisite gating | VERIFIED | `TechTreeNode.UpdateState()` handles all 4 states with exact hex colors. `TechTreeController.IsRevealed()` and `CanAfford()` enforce prerequisite gating per TECH-07 |
| 8 | Purchase applies stat effects immediately to ECS singletons | VERIFIED | `TechTreeController.ApplyStatEffect()` covers all 27 `StatTarget` values, reads/writes the correct ECS singleton (MiningConfigData, CritConfigData, PlayerBonusData, RunConfigData, SkillUnlockData, SkillStatsData) |
| 9 | Pan/zoom uses New Input System (Mouse.current) | VERIFIED | `TechTreeController.HandleZoom()` reads `Mouse.current.scroll.ReadValue().y`; `HandlePan()` reads `Mouse.current.rightButton.isPressed` and `Mouse.current.middleButton.isPressed`; `using UnityEngine.InputSystem` confirmed |
| 10 | Hover tooltip shows name, cost, description, and stat preview | VERIFIED | `TechTreeTooltip.cs` builds complete UI hierarchy; `Show()` method formats name, cost (gold), description, and computes current->next stat preview via `controller.GetCurrentStatValue()`. Clamped to screen bounds |
| 11 | 5 levels with distinct drop tables; higher levels spawn rarer tiers | VERIFIED | `AsteroidSpawnSystem` reads `RunConfigData.TierWeight0-5` for weighted random tier selection via `PickResourceTier()`. `PlayingState.ApplyLevelConfig()` writes level config into RunConfigData at every run start. Drop tables confirmed in `LevelConfigDefinitions.cs` |
| 12 | 6 resource tiers with distinct mineral colors applied to rendered minerals | VERIFIED | `ResourceTierDefinitions.cs` defines 6 tiers: Iron grey (0.6,0.6,0.6), Copper orange (0.9,0.5,0.2), Silver white (0.9,0.9,0.95), Cobalt blue (0.3,0.5,0.95), Gold yellow (1.0,0.85,0.2), Titanium purple (0.8,0.3,0.9). `MineralRenderer.ConfigureMineralVisual()` reads `MineralData.ResourceTier` and calls `ResourceTierDefinitions.GetTier()` to apply color and emissive |
| 13 | Economy bonuses (ResourceMultiplier, LuckyStrike) apply during mineral collection | VERIFIED | `MineralCollectionSystem` reads `PlayerBonusData` singleton, computes `finalCredits = (int)(baseCredits * bonus.ResourceMultiplier)`, then rolls `rng.NextFloat() < bonus.LuckyStrikeChance` for 2x. Final value written to GameState and CollectionEvent |

**Score:** 13/13 truths verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|---------|---------|--------|---------|
| `Assets/Scripts/Data/UpgradeNodeSO.cs` | Individual upgrade node SO with ID, cost, tier, prerequisites, stat effects | VERIFIED | 56 lines, `class UpgradeNodeSO : ScriptableObject`, `ActualCost` computed property with `TierMultipliers = {1,3,8}`, `CreateAssetMenu` attribute |
| `Assets/Scripts/Data/TechTreeSO.cs` | Aggregation of all upgrade nodes plus START node reference | VERIFIED | `StartNode`, `AllNodes[]`, `GetNodeIndex(string)` helper |
| `Assets/Scripts/Data/LevelConfigSO.cs` | Per-level config SO with drop tables, HP multiplier, spawn overrides | VERIFIED | All required fields including `ResourceTierWeight[] DropTable`, `CreateAssetMenu` attribute |
| `Assets/Scripts/ECS/Components/EconomyComponents.cs` | PlayerBonusData and RunConfigData ECS singletons | VERIFIED | Both structs implement `IComponentData`; `RunConfigData` includes `TierWeight0-5` fields for Burst-accessible weighted random |
| `Assets/Scripts/MonoBehaviours/Save/SaveData.cs` | Expanded save schema with TechTreeUnlocks, SkillUnlocks, PlayerStatsData | VERIFIED | `SaveVersion = 2`, `bool[] TechTreeUnlocks`, `bool[] SkillUnlocks = new bool[4]`, `PlayerStatsData` with 25 fields |
| `Assets/Scripts/MonoBehaviours/Save/SaveManager.cs` | Full singleton round-trip, v1->v2 migration, SaveTechTreeState | VERIFIED | `AutoSave()` reads all 7 ECS singletons, `LoadIntoECS()` restores all. `MigrateIfNeeded()` handles v1->v2 with backward compat skill unlock preservation. `SaveTechTreeState(bool[])` for immediate persistence |
| `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` | Creates PlayerBonusData and RunConfigData singletons; skills locked by default | VERIFIED | Both singletons created with `GameConstants` defaults. `SkillUnlockData` all false. `RunConfigData.TierWeight0 = 100f` (Level 1 Iron-only bootstrap) |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeController.cs` | Main tech tree controller: node graph, pan/zoom, purchase, stat application | VERIFIED | 804 lines, all required methods present. `ApplyStatEffect` covers all 27 StatTarget values. ECS lazy init pattern. `using UnityEngine.InputSystem` |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeNode.cs` | Individual node UI: 4-state color, purchase animation, hover event handlers | VERIFIED | `NodeState` enum, `UpdateState()`, `PlayPurchaseEffect()` coroutine, `IPointerEnterHandler` + `IPointerExitHandler` |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeTooltip.cs` | Hover tooltip with stat preview, screen-clamped positioning | VERIFIED | Full UI hierarchy creation, `Show()` with stat preview via `controller.GetCurrentStatValue()`, `UpdatePosition()` with screen clamping |
| `Assets/Scripts/Data/TechTreeDefinitions.cs` | All 40 node definitions programmatically across 5 branches | VERIFIED | 41 `NodeDescriptor` instances (1 START + 40 upgrade nodes). All 5 branches covered. Two-pass build resolves prerequisites |
| `Assets/Scripts/Data/LevelConfigDefinitions.cs` | Runtime 5-level config with weighted drop tables | VERIFIED | 5 `LevelConfig` entries with correct HP multipliers, spawn overrides, and drop tables matching plan |
| `Assets/Scripts/Data/ResourceTierDefinitions.cs` | 6-tier resource data with correct colors and credit values | VERIFIED | All 6 tiers with GameConstants credit values, correct colors, emissive intensities, mineral count ranges |
| `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs` | Reads RunConfigData for HP multiplier and tier selection | VERIFIED | `RequireForUpdate<RunConfigData>()`, reads `runConfig.AsteroidHPMultiplier` for HP scaling, `PickResourceTier()` uses TierWeight0-5 |
| `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` | Reads asteroid tier for per-tier credit values and mineral counts | VERIFIED | Queries `AsteroidResourceTier` component, `GetCreditValueForTier()` and `GetMineralCountRange()` both Burst-safe via if/else chains |
| `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` | ResourceMultiplier and LuckyStrike bonus application | VERIFIED | `RequireForUpdate<PlayerBonusData>()`, multiplier applied, Lucky Strike rolls `rng.NextFloat() < bonus.LuckyStrikeChance` for 2x |
| `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` | Per-tier mineral colors from ResourceTierDefinitions | VERIFIED | `mineralQuery` includes `MineralData`, `ConfigureMineralVisual(go, mineralData.ResourceTier)` calls `ResourceTierDefinitions.GetTier()` |
| `Assets/Scripts/States/PlayingState.cs` | ApplyLevelConfig on run start; RunDuration from RunConfigData | VERIFIED | `ApplyLevelConfig()` called in `Enter()`. Reads `RunConfigData.RunDuration` for timer. Applies drop table, HP mult, spawn settings, and syncs `AsteroidSpawnTimer` |
| `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` | HP-based asteroid size scaling | VERIFIED | `ConfigureAsteroidVisual(go, maxHP)` with `float hpRatio = maxHP / GameConstants.DefaultAsteroidHP; float scale = 1f + (hpRatio - 1f) * 0.3f` |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `TechTreeController.cs` | `ECS/Components/EconomyComponents.cs` | `ApplyStatEffect` writes `PlayerBonusData`, `RunConfigData` | WIRED | Direct `em.GetComponentData<PlayerBonusData>` and `em.GetComponentData<RunConfigData>` confirmed in switch cases |
| `TechTreeController.cs` | `ECS/Components/SkillComponents.cs` | `ApplyStatEffect` writes `SkillStatsData`, `SkillUnlockData` | WIRED | Both singleton entities cached in `TryInitECS()`; all skill stat cases in `ApplyStatEffect()` confirmed |
| `TechTreeController.cs` | `SaveManager.cs` | Persists tech tree unlocks after each purchase | WIRED | `OnNodeClicked()` line 344: `SaveManager.Instance.Save(save)` after updating `TechTreeUnlocks` and calling `UpdateSaveStats()` |
| `UISetup.cs` | `TechTreeController.cs` | Creates canvas structure and initializes TechTreeController | WIRED | Lines 362-382: `upgradeCanvasGO.AddComponent<TechTreeController>()`, `TechTreeDefinitions.BuildTree()`, `techTreeController.Initialize(...)`, `UpgradeScreen.TechTreeController = techTreeController` |
| `AsteroidSpawnSystem.cs` | `EconomyComponents.cs` | Reads `RunConfigData` for HP multiplier, tier weights, spawn config | WIRED | `RequireForUpdate<RunConfigData>()`, `SystemAPI.GetSingleton<RunConfigData>()`, `PickResourceTier(ref rng, runConfig)` |
| `MineralCollectionSystem.cs` | `EconomyComponents.cs` | Reads `PlayerBonusData` for ResourceMultiplier and LuckyStrikeChance | WIRED | `RequireForUpdate<PlayerBonusData>()`, `SystemAPI.GetSingleton<PlayerBonusData>()`, both fields used in credit calculation |
| `MineralRenderer.cs` | `ResourceTierDefinitions.cs` | Reads mineral entity `ResourceTier` to apply color | WIRED | `mineralQuery` includes `MineralData`, `em.GetComponentData<MineralData>(entity).ResourceTier` passed to `ConfigureMineralVisual()` which calls `ResourceTierDefinitions.GetTier()` |
| `SaveManager.cs` | `EconomyComponents.cs` | `LoadIntoECS` restores `PlayerBonusData`, `RunConfigData` from save | WIRED | Both restoration blocks confirmed in `LoadIntoECS()` |
| `PlayingState.cs` | `LevelConfigDefinitions.cs` | Applies level config to `RunConfigData` at run start | WIRED | `LevelConfigDefinitions.GetLevelConfig(runConfig.CurrentLevel)` called in `ApplyLevelConfig()`, all fields written to `RunConfigData` and `AsteroidSpawnTimer` |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| TECH-01 | 06-02 | Tech tree UI between runs with 5 branches | SATISFIED | `TechTreeController` + `UISetup.CreateUpgradeCanvas` builds 5-branch graph, shown in Upgrading phase |
| TECH-02 | 06-01/06-02 | Mining branch nodes (Radius I/II/III, Damage I/II, Rate I/II, Crit Chance, Crit Multi, DoT I/II) | SATISFIED | All 11 Mining nodes confirmed in `TechTreeDefinitions.GetAllDescriptors()` with correct IDs |
| TECH-03 | 06-01/06-02 | Economy branch nodes (Resource Multi I/II/III, Lucky Strike I/II/III, Abundance I/II) | SATISFIED | All 8 Economy nodes confirmed in `TechTreeDefinitions` |
| TECH-04 | 06-01/06-02 | Ship branch nodes (Laser I/II/III, Chain I/II/III, EMP I/II/III, Overcharge I/II/III, Combo Mastery) | SATISFIED | All 13 Ship nodes confirmed in `TechTreeDefinitions` |
| TECH-05 | 06-01/06-02 | Run branch nodes (Level Time I/II/III) | SATISFIED | All 3 Run nodes confirmed in `TechTreeDefinitions` |
| TECH-06 | 06-01/06-02 | Progression branch nodes (Advance to Level 2/3/4/5) | SATISFIED | All 4 Progression nodes confirmed in `TechTreeDefinitions` with AdvanceLevel effects |
| TECH-07 | 06-02 | Prerequisite gating enforced | SATISFIED | `TechTreeController.CanAfford()` checks all prerequisites; `IsRevealed()` only shows node when at least one prereq purchased |
| TECH-08 | 06-01 | Tier cost multipliers 1x/3x/8x | SATISFIED | `UpgradeNodeSO.ActualCost` computes `BaseCost * TierMultipliers[tier-1]` with `{1,3,8}` |
| TECH-09 | 06-02 | Purchasing immediately applies stat effect | SATISFIED | `TechTreeController.OnNodeClicked()` calls `ApplyStatEffect()` before `RefreshAllNodeStates()` |
| TECH-10 | 06-01 | Tech tree data in ScriptableObjects | SATISFIED | `UpgradeNodeSO`, `TechTreeSO`, `LevelConfigSO` all have `CreateAssetMenu` attributes; runtime instances created via `ScriptableObject.CreateInstance` |
| LEVL-01 | 06-03 | Fixed 5 levels with predefined drop table configs | SATISFIED | `LevelConfigDefinitions.cs` defines 5 levels; `PlayingState.ApplyLevelConfig()` applies at run start |
| LEVL-02 | 06-03 | Higher levels: rarer tiers, tougher asteroids | SATISFIED | L1=1.0x HP Iron-only through L5=3.0x HP all 6 tiers; `AsteroidSpawnSystem` HP formula includes `runConfig.AsteroidHPMultiplier` |
| LEVL-03 | 06-02 | Level advancement requires credit threshold + Advance upgrade purchase | SATISFIED | Progression branch Advance nodes cost 3000/8000/20000/50000 credits; `AdvanceLevel` StatEffect increments `RunConfigData.CurrentLevel` |
| LEVL-04 | 06-01 | Upgrades carry over between levels | SATISFIED | All ECS singletons persist via `SaveManager.AutoSave()`; `LoadIntoECS()` restores on session load; no reset on level advance |
| LEVL-05 | 06-01 | Level configs in ScriptableObjects (drop rates, HP multipliers, spawn rate, thresholds) | SATISFIED | `LevelConfigSO.cs` with `DropTable`, `AsteroidHPMultiplier`, `SpawnIntervalOverride`, `AdvanceCreditThreshold` |
| ASTR-06 | 06-03 | 6 resource tiers (Iron, Copper, Silver, Cobalt, Gold, Titanium) with distinct visuals | SATISFIED | `ResourceTierDefinitions.cs` with 6 tiers and distinct colors; `MineralRenderer` applies per-tier color and emissive via `MaterialPropertyBlock` |

**All 16 phase requirements satisfied.** No orphaned requirements found.

---

## Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|---------|--------|
| None found | — | — | — |

Scanned all phase files for TODO/FIXME, `return null`, `return {}`, `return []`, `console.log`-only implementations, and placeholder text. None found in the 17 files created or modified by this phase.

---

## Human Verification Required

### 1. Tech Tree Visual Layout

**Test:** Launch game, play a run, proceed to the Upgrade screen
**Expected:** Five branches radiate from a center START node. Mining is upper-left, Economy upper-right, Ship left, Run lower-left, Progression right. Nodes adjacent to START are green (if affordable) or red. Nodes further from START are hidden.
**Why human:** UI layout and visual correctness cannot be verified by code inspection alone

### 2. Node Purchase Flow

**Test:** Click a green (affordable) node
**Expected:** Credits deduct immediately from the top display; node turns blue with a brief scale-punch animation; adjacent hidden nodes reveal; credit display at top updates to reflect new balance
**Why human:** Runtime interaction, coroutine animation, and visual feedback require a running game

### 3. Pan and Zoom Navigation

**Test:** Right-click drag to pan; scroll wheel to zoom
**Expected:** Pan: content panel moves with drag delta. Zoom: content scales toward mouse cursor position (not screen center). Both responsive without lag.
**Why human:** Input behavior requires runtime testing

### 4. Hover Tooltip Display

**Test:** Hover mouse over a visible (non-hidden) node
**Expected:** Tooltip appears near cursor with: node DisplayName in white/bold, cost in gold, separator line, description in light gray, stat preview in green (e.g., "Mining Radius: 2.5 -> 3.0"). Tooltip does not clip off screen edges.
**Why human:** Tooltip positioning and visual correctness requires runtime testing

### 5. Cross-Session Save/Load

**Test:** Purchase several upgrades, quit the game, reopen and proceed to the Upgrade screen
**Expected:** Purchased nodes remain blue. Credits match the post-purchase total. Level is preserved.
**Why human:** Cross-session persistence requires a runtime save/load cycle

### 6. Level Advancement Effects

**Test:** Purchase "Advance to Level 2" node, start a new run
**Expected:** Some asteroids spawn as Copper (orange minerals worth 25 credits instead of grey Iron at 10); asteroids have slightly more HP than Level 1; the level transition is applied at run start
**Why human:** Level config application and mineral color differences require runtime testing

### 7. Skill Unlock via Ship Branch

**Test:** Purchase the "Laser Burst" node in the Ship branch
**Expected:** Laser Burst skill appears in the skill bar and can be activated with key 1 during gameplay
**Why human:** SkillBarController visibility update and skill bar rendering require runtime testing

### 8. Economy Bonus Effects

**Test:** Purchase "Resource Multi I" and play a run
**Expected:** Each mineral collected awards 15% more credits (e.g., Iron minerals worth 11 or 12 instead of 10). Visible in floating collection numbers.
**Why human:** Economy bonus effect on credit earnings requires runtime play

---

## Summary

Phase 6 delivers a complete tech tree and level progression system. All 17 source files (4 new data SOs, 4 new UI/definition files, 9 modified system/bootstrap/save files) are present, substantive, and correctly wired.

**Data layer (06-01):** UpgradeNodeSO and TechTreeSO provide a full ScriptableObject data model with tiered costs (1x/3x/8x), prerequisite gating support, and 27 StatTarget values. LevelConfigSO enables per-level configuration. EconomyComponents.cs adds PlayerBonusData and RunConfigData singletons with Burst-accessible tier weight fields. SaveData v2 and SaveManager round-trip all ECS singletons with v1 backward compatibility migration.

**Tech tree UI (06-02):** TechTreeController builds a 40-node center-outward graph from TechTreeDefinitions at runtime (no .asset files needed), supports pan/zoom via New Input System (Mouse.current), enforces prerequisite gating, applies all 27 stat effects to ECS singletons on purchase, saves immediately, and progressively reveals nodes. TechTreeTooltip shows stat previews. UISetup wires everything programmatically consistent with the project's code-driven architecture.

**Level progression (06-03):** AsteroidSpawnSystem reads RunConfigData tier weights and HP multiplier. MineralSpawnSystem assigns per-tier credit values and counts. MineralCollectionSystem applies ResourceMultiplier and LuckyStrike bonuses. MineralRenderer applies tier-specific colors from ResourceTierDefinitions. AsteroidRenderer scales asteroid size with HP. PlayingState.ApplyLevelConfig() is the single point of truth for level config application at each run start.

The only items requiring human verification are visual/runtime behaviors (UI layout, animation, input feel, color accuracy) that cannot be validated through code inspection.

---

_Verified: 2026-02-18T16:30:00Z_
_Verifier: Claude (gsd-verifier)_
