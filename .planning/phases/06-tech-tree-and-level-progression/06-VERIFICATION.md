---
phase: 06-tech-tree-and-level-progression
verified: 2026-02-18T17:30:00Z
status: human_needed
score: 15/15 must-haves verified
re_verification:
  previous_status: passed
  previous_score: 13/13
  gaps_closed:
    - "Scroll wheel zooms the tech tree with reasonable sensitivity (~3-4 notches to traverse full range)"
    - "Tooltip appears near the hovered node and follows the mouse without jumping to corners"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Open Upgrade screen and verify tech tree renders with 5 branches radiating from a center START node"
    expected: "START node visible at center (0,0), Mining upper-left, Economy upper-right, Ship left, Run lower-left, Progression right. Nodes adjacent to START are green (affordable) or red (too expensive)."
    why_human: "UI layout and visual correctness cannot be verified by code inspection alone"
  - test: "Click a green node and verify purchase flow"
    expected: "Credits deduct immediately, node turns blue with scale-punch animation, adjacent hidden nodes reveal, credit display at top updates"
    why_human: "Runtime interaction and visual feedback requires a running game"
  - test: "Right-click drag and scroll wheel navigation"
    expected: "Pan follows drag delta; scroll traverses 0.3-2.0 zoom range in approximately 3-4 notches (ZoomSpeed=0.5f, /120 normalisation). Both use Mouse.current from New Input System."
    why_human: "Input behavior requires runtime testing; code confirms fix but feel needs runtime verification"
  - test: "Hover a visible node and verify tooltip"
    expected: "Tooltip appears near cursor (offset +15,-15 from mouse, with right/bottom clamping). Tooltip does NOT jump to screen corners. Anchor is center (0.5,0.5) so ScreenPointToLocalPointInRectangle coordinates align correctly."
    why_human: "Tooltip positioning anchor fix (0,0->0.5,0.5) confirmed in code; correct behavior requires runtime verification"
  - test: "Purchase an upgrade, quit game, reopen -- verify state persists"
    expected: "Purchased nodes remain blue; credits match post-purchase total; level persists"
    why_human: "Cross-session persistence requires runtime save/load cycle"
  - test: "Purchase 'Advance to Level 2' and start a new run -- verify Level 2 behavior"
    expected: "Some asteroids spawn as Copper (orange minerals worth 25 credits); asteroids have 1.3x HP; spawn settings unchanged vs Level 1"
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
**Verified:** 2026-02-18T17:30:00Z
**Status:** human_needed
**Re-verification:** Yes -- after UAT gap closure (plan 06-04 fixed zoom sensitivity and tooltip anchor)

## Re-Verification Context

The initial VERIFICATION.md (2026-02-18T16:30:00Z) declared `status: passed`. A UAT was subsequently conducted and revealed 2 runtime issues:

1. Scroll zoom too slow (minor): ZoomSpeed=0.1f with *0.01f multiplier gave ~14 notches for full range
2. Tooltip jumping to lower-left corner (major): anchor at (0,0) mismatched center-origin ScreenPointToLocalPointInRectangle output

Plan 06-04 was executed (commit `e242663`) with surgical fixes:
- `TechTreeController.cs` line 44: `ZoomSpeed = 0.5f`; line 643: `(scroll / 120f) * ZoomSpeed`
- `TechTreeTooltip.cs` lines 37-38: `anchorMin/Max = new Vector2(0.5f, 0.5f)`

This re-verification confirms both fixes are in the codebase and verifies all 15 must-haves.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | All tech tree data (5 branches, ~40 nodes, costs, effects, prerequisites) is defined in code | VERIFIED | `TechTreeDefinitions.cs`: 41 `NodeDescriptor` instances (START + 11 Mining + 8 Economy + 13 Ship + 3 Run + 4 Progression). Two-pass build resolves prerequisites. All BaseCosts and TierLevels match plan. |
| 2 | New ECS singletons (PlayerBonusData, RunConfigData) exist for economy bonuses and run configuration | VERIFIED | `EconomyComponents.cs`: both structs implement `IComponentData`. `RunConfigData` includes `TierWeight0-5` fields for Burst-accessible weighted random. `ECSBootstrap.cs` creates both with `GameConstants` defaults including `TierWeight0=100f` for Level 1 Iron-only bootstrap. |
| 3 | SaveData persists tech tree unlock state and all upgradeable stat values across sessions | VERIFIED | `SaveData.cs` v2: `bool[] TechTreeUnlocks`, `bool[] SkillUnlocks = new bool[4]`, `PlayerStatsData` with 25 fields. `SaveManager.AutoSave()` reads all 7 ECS singletons. `LoadIntoECS()` restores all. `MigrateIfNeeded()` handles v1->v2 with backward compat skill unlock preservation. |
| 4 | Skills default to locked in ECSBootstrap ready for tech tree purchases | VERIFIED | `ECSBootstrap.cs` lines 97-104: `Skill1Unlocked = false`, `Skill2Unlocked = false`, `Skill3Unlocked = false`, `Skill4Unlocked = false` |
| 5 | Level configs for 5 levels with drop tables and HP multipliers are defined | VERIFIED | `LevelConfigDefinitions.cs`: L1 Iron-only (1.0x HP), L2 Iron+Copper (1.3x), L3 +Silver (1.7x, 1.3s spawn), L4 +Cobalt (2.2x, max 60), L5 all 6 tiers (3.0x, max 70) |
| 6 | Tech tree UI displays between runs with 5-branch center-outward graph | VERIFIED | `TechTreeController.cs` (804 lines): `Initialize`, `BuildNodeGraph`, `BuildConnectionLines`, `RefreshAllNodeStates`. `UISetup.CreateUpgradeCanvas` adds `TechTreeController`, calls `TechTreeDefinitions.BuildTree()`, `techTreeController.Initialize(...)`, `UpgradeScreen.TechTreeController = techTreeController` |
| 7 | 4-state node color coding (blue/green/red/hidden) with prerequisite gating | VERIFIED | `TechTreeNode.cs`: `NodeState` enum, `UpdateState()` with hex colors `#3498DB`/`#2ECC71`/`#E74C3C`. `TechTreeController.IsRevealed()` and `CanAfford()` enforce prerequisite gating per TECH-07 |
| 8 | Purchase applies stat effects immediately to ECS singletons | VERIFIED | `TechTreeController.ApplyStatEffect()` covers all 27 `StatTarget` values across `MiningConfigData`, `CritConfigData`, `PlayerBonusData`, `RunConfigData`, `SkillUnlockData`, `SkillStatsData`. Called before `RefreshAllNodeStates()` in `OnNodeClicked()`. |
| 9 | Pan/zoom uses New Input System (Mouse.current) | VERIFIED | `TechTreeController.cs` line 9: `using UnityEngine.InputSystem`. Line 640: `Mouse.current.scroll.ReadValue().y`. Line 643: `(scroll / 120f) * ZoomSpeed` with `ZoomSpeed = 0.5f`. Lines 661-663: `Mouse.current.rightButton.isPressed`, `Mouse.current.middleButton.isPressed`. |
| 10 | Hover tooltip shows name, cost, description, and stat preview with correct positioning | VERIFIED | `TechTreeTooltip.cs` (286 lines): full UI hierarchy, `Show()` formats name/cost/description/stat preview via `controller.GetCurrentStatValue()`. Lines 37-38: `anchorMin = new Vector2(0.5f, 0.5f)` (gap closure fix). `UpdatePosition()` clamps with center-origin halfW/halfH. |
| 11 | 5 levels with distinct drop tables; higher levels spawn rarer tiers | VERIFIED | `AsteroidSpawnSystem.cs`: `RequireForUpdate<RunConfigData>()`, `PickResourceTier(ref rng, runConfig)` uses `TierWeight0-5`. `PlayingState.ApplyLevelConfig()` writes level config into `RunConfigData` at every run start. Drop tables confirmed in `LevelConfigDefinitions.cs`. |
| 12 | 6 resource tiers with distinct mineral colors applied to rendered minerals | VERIFIED | `ResourceTierDefinitions.cs`: Iron grey (0.6,0.6,0.6), Copper orange (0.9,0.5,0.2), Silver white (0.9,0.9,0.95), Cobalt blue (0.3,0.5,0.95), Gold yellow (1.0,0.85,0.2), Titanium purple (0.8,0.3,0.9). `MineralRenderer.ConfigureMineralVisual()` calls `ResourceTierDefinitions.GetTier()` and applies via `MaterialPropertyBlock`. |
| 13 | Economy bonuses (ResourceMultiplier, LuckyStrike) apply during mineral collection | VERIFIED | `MineralCollectionSystem.cs`: `RequireForUpdate<PlayerBonusData>()`, `finalCredits = (int)(baseCredits * bonus.ResourceMultiplier)`, `rng.NextFloat(0f, 1f) < bonus.LuckyStrikeChance` for 2x. Final value written to `GameState.Credits` and `CollectionEvent.CreditValue`. |
| 14 | Scroll zoom traverses full range in ~3-4 notches (gap closure) | VERIFIED | `TechTreeController.cs` line 44: `const float ZoomSpeed = 0.5f`. Line 643: `float newZoom = Mathf.Clamp(currentZoom + (scroll / 120f) * ZoomSpeed, MinZoom, MaxZoom)`. Math: 120 * 0.5 / 120 = 0.5 zoom/notch; range 0.3-2.0 = 1.7 / 0.5 = 3.4 notches. |
| 15 | Tooltip anchor is center-aligned (gap closure) | VERIFIED | `TechTreeTooltip.cs` line 37: `tooltipRect.anchorMin = new Vector2(0.5f, 0.5f)`. Line 38: `tooltipRect.anchorMax = new Vector2(0.5f, 0.5f)`. Pivot remains `(0, 1)` for top-left tooltip expansion. Clamping logic unchanged -- correct for center-origin coordinates. |

**Score:** 15/15 truths verified

---

## Required Artifacts

| Artifact | Status | Details |
|---------|--------|---------|
| `Assets/Scripts/Data/UpgradeNodeSO.cs` | VERIFIED | `class UpgradeNodeSO : ScriptableObject`, `ActualCost` computed property with `TierMultipliers = {1,3,8}`, `CreateAssetMenu` attribute |
| `Assets/Scripts/Data/TechTreeSO.cs` | VERIFIED | `StartNode`, `AllNodes[]`, `GetNodeIndex(string)` helper |
| `Assets/Scripts/Data/LevelConfigSO.cs` | VERIFIED | All required fields including `ResourceTierWeight[] DropTable`, `CreateAssetMenu` attribute |
| `Assets/Scripts/Data/TechTreeDefinitions.cs` | VERIFIED | 41 `NodeDescriptor` instances (1 START + 40 upgrade nodes across 5 branches). Two-pass build resolves prerequisites. |
| `Assets/Scripts/Data/LevelConfigDefinitions.cs` | VERIFIED | 5 `LevelConfig` entries: correct HP multipliers (1.0/1.3/1.7/2.2/3.0), spawn overrides, drop tables |
| `Assets/Scripts/Data/ResourceTierDefinitions.cs` | VERIFIED | 6 tiers with `GameConstants` credit values, correct colors, emissive intensities, mineral count ranges |
| `Assets/Scripts/ECS/Components/EconomyComponents.cs` | VERIFIED | `PlayerBonusData` and `RunConfigData` both implement `IComponentData`; `RunConfigData` has `TierWeight0-5` |
| `Assets/Scripts/MonoBehaviours/Save/SaveData.cs` | VERIFIED | `SaveVersion = 2`, `bool[] TechTreeUnlocks`, `bool[] SkillUnlocks = new bool[4]`, `PlayerStatsData` with 25 fields |
| `Assets/Scripts/MonoBehaviours/Save/SaveManager.cs` | VERIFIED | `AutoSave()` reads all 7 ECS singletons, `LoadIntoECS()` restores all, `MigrateIfNeeded()` handles v1->v2, `SaveTechTreeState(bool[])` for immediate persistence |
| `Assets/Scripts/MonoBehaviours/Core/ECSBootstrap.cs` | VERIFIED | Both singletons created with `GameConstants` defaults; `SkillUnlockData` all false; `RunConfigData.TierWeight0 = 100f` |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeController.cs` | VERIFIED | 804 lines, all required methods present; `ApplyStatEffect` covers all 27 StatTarget values; `ZoomSpeed = 0.5f`; `(scroll / 120f) * ZoomSpeed` normalization; `using UnityEngine.InputSystem` |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeNode.cs` | VERIFIED | `NodeState` enum, `UpdateState()` with 4-state colors, `PlayPurchaseEffect()` coroutine (scale punch 1.0->1.2->1.0), `IPointerEnterHandler` + `IPointerExitHandler` |
| `Assets/Scripts/MonoBehaviours/UI/TechTreeTooltip.cs` | VERIFIED | Full UI hierarchy, `Show()` with stat preview via `controller.GetCurrentStatValue()`, center anchor `(0.5f, 0.5f)` (gap closure), clamping to screen bounds |
| `Assets/Scripts/ECS/Systems/AsteroidSpawnSystem.cs` | VERIFIED | `RequireForUpdate<RunConfigData>()`, reads `runConfig.AsteroidHPMultiplier`, `PickResourceTier()` with `TierWeight0-5` weighted random, adds `AsteroidResourceTier` component |
| `Assets/Scripts/ECS/Systems/MineralCollectionSystem.cs` | VERIFIED | `RequireForUpdate<PlayerBonusData>()`, multiplier applied, Lucky Strike rolls `rng.NextFloat() < bonus.LuckyStrikeChance` for 2x |
| `Assets/Scripts/ECS/Systems/MineralSpawnSystem.cs` | VERIFIED | Queries `AsteroidResourceTier`, `GetCreditValueForTier()` and `GetMineralCountRange()` Burst-safe via if/else chains |
| `Assets/Scripts/MonoBehaviours/Rendering/MineralRenderer.cs` | VERIFIED | `mineralQuery` includes `MineralData`, `em.GetComponentData<MineralData>(entity).ResourceTier` passed to `ConfigureMineralVisual()` which calls `ResourceTierDefinitions.GetTier()` |
| `Assets/Scripts/States/PlayingState.cs` | VERIFIED | `ApplyLevelConfig()` called in `Enter()`. Reads `RunConfigData.RunDuration` for timer. Applies drop table, HP mult, spawn settings, and syncs `AsteroidSpawnTimer`. |
| `Assets/Scripts/MonoBehaviours/Core/AsteroidRenderer.cs` | VERIFIED | `ConfigureAsteroidVisual(go, maxHP)` with `float hpRatio = maxHP / GameConstants.DefaultAsteroidHP; float hpScaleBonus = (hpRatio - 1f) * 0.3f` |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `TechTreeController.cs` | `ECS/Components/EconomyComponents.cs` | `ApplyStatEffect` writes `PlayerBonusData`, `RunConfigData` | WIRED | Direct `em.GetComponentData<PlayerBonusData>` and `em.GetComponentData<RunConfigData>` confirmed in switch cases |
| `TechTreeController.cs` | `ECS/Components/SkillComponents.cs` | `ApplyStatEffect` writes `SkillStatsData`, `SkillUnlockData` | WIRED | Both singleton entities cached in `TryInitECS()`; all skill stat cases in `ApplyStatEffect()` confirmed |
| `TechTreeController.cs` | `SaveManager.cs` | Persists tech tree unlocks after each purchase | WIRED | `OnNodeClicked()`: `save.TechTreeUnlocks = (bool[])purchasedNodes.Clone(); UpdateSaveStats(save); SaveManager.Instance.Save(save)` |
| `UISetup.cs` | `TechTreeController.cs` | Creates canvas structure and initializes TechTreeController | WIRED | `upgradeCanvasGO.AddComponent<TechTreeController>()`, `TechTreeDefinitions.BuildTree()`, `techTreeController.Initialize(...)`, `UpgradeScreen.TechTreeController = techTreeController` |
| `AsteroidSpawnSystem.cs` | `EconomyComponents.cs` | Reads `RunConfigData` for HP multiplier, tier weights | WIRED | `RequireForUpdate<RunConfigData>()`, `SystemAPI.GetSingleton<RunConfigData>()`, `PickResourceTier(ref rng, runConfig)` |
| `MineralCollectionSystem.cs` | `EconomyComponents.cs` | Reads `PlayerBonusData` for ResourceMultiplier and LuckyStrikeChance | WIRED | `RequireForUpdate<PlayerBonusData>()`, `SystemAPI.GetSingleton<PlayerBonusData>()`, both fields used in credit calculation |
| `MineralRenderer.cs` | `ResourceTierDefinitions.cs` | Reads mineral entity `ResourceTier` to apply color | WIRED | `mineralQuery` includes `MineralData`, `em.GetComponentData<MineralData>(entity).ResourceTier` passed to `ConfigureMineralVisual()` which calls `ResourceTierDefinitions.GetTier()` |
| `SaveManager.cs` | `EconomyComponents.cs` | `LoadIntoECS` restores `PlayerBonusData`, `RunConfigData` from save | WIRED | Both restoration blocks confirmed at lines 388-413 of `SaveManager.cs` |
| `PlayingState.cs` | `LevelConfigDefinitions.cs` | Applies level config to `RunConfigData` at run start | WIRED | `LevelConfigDefinitions.GetLevelConfig(runConfig.CurrentLevel)` called in `ApplyLevelConfig()`, all fields written to `RunConfigData` and `AsteroidSpawnTimer` |
| `TechTreeTooltip.cs` | Center anchor (0.5, 0.5) | Fixes coordinate mismatch with `ScreenPointToLocalPointInRectangle` | WIRED | `tooltipRect.anchorMin = new Vector2(0.5f, 0.5f)` / `anchorMax = new Vector2(0.5f, 0.5f)` at lines 37-38 (gap closure commit `e242663`) |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| TECH-01 | 06-02 | Tech tree UI between runs with 5 branches | SATISFIED | `TechTreeController` + `UISetup.CreateUpgradeCanvas` builds 5-branch graph, shown in Upgrading phase |
| TECH-02 | 06-01/06-02 | Mining branch nodes (Radius I/II/III, Damage I/II, Rate I/II, Crit Chance, Crit Multi, DoT I/II) | SATISFIED | All 11 Mining nodes confirmed in `TechTreeDefinitions.GetAllDescriptors()` with correct IDs |
| TECH-03 | 06-01/06-02 | Economy branch nodes (Resource Multi I/II/III, Lucky Strike I/II/III, Abundance I/II) | SATISFIED | All 8 Economy nodes confirmed in `TechTreeDefinitions` |
| TECH-04 | 06-01/06-02 | Ship branch nodes (Laser I/II/III, Chain I/II/III, EMP I/II/III, Overcharge I/II/III, Combo Mastery) | SATISFIED | All 13 Ship nodes confirmed in `TechTreeDefinitions` |
| TECH-05 | 06-01/06-02/06-04 | Run branch nodes (Level Time I/II/III) | SATISFIED | All 3 Run nodes confirmed in `TechTreeDefinitions`; gap closure plan claimed TECH-05 in `requirements-completed` |
| TECH-06 | 06-01/06-02/06-04 | Progression branch nodes (Advance to Level 2/3/4/5) | SATISFIED | All 4 Progression nodes confirmed in `TechTreeDefinitions` with AdvanceLevel effects |
| TECH-07 | 06-02 | Prerequisite gating enforced | SATISFIED | `TechTreeController.CanAfford()` checks all prerequisites must be purchased; `IsRevealed()` only shows when at least one prereq purchased |
| TECH-08 | 06-01 | Tier cost multipliers 1x/3x/8x | SATISFIED | `UpgradeNodeSO.ActualCost` computes `BaseCost * TierMultipliers[tier-1]` with `{1,3,8}` |
| TECH-09 | 06-02 | Purchasing immediately applies stat effect | SATISFIED | `TechTreeController.OnNodeClicked()` calls `ApplyStatEffect()` before `RefreshAllNodeStates()` |
| TECH-10 | 06-01 | Tech tree data in ScriptableObjects | SATISFIED | `UpgradeNodeSO`, `TechTreeSO`, `LevelConfigSO` all have `CreateAssetMenu` attributes; runtime instances via `ScriptableObject.CreateInstance` |
| LEVL-01 | 06-03 | Fixed 5 levels with predefined drop table configs | SATISFIED | `LevelConfigDefinitions.cs` defines 5 levels; `PlayingState.ApplyLevelConfig()` applies at run start |
| LEVL-02 | 06-03 | Higher levels: rarer tiers, tougher asteroids | SATISFIED | L1=1.0x HP Iron-only through L5=3.0x HP all 6 tiers; `AsteroidSpawnSystem` HP formula includes `runConfig.AsteroidHPMultiplier` |
| LEVL-03 | 06-02 | Level advancement requires credit threshold + Advance upgrade purchase | SATISFIED | Progression branch Advance nodes cost 3000/8000/20000/50000 credits; `AdvanceLevel` StatEffect increments `RunConfigData.CurrentLevel` |
| LEVL-04 | 06-01 | Upgrades carry over between levels | SATISFIED | All ECS singletons persist via `SaveManager.AutoSave()`; `LoadIntoECS()` restores on session load; no reset on level advance |
| LEVL-05 | 06-01 | Level configs in ScriptableObjects (drop rates, HP multipliers, spawn rate, thresholds) | SATISFIED | `LevelConfigSO.cs` with `DropTable`, `AsteroidHPMultiplier`, `SpawnIntervalOverride`, `AdvanceCreditThreshold` |
| ASTR-06 | 06-03 | 6 resource tiers (Iron, Copper, Silver, Cobalt, Gold, Titanium) with distinct visuals | SATISFIED | `ResourceTierDefinitions.cs` with 6 tiers and distinct colors; `MineralRenderer` applies per-tier color and emissive via `MaterialPropertyBlock` |

**All 16 phase requirements satisfied. No orphaned requirements found.**

---

## Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|---------|--------|
| None found | — | — | — |

Scanned all phase files (including the two modified by 06-04) for TODO/FIXME, `return null`, `return {}`, `return []`, console.log-only implementations, and placeholder text. None found in any of the 19 files created or modified by this phase.

---

## Human Verification Required

### 1. Tech Tree Visual Layout

**Test:** Launch game, play a run, proceed to the Upgrade screen
**Expected:** Five branches radiate from a center START node. Mining is upper-left, Economy upper-right, Ship left, Run lower-left, Progression right. Nodes adjacent to START are green (if affordable) or red. Nodes further from START are hidden.
**Why human:** UI layout and visual correctness cannot be verified by code inspection alone

### 2. Node Purchase Flow

**Test:** Click a green (affordable) node
**Expected:** Credits deduct immediately from the top display; node turns blue with a brief scale-punch animation (1.0->1.2->1.0 over 0.3s); adjacent hidden nodes reveal; credit display updates to reflect new balance; "cha-ching" SFX plays
**Why human:** Runtime interaction, coroutine animation, and visual feedback require a running game

### 3. Scroll Zoom Sensitivity (Gap Closure Verification)

**Test:** Scroll the mouse wheel up and down on the tech tree screen
**Expected:** Approximately 3-4 scroll notches should traverse the full zoom range (min 0.3x to max 2.0x). Zoom pivots around the mouse cursor position, not the screen center. Each notch feels like a meaningful zoom step.
**Why human:** Code confirms `ZoomSpeed = 0.5f` with `/120` normalization giving 0.5 zoom/notch, but feel requires runtime testing

### 4. Tooltip Positioning (Gap Closure Verification)

**Test:** Hover mouse over any visible tech tree node, especially near the center of the screen
**Expected:** Tooltip appears adjacent to the mouse cursor (offset +15 right, -15 down) and follows the mouse smoothly. Tooltip does NOT jump to the lower-left corner. When near screen edges, tooltip flips to stay within bounds.
**Why human:** Code confirms center anchor `(0.5f, 0.5f)` fix, but correct positioning behavior requires runtime testing

### 5. Cross-Session Save/Load

**Test:** Purchase several upgrades, quit the game, reopen and proceed to the Upgrade screen
**Expected:** Purchased nodes remain blue. Credits match the post-purchase total. Level is preserved.
**Why human:** Cross-session persistence requires a runtime save/load cycle

### 6. Level Advancement Effects

**Test:** Purchase "Advance to Level 2" node, start a new run
**Expected:** Some asteroids spawn as Copper (orange minerals worth 25 credits instead of grey Iron at 10); asteroids have slightly more HP than Level 1 (1.3x); the level transition is applied at run start
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

## Gap Closure Verification

### Gap 1: Scroll Zoom Sensitivity (CLOSED)

**Previous finding:** ZoomSpeed=0.1f with *0.01f multiplier gave 0.001 zoom per scroll unit (~14 notches for full range)

**Code confirmation:**
- `TechTreeController.cs` line 44: `private const float ZoomSpeed = 0.5f;`
- `TechTreeController.cs` line 643: `float newZoom = Mathf.Clamp(currentZoom + (scroll / 120f) * ZoomSpeed, MinZoom, MaxZoom);`
- Math: scroll=120 per notch; 120/120 * 0.5 = 0.5 zoom/notch; range 1.7 / 0.5 = 3.4 notches for full traversal

**Commit:** `e242663` (fix(06-04): correct zoom sensitivity and tooltip positioning)

**Status:** CLOSED in code. Awaiting runtime verification.

### Gap 2: Tooltip Positioning (CLOSED)

**Previous finding:** `anchorMin/Max = (0,0)` with pivot `(0,1)` caused coordinate mismatch -- clamping math assumed center-origin but anchors were bottom-left, pushing tooltip to lower-left corner

**Code confirmation:**
- `TechTreeTooltip.cs` line 37: `tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);`
- `TechTreeTooltip.cs` line 38: `tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);`
- Pivot remains `(0, 1)` -- correct for top-left expansion from cursor
- Clamping logic unchanged (was already correct for center-origin coordinates)

**Commit:** `e242663` (fix(06-04): correct zoom sensitivity and tooltip positioning)

**Status:** CLOSED in code. Awaiting runtime verification.

---

## Summary

Phase 6 delivers a complete tech tree and level progression system. All 19 source files (4 new data SOs, 7 new UI/definition/system files, 8 modified system/bootstrap/save files) are present, substantive, and correctly wired.

**Data layer (06-01):** UpgradeNodeSO and TechTreeSO provide a full ScriptableObject data model with tiered costs (1x/3x/8x), prerequisite gating, and 27 StatTarget values. LevelConfigSO enables per-level configuration. EconomyComponents.cs adds PlayerBonusData and RunConfigData singletons with Burst-accessible tier weight fields. SaveData v2 and SaveManager round-trip all ECS singletons with v1 backward compatibility migration.

**Tech tree UI (06-02):** TechTreeController builds a 40-node center-outward graph from TechTreeDefinitions at runtime (no .asset files needed), supports pan/zoom via New Input System (Mouse.current), enforces prerequisite gating, applies all 27 stat effects to ECS singletons on purchase, saves immediately, and progressively reveals nodes. TechTreeTooltip shows stat previews. UISetup wires everything programmatically.

**Level progression (06-03):** AsteroidSpawnSystem reads RunConfigData tier weights and HP multiplier. MineralSpawnSystem assigns per-tier credit values and counts. MineralCollectionSystem applies ResourceMultiplier and LuckyStrike bonuses. MineralRenderer applies tier-specific colors from ResourceTierDefinitions. AsteroidRenderer scales asteroid size with HP. PlayingState.ApplyLevelConfig() is the single point of truth for level config application at each run start.

**Gap closure (06-04):** Two targeted runtime-identified fixes: scroll zoom normalized by 120 for ~3.4 notches full traversal, tooltip anchor corrected to center (0.5, 0.5) eliminating lower-left corner jump bug. Both fixes confirmed in code at commit `e242663`.

All 15 automated checks pass. Items flagged for human verification are visual/runtime behaviors (layout, animation, input feel, color accuracy, zoom and tooltip behavior post-fix) that cannot be validated through code inspection.

---

_Verified: 2026-02-18T17:30:00Z_
_Verifier: Claude (gsd-verifier)_
_Re-verification after: UAT gaps closed by plan 06-04 (commit e242663)_
