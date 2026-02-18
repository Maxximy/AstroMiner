# Phase 2: Core Mining Loop - Research

**Researched:** 2026-02-17
**Domain:** Unity ECS (DOTS) asteroid spawning/destruction, tick-based AoE damage, mining circle visuals (URP bloom), hybrid ECS-to-GameObject rendering
**Confidence:** HIGH -- patterns verified against existing Phase 1 codebase, official Unity Entities 1.4 documentation, and architecture research from pre-planning. The hybrid ECS approach is already proven in Phase 1; Phase 2 extends the same patterns with gameplay-specific components and systems.

## Summary

Phase 2 transforms the placeholder entities from Phase 1 into a playable mining interaction. The core technical challenges are: (1) an asteroid spawning system that creates entities at the top of the screen, drifts them downward, and destroys them when they exit the play area or reach 0 HP, (2) a mining damage system that reads the mouse world position from the existing InputData singleton and applies tick-based AoE damage to all asteroids within the configurable mining circle radius, and (3) a visual layer that renders the mining circle as a glowing cyan ring using a LineRenderer with HDR emissive material (picked up by URP bloom), renders asteroids as 3D objects via the existing pooled GameObject sync pattern, and shows a stationary ship placeholder.

The existing codebase provides strong foundations: InputBridge already writes mouse world position to an ECS singleton every frame (using the new Input System), PlaceholderMovementSystem demonstrates the ISystem + IJobEntity + Burst pattern for entity movement, PlaceholderRenderer shows the ECS-to-pooled-GameObject sync pattern in LateUpdate, and GameObjectPool provides pre-warmed object pooling. Phase 2 replaces the placeholder systems with real asteroid and mining systems but follows identical architectural patterns.

**Primary recommendation:** Build the asteroid lifecycle system first (spawn, drift, despawn, HP tracking) as an ISystem with ECB for structural changes, then layer the mining damage system on top reading InputData for the cursor position. Use brute-force distance checks for the damage radius (Burst-compiled O(n) over ~100 asteroids is negligible). Render the mining circle as a GameObject with LineRenderer + HDR emissive material for bloom glow -- no custom shaders needed.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MINE-01 | Mining circle follows mouse cursor on the gameplay plane | InputData singleton already provides mouse world position via InputBridge. A MonoBehaviour reads InputData each frame and positions a mining circle GameObject at that world position. No new ECS work needed for cursor tracking. |
| MINE-02 | Mining circle deals tick-based AoE damage to all asteroids within radius | MiningDamageSystem (ISystem, Burst) iterates all asteroids, checks distance to mining circle center (from InputData), accumulates a DamageTickTimer, and applies damage when timer fires. Brute-force O(n) distance check is fine for 100 asteroids. |
| MINE-03 | Damage rate and damage amount are configurable and upgradeable | Store mining stats (DamagePerTick, TickInterval, Radius) in a MiningConfigData singleton component. Systems read from this singleton. Future phases update it via tech tree purchases. |
| MINE-04 | Mining circle radius is upgradeable via tech tree | MiningConfigData.Radius field read by both the damage system (for distance check) and the visual system (for ring scale). Tech tree writes new value in Phase 6. |
| MINE-05 | Mining circle has visual feedback (cyan emissive ring with bloom glow) | LineRenderer circle with HDR cyan emissive material. URP bloom (already configured in Phase 1 with 0.8 threshold, 0.3 intensity) picks up the HDR emission automatically. No Shader Graph or custom shader needed. |
| ASTR-01 | Asteroids spawn at top of screen and drift downward | AsteroidSpawnSystem (ISystem) uses ECB to create asteroid entities at configurable intervals. Reuses DriftData component from Phase 1 for downward movement. AsteroidMovementSystem (ISystem + IJobEntity) handles drift. |
| ASTR-02 | Each asteroid has HP determined by its resource tier | HealthData component (float MaxHP, float CurrentHP). Phase 2 uses a single default tier. Phase 6 introduces 6 tiers via ScriptableObject configs. |
| ASTR-03 | Asteroids have 3D PBR models with per-resource-type visual appearance | Phase 2 uses Unity primitive spheres with MaterialPropertyBlock color tinting (pattern established in PlaceholderRenderer). Phase 4/6 replaces with proper 3D models and PBR materials. |
| ASTR-04 | Asteroids rotate (spin) while drifting for visual interest | SpinData component already exists from Phase 1. PlaceholderSpinJob already handles rotation. Reuse directly. |
| ASTR-05 | Asteroids that reach the bottom of the screen are destroyed (missed) | AsteroidBoundsSystem (ISystem) checks Y position against lower bound, uses ECB.DestroyEntity for out-of-bounds asteroids. A DestroyTag component triggers visual cleanup in the renderer. |
| VISL-05 | Ship visual at bottom of screen (stationary) | PlaceholderRenderer already creates a ship placeholder cube at Y=-3. Phase 2 can keep this or replace with a slightly better placeholder. No ECS entity needed -- pure GameObject. |
</phase_requirements>

## Standard Stack

### Core (already installed)

| Package | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.entities | 1.4.4 | ECS framework -- ISystem, IJobEntity, ECB, SystemAPI | Already installed in Phase 1. All gameplay systems built on this. |
| com.unity.burst | (entities dep) | Burst compiler for ISystem hot paths | Already available. All movement/damage systems compiled with [BurstCompile]. |
| com.unity.mathematics | (entities dep) | float2/float3/math.distancesq for damage calculations | Already available. Used for distance checks and position math. |
| com.unity.collections | (entities dep) | NativeArray for entity queries in systems | Already available. Used for entity iteration in jobs. |
| com.unity.inputsystem | 1.18.0 | New Input System for mouse input | Already installed and used in InputBridge. |
| URP | 17.3.0 | Rendering pipeline with bloom post-processing | Already configured with bloom in Phase 1. |

### Supporting (no new packages needed)

| Library | Purpose | When to Use |
|---------|---------|-------------|
| UnityEngine.LineRenderer | Mining circle ring visual | Render the cyan emissive circle. Standard Unity component, works on WebGL. |
| MaterialPropertyBlock | Per-asteroid color tinting without material instancing | Already used in PlaceholderRenderer for asteroid/mineral colors. |
| Unity.Transforms.LocalTransform | ECS entity position/rotation | Already used for all entity transforms in Phase 1. |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| LineRenderer for mining circle | Procedural mesh (annulus) | More control over shape, but LineRenderer is simpler, works on WebGL, and supports HDR material for bloom. LineRenderer is the right choice for a ring. |
| LineRenderer for mining circle | Projector/Decal | URP Decal Renderer Feature adds overhead and complexity. Not needed for a single ring. |
| Brute-force distance check | Spatial hash grid (NativeMultiHashMap) | Overkill for 100 asteroids. The architecture doc recommends spatial partitioning at 500+ asteroids. Save for later. |
| com.unity.physics OverlapSphere | Manual distance check | Physics package adds significant overhead (collision world, broadphase). Manual distance check with Burst is simpler and faster for 100 entities with a single query point. |

**No new packages needed for Phase 2.** Everything required is already installed.

## Architecture Patterns

### Recommended Project Structure (Phase 2 additions)

```
Assets/Scripts/
├── ECS/
│   ├── Components/
│   │   ├── GameStateComponents.cs      # existing (GameStateData, InputData)
│   │   ├── PlaceholderComponents.cs    # existing (DriftData, SpinData, PlaceholderTag) -- REUSE DriftData/SpinData
│   │   ├── AsteroidComponents.cs       # NEW: AsteroidTag, HealthData, AsteroidSpawnTimer
│   │   └── MiningComponents.cs         # NEW: MiningConfigData singleton
│   └── Systems/
│       ├── PlaceholderMovementSystem.cs # existing -- REPLACE with real systems
│       ├── AsteroidSpawnSystem.cs       # NEW: spawns asteroids via ECB
│       ├── AsteroidMovementSystem.cs    # NEW: drift + spin (replaces PlaceholderMovementSystem)
│       ├── AsteroidBoundsSystem.cs      # NEW: destroys out-of-bounds asteroids via ECB
│       ├── MiningDamageSystem.cs        # NEW: tick-based AoE damage
│       └── AsteroidDestructionSystem.cs # NEW: destroys 0-HP asteroids, writes events
├── MonoBehaviours/
│   ├── Core/
│   │   ├── PlaceholderSpawner.cs       # existing -- REMOVE or disable (replaced by ECS spawner)
│   │   ├── PlaceholderRenderer.cs      # existing -- EVOLVE into AsteroidRenderer
│   │   └── AsteroidRenderer.cs         # NEW: syncs asteroid ECS entities to pooled GameObjects
│   ├── Bridge/
│   │   └── InputBridge.cs              # existing -- no changes needed
│   └── Rendering/
│       └── MiningCircleVisual.cs       # NEW: LineRenderer circle, follows mouse, HDR glow
└── Shared/
    └── GameConstants.cs                # NEW: play area bounds, default mining stats
```

### Pattern 1: ECS Spawn-Drift-Despawn Lifecycle (ISystem + ECB)

**What:** An ISystem manages the full lifecycle of asteroid entities: spawning new ones at intervals using EntityCommandBuffer, drifting them downward with IJobEntity, and destroying them via ECB when they go out of bounds or reach 0 HP.

**When to use:** All entity creation and destruction in DOTS. Never use EntityManager.CreateEntity/DestroyEntity inside systems (causes sync points). Always use ECB.

**Example:**
```csharp
// Source: Unity Entities 1.4 documentation + Phase 1 codebase patterns
[BurstCompile]
public partial struct AsteroidSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only spawn during Playing state
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        if (gameState.Phase != GamePhase.Playing)
            return;

        // Get spawn timer singleton (tracks time until next spawn)
        var spawnTimer = SystemAPI.GetSingletonRW<AsteroidSpawnTimer>();
        spawnTimer.ValueRW.TimeUntilNextSpawn -= SystemAPI.Time.DeltaTime;

        if (spawnTimer.ValueRO.TimeUntilNextSpawn > 0f)
            return;

        // Reset timer
        spawnTimer.ValueRW.TimeUntilNextSpawn = spawnTimer.ValueRO.SpawnInterval;

        // Get ECB for structural changes (played back at end of simulation)
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Create asteroid entity
        var entity = ecb.CreateEntity();
        ecb.AddComponent(entity, new AsteroidTag());
        ecb.AddComponent(entity, LocalTransform.FromPosition(new float3(
            /* random X within play bounds */, spawnYPosition, 0f)));
        ecb.AddComponent(entity, new LocalToWorld());
        ecb.AddComponent(entity, new DriftData { Speed = driftSpeed });
        ecb.AddComponent(entity, new SpinData { RadiansPerSecond = spinSpeed });
        ecb.AddComponent(entity, new HealthData { MaxHP = hp, CurrentHP = hp });
    }
}
```

**Confidence:** HIGH -- ECB CreateEntity is the documented pattern for runtime entity creation in DOTS. The SystemAPI.GetSingleton for ECB systems is verified in official Entities 1.4 docs.

### Pattern 2: Tick-Based AoE Damage (Timer Accumulator in ISystem)

**What:** The MiningDamageSystem reads the mouse world position from InputData, iterates all asteroid entities, checks if each asteroid is within the mining circle radius, and if so, accumulates a per-asteroid damage tick timer. When the timer fires, damage is applied.

**When to use:** Any periodic effect that should apply at a fixed rate independent of frame rate (damage ticks, DoT, healing).

**Approach A -- Per-asteroid timer:**
Each asteroid has a `DamageTickTimer` component. When inside the mining circle, the timer counts down. When it fires, damage is applied and the timer resets. This approach means damage starts immediately when an asteroid enters the circle (no global tick alignment issues).

**Approach B -- Global timer:**
A single global timer on the MiningConfigData singleton fires every N seconds, and ALL asteroids in range take damage simultaneously. Simpler, but produces "pulsed" damage that might feel less responsive.

**Recommendation: Use Approach A (per-asteroid timer).** It provides more responsive feel and is trivial to implement with Burst since the timer is just a float on each entity.

```csharp
// Source: Synthesized from Unity Entities docs + timer accumulator pattern
[BurstCompile]
public partial struct MiningDamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gameState = SystemAPI.GetSingleton<GameStateData>();
        if (gameState.Phase != GamePhase.Playing)
            return;

        var input = SystemAPI.GetSingleton<InputData>();
        if (!input.MouseValid)
            return;

        var miningConfig = SystemAPI.GetSingleton<MiningConfigData>();
        var radiusSq = miningConfig.Radius * miningConfig.Radius;
        var dt = SystemAPI.Time.DeltaTime;

        // Iterate all asteroids with health
        foreach (var (transform, health, tickTimer) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthData>, RefRW<DamageTickTimer>>())
        {
            float2 asteroidPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z);
            float distSq = math.distancesq(asteroidPos, input.MouseWorldPos);

            if (distSq <= radiusSq)
            {
                // Inside mining circle -- accumulate tick timer
                tickTimer.ValueRW.Elapsed += dt;
                if (tickTimer.ValueRO.Elapsed >= miningConfig.TickInterval)
                {
                    tickTimer.ValueRW.Elapsed -= miningConfig.TickInterval;
                    health.ValueRW.CurrentHP -= miningConfig.DamagePerTick;
                }
            }
            else
            {
                // Outside mining circle -- reset tick timer
                tickTimer.ValueRW.Elapsed = 0f;
            }
        }
    }
}
```

**Confidence:** HIGH -- SystemAPI.Query with RefRW is the documented pattern for iterating and modifying components. Timer accumulator is a standard game dev pattern.

**IMPORTANT NOTE on coordinate system:** The existing codebase uses Y as the vertical axis (entities drift along Y, camera looks down from Y=18 at 60-degree angle). The InputBridge projects mouse to the XZ plane (float2 with x,z), but PlaceholderSpawner creates entities at (x, y, 0) where Y is vertical and Z=0. This means the distance check should compare the asteroid's (x, y) position against the mouse's (x, z) world projection. This coordinate mapping MUST be verified during implementation -- the InputBridge writes `float2(worldPoint.x, worldPoint.z)` but entities live in `(x, y, 0)` space. The damage system needs to account for this coordinate discrepancy.

### Pattern 3: ECS-to-GameObject Rendering Sync (Established in Phase 1)

**What:** A MonoBehaviour maintains a Dictionary<Entity, GameObject> and syncs ECS entity positions to pooled GameObjects each frame in LateUpdate. Entities are identified by tag components (AsteroidTag replaces PlaceholderTag).

**When to use:** All entity rendering in this project (no Entities Graphics package due to WebGL incompatibility).

**Evolution from Phase 1:** PlaceholderRenderer already does this. The Phase 2 AsteroidRenderer will:
- Query entities with `AsteroidTag` instead of `PlaceholderTag`
- Handle dynamically spawned/destroyed entities (check for new entities not in the dictionary, clean up destroyed ones)
- Use the same GameObjectPool for efficient recycling
- Apply MaterialPropertyBlock for per-asteroid coloring

**Confidence:** HIGH -- Pattern proven in Phase 1 with 1100 entities at 60 FPS on WebGL.

### Pattern 4: Mining Circle as LineRenderer GameObject

**What:** A single GameObject with LineRenderer draws the mining circle. A MonoBehaviour reads the mouse world position from InputData (or directly from the new Input System) and positions the circle. The material uses URP/Lit or Unlit with HDR emission color, picked up by bloom.

**Key implementation details:**
- LineRenderer with 64 segments forming a circle, `useWorldSpace = false`
- `loop = true` to close the ring
- Width set to ~0.1 units for a thin ring
- Material with HDR emission color: cyan (0, 1, 1) at intensity 3-5 (above bloom threshold of 0.8)
- Scale the parent transform to match MiningConfigData.Radius
- Position updated every frame from InputData.MouseWorldPos

**Confidence:** HIGH -- LineRenderer is a standard Unity component that works on all platforms including WebGL. HDR emission with URP bloom is the standard approach for glow effects.

### Anti-Patterns to Avoid

- **Using com.unity.physics for damage radius checks:** The Physics package requires building a collision world every frame, which is massive overhead for a simple "is this asteroid within distance X of the cursor" check. A Burst-compiled distance check on 100 entities takes microseconds.

- **Creating/Destroying GameObjects for asteroid spawning:** Must use the existing GameObjectPool pattern. The pool pre-warms objects; AsteroidRenderer gets/releases from the pool as entities appear/disappear.

- **Storing managed types in ECS components:** HealthData, DriftData, SpinData, AsteroidTag, MiningConfigData must all be unmanaged IComponentData structs. No strings, no arrays, no object references. This ensures Burst compatibility.

- **Putting mining circle in ECS:** The mining circle is a single visual element tied to input. It belongs in the GameObject layer as a MonoBehaviour, not as an ECS entity.

- **Using EntityManager.DestroyEntity directly in systems:** Always use ECB for structural changes. Direct EntityManager calls cause sync points that stall jobs.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Object pooling | Custom pool with linked lists | `GameObjectPool` (Phase 1) wrapping Unity's `ObjectPool<T>` | Already built, pre-warms correctly, handles get/release lifecycle. |
| Circle rendering | Custom mesh generation for ring | `LineRenderer` with `loop = true` and 64 segments | Standard component, trivial to configure, works on WebGL, supports HDR materials. |
| Mouse-to-world projection | Custom raycasting code | Existing `InputBridge.cs` | Already writes to ECS InputData singleton every frame using new Input System. |
| Entity lifecycle timing | Custom spawn timers with coroutines | ECB + ISystem timer accumulator | ECB is the DOTS-standard structural change mechanism. Coroutines can't run in Burst. |
| Random number generation | `System.Random` or `UnityEngine.Random` | `Unity.Mathematics.Random` | Already used in Phase 1. Thread-safe, Burst-compatible, deterministic seed support. |

**Key insight:** Phase 2 is almost entirely about composing existing patterns from Phase 1 with new components and systems. The risky parts are not in the technology but in the coordinate system mapping (InputBridge XZ plane vs entity XY space) and the ECB lifecycle management for dynamic spawning/destruction.

## Common Pitfalls

### Pitfall 1: Coordinate System Mismatch Between Input and Entities

**What goes wrong:** InputBridge writes mouse position as `float2(worldPoint.x, worldPoint.z)` (XZ plane at Y=0). But PlaceholderSpawner creates entities at `(x, y, 0)` where Y is the vertical axis and Z=0. If the damage system naively compares `InputData.MouseWorldPos` (xz) against `entity.Position.xy` (xy), the mapping is incorrect because mouse.y corresponds to entity.y, but mouse is stored as (x, z).

**Why it happens:** The camera is at Y=18 looking downward at a 60-degree angle. A ray from the camera to the gameplay plane (Y=0) hits at some (x, 0, z) point. But entities use Y as height and sit at Z=0. So the "forward/back" direction for entities is Y, but for mouse projection it's Z.

**How to avoid:** Either: (a) Change entity spawning to use the XZ plane (entities at Y=0, drift along -Z), which aligns with standard Unity 3D conventions and InputBridge, OR (b) Change InputBridge to map mouse Z to Y. Option (a) is strongly recommended -- it aligns with Unity conventions, makes camera math easier, and prevents confusion in all future systems.

**Warning signs:** Mining circle appears at wrong position, damage doesn't apply when cursor is visually over an asteroid.

### Pitfall 2: ECB Structural Changes and Entity Query Staleness

**What goes wrong:** When an asteroid is destroyed via ECB, the entity still exists until the ECB is played back. If another system (e.g., the damage system) processes the same entity in the same frame, it may try to damage an entity that's about to be destroyed, or the renderer may try to sync a no-longer-existing entity.

**Why it happens:** ECB commands are deferred. Between recording and playback, the entity is in a liminal state.

**How to avoid:** Use EndSimulationEntityCommandBufferSystem for destruction, so all gameplay systems process the entity first, then it's destroyed at end of frame. The renderer (in LateUpdate) queries after ECS has finished, so destroyed entities will already be gone. For double-safety, check entity existence before accessing components in the renderer: `_em.Exists(entity) && _em.HasComponent<AsteroidTag>(entity)`.

**Warning signs:** NullReferenceException or EntityNotFoundException in the renderer. Asteroids flickering for one frame after destruction.

### Pitfall 3: GameObjectPool Leak on Entity Destruction

**What goes wrong:** When an ECS entity is destroyed, the corresponding pooled GameObject must be released back to the pool. If the renderer doesn't detect the entity's removal, the GameObject stays active but no longer updates, creating visual ghosts.

**Why it happens:** The renderer maintains a Dictionary<Entity, GameObject>. If entities are destroyed but the dictionary isn't cleaned, orphaned GameObjects remain.

**How to avoid:** In the renderer's LateUpdate, after syncing positions, iterate the dictionary and check each entity's existence. If an entity no longer exists in ECS, release its GameObject back to the pool and remove from dictionary. Alternatively, use a DynamicBuffer event pattern where the destruction system writes an event that the renderer reads.

**Warning signs:** GameObjects frozen in place, pool depleting over time, "ghost" asteroids visible.

### Pitfall 4: Forgetting GamePhase Guard in New Systems

**What goes wrong:** Asteroid spawning and damage systems run during non-Playing states (Collecting, GameOver, Upgrading), creating asteroids or dealing damage when the game should be paused.

**Why it happens:** Easy to forget the game state check when creating new systems. PlaceholderMovementSystem has this guard, but new systems need it too.

**How to avoid:** Every gameplay ECS system must start with:
```csharp
var gameState = SystemAPI.GetSingleton<GameStateData>();
if (gameState.Phase != GamePhase.Playing) return;
```
Consider creating a helper or using a [UpdateInGroup] with a custom group that only runs during Playing.

**Warning signs:** Asteroids spawning during upgrade screen, damage ticking during game over.

### Pitfall 5: LineRenderer HDR Color Below Bloom Threshold

**What goes wrong:** The mining circle renders but doesn't glow. It appears as a flat cyan ring with no bloom effect.

**Why it happens:** The bloom threshold in Phase 1 is set to 0.8. If the emission color intensity is below this threshold, bloom won't pick it up. Standard Color(0, 1, 1) has a max channel of 1.0, which may be just above threshold but won't produce strong bloom.

**How to avoid:** Use HDR color with intensity multiplier. In material setup: emission color = `new Color(0, 1, 1) * 5f` (intensity 5). This pushes the color well above the 0.8 bloom threshold. Verify in editor that the ring visibly glows.

**Warning signs:** Mining circle is visible but flat, no halo/glow around it.

## Code Examples

### Example 1: ECS Components for Phase 2

```csharp
// Source: Synthesized from Phase 1 patterns + Entities 1.4 docs
using Unity.Entities;
using Unity.Mathematics;

/// <summary>Tag identifying asteroid entities.</summary>
public struct AsteroidTag : IComponentData {}

/// <summary>Health data for damageable entities.</summary>
public struct HealthData : IComponentData
{
    public float MaxHP;
    public float CurrentHP;
}

/// <summary>Per-asteroid damage tick timer. Tracks time since last damage application.</summary>
public struct DamageTickTimer : IComponentData
{
    public float Elapsed;
}

/// <summary>Singleton: mining circle configuration.</summary>
public struct MiningConfigData : IComponentData
{
    public float Radius;
    public float DamagePerTick;
    public float TickInterval; // seconds between damage ticks
}

/// <summary>Singleton: asteroid spawn configuration and timer.</summary>
public struct AsteroidSpawnTimer : IComponentData
{
    public float SpawnInterval;
    public float TimeUntilNextSpawn;
    public int MaxActiveAsteroids;
}
```

### Example 2: ECB Entity Destruction in ISystem

```csharp
// Source: Unity Entities 1.4 documentation -- ECB automatic playback
[BurstCompile]
public partial struct AsteroidDestructionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, entity) in
            SystemAPI.Query<RefRO<HealthData>>()
                .WithAll<AsteroidTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.CurrentHP <= 0f)
            {
                ecb.DestroyEntity(entity);
                // Future: write destruction event to DynamicBuffer for VFX/audio
            }
        }
    }
}
```

### Example 3: LineRenderer Circle Setup

```csharp
// Source: Unity LineRenderer documentation + URP bloom pattern
private void CreateMiningCircle()
{
    var go = new GameObject("MiningCircle");
    var lr = go.AddComponent<LineRenderer>();

    int segments = 64;
    lr.positionCount = segments;
    lr.loop = true;
    lr.useWorldSpace = false;
    lr.startWidth = 0.08f;
    lr.endWidth = 0.08f;

    // Generate circle points
    for (int i = 0; i < segments; i++)
    {
        float angle = (float)i / segments * Mathf.PI * 2f;
        float x = Mathf.Cos(angle);
        float z = Mathf.Sin(angle); // circle on XZ plane
        lr.SetPosition(i, new Vector3(x, 0f, z));
    }

    // Create HDR emissive material for bloom glow
    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
    // HDR cyan: intensity well above bloom threshold (0.8)
    Color hdrCyan = new Color(0f, 1f, 1f) * 4f;
    mat.SetColor("_BaseColor", hdrCyan);
    // For Unlit shader, _BaseColor IS the output color.
    // If using Lit shader, set _EmissionColor instead.
    lr.material = mat;
}
```

### Example 4: Renderer Entity Lifecycle Management

```csharp
// Source: Evolved from Phase 1 PlaceholderRenderer pattern
void LateUpdate()
{
    if (!_initialized) return;

    var entities = _asteroidQuery.ToEntityArray(Allocator.Temp);
    var activeEntities = new HashSet<Entity>();

    // Sync positions for all living entities
    for (int i = 0; i < entities.Length; i++)
    {
        var entity = entities[i];
        activeEntities.Add(entity);

        if (!_entityToGO.TryGetValue(entity, out var go))
        {
            // New entity -- assign a pooled GameObject
            go = _asteroidPool.Get();
            ConfigureAsteroidVisual(go, entity);
            _entityToGO[entity] = go;
        }

        var lt = _em.GetComponentData<LocalTransform>(entity);
        go.transform.position = new Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
        go.transform.rotation = lt.Rotation;
    }

    // Clean up destroyed entities -- release GameObjects back to pool
    _entitiesToRemove.Clear();
    foreach (var kvp in _entityToGO)
    {
        if (!activeEntities.Contains(kvp.Key))
        {
            _asteroidPool.Release(kvp.Value);
            _entitiesToRemove.Add(kvp.Key);
        }
    }
    foreach (var entity in _entitiesToRemove)
    {
        _entityToGO.Remove(entity);
    }

    entities.Dispose();
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| SubScene baking for entity prefabs | Runtime EntityManager.CreateEntity or ECB.CreateEntity with archetypes | Entities 1.0+ | No SubScene complexity; all entities created procedurally at runtime. This project uses runtime creation exclusively (proven in Phase 1). |
| SystemBase for all systems | ISystem for hot paths, SystemBase only for managed access | Entities 1.0+ | ISystem enables Burst compilation. All Phase 2 gameplay systems should be ISystem. |
| Entities Graphics for rendering | Manual ECS-to-GameObject sync via MonoBehaviour | Project-specific decision | Entities Graphics is WebGL-incompatible per project constraint. The manual sync pattern is proven at 1100 entities/60 FPS. |
| EntityManager.CreateEntity in MonoBehaviour for spawning | ECB in ISystem for spawning | Entities 1.0+ best practice | Phase 1 used MonoBehaviour spawning (PlaceholderSpawner). Phase 2 moves spawning to ECS systems with ECB for proper lifecycle management. |

**Deprecated/outdated patterns to avoid:**
- `EntityManager.Instantiate(prefabEntity)` inside system OnUpdate -- causes sync points. Use ECB instead.
- `SystemBase` with `Entities.ForEach` -- deprecated in favor of ISystem + IJobEntity or SystemAPI.Query.
- `WithStructuralChanges()` -- forces main-thread execution. Use ECB.

## Open Questions

1. **Coordinate system alignment (XZ vs XY)**
   - What we know: InputBridge maps mouse to XZ plane (worldPoint.x, worldPoint.z). Entities in Phase 1 are positioned at (x, y, 0) with Y as vertical.
   - What's unclear: Should Phase 2 switch entities to XZ plane (standard 3D convention, matches InputBridge), or should InputBridge be changed to output (x, y)?
   - Recommendation: **Switch entity positions to XZ plane** -- entities at Y=0, drifting along -Z (from top of screen = +Z to bottom = -Z). This aligns with Unity's standard 3D coordinate system, the camera's perspective projection, and InputBridge's existing output. The camera at Y=18 looking down at 60 degrees already uses Y as "up." This is the cleanest fix and should be done in plan 02-01.

2. **Asteroid count cap and spawn rate**
   - What we know: WebGL validated at 100 asteroids + 1000 minerals. Phase 2 needs only asteroids (no minerals yet).
   - What's unclear: Exact spawn interval and max active count for good gameplay feel.
   - Recommendation: Default to ~2-second spawn interval, max 30 active asteroids. These are configurable via AsteroidSpawnTimer singleton. Tuning happens during playtesting. The 100-asteroid performance ceiling gives plenty of headroom.

3. **Placeholder-to-real transition strategy**
   - What we know: Phase 1 has PlaceholderSpawner, PlaceholderMovementSystem, PlaceholderRenderer, and PlaceholderComponents.
   - What's unclear: Should these be deleted, modified in-place, or kept alongside new systems?
   - Recommendation: **Remove placeholder systems** and replace with real ones. PlaceholderMovementSystem is superseded by AsteroidMovementSystem. PlaceholderSpawner is superseded by AsteroidSpawnSystem. PlaceholderRenderer evolves into AsteroidRenderer. DriftData and SpinData components are reused. PlaceholderTag is replaced by AsteroidTag.

4. **ASTR-03: "3D PBR models" scope for Phase 2**
   - What we know: Requirement says "3D PBR models with per-resource-type visual appearance." Full 3D model support with per-tier materials is a Phase 4/6 concern.
   - What's unclear: How far to push visuals in Phase 2.
   - Recommendation: Phase 2 uses Unity primitive spheres with MaterialPropertyBlock color tinting (iron = dark gray), matching the placeholder approach. This satisfies the functional requirement (asteroids are visible 3D objects with rotation) while deferring the art pipeline. The GDD notes "exact values to be defined per level in config" -- Phase 2 only needs a single default asteroid type.

## Sources

### Primary (HIGH confidence)
- Unity Entities 1.4.4 installed package -- ECB patterns, SystemAPI, ISystem, IJobEntity verified against installed version
- [Entity command buffer overview | Entities 1.0.16](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffers.html) -- ECB creation, playback, automatic systems
- [SystemAPI overview | Entities 1.0.16](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-systemapi.html) -- GetSingleton, GetSingletonRW, Query patterns
- [EndSimulationEntityCommandBufferSystem.Singleton.CreateCommandBuffer](https://docs.unity3d.com/Packages/com.unity.entities@1.0/api/Unity.Entities.EndSimulationEntityCommandBufferSystem.Singleton.CreateCommandBuffer.html) -- ECB creation API
- [Automatic playback of ECBs | Entities 1.4.3](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/systems-entity-command-buffer-automatic-playback.html) -- Verified for project's Entities version
- [Bloom Volume Override reference for URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-bloom.html) -- Bloom threshold, HDR color requirements
- Phase 1 codebase -- PlaceholderMovementSystem, PlaceholderRenderer, InputBridge, GameObjectPool, GameStateComponents verified on disk

### Secondary (MEDIUM confidence)
- [OverlapSphere vs EntityQuery distance check](https://discussions.unity.com/t/overlapsphere-vs-entityquery-distance-check-performance-comparison/856905) -- Confirms brute-force distance check is acceptable at <500 entities with Burst
- [Destroying entities design pattern](https://discussions.unity.com/t/destroying-entities-design-pattern/909737) -- ECB.DestroyEntity lifecycle and cleanup patterns
- [Unity ECS Performance Testing](https://gamedev.center/unity-ecs-performance-testing-the-way-to-the-best-performance/) -- Burst brute-force distance at 300 agents = 0.47ms
- [LineRenderer API | Unity 6000.1](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/LineRenderer.html) -- Loop, positionCount, material properties
- [Web performance considerations](https://docs.unity3d.com/Manual/webgl-performance.html) -- WebGL draw call and shader constraints

### Tertiary (LOW confidence)
- [Procedural asteroid generation discussion](https://discussions.unity.com/t/procedural-asteroid-generation/639383) -- Community approaches to asteroid visuals. Not needed for Phase 2 (using primitives).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- No new packages. All technology already installed and proven in Phase 1.
- Architecture: HIGH -- Phase 2 extends Phase 1 patterns (ISystem, ECB, pooled GameObject sync). No new architectural concepts.
- Pitfalls: HIGH -- Coordinate system mismatch identified from direct code analysis. ECB lifecycle issues documented from official docs.
- Code examples: HIGH -- Patterns synthesized from working Phase 1 code and official Entities 1.4 documentation.

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (30 days -- stable stack, no anticipated package changes)
