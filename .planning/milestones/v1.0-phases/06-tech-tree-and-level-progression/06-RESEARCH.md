# Phase 6: Tech Tree and Level Progression - Research

**Researched:** 2026-02-18
**Domain:** Unity uGUI node-graph tech tree, ScriptableObject data models, idle game economy, ECS singleton stat application, level/resource tier progression
**Confidence:** HIGH -- all patterns verified against existing codebase; external research confirms standard idle game economy approaches

## Summary

Phase 6 is the meta-game capstone: a branching tech tree UI with 5 branches (Mining, Economy, Ship, Run, Progression), node purchase mechanics that modify ECS singletons at runtime, and a 5-level progression system introducing 6 resource tiers. The implementation sits entirely in the MonoBehaviour/Data layers (ScriptableObjects for data, uGUI for UI, MonoBehaviour for purchase logic), writing into existing ECS singletons (`MiningConfigData`, `CritConfigData`, `SkillStatsData`, `SkillUnlockData`, `SkillCooldownData`, `AsteroidSpawnTimer`, `GameStateData`) that are already consumed by Burst-compiled systems.

The codebase has been explicitly prepared for this phase: `SaveData` has `TechTreeUnlocks` and `PlayerStatsData` placeholders, `SkillUnlockData` defaults to all-unlocked with comments saying "Phase 6 flips to locked-by-default", `SkillStatsData` is the runtime-mutable singleton pattern designed for Phase 6 modification, and the `UpgradeScreen` has a placeholder "Tech tree coming in Phase 6" text. The primary engineering challenges are: (1) building a pan/zoom node-and-line graph UI programmatically via uGUI, (2) defining ~40 upgrade nodes with prerequisite chains and stat effects in ScriptableObjects, (3) expanding the existing single-resource-tier systems to support 6 tiers with per-level drop tables, and (4) balancing the economy so the first upgrade is affordable in 1-2 runs.

**Primary recommendation:** Build the tech tree data model first (ScriptableObjects with prerequisite references and stat effect definitions), then the purchase logic that writes to ECS singletons, then the UI, and finally the level/resource tier system. This order enables testing upgrade stat application before the full UI is ready.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
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
- Quick & satisfying: fast color transition (green to blue), "cha-ching" sound, brief particle burst on the node
- One click to buy -- no confirmation dialog, instant purchase on click
- Stat changes are gradual stacking: individual upgrades are subtle, but buying several in a branch feels strong
- First upgrade affordable in 1-2 runs: immediate hook
- Cost curve: increasing base cost for deeper nodes AND tier scaling (1x/3x/8x)
- Credits scale with level: higher levels yield rarer tiers worth more credits
- Seamless progression: "Advance to Level N" node turns blue like any other purchase
- Level 1: Iron only, Level 2: +Copper, Level 3: +Silver, Level 4: +Cobalt, Level 5: all 6 tiers
- Resource tier colors: Iron=grey, Copper=orange, Silver=white, Cobalt=blue, Gold=yellow, Titanium=purple/magenta
- Higher-HP asteroids are larger (size scales with HP) -- no color tinting

### Claude's Discretion
- Exact node layout algorithm and spacing for center-outward graph
- Pan/zoom implementation details and camera bounds
- Particle burst design for purchase feedback
- Exact base cost values and per-level credit targets (respecting 1-2 runs to first buy)
- Tooltip styling and positioning
- Animation timing for node state transitions

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| TECH-01 | Tech tree UI displayed between runs with 5 branches visible | uGUI Canvas with pan/zoom via RectTransform scaling on content panel; replaces existing UpgradeScreen placeholder; shown during Upgrading GamePhase |
| TECH-02 | Mining branch: Circle Radius (I/II/III), Damage (I/II), Rate (I/II), Crit Chance, Crit Multi, DoT (I/II) | 11 nodes writing to MiningConfigData (Radius, DamagePerTick, TickInterval) and CritConfigData (CritChance, CritMultiplier) singletons. DoT modifies GameConstants-seeded values or new DoT config singleton |
| TECH-03 | Economy branch: Resource Multiplier (I/II/III), Lucky Strike (I/II/III), Abundance (I/II) | 8 nodes. Resource Multiplier = new field on a PlayerBonus singleton. Lucky Strike = double drop chance. Abundance = modify AsteroidSpawnTimer.SpawnInterval |
| TECH-04 | Ship branch: Laser Burst (I/II/III), Chain Lightning (I/II/III), EMP (I/II/III), Overcharge (I/II/III), Combo Mastery | 13 nodes. First node of each skill = unlock (flips SkillUnlockData). Tiers II/III modify SkillStatsData fields. Combo Mastery = new bonus mechanism |
| TECH-05 | Run branch: Level Time (I/II/III) | 3 nodes modifying GameConstants.DefaultRunDuration equivalent (needs runtime-mutable singleton or field) |
| TECH-06 | Progression branch: Advance to Level N (one per level transition) | 4 nodes (Advance to 2, 3, 4, 5). Each increments SaveData.CurrentLevel and triggers level config swap |
| TECH-07 | Upgrades have prerequisite gating (must unlock parent before child) | UpgradeNodeSO.Prerequisites array; TechTreeController.CanPurchase() validates all prereqs in TechTreeUnlocks |
| TECH-08 | Upgrade costs scale per tier (1x/3x/8x base cost) | UpgradeNodeSO.BaseCost * TierMultiplier. Tier multipliers: [1, 3, 8] |
| TECH-09 | Purchasing an upgrade immediately applies its stat effect | TechTreeController writes to ECS singletons via EntityManager on main thread (same pattern as existing SkillBarController/InputBridge) |
| TECH-10 | Tech tree data is defined in ScriptableObjects for easy tuning | UpgradeNodeSO (per-node data) + TechTreeSO (collection of all nodes) + LevelConfigSO (per-level data) |
| LEVL-01 | Fixed set of levels (5+), each with a predefined drop table config | LevelConfigSO array. Each defines resource tier weights, HP multipliers, spawn rates |
| LEVL-02 | Higher levels introduce rarer resource tiers and tougher asteroids | Level config drop table adds tier entries. HP multiplier scales asteroid HealthData.MaxHP. Size scales with HP via AsteroidRenderer |
| LEVL-03 | Level advancement requires credit threshold + Advance upgrade purchase | Advance nodes in Progression branch have prerequisite = previous Advance + credit check (threshold stored in LevelConfigSO) |
| LEVL-04 | Upgrades carry over between levels (no resets) | TechTreeUnlocks bool array persists in SaveData. Only CurrentLevel increments on Advance |
| LEVL-05 | Level configs defined in ScriptableObjects | LevelConfigSO with fields: dropTable, hpMultiplier, spawnRate, advanceThreshold |
| ASTR-06 | 6 resource tiers: Iron, Copper, Silver, Cobalt, Gold, Titanium with distinct visuals | ResourceTierSO instances (class already exists in Data/). Mineral colors and asteroid size scaling configured per-tier. MineralRenderer must read ResourceTier from MineralData.ResourceTier to select color |
</phase_requirements>

## Standard Stack

### Core (already in project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| uGUI (com.unity.ugui) | 2.0.0 | Tech tree canvas UI | Already used for all game UI; UISetup creates programmatically |
| ScriptableObjects | Unity built-in | Tech tree node/level data definitions | Standard Unity data authoring; ResourceTierSO already exists |
| Unity.Entities | 1.4.4 | ECS singletons for stat application | All gameplay reads from ECS singletons; Phase 6 writes to them |
| TMPro | built-in | Node labels, tooltips, credit display | Already used across all existing UI |
| UnityEngine.InputSystem | 1.18.0 | Pan/zoom mouse input on tree UI | Project mandate: new Input System only |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| RectTransform scaling | Unity built-in | Zoom by scaling content panel's localScale | For scroll-wheel zoom on tech tree canvas |
| EventTrigger / IPointerHandler | Unity built-in | Hover tooltip, drag-to-pan, click-to-buy | For mouse interaction on tree nodes |
| Image.color / ColorBlock | Unity built-in | Node state color coding (blue/green/red) | For visual node state |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| uGUI programmatic nodes | UI Toolkit TreeView | UI Toolkit has built-in TreeView, but entire project uses uGUI/UISetup pattern; switching frameworks mid-project is high risk for no benefit |
| Unity UI Extensions UILineRenderer | Custom Image-based line drawing | UILineRenderer is a third-party package; hand-rolling lines via stretched Image RectTransforms is simpler and has zero dependencies |
| Camera-based pan/zoom | RectTransform-based pan/zoom | Camera approach would require a second camera and canvas; RectTransform approach keeps everything in the existing overlay canvas system |

## Architecture Patterns

### Recommended Project Structure

```
Assets/
  Scripts/
    Data/
      UpgradeNodeSO.cs          # ScriptableObject: individual upgrade node definition
      TechTreeSO.cs             # ScriptableObject: collection of all nodes + layout
      LevelConfigSO.cs          # ScriptableObject: per-level drop table, HP, spawn config
      ResourceTierSO.cs         # EXISTING: mineral tier definition (color, credit value)
    ECS/
      Components/
        GameStateComponents.cs   # MODIFY: add CurrentLevel field to GameStateData
        AsteroidComponents.cs    # MODIFY: add ResourceTier to asteroid spawn data
        MineralComponents.cs     # EXISTS: MineralData.ResourceTier already present
        EconomyComponents.cs     # NEW: PlayerBonusData singleton (resource mult, lucky strike, etc.)
      Systems/
        AsteroidSpawnSystem.cs   # MODIFY: read level config for HP/tier selection
        MineralSpawnSystem.cs    # MODIFY: read asteroid ResourceTier, set mineral credit value per tier
        MineralCollectionSystem.cs # MODIFY: apply resource multiplier bonus
    MonoBehaviours/
      UI/
        TechTreeController.cs    # NEW: node graph rendering, pan/zoom, purchase logic
        TechTreeNode.cs          # NEW: individual node UI element (Image + Text + click handler)
        TechTreeTooltip.cs       # NEW: hover tooltip with stat preview
        UpgradeScreen.cs         # REPLACE: currently placeholder, becomes TechTree host
      Core/
        ECSBootstrap.cs          # MODIFY: create new singletons, set default skill unlocks to false
        UISetup.cs               # MODIFY: create tech tree canvas instead of placeholder upgrade canvas
      Save/
        SaveData.cs              # MODIFY: expand TechTreeUnlocks, PlayerStatsData for all upgrade stats
        SaveManager.cs           # MODIFY: save/load tech tree state, apply upgrades on load
  ScriptableObjects/
    TechTree/
      TechTreeDefinition.asset   # Single TechTreeSO instance referencing all nodes
      Nodes/                     # ~40 UpgradeNodeSO instances
        Mining_CircleRadius_I.asset
        Mining_CircleRadius_II.asset
        ... (all node assets)
    Levels/
      Level_1.asset ... Level_5.asset  # LevelConfigSO instances
    ResourceTiers/
      Iron.asset ... Titanium.asset    # ResourceTierSO instances (6 total)
```

### Pattern 1: ScriptableObject Tech Tree Data Model

**What:** Each upgrade node is a ScriptableObject with prerequisite references, cost, tier multiplier, and an enum-based stat effect. The TechTreeSO aggregates all nodes and defines their spatial positions for the center-outward graph layout.

**When to use:** All tech tree data definition. ScriptableObjects enable designer-friendly tuning in Unity Inspector without code changes.

**Example:**
```csharp
// Data/UpgradeNodeSO.cs
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "AstroMiner/Upgrade Node")]
public class UpgradeNodeSO : ScriptableObject
{
    public string NodeId;              // Unique string ID for save persistence
    public string DisplayName;         // "Circle Radius I"
    public string Description;         // "Increases mining circle radius by 10%"
    public UpgradeBranch Branch;       // Mining, Economy, Ship, Run, Progression
    public int BaseCost;               // Base credit cost (before tier multiplier)
    public int TierLevel;              // 1, 2, or 3 (for 1x/3x/8x multiplier)
    public UpgradeNodeSO[] Prerequisites; // Nodes that must be purchased first
    public StatEffect[] Effects;       // What stats this upgrade modifies

    // Computed: actual cost = BaseCost * TierMultiplier[TierLevel]
    public int ActualCost => BaseCost * TierMultipliers[Mathf.Clamp(TierLevel - 1, 0, 2)];
    private static readonly int[] TierMultipliers = { 1, 3, 8 };

    // Layout position (center-outward graph)
    public Vector2 GraphPosition;      // XY position relative to center (0,0)
}

public enum UpgradeBranch { Mining, Economy, Ship, Run, Progression }

[System.Serializable]
public struct StatEffect
{
    public StatTarget Target;  // Which stat to modify
    public float Value;        // Amount (additive or multiplicative depending on target)
}

public enum StatTarget
{
    // Mining branch
    MiningRadius,          // Additive to MiningConfigData.Radius
    MiningDamage,          // Additive to MiningConfigData.DamagePerTick
    MiningTickInterval,    // Subtractive from MiningConfigData.TickInterval
    CritChance,            // Additive to CritConfigData.CritChance
    CritMultiplier,        // Additive to CritConfigData.CritMultiplier
    DotDamage,             // Multiplier on burning damage
    DotDuration,           // Additive to burning duration

    // Economy branch
    ResourceMultiplier,    // Multiplicative bonus on credit awards
    LuckyStrikeChance,     // Chance for double mineral drop
    SpawnRateReduction,    // Subtractive from AsteroidSpawnTimer.SpawnInterval

    // Ship branch (first tier = unlock, subsequent = stat boost)
    SkillUnlock,           // Flips SkillUnlockData for the skill
    LaserDamage, LaserCooldown,
    ChainDamage, ChainCooldown, ChainTargets,
    EmpDamage, EmpCooldown, EmpRadius,
    OverchargeCooldown, OverchargeDuration, OverchargeDamageMultiplier,
    ComboMastery,          // New mechanic: bonus damage for rapid skill use

    // Run branch
    RunDuration,           // Additive seconds to run timer

    // Progression branch
    AdvanceLevel           // Special: increments current level
}
```

### Pattern 2: Purchase Logic Writing to ECS Singletons

**What:** TechTreeController (MonoBehaviour) validates purchase conditions, deducts credits from GameStateData, marks the node as purchased in a local `bool[]`, then applies each StatEffect by reading the target ECS singleton, modifying the relevant field, and writing it back. This follows the exact same EntityManager read/write pattern already used by InputBridge, SkillBarController, and SaveManager.

**When to use:** Every time a player clicks a purchasable (green) node.

**Example:**
```csharp
// In TechTreeController.cs
private void ApplyStatEffect(StatEffect effect)
{
    switch (effect.Target)
    {
        case StatTarget.MiningRadius:
            var mining = em.GetComponentData<MiningConfigData>(miningEntity);
            mining.Radius += effect.Value;
            em.SetComponentData(miningEntity, mining);
            break;

        case StatTarget.CritChance:
            var crit = em.GetComponentData<CritConfigData>(critEntity);
            crit.CritChance += effect.Value;
            em.SetComponentData(critEntity, crit);
            break;

        case StatTarget.SkillUnlock:
            var unlock = em.GetComponentData<SkillUnlockData>(skillUnlockEntity);
            // effect.Value encodes skill index (1-4)
            int skillIdx = (int)effect.Value;
            switch (skillIdx)
            {
                case 1: unlock.Skill1Unlocked = true; break;
                case 2: unlock.Skill2Unlocked = true; break;
                case 3: unlock.Skill3Unlocked = true; break;
                case 4: unlock.Skill4Unlocked = true; break;
            }
            em.SetComponentData(skillUnlockEntity, unlock);
            break;

        case StatTarget.RunDuration:
            // Need new singleton or modify GameConstants approach
            break;
        // ... other cases
    }
}
```

**Confidence:** HIGH -- follows the exact EntityManager.GetComponentData/SetComponentData pattern used throughout the codebase (InputBridge.Update, SkillBarController.OnSkillButtonClicked, SaveManager.LoadIntoECS).

### Pattern 3: Pan/Zoom on uGUI Content Panel

**What:** A large RectTransform "content panel" holds all node UI elements. Zoom is achieved by scaling content panel's `localScale`. Pan is achieved by translating content panel's `anchoredPosition`. A parent panel with a Mask or RectMask2D clips content to the visible area.

**When to use:** Tech tree screen navigation.

**Implementation approach:**
```csharp
// Zoom: scale content around mouse pivot
float scroll = Mouse.current.scroll.ReadValue().y;
if (Mathf.Abs(scroll) > 0.01f)
{
    float oldScale = contentPanel.localScale.x;
    float newScale = Mathf.Clamp(oldScale + scroll * zoomSpeed, minZoom, maxZoom);

    // Zoom toward mouse position
    Vector2 mouseLocal;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        contentPanel, Mouse.current.position.ReadValue(), null, out mouseLocal);

    float scaleDelta = newScale / oldScale;
    contentPanel.anchoredPosition -= mouseLocal * (scaleDelta - 1f);
    contentPanel.localScale = Vector3.one * newScale;
}

// Pan: drag with middle mouse or left-hold-drag
if (Mouse.current.leftButton.isPressed)
{
    Vector2 delta = Mouse.current.delta.ReadValue();
    contentPanel.anchoredPosition += delta;
}
```

**Confidence:** MEDIUM -- RectTransform scaling for zoom is a widely-used pattern but the pivot-point zoom math needs careful testing. The exact implementation may need iteration.

### Pattern 4: Line Drawing Between Nodes

**What:** Prerequisite connections visualized as lines between parent and child nodes. Rather than adding a third-party package (Unity UI Extensions), use stretched Image RectTransforms rotated to connect node positions. This avoids external dependencies.

**When to use:** Drawing connection lines between tech tree nodes.

**Implementation:**
```csharp
private void CreateConnectionLine(Vector2 fromPos, Vector2 toPos, Transform parent)
{
    var lineGO = new GameObject("Connection");
    lineGO.transform.SetParent(parent, false);

    var rect = lineGO.AddComponent<RectTransform>();
    var image = lineGO.AddComponent<Image>();
    image.color = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Gray connection line

    Vector2 diff = toPos - fromPos;
    float distance = diff.magnitude;
    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

    rect.anchoredPosition = (fromPos + toPos) / 2f;
    rect.sizeDelta = new Vector2(distance, 3f); // 3px thick line
    rect.localRotation = Quaternion.Euler(0, 0, angle);
}
```

**Confidence:** HIGH -- this is a simple geometric calculation with no external dependencies.

### Pattern 5: Resource Tier System Extension

**What:** Asteroids currently spawn as tier 0 (Iron) only. Phase 6 adds resource tier data to asteroid entities, uses LevelConfigSO to determine which tiers can spawn and at what weights, and expands MineralRenderer to color minerals by tier.

**Key changes needed:**
1. Add `ResourceTier` field to asteroid entity data (new component or extend existing)
2. AsteroidSpawnSystem reads current level config, picks tier based on weighted random
3. MineralSpawnSystem copies asteroid's ResourceTier to spawned minerals, sets CreditValue from ResourceTierSO
4. AsteroidRenderer sizes asteroids by HP (larger = tougher, no color change)
5. MineralRenderer reads MineralData.ResourceTier to select color from ResourceTierSO array

**Confidence:** HIGH -- MineralData.ResourceTier already exists (currently hardcoded to 0). The extension is additive.

### Anti-Patterns to Avoid

- **Storing tech tree state in ECS components:** The tree is pure UI/persistence state. Keep it in MonoBehaviour + SaveData. Only the EFFECTS of upgrades write to ECS singletons.
- **Creating a new ECS entity per upgrade node:** Upgrade nodes are not simulation entities. They are UI elements backed by ScriptableObjects.
- **Using BlobAssets for mutable upgrade data:** BlobAssets are read-only after baking. Tech tree state changes at runtime, so use singletons for mutable data and ScriptableObjects for static definitions.
- **Modifying GameConstants (const fields) at runtime:** C# const fields are compile-time literals and cannot be changed. All values that Phase 6 must modify need runtime-mutable ECS singletons. MiningConfigData, CritConfigData, and SkillStatsData already exist for this purpose. New singletons needed for: run duration, economy bonuses, DoT base stats.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Node graph layout | Force-directed layout algorithm | Pre-computed positions in ScriptableObject | With only ~40 nodes in a known 5-branch radial layout, manual/formula-based positioning is simpler and gives exact artistic control |
| Tooltip system | Full tooltip framework | Single TechTreeTooltip MonoBehaviour that repositions on hover | Only used on tech tree screen; minimal scope doesn't justify a framework |
| Economy balance formulas | Complex simulation engine | Spreadsheet-derived constants baked into ScriptableObjects | Idle game economy math (exponential cost = base * growth^n) is straightforward; the Kongregate model is standard |
| Save migration | Schema versioning framework | SaveData.SaveVersion increment + single migration method | Only one migration needed (v1 -> v2 for tech tree expansion) |

**Key insight:** The tech tree is fundamentally a UI + data + persistence problem, not a simulation problem. All the expensive simulation work (damage, movement, collection) already exists in ECS. Phase 6 writes configuration values into existing singletons.

## Common Pitfalls

### Pitfall 1: Skills Locked on First Play After Phase 6

**What goes wrong:** ECSBootstrap currently sets all SkillUnlockData to true. Phase 6 changes defaults to false. If SaveManager.LoadIntoECS doesn't restore unlock state from save, skills will be locked even for players who haven't bought into the Ship branch yet.
**Why it happens:** The save system currently only persists credits. Tech tree unlock state is a placeholder empty array.
**How to avoid:** On fresh save (no tech tree data), initialize with a starter set that may include some defaults. Or keep skills unlocked if TechTreeUnlocks is empty (backward compatibility). The safest approach: if `SaveData.TechTreeUnlocks.Length == 0`, treat it as a pre-Phase-6 save and grant all current unlocks.
**Warning signs:** Skills disappear from skill bar on game launch.

### Pitfall 2: Cost Balance Makes Game Feel Broken

**What goes wrong:** First upgrade costs too much (player grinds 10+ runs with no reward) or too little (everything is trivially purchasable).
**Why it happens:** Economy values not tested against actual credit earn rates.
**How to avoid:** Calculate exact credits-per-run for Level 1 (current: DefaultCreditValuePerMineral=10, MinMinerals=3, MaxMinerals=8, ~50 asteroids in 20s run). Rough estimate: 50 asteroids * 5.5 avg minerals * 10 credits = 2,750 credits/run. First upgrade should cost ~2,000-3,000 credits. Build a simple spreadsheet BEFORE implementing.
**Warning signs:** Play one run and check if credit total is in expected range.

### Pitfall 3: Pan/Zoom Coordinate Math Bugs

**What goes wrong:** Click targets don't align with visual node positions after zooming, or nodes render off-screen after panning.
**Why it happens:** RectTransform localScale changes the coordinate space. Click detection via RectTransformUtility.ScreenPointToLocalPointInRectangle must account for the scaled content panel.
**How to avoid:** Use RectTransformUtility for all screen-to-local coordinate conversions. Test at min zoom, max zoom, and after pan+zoom combined.
**Warning signs:** Nodes not clickable after zooming; tooltip appears at wrong position.

### Pitfall 4: Singleton Query Misses After New Singletons Added

**What goes wrong:** New ECS singletons (PlayerBonusData, RunConfigData) created in ECSBootstrap but TechTreeController can't find them.
**Why it happens:** EntityQuery creation timing -- if TechTreeController initializes before ECSBootstrap runs, singleton entities don't exist yet.
**How to avoid:** Use lazy initialization pattern (same as SkillBarController.TryInitECS). Don't cache EntityQuery in Start(); resolve in first LateUpdate after world is ready.
**Warning signs:** "Singleton not found" warnings in console.

### Pitfall 5: SaveData Schema Breaks Existing Saves

**What goes wrong:** Expanding PlayerStatsData or TechTreeUnlocks causes JsonUtility.FromJson to fail on old save files.
**Why it happens:** Array length mismatch or new fields without defaults.
**How to avoid:** JsonUtility handles missing fields by using C# defaults. Ensure all new fields have sensible defaults in class definition. Increment SaveVersion and add a migration path that initializes empty TechTreeUnlocks to the correct length.
**Warning signs:** "Load failed" error on game start with existing save.

### Pitfall 6: Resource Tier Colors Not Visible on Dark Background

**What goes wrong:** Some mineral colors (grey for Iron, white for Silver) blend into the space background.
**Why it happens:** Mineral spheres are small and colors may lack contrast against dark space.
**How to avoid:** Use HDR emissive colors for ALL mineral tiers (already done for Iron). Ensure each tier's emissive intensity is sufficient. The existing MineralEmissiveIntensity=2.0 pattern should be replicated per-tier.
**Warning signs:** Minerals invisible against background during gameplay.

## Code Examples

### Complete Node Data Structure

```csharp
// Source: derived from existing ResourceTierSO pattern + GDD tech tree requirements
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "AstroMiner/Upgrade Node")]
public class UpgradeNodeSO : ScriptableObject
{
    [Header("Identity")]
    public string NodeId;
    public string DisplayName;
    [TextArea] public string Description;
    public UpgradeBranch Branch;

    [Header("Cost")]
    public int BaseCost;
    public int TierLevel; // 1=1x, 2=3x, 3=8x

    [Header("Prerequisites")]
    public UpgradeNodeSO[] Prerequisites;

    [Header("Effects")]
    public StatEffect[] Effects;

    [Header("Layout")]
    public Vector2 GraphPosition; // Relative to center (0,0)
}
```

### Level Configuration Structure

```csharp
// Source: derived from GDD level system + LEVL-05
[CreateAssetMenu(fileName = "NewLevel", menuName = "AstroMiner/Level Config")]
public class LevelConfigSO : ScriptableObject
{
    public int LevelNumber;                    // 1-5
    public long AdvanceCreditThreshold;        // Credits needed to buy Advance node
    public float AsteroidHPMultiplier;         // 1.0 for level 1, scaling up
    public float SpawnIntervalOverride;        // Seconds between spawns (or -1 for default)
    public ResourceTierWeight[] DropTable;     // Weighted random for tier selection
    public float RunDurationOverride;          // -1 for default
}

[System.Serializable]
public struct ResourceTierWeight
{
    public int TierIndex;    // 0=Iron, 1=Copper, ... 5=Titanium
    public float Weight;     // Relative probability
}
```

### Save Data Expansion

```csharp
// Source: existing SaveData.cs with Phase 6 expansion
[Serializable]
public class SaveData
{
    public int SaveVersion = 2; // Bumped from 1 to 2 for Phase 6

    public long TotalCredits;
    public int CurrentLevel = 1;

    // Phase 6: sized to match tech tree node count
    // Index matches UpgradeNodeSO array index in TechTreeSO
    public bool[] TechTreeUnlocks = new bool[0];

    // Phase 6: expanded player stats for all upgradeable values
    public PlayerStatsData Stats = new PlayerStatsData();
}

[Serializable]
public class PlayerStatsData
{
    // Mining
    public float MiningRadius = 2.5f;      // GameConstants.DefaultMiningRadius
    public float DamagePerTick = 10f;       // GameConstants.DefaultDamagePerTick
    public float TickInterval = 0.25f;      // GameConstants.DefaultTickInterval
    public float CritChance = 0.08f;        // GameConstants.CritChance
    public float CritMultiplier = 2f;       // GameConstants.CritMultiplier

    // Economy
    public float ResourceMultiplier = 1f;
    public float LuckyStrikeChance = 0f;

    // Run
    public float RunDuration = 20f;         // GameConstants.DefaultRunDuration

    // Skills (already in SkillStatsData, but need save persistence)
    // Phase 6 will restore SkillStatsData values from save on load
}
```

### New ECS Singletons Needed

```csharp
// ECS/Components/EconomyComponents.cs -- NEW
public struct PlayerBonusData : IComponentData
{
    public float ResourceMultiplier;     // 1.0 = no bonus, 1.5 = +50%
    public float LuckyStrikeChance;      // 0.0 = no lucky strikes
    public float RunDurationBonus;       // Additional seconds added to run timer
}
```

### Credits-Per-Run Calculation for Economy Balance

```
Level 1 (Iron only):
- Run duration: 20 seconds
- Spawn interval: 1.5s -> ~13 asteroids spawn
- Not all destroyed in time; estimate ~10 destroyed
- Minerals per asteroid: avg 5.5 (3-8 range)
- Credits per mineral: 10 (Iron)
- Credits per run: ~10 * 5.5 * 10 = 550 credits

Adjusted estimate (players improve):
- Good player destroys ~15-20 asteroids: 15 * 5.5 * 10 = 825 credits
- With skills active: maybe 20-25 asteroids: 25 * 5.5 * 10 = 1,375 credits

Target for first upgrade affordable in 1-2 runs:
- First upgrade base cost: ~500-1,000 credits
- This makes it achievable in 1 run for skilled players, 2 runs for new players

Recommended starting costs:
- Tier I nodes (outermost ring): 500-800 base cost
- Tier II nodes: 800-1,500 base cost * 3x = 2,400-4,500
- Tier III nodes: 1,500-3,000 base cost * 8x = 12,000-24,000
- Advance to Level 2: 3,000-5,000 (needs a few runs to save up)

Level 2+ scaling:
- Copper minerals worth 25 credits (2.5x Iron)
- Silver: 75 credits, Cobalt: 150, Gold: 400, Titanium: 1,000
- Higher levels earn more credits, offsetting deeper node costs
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| GameConstants const fields | Runtime-mutable ECS singletons (MiningConfigData, SkillStatsData) | Phase 5 (05-03) | Phase 6 can now modify any gameplay stat at runtime without recompilation |
| Skills all unlocked | SkillUnlockData gating with UI + system guards | Phase 5 (05-03) | Phase 6 just flips defaults from true to false and adds Ship branch purchase triggers |
| Single resource tier (Iron) | MineralData.ResourceTier field exists (hardcoded 0) | Phase 3 (03-01) | Phase 6 populates this with actual tier values from level drop table |
| Empty TechTreeUnlocks in SaveData | Placeholder ready for expansion | Phase 3 (03-03) | Phase 6 sizes the array and populates it |

## Open Questions

1. **Combo Mastery (TECH-04) mechanic design**
   - What we know: GDD lists "Combo Mastery" as a Ship branch upgrade
   - What's unclear: No specific mechanic defined. "Bonus damage when using skills in quick succession" is vague.
   - Recommendation: Define as a simple buff: "After using 2 skills within 5 seconds, next skill does 1.5x damage." Single-node purchase, no tiers. Can be implemented as a timestamp tracker in a new singleton.

2. **Lucky Strike (TECH-03) exact mechanic**
   - What we know: "Double drop chance" per GDD
   - What's unclear: Does "double drop" mean twice as many minerals per asteroid, or a random chance for any mineral to count double?
   - Recommendation: Implement as a percentage chance per mineral collection to award double credits (simpler, no entity spawning change). LuckyStrikeChance field in PlayerBonusData singleton, checked in MineralCollectionSystem.

3. **Resource Multiplier (TECH-03) application point**
   - What we know: Global credit bonus
   - What's unclear: Applied in MineralCollectionSystem (per-mineral) or as a post-run multiplier?
   - Recommendation: Apply in MineralCollectionSystem per mineral collection: `credits = mineralCreditValue * resourceMultiplier`. This makes the bonus feel immediate during gameplay.

4. **DoT upgrades (TECH-02) -- which DoT values to modify**
   - What we know: DoT I and DoT II are Mining branch nodes
   - What's unclear: Do they increase the mining circle's DoT (which doesn't exist -- DoT is currently only on Laser/EMP skills), or do they ADD a DoT effect to the mining circle?
   - Recommendation: Add a new mining circle DoT mechanic: asteroids that leave the mining circle after taking damage get a brief burning effect. DoT I enables it, DoT II increases damage/duration. This makes the Mining branch DoT nodes meaningful and distinct from Ship branch DoT (which is skill-based).

5. **Abundance (TECH-03) interaction with max asteroid cap**
   - What we know: Increased asteroid spawn rate
   - What's unclear: If spawn rate increases but MaxActiveAsteroids stays at 50, the benefit plateaus
   - Recommendation: Abundance upgrades reduce SpawnInterval AND increase MaxActiveAsteroids proportionally. Both values already exist in AsteroidSpawnTimer singleton.

## Sources

### Primary (HIGH confidence)
- **Codebase analysis** -- all 50+ source files read and cross-referenced
- Existing singletons: GameStateData, MiningConfigData, CritConfigData, SkillStatsData, SkillUnlockData, OverchargeBuffData, AsteroidSpawnTimer
- Existing data: SaveData, PlayerStatsData, ResourceTierSO, GameConstants
- Existing UI patterns: UISetup programmatic canvas creation, SkillBarController ECS access
- Existing bridge patterns: InputBridge, FeedbackEventBridge, SaveManager ECS read/write

### Secondary (MEDIUM confidence)
- [Wayline: Building a Robust Skill Tree in Unity with Scriptable Objects](https://www.wayline.io/blog/unity-skill-tree-scriptable-objects) -- ScriptableObject skill tree data model and prerequisite validation
- [Kongregate: The Math of Idle Games](https://blog.kongregate.com/the-math-of-idle-games-part-i/) -- Exponential cost formula: `cost = base * rate^owned`, income-cost balancing
- [Unity uGUI ScrollRect docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-ScrollRect.html) -- RectTransform scaling and content panel patterns
- [Unity Discussions: Drawing lines between UI elements](https://discussions.unity.com/t/any-good-way-to-draw-lines-between-ui-elements/575979) -- Stretched Image approach for UI connections

### Tertiary (LOW confidence)
- [Unity UI Extensions UILineRenderer](https://unity-ui-extensions.github.io/Controls/UILineRenderer.html) -- Third-party option for UI line drawing (not recommended due to external dependency)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- entire project already uses uGUI + ScriptableObjects + ECS singletons; Phase 6 extends existing patterns
- Architecture: HIGH -- all integration points verified against actual codebase; singleton read/write pattern proven in 5 prior phases
- Pitfalls: HIGH -- derived from concrete code analysis (SkillUnlockData defaults, SaveData schema, GameConstants const limitation)
- Economy balance: MEDIUM -- formulas are standard (Kongregate model) but exact values need playtesting; estimates based on current GameConstants values
- Pan/zoom UI: MEDIUM -- approach is standard but coordinate math under RectTransform scaling needs testing

**Research date:** 2026-02-18
**Valid until:** 2026-03-18 (stable -- no external dependencies or fast-moving libraries)

---
*Research for Phase 6: Tech Tree and Level Progression*
*Domain: Unity uGUI tech tree UI + ScriptableObject data models + ECS singleton stat application*
