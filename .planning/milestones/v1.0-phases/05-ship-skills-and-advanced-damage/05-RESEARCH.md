# Phase 5: Ship Skills and Advanced Damage - Research

**Researched:** 2026-02-18
**Domain:** ECS skill systems, cooldown management, line/AoE/chain damage, critical hits, DoT, skill bar UI
**Confidence:** HIGH -- All implementation built on existing proven architecture patterns; no new packages or external libraries needed

## Summary

Phase 5 adds four active combat skills, a critical hit system, and damage-over-time burning to the existing Hybrid ECS architecture. The good news: every infrastructure pattern this phase needs already exists in the codebase. Skills are ECS systems that read input singletons and apply damage to entities using the same `HealthData` + `DamageEvent` pipeline from Phase 2/4. The skill bar UI is a MonoBehaviour (like HUDController), and skill activation flows through InputBridge just like mouse position already does. No new packages, no new architectural patterns -- just new ECS components, new systems, new UI, and new visual effects.

The key technical challenges are: (1) line-of-fire collision detection for Laser Burst (ray-vs-circle on XZ plane), (2) nearest-neighbor chain logic for Chain Lightning (sorted distance iteration), (3) extending InputBridge to read keyboard keys 1-4 via the new Input System, and (4) radial cooldown fill UI using uGUI Image.fillAmount. All of these are well-understood algorithms with no library dependencies.

**Primary recommendation:** Build this in two plans. Plan 1: ECS skill components + systems + input extension + all four skill damage implementations + critical hit integration into MiningDamageSystem. Plan 2: Skill bar UI (radial cooldown, keybind badges, click activation) + DoT system + visual effects (beam, lightning arcs, EMP blast, Overcharge glow, ember particles).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- All skills fire from the ship toward the mouse position (not centered on mining circle)
- **Laser Burst**: Single powerful beam from ship to mouse -- damages everything in the line (sniper feel)
- **Chain Lightning**: Hits asteroid nearest to mouse, then auto-arcs to 3-4 nearby asteroids (satisfying chain reaction)
- **EMP Pulse**: Big AoE damage blast at target area hitting many asteroids (crowd damage)
- **Overcharge**: Temporarily buffs mining circle damage and size for a few seconds (self-buff, not a projectile)
- Horizontal row of 4 skill slots, bottom center of screen
- Cooldown display: radial sweep (dark overlay sweeps clockwise) + remaining seconds as number
- Keybind hints (1, 2, 3, 4) always visible as small badges on each slot corner
- All 4 skills unlocked and available by default -- Phase 6 adds lock/unlock gating via tech tree
- Noticeable but not disruptive: yellow "CRIT!" text larger than normal popups + distinct crit sound, no extra screen flash or shake
- Base crit rate: 5-10% (rare -- each one feels like a lucky event)
- Both skills and normal mining damage can crit
- Crit damage multiplier: 2x
- Upgradeable crit rate deferred to Phase 6 tech tree
- Only specific skills apply DoT (not automatic from mining circle) -- DoT is a skill bonus
- Ember particle effect only on burning asteroids -- no color tint to the asteroid sprite
- Short burn duration: 2-3 seconds after asteroid leaves affected area
- DoT damage popups use same white style as normal damage (no distinct DoT popup color)

### Claude's Discretion
- Individual skill cooldown durations and damage values (balance tuning)
- Which specific skills apply the DoT burning effect
- Skill visual effects (beam width, lightning arc style, EMP blast radius visual, Overcharge glow)
- Slot icon design and ready-state flash animation
- DoT tick frequency and damage per tick
- Overcharge buff duration and multiplier values

### Deferred Ideas (OUT OF SCOPE)
- Skill unlock gating via Ship branch of tech tree -- Phase 6
- Crit rate upgrade nodes -- Phase 6 tech tree
- DoT damage upgrade nodes -- Phase 6 tech tree
- Skill level/rank progression -- Phase 6 if scoped
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SKIL-01 | Laser Burst fires a beam from ship to mouse position, damages asteroids in path | Ray-vs-circle line intersection on XZ plane; new `LaserBurstSystem` ISystem; extends existing DamageEvent pipeline |
| SKIL-02 | Chain Lightning launches to nearest asteroid near mouse, chains to 3-4 nearby | Nearest-neighbor search + iterative distance sort; `ChainLightningSystem` ISystem; DamageEvent per chain target |
| SKIL-03 | EMP Pulse emits AoE damage centered on mouse position | Circle-vs-circle distance check identical to MiningDamageSystem pattern; configurable blast radius |
| SKIL-04 | Overcharge temporarily doubles mining circle damage rate | Writes to MiningConfigData singleton (multiplied values); timer-based buff on ECS component |
| SKIL-05 | Skills activated via keyboard (1-4) and on-screen UI buttons | InputBridge extended with `Keyboard.current[Key.Digit1-4].wasPressedThisFrame`; UI buttons write to same InputData |
| SKIL-06 | Each skill has a cooldown timer displayed on the skill bar UI | `SkillCooldownData` ECS singleton with 4 float timers; UI reads via EntityManager; Image.fillAmount for radial sweep |
| SKIL-07 | Skills must be unlocked via Ship branch of tech tree before use | DEFERRED to Phase 6 per CONTEXT.md -- all skills unlocked by default this phase |
| SKIL-08 | Skills upgradeable via tech tree | DEFERRED to Phase 6 per CONTEXT.md -- base values only this phase |
| DMGS-01 | Critical hit system with configurable chance and multiplier | RNG roll in MiningDamageSystem + each skill system; `CritConfigData` ECS singleton; DamageType.Critical already in FeedbackComponents |
| DMGS-02 | DoT burning effect that persists after asteroid leaves mining circle | `BurningData` IComponentData on individual asteroids; `BurningDamageSystem` ISystem ticks damage over time |
| DMGS-03 | DoT visually indicated by ember particles trailing from affected asteroid | MonoBehaviour ember ParticleSystem pooled per-asteroid; AsteroidRenderer tracks BurningData presence |
</phase_requirements>

## Standard Stack

### Core (Already Installed -- No New Packages)

| Package | Version | Purpose | Status |
|---------|---------|---------|--------|
| `com.unity.entities` | 1.4.4 | ECS framework for skill systems, cooldown components | Already installed |
| `com.unity.inputsystem` | 1.18.0 | Keyboard input (keys 1-4) for skill activation | Already installed |
| `com.unity.ugui` | 2.0.0 | Skill bar UI (Image, Button, radial fill) | Already installed |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP materials for skill VFX (Unlit, Particles/Unlit) | Already installed |
| `com.unity.modules.particlesystem` | 1.0.0 | Ember particles for DoT, skill VFX particles | Already installed |

### Supporting (Built-in Unity APIs)

| API | Purpose | When to Use |
|-----|---------|-------------|
| `UnityEngine.UI.Image.fillAmount` | Radial cooldown sweep overlay | Skill bar cooldown display |
| `UnityEngine.LineRenderer` | Laser beam visual, chain lightning arcs | Skill VFX (same pattern as MiningCircleVisual) |
| `UnityEngine.ParticleSystem` | EMP blast effect, ember DoT trails | Skill VFX + DoT indication |
| `UnityEngine.InputSystem.Keyboard` | `Keyboard.current[Key.DigitN].wasPressedThisFrame` | Skill activation input |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| LineRenderer for laser | TrailRenderer with short lifetime | LineRenderer gives instant full-length beam; TrailRenderer requires a moving emitter. LineRenderer is simpler for a flash-on beam. |
| uGUI Image.fillAmount for cooldown | Shader-based radial fill | Image.fillAmount is built-in, zero shader work, and exactly what it's designed for. No reason to hand-roll. |
| Per-entity BurningData component | Shared burning set (NativeHashSet) | Per-entity component follows existing architecture (like DamageTickTimer). Cleaner for Burst queries. |

## Architecture Patterns

### Recommended File Structure

```
Assets/Scripts/
├── ECS/
│   ├── Components/
│   │   ├── SkillComponents.cs          # SkillInputData, SkillCooldownData, CritConfigData, OverchargeBuffData
│   │   ├── AsteroidComponents.cs       # (add BurningData here alongside existing HealthData)
│   │   └── FeedbackComponents.cs       # (existing -- DamageEvent already supports DamageType.Critical/DoT/Skill)
│   └── Systems/
│       ├── SkillCooldownSystem.cs       # Decrements cooldowns each frame
│       ├── LaserBurstSystem.cs          # Line damage from ship to mouse
│       ├── ChainLightningSystem.cs      # Chain-target damage
│       ├── EmpPulseSystem.cs            # AoE damage at target
│       ├── OverchargeSystem.cs          # Buff application/expiry on MiningConfigData
│       ├── BurningDamageSystem.cs       # Per-entity DoT tick system
│       └── MiningDamageSystem.cs        # (modify: add crit roll)
├── MonoBehaviours/
│   ├── Bridge/
│   │   ├── InputBridge.cs              # (modify: add keyboard skill reads)
│   │   └── FeedbackEventBridge.cs      # (modify: add SkillEvent buffer drain)
│   ├── UI/
│   │   └── SkillBarController.cs       # Skill bar UI: 4 slots, radial cooldowns, click handlers
│   ├── Rendering/
│   │   ├── SkillVFXManager.cs          # Pools/plays laser beam, lightning arcs, EMP blast VFX
│   │   └── BurningEffectManager.cs     # Ember particle tracking on burning asteroids
│   └── Core/
│       └── UISetup.cs                  # (modify: add CreateSkillBarCanvas)
└── Shared/
    └── GameConstants.cs                # (add skill balance constants)
```

### Pattern 1: Skill Input via Extended InputData Singleton

**What:** Extend the existing `InputData` ECS singleton with skill activation flags. InputBridge writes keyboard state each frame. UI buttons also write to the same singleton through a bridge method.

**When to use:** All skill activation input.

**Existing pattern this extends:**
```csharp
// Current InputData (GameStateComponents.cs line 14-18)
public struct InputData : IComponentData
{
    public float2 MouseWorldPos;
    public bool MouseValid;
}

// Extended for Phase 5:
public struct SkillInputData : IComponentData
{
    public bool Skill1Pressed;  // Laser Burst
    public bool Skill2Pressed;  // Chain Lightning
    public bool Skill3Pressed;  // EMP Pulse
    public bool Skill4Pressed;  // Overcharge
}
```

**InputBridge extension (New Input System -- MANDATORY per user preference):**
```csharp
// In InputBridge.Update():
var keyboard = Keyboard.current;
var skillInput = new SkillInputData();
if (keyboard != null)
{
    skillInput.Skill1Pressed = keyboard.digit1Key.wasPressedThisFrame;
    skillInput.Skill2Pressed = keyboard.digit2Key.wasPressedThisFrame;
    skillInput.Skill3Pressed = keyboard.digit3Key.wasPressedThisFrame;
    skillInput.Skill4Pressed = keyboard.digit4Key.wasPressedThisFrame;
}
em.SetComponentData(skillInputEntity, skillInput);
```

**Confidence:** HIGH -- Follows exact same pattern as existing InputData/MouseWorldPos flow.

### Pattern 2: Skill Cooldown via ECS Singleton

**What:** A single ECS singleton tracks cooldown state for all 4 skills. Skill systems check cooldown before activating. A dedicated `SkillCooldownSystem` decrements timers each frame.

**Example:**
```csharp
public struct SkillCooldownData : IComponentData
{
    // Remaining cooldown in seconds (0 = ready)
    public float Skill1Remaining;
    public float Skill2Remaining;
    public float Skill3Remaining;
    public float Skill4Remaining;

    // Max cooldowns for UI fill calculation
    public float Skill1MaxCooldown;
    public float Skill2MaxCooldown;
    public float Skill3MaxCooldown;
    public float Skill4MaxCooldown;
}

// SkillCooldownSystem (ISystem, BurstCompile):
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    float dt = SystemAPI.Time.DeltaTime;
    var cooldowns = SystemAPI.GetSingletonRW<SkillCooldownData>();
    cooldowns.ValueRW.Skill1Remaining = math.max(0f, cooldowns.ValueRO.Skill1Remaining - dt);
    cooldowns.ValueRW.Skill2Remaining = math.max(0f, cooldowns.ValueRO.Skill2Remaining - dt);
    cooldowns.ValueRW.Skill3Remaining = math.max(0f, cooldowns.ValueRO.Skill3Remaining - dt);
    cooldowns.ValueRW.Skill4Remaining = math.max(0f, cooldowns.ValueRO.Skill4Remaining - dt);
}
```

**Confidence:** HIGH -- Singleton pattern identical to existing GameStateData/MiningConfigData.

### Pattern 3: Line Damage for Laser Burst (Ray-Circle Intersection on XZ Plane)

**What:** Laser Burst fires from ship position to mouse position. Every asteroid whose 2D circle (center + radius on XZ) intersects the line segment takes damage. This is a standard point-to-line-segment distance test.

**Algorithm:**
```csharp
// Ship at (ShipPositionX, ShipPositionZ), mouse at (input.MouseWorldPos.x, input.MouseWorldPos.y)
// For each asteroid at position P with bounding radius R:
// 1. Compute closest point on line segment to P
// 2. If distance < R + beamHalfWidth, asteroid is hit

float2 lineStart = new float2(GameConstants.ShipPositionX, GameConstants.ShipPositionZ);
float2 lineEnd = input.MouseWorldPos;
float2 lineDir = lineEnd - lineStart;
float lineLenSq = math.lengthsq(lineDir);

// For each asteroid:
float2 toAsteroid = asteroidPos - lineStart;
float t = math.clamp(math.dot(toAsteroid, lineDir) / lineLenSq, 0f, 1f);
float2 closest = lineStart + t * lineDir;
float distSq = math.distancesq(asteroidPos, closest);
float hitRadiusSq = (asteroidRadius + beamHalfWidth) * (asteroidRadius + beamHalfWidth);
if (distSq <= hitRadiusSq) { /* HIT */ }
```

**Confidence:** HIGH -- Standard 2D geometry. All math is Burst-compatible with `Unity.Mathematics`.

### Pattern 4: Chain Target Selection for Chain Lightning

**What:** Find asteroid nearest to mouse, then iteratively find the 3-4 nearest unvisited asteroids to chain to.

**Algorithm:**
```csharp
// Step 1: Find nearest asteroid to mouse position
// (iterate all, track minimum distance -- O(n) for ~100 asteroids is trivial)

// Step 2: From primary target, find nearest unvisited asteroid
// Use a small NativeList<Entity> as "visited" set (max 5 entries)
// Repeat 3-4 times, each time finding nearest to the LAST hit target
// Apply damage + DamageEvent to each target in the chain
```

**Why not a spatial hash:** With only ~50-100 asteroids max, a brute force O(n) scan per chain step (4 steps = O(4n)) is trivially fast. Spatial partitioning is overkill at this entity count.

**Confidence:** HIGH -- Straightforward iteration. Entities package NativeList works in Burst for the visited set.

### Pattern 5: Critical Hit Integration into Existing Damage Pipeline

**What:** Add a crit roll wherever damage is applied (MiningDamageSystem + each skill system). On crit, multiply damage by 2x and set DamageType.Critical on the DamageEvent. The existing FeedbackEventBridge already handles DamageType.Critical -- it spawns yellow "CRIT!" popup and triggers CameraShake.

**Integration points:**
1. MiningDamageSystem.OnUpdate -- after computing damage, roll crit
2. Each skill system -- after computing skill damage, roll crit
3. BurningDamageSystem -- DoT ticks can crit per user decision ("Both skills and normal mining damage can crit")

**Crit roll (Burst-compatible):**
```csharp
// CritConfigData singleton
public struct CritConfigData : IComponentData
{
    public float CritChance;      // 0.05 to 0.10 (5-10%)
    public float CritMultiplier;  // 2.0
}

// In damage application:
bool isCrit = rng.NextFloat() < critConfig.CritChance;
float finalDamage = isCrit ? baseDamage * critConfig.CritMultiplier : baseDamage;
var damageType = isCrit ? DamageType.Critical : DamageType.Normal; // or .Skill for skills
```

**Note on RNG in Burst:** `Unity.Mathematics.Random` is Burst-compatible. Each system should maintain its own Random instance seeded in OnCreate, advanced each frame. This is the same pattern used in AsteroidSpawnSystem and MineralSpawnSystem already.

**Confidence:** HIGH -- DamageType.Critical, DamageEvent pipeline, and CameraShake on crits are all already implemented in Phase 4.

### Pattern 6: DoT Burning via Per-Entity Component

**What:** When a skill that applies DoT hits an asteroid, add a `BurningData` component to that entity. A `BurningDamageSystem` iterates all entities with `BurningData`, ticks damage, and removes the component when the burn expires.

**Example:**
```csharp
public struct BurningData : IComponentData
{
    public float DamagePerTick;
    public float TickInterval;
    public float RemainingDuration;  // Decrement each frame, remove at 0
    public float TickAccumulator;    // Same pattern as DamageTickTimer
}

// In the skill system (e.g., EMP Pulse):
ecb.AddComponent(asteroidEntity, new BurningData
{
    DamagePerTick = dotDamage,
    TickInterval = 0.5f,
    RemainingDuration = 3f,
    TickAccumulator = 0f
});

// BurningDamageSystem iterates entities with BurningData:
// - Decrement RemainingDuration
// - Accumulate TickAccumulator, apply damage on each tick
// - Emit DamageEvent with DamageType.Normal (per user decision: same white style)
// - When RemainingDuration <= 0, ecb.RemoveComponent<BurningData>(entity)
```

**Key detail:** Adding a component to an entity that already has it is a no-op / error. If a skill re-hits a burning asteroid, it should **refresh** the duration rather than add a second component. The system should use `ecb.SetComponent` if BurningData exists, or `ecb.AddComponent` if not. Alternatively, the skill system can check `SystemAPI.HasComponent<BurningData>(entity)` before adding.

**Confidence:** HIGH -- Follows same per-entity component pattern as DamageTickTimer. ECB add/remove is proven in the codebase.

### Pattern 7: Skill Bar UI with Radial Cooldown Fill

**What:** A horizontal row of 4 UI slots at bottom center, each containing an icon Image, a dark overlay Image with `fillAmount` for radial sweep, a cooldown seconds TextMeshProUGUI, and a keybind badge. Built programmatically in UISetup (same as all other UI).

**Radial cooldown implementation:**
```csharp
// In UISetup.CreateSkillBarCanvas():
var overlayImage = slotGO.AddComponent<Image>();
overlayImage.type = Image.Type.Filled;
overlayImage.fillMethod = Image.FillMethod.Radial360;
overlayImage.fillOrigin = (int)Image.Origin360.Top;
overlayImage.fillClockwise = true;
overlayImage.fillAmount = 0f; // 0 = ready, 1 = full cooldown
overlayImage.color = new Color(0, 0, 0, 0.7f); // Dark overlay

// In SkillBarController.LateUpdate():
float remaining = cooldownData.Skill1Remaining;
float max = cooldownData.Skill1MaxCooldown;
overlayImage.fillAmount = max > 0 ? remaining / max : 0f;
cooldownText.text = remaining > 0 ? Mathf.CeilToInt(remaining).ToString() : "";
```

**Click activation:** Each slot also has a Button component. OnClick writes to the SkillInputData singleton (same as keyboard). The `SkillBarController` holds a reference to the ECS world and sets the activation flag.

**Confidence:** HIGH -- `Image.Type.Filled` with `FillMethod.Radial360` is the standard Unity approach for cooldown radial sweeps. Used extensively in every Unity ability bar tutorial and production game.

### Pattern 8: Skill Visual Effects via MonoBehaviour Managers

**What:** Skill VFX are GameObjects (LineRenderer, ParticleSystem) managed by a SkillVFXManager singleton. This follows the exact pattern of ExplosionManager and DamagePopupManager -- pooled GameObjects, MonoBehaviour manages lifecycle. A new `SkillEvent` DynamicBuffer carries activation data from ECS to MonoBehaviour.

**Laser Burst VFX:** LineRenderer with 2 points (ship pos to mouse pos), HDR cyan material, enabled for ~0.15s then disabled. Pooled single instance.

**Chain Lightning VFX:** LineRenderer with N+1 points (ship -> target1 -> target2 -> ...), HDR blue-white material, enabled for ~0.2s. Slight random offset on intermediate points for jagged look.

**EMP Pulse VFX:** ParticleSystem burst at target position -- expanding ring/sphere of particles. Similar to ExplosionManager pattern.

**Overcharge VFX:** Modify MiningCircleVisual material -- increase HDR intensity, add yellow tint, scale up. Could also add a pulsing particle ring. Controlled by reading OverchargeBuffData from ECS.

**Confidence:** HIGH -- All use existing patterns (LineRenderer from MiningCircleVisual, ParticleSystem from ExplosionManager, Material changes from AsteroidRenderer).

### Anti-Patterns to Avoid

- **Creating a separate ECS entity per skill instance:** Skills are global player abilities, not entities. Use singleton components, not per-skill entities. One `SkillCooldownData` singleton with 4 float fields is far simpler than 4 entities with individual cooldown components.

- **Using SystemBase for skill damage systems:** Skill damage logic is pure math (distance checks, line intersection, damage application). These MUST be `ISystem` with `[BurstCompile]` for performance. Only the VFX bridge needs managed types.

- **Modifying MiningConfigData permanently for Overcharge:** Overcharge is a temporary buff. Store the original values and restore them when the buff expires. Better: use a separate `OverchargeBuffData` singleton that the MiningDamageSystem checks as a multiplier, leaving MiningConfigData untouched.

- **Spawning companion GameObjects for burning asteroids:** Do NOT attach a ParticleSystem to each burning entity. Instead, the `BurningEffectManager` MonoBehaviour queries entities with `BurningData` each frame and manages a pool of ember particles at their positions (same decouple pattern as AsteroidRenderer).

- **Using old Input API:** User explicitly mandated `UnityEngine.InputSystem` only. Never use `Input.GetKeyDown` -- always use `Keyboard.current[Key.Digit1].wasPressedThisFrame`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Radial cooldown UI | Custom shader or manual mesh | `Image.Type.Filled` + `FillMethod.Radial360` | Built-in, zero maintenance, works on WebGL |
| Line-segment distance | Full physics raycast | 2D math: `math.dot` + `math.clamp` | Physics package is heavy; we only need 2D distance on XZ plane. Burst-compatible math is simpler and faster. |
| Nearest-neighbor search | Spatial hash / k-d tree | Brute force O(n) scan | 50-100 asteroids max. Spatial structures add complexity with zero benefit at this scale. |
| Skill definitions | ScriptableObject + Baker pipeline | `GameConstants` static class | Phase 5 has fixed 4 skills with const values. Phase 6 adds SO-driven tuning. Don't over-architect now. |
| Random number generation | System.Random or custom PRNG | `Unity.Mathematics.Random` | Already used in 3 systems. Burst-compatible, deterministic, fast. |

**Key insight:** This phase adds mechanics, not infrastructure. Every pattern needed (singletons, ECB, DynamicBuffer events, pooled VFX, LineRenderer, ParticleSystem) is already proven in Phases 1-4. The work is applying existing patterns to new gameplay features.

## Common Pitfalls

### Pitfall 1: Adding BurningData to Already-Burning Entity
**What goes wrong:** `ecb.AddComponent` on an entity that already has `BurningData` causes a duplicate component error or silent failure.
**Why it happens:** A skill hits an asteroid that is already burning from a previous activation.
**How to avoid:** Before adding BurningData in the skill system, check `SystemAPI.HasComponent<BurningData>(entity)`. If already burning, use `ecb.SetComponent` to refresh the duration instead of `ecb.AddComponent`.
**Warning signs:** Console errors about duplicate components; asteroids losing their burn unexpectedly.

### Pitfall 2: Keyboard Input Consumed by UI
**What goes wrong:** Pressing "1" activates the skill AND types "1" into any focused UI element, or EventSystem consumes the input before InputBridge reads it.
**How to avoid:** Check `EventSystem.current.currentSelectedGameObject == null` before processing keyboard skill input, or use the InputSystem's action priority system. In practice, this game has no text input fields, so this is low risk but worth guarding.
**Warning signs:** Skills not activating when a UI button is focused.

### Pitfall 3: Overcharge Modifying Shared Singleton Permanently
**What goes wrong:** Overcharge doubles MiningConfigData values but the buff expiry fails (exception, timing issue), leaving the mining circle permanently buffed.
**How to avoid:** Never modify MiningConfigData directly. Instead, add an `OverchargeBuffData` singleton with remaining duration and multiplier. MiningDamageSystem reads this and applies the multiplier inline. When duration hits 0, the multiplier returns to 1x automatically.
**Warning signs:** Mining circle stays huge/powerful after Overcharge ends.

### Pitfall 4: DamageEvent Buffer Overflow with Skill AoE
**What goes wrong:** EMP Pulse hits 30+ asteroids simultaneously, generating 30+ DamageEvents in one frame, causing a spike in popup spawns.
**How to avoid:** DynamicBuffer handles this fine (auto-resizes). The real concern is DamagePopupManager spawning 30 popups at once. The existing pool has 100 pre-warmed, max 300 -- sufficient. But consider: if this becomes a problem, batch nearby damage numbers into a single larger popup.
**Warning signs:** FPS drop when EMP Pulse hits many asteroids; popup pool exhaustion warnings.

### Pitfall 5: Skill Systems Running During Wrong Game Phase
**What goes wrong:** A skill fires during Collecting or GameOver phase because the phase guard is missing.
**How to avoid:** Every skill system must check `gameState.Phase == GamePhase.Playing` at the top of OnUpdate, same as MiningDamageSystem.
**Warning signs:** Damage applied after run timer expires.

### Pitfall 6: BurstCompile on OnCreate
**What goes wrong:** Compilation error when `[BurstCompile]` attribute is on the `OnCreate` method of an ISystem.
**Why it happens:** Known from Phase 2 -- `[BurstCompile]` must NOT be on OnCreate methods, only on the struct and OnUpdate.
**How to avoid:** Only apply `[BurstCompile]` to the struct declaration and to OnUpdate. Never on OnCreate or OnDestroy.
**Warning signs:** Build errors referencing BurstCompile + managed code in OnCreate.

### Pitfall 7: SkillEvent DynamicBuffer Not Cleared
**What goes wrong:** A `SkillEvent` buffer is written by ECS but never cleared by the MonoBehaviour bridge, causing the same skill VFX to replay every frame.
**How to avoid:** Follow the exact drain pattern from FeedbackEventBridge: read all events, dispatch to VFX manager, call `buffer.Clear()` at the end.
**Warning signs:** Laser beam VFX playing continuously; lightning arcs repeating every frame.

## Code Examples

### Example 1: Extending InputBridge for Skill Keys (New Input System)

```csharp
// In InputBridge.cs Update() -- after existing mouse position code:
using UnityEngine.InputSystem;

var keyboard = Keyboard.current;
var skillInput = new SkillInputData();
if (keyboard != null && gamePhase == GamePhase.Playing)
{
    skillInput.Skill1Pressed = keyboard.digit1Key.wasPressedThisFrame;
    skillInput.Skill2Pressed = keyboard.digit2Key.wasPressedThisFrame;
    skillInput.Skill3Pressed = keyboard.digit3Key.wasPressedThisFrame;
    skillInput.Skill4Pressed = keyboard.digit4Key.wasPressedThisFrame;
}
em.SetComponentData(skillInputEntity, skillInput);
```

### Example 2: Radial Cooldown Fill in UI

```csharp
// Creating the cooldown overlay in UISetup:
var overlay = new GameObject("CooldownOverlay");
overlay.transform.SetParent(slotGO.transform, false);
var overlayRect = overlay.AddComponent<RectTransform>();
overlayRect.anchorMin = Vector2.zero;
overlayRect.anchorMax = Vector2.one;
overlayRect.offsetMin = Vector2.zero;
overlayRect.offsetMax = Vector2.zero;

var overlayImage = overlay.AddComponent<Image>();
overlayImage.color = new Color(0f, 0f, 0f, 0.7f);
overlayImage.type = Image.Type.Filled;
overlayImage.fillMethod = Image.FillMethod.Radial360;
overlayImage.fillOrigin = (int)Image.Origin360.Top;
overlayImage.fillClockwise = true;
overlayImage.fillAmount = 0f;

// Updating each frame in SkillBarController:
float ratio = maxCooldown > 0 ? remaining / maxCooldown : 0f;
overlayImage.fillAmount = ratio;
```

### Example 3: Line-Segment Distance Test (Burst-Compatible)

```csharp
// Returns squared distance from point P to line segment AB on XZ plane
static float PointToSegmentDistSq(float2 p, float2 a, float2 b)
{
    float2 ab = b - a;
    float2 ap = p - a;
    float abLenSq = math.dot(ab, ab);
    if (abLenSq < 0.0001f) return math.distancesq(p, a); // degenerate segment
    float t = math.saturate(math.dot(ap, ab) / abLenSq);
    float2 closest = a + t * ab;
    return math.distancesq(p, closest);
}
```

### Example 4: SkillEvent Buffer for VFX Bridge

```csharp
// New buffer element in SkillComponents.cs:
public struct SkillEvent : IBufferElementData
{
    public byte SkillType;       // 0=Laser, 1=Chain, 2=EMP, 3=Overcharge
    public float2 OriginPos;     // Ship position (XZ)
    public float2 TargetPos;     // Mouse position (XZ)
    // Chain lightning targets (up to 4 chain positions)
    public float2 Chain1; public float2 Chain2;
    public float2 Chain3; public float2 Chain4;
    public int ChainCount;
}

// ECS skill system writes:
var skillBuffer = SystemAPI.GetSingletonBuffer<SkillEvent>();
skillBuffer.Add(new SkillEvent { SkillType = 0, OriginPos = shipPos, TargetPos = mousePos });

// FeedbackEventBridge drains in LateUpdate:
// - Dispatches to SkillVFXManager.PlayLaser/PlayChainLightning/PlayEMP/PlayOvercharge
// - Calls AudioManager.PlaySkillSfx(evt.SkillType)
// - buffer.Clear()
```

## Discretion Recommendations

These are areas marked "Claude's Discretion" in CONTEXT.md. Recommendations based on game feel and balance analysis:

### Skill Balance Values

| Skill | Cooldown | Damage | Notes |
|-------|----------|--------|-------|
| Laser Burst | 8s | 150 (single hit per asteroid in line) | High single-target. Beam width ~0.5 units. Should one-shot weak asteroids. |
| Chain Lightning | 10s | 60 per target, 4 chain targets max | Total 240-300 damage across 4-5 targets. Strong crowd clear. |
| EMP Pulse | 12s | 80 to all in 4-unit radius | High total damage if many asteroids clustered. Blast radius visual = expanding ring. |
| Overcharge | 15s | N/A (buff) | Duration: 5 seconds. Mining damage 2x, mining radius 1.5x. |

**Rationale:** Cooldowns are staggered so the player always has something approaching ready. Laser = low CD, high single. Chain = medium CD, medium multi. EMP = high CD, highest total. Overcharge = highest CD, sustained value.

### DoT Application

**Recommendation:** Laser Burst and EMP Pulse apply DoT burning. Chain Lightning does not (it's already multi-target, adding DoT would make it overpowered).

- **Laser Burst DoT:** Thematic (beam scorches asteroids). 5 damage/tick, 0.5s interval, 3s duration = 30 bonus damage. Rewards precision aim.
- **EMP Pulse DoT:** Thematic (electromagnetic burn). 3 damage/tick, 0.5s interval, 2s duration = 12 bonus damage per asteroid. Lower per-target but hits many.

### Skill Visual Effects

| Skill | VFX Approach |
|-------|-------------|
| Laser Burst | LineRenderer: 2 points (ship to mouse), HDR cyan beam, width 0.3 units, flash on for 0.15s. Material: URP/Unlit, HDR color (0, 1, 1) * 6 for bloom. |
| Chain Lightning | LineRenderer: N+1 points with slight random perpendicular offset (0.2 units) for jagged look. HDR blue-white (0.5, 0.7, 1) * 5. Flash on for 0.2s. |
| EMP Pulse | ParticleSystem burst: 30 particles, expanding sphere shape, short lifetime (0.4s), blue-purple color. Similar to ExplosionManager pattern. |
| Overcharge | MiningCircleVisual color shift to gold/yellow HDR (1, 0.8, 0) * 6, scale increase by 1.5x factor, subtle pulsing particle ring. Revert on expiry. |

### Slot Icon Design

Simple colored shapes on dark background (programmatic, no texture assets needed):
- Slot 1 (Laser): Cyan line icon (thin Image stretched horizontally)
- Slot 2 (Chain): Blue zigzag (3-segment line or lightning bolt shape)
- Slot 3 (EMP): Purple circle (ring Image)
- Slot 4 (Overcharge): Gold up-arrow or star

Ready-state flash: Brief 0.3s white flash on the overlay when cooldown reaches 0 (set overlay color to white at alpha 0.5 and fade to transparent).

### DoT Tick Values

- Tick frequency: Every 0.5 seconds (2 ticks/sec)
- Laser DoT: 5 damage per tick, 3s duration = 6 ticks = 30 total
- EMP DoT: 3 damage per tick, 2s duration = 4 ticks = 12 total per asteroid
- DoT popups: White text, normal style (per locked decision), smaller font (same as mining damage)

### Overcharge Buff Values

- Duration: 5 seconds
- Mining damage multiplier: 2.0x
- Mining radius multiplier: 1.5x (from 2.5 to 3.75 units)
- Both revert to base values when buff expires

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| SystemBase for all systems | ISystem for hot paths, SystemBase only for managed bridge | Entities 1.0+ (2023) | Skill damage systems MUST be ISystem for Burst |
| EntityManager.AddComponent in main thread | ECB for structural changes | Standard since Entities 1.0 | BurningData add/remove must use ECB |
| Legacy Input (UnityEngine.Input) | New Input System (UnityEngine.InputSystem) | Per user mandate | All keyboard reads must use Keyboard.current |

**Deprecated/outdated:**
- `Input.GetKeyDown()` -- deprecated in this project per user mandate. Use `Keyboard.current[key].wasPressedThisFrame`.
- Companion GameObjects on entities -- anti-pattern per CLAUDE.md. Burning particles tracked externally by MonoBehaviour.

## Open Questions

1. **Skill activation during Collecting phase**
   - What we know: Context says "skills fire from ship toward mouse." MiningDamageSystem only runs during Playing phase.
   - What's unclear: Should skills be usable during the Collecting phase (timer expired, minerals still flying)?
   - Recommendation: Restrict skills to Playing phase only, matching MiningDamageSystem. Collecting is a wind-down phase.

2. **Crit sound distinction from normal hit**
   - What we know: User wants "distinct crit sound" per context.
   - What's unclear: AudioManager currently has only `PlayDamageHit()`. Need a separate `PlayCritHit()` method.
   - Recommendation: Add `PlayCritHit()` to AudioManager loading a `Resources/Audio/SFX/CritHit` clip. FeedbackEventBridge already checks DamageType.Critical and can call the appropriate method.

3. **SkillEvent buffer vs separate buffers per skill**
   - What we know: Current pattern uses one buffer per event type (DamageEvent, DestructionEvent, CollectionEvent).
   - What's unclear: Whether to use one `SkillEvent` buffer with a type field, or 4 separate buffers.
   - Recommendation: Single `SkillEvent` buffer with a `SkillType` byte field. Skills fire at most once per frame, so the buffer is tiny. Fewer singletons = simpler bootstrap.

4. **Overcharge visual feedback path**
   - What we know: MiningCircleVisual reads MiningConfigData for radius. Overcharge changes radius.
   - What's unclear: How MiningCircleVisual knows to change color for overcharge visual.
   - Recommendation: MiningCircleVisual also reads `OverchargeBuffData` singleton. If remaining duration > 0, swap material color to gold HDR and scale. This keeps the visual logic centralized in the existing component.

## Sources

### Primary (HIGH confidence)
- **Codebase inspection:** All 43 C# source files read and analyzed. Architecture patterns verified against running implementation.
- `GameStateComponents.cs` -- InputData singleton pattern (line 14-18)
- `FeedbackComponents.cs` -- DamageType enum with Critical/DoT/Skill already defined (line 12-18)
- `MiningDamageSystem.cs` -- Damage tick + DamageEvent emission pattern (line 46-84)
- `FeedbackEventBridge.cs` -- DamageType.Critical handling with CameraShake already wired (line 83-87)
- `DamagePopupManager.cs` -- Yellow crit popup + orange DoT styling already implemented (line 137-161)
- `InputBridge.cs` -- `UnityEngine.InputSystem.Mouse.current` usage pattern (line 31)
- `ECSBootstrap.cs` -- Singleton creation pattern for new components (line 11-62)
- `UISetup.cs` -- Programmatic UI creation pattern used for skill bar (entire file)
- `AsteroidRenderer.cs` -- Entity-to-GameObject sync pattern for BurningEffectManager (line 151-195)
- `ExplosionManager.cs` -- Pooled VFX pattern for SkillVFXManager (entire file)

### Secondary (MEDIUM confidence)
- Unity UI Image.fillAmount documentation -- radial fill for cooldown UI is standard API
- Unity.Mathematics.Random Burst compatibility -- verified by existing usage in AsteroidSpawnSystem and MineralSpawnSystem
- Line-segment distance formula -- standard computational geometry, no library needed

### Tertiary (LOW confidence)
- None. All patterns are verified against the existing codebase.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- No new packages needed. All APIs already in use.
- Architecture: HIGH -- Every pattern extends existing proven implementations. No new architectural concepts.
- Pitfalls: HIGH -- Most pitfalls identified from actual codebase patterns (duplicate component, buffer clearing, phase guards).
- Balance values: MEDIUM -- Damage/cooldown numbers are recommendations that will need playtesting.

**Research date:** 2026-02-18
**Valid until:** Indefinitely (no external dependency version concerns; all patterns are codebase-internal)