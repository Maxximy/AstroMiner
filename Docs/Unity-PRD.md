# Astrominer — Unity Rebuild PRD

## Context

Astrominer is an existing 3D asteroid mining game built in Rust/Bevy that runs natively and in the browser via WASM. The game is being rebuilt in Unity (URP) to serve as a foundation for an expanded version described in a separate GDD. This PRD covers a faithful technical recreation of the current gameplay with improved visuals (realistic space aesthetic) and audio.

**Targets:** WebGL + Desktop (Windows/Mac)
**Rendering:** Universal Render Pipeline (URP)
**Art Direction:** Realistic space (upgraded from current low-poly)
**Audio:** Full (SFX + ambient/music) — new addition

---

## 1. Game Overview

Players control a mining circle (cursor-locked) to damage asteroids orbiting a central star. Destroyed asteroids release mineral fragments that accelerate toward the star for collection. A 20-second timer constrains the mining phase; after it expires, remaining minerals are collected before the round ends.

**Core Loop:** Mine asteroids → Release minerals → Collect at star → Score

---

## 2. Game States

| State | Description | Entry Condition |
|-------|-------------|-----------------|
| **Playing** | Active mining, timer counting down, damage applied | Game start / restart complete |
| **Collecting** | Timer expired, no more damage, minerals still being pulled to center | Timer reaches 0 |
| **GameOver** | All minerals collected, final score shown | No DetachedMineral entities remain |
| **Restarting** | Cleanup all entities, reset resources | Player clicks Restart |

**Transitions:**
```
Playing → Collecting    (timer.remaining <= 0)
Collecting → GameOver   (no detached minerals exist)
GameOver → Restarting   (player requests restart)
Restarting → Playing    (cleanup complete, new asteroids spawned)
```

---

## 3. World Setup

### 3.1 Camera
- **Type:** Perspective, fixed position
- **Position:** (0, 20, 0) — directly above origin
- **Look At:** (0, 0, 0) — world center
- **Result:** Top-down overhead view of the orbital plane

### 3.2 Star (Central Body)
- **Geometry:** Sphere, radius 0.5
- **Position:** (0, 5, 0) — floating above the orbital plane
- **Material:** Yellow emissive (self-illuminating, bloom-responsive)
- **Light:** Point light child — white, range 500, radius 0.5

### 3.3 Environment
- **Background:** Black (clear color)
- **Post-processing (URP Volume):**
  - Bloom: intensity 0.3, natural falloff
  - Tonemapping: ACES or equivalent to Bevy's TonyMcMapface
- **Skybox:** Realistic star field (new — current version is flat black)

### 3.4 Coordinate System
- **Gameplay plane:** Y = 0 (X-Z horizontal)
- **Y-axis:** Vertical (camera up, star above plane)
- All orbital movement happens in X-Z; vertical_offset gives Y variation to asteroids

---

## 4. Asteroids

### 4.1 Spawning
- **Count:** 100 asteroids per round
- **RNG:** Seeded (seed = 42) for deterministic spawning — use `System.Random` with fixed seed
- **Model:** Single asteroid mesh (currently `Asteroid1.glb`), reused for all; replace with higher-fidelity model(s) for realistic style

### 4.2 Per-Asteroid Randomized Properties

| Property | Range | Notes |
|----------|-------|-------|
| Scale (radius) | 0.25 – 0.30 | Uniform random |
| Orbital distance | 3.0 – 7.0 | Distance from origin in X-Z |
| Starting angle | 0 – 2π | Position on orbit |
| Initial rotation | -3.0 – 3.0 rad | Initial mesh facing |
| Orbital speed | 0.075 – 0.125 | Base 0.1 ± 0.025 rad/sec |
| Vertical offset | -1.0 – 1.0 | Y-axis height variation |
| Spin speed | -0.5 – 0.5 rad/sec | Local Y-axis self-rotation |
| Mineral level | 0, 1, or 2 | Determines HP and mineral value |

### 4.3 Orbital Movement
Each frame:
```
angle += orbital_speed * deltaTime
position.x = cos(angle) * distance
position.y = vertical_offset
position.z = sin(angle) * distance
mesh rotates around local Y: spin_speed * deltaTime
```

### 4.4 Health
Determined by mineral level: `HP = (mineral_level + 1) * 2`

| Mineral Level | HP | Visual Tier |
|---------------|-----|-------------|
| 0 | 2 | Orange (common) |
| 1 | 4 | Silver (uncommon) |
| 2 | 6 | Yellow/Gold (rare) |

---

## 5. Minerals

### 5.1 Attached Minerals (Children of Asteroid)
- **Count:** 10 per asteroid (1000 total in scene)
- **Mesh:** Same asteroid model at smaller scale (0.40 – 0.45)
- **Placement:** Distributed on sphere surface (radius 1.0 from parent center) using random spherical coordinates (theta, phi)
- **Rotation:** Random per mineral, fixed relative to parent
- **Material:** PBR metallic based on level:

| Level | Color | Metallic | Roughness | Emissive |
|-------|-------|----------|-----------|----------|
| 0 (Common) | #FF6A00 (orange) | 1.0 | 0.25 | None |
| 1 (Uncommon) | #A0A0A0 (silver) | 1.0 | 0.25 | None |
| 2 (Rare) | Yellow-500 | 1.0 | 0.25 | Yellow * 0.25 (glow) |

### 5.2 Detached Minerals (Free-floating)
When parent asteroid is destroyed:
1. Remove parent-child relationship
2. Convert world-space transform to independent entity
3. Add `DetachedMineral` component with `pull_speed = 0`

**Pull behavior (each frame):**
```
pull_speed += 2.0 * deltaTime       // Accelerate toward center
direction = -position.normalized     // Toward origin
position += direction * pull_speed * deltaTime
mesh rotates Y-axis at 2.0 rad/sec  // Visual spin
```

### 5.3 Collection
- **Threshold:** distance from origin < 0.3 units
- **Action:** Destroy entity, award points

### 5.4 Scoring

| Mineral Level | Points |
|---------------|--------|
| 1 (Common) | 1 |
| 2 (Uncommon) | 2 |
| 3 (Rare) | 5 |

---

## 6. Mining System

### 6.1 Mouse → World Projection
1. Get mouse screen position
2. Cast ray from camera through mouse position
3. Intersect with Y = 0 plane: `t = -ray.origin.y / ray.direction.y`
4. World position = `ray.origin + ray.direction * t`

**Unity implementation:** `Camera.main.ScreenPointToRay()` + manual plane intersection or `Plane.Raycast()`

### 6.2 Mining Circle
- **Geometry:** Torus (ring)
  - Major radius: 1.5 (= MINING_RADIUS)
  - Minor radius: 0.05 (tube thickness)
- **Position:** `(cursor_world.x, 0.1, cursor_world.z)` — follows mouse, slight Y lift
- **Material:** Cyan (0, 1, 1) with 0.7 alpha, emissive cyan (bloom glow), alpha blend

### 6.3 Damage Application
- **Timer:** Every 1.0 second (repeating), only during **Playing** state
- **Check:** For each asteroid, compute XZ distance to cursor: `sqrt((ax-cx)² + (az-cz)²)`
- **Condition:** distance <= MINING_RADIUS (1.5)
- **Effect:** `health -= 1`, spawn damage popup at asteroid position

### 6.4 Asteroid Destruction
Each frame, check all asteroids:
- If `health <= 0`: detach all child minerals, destroy parent entity

---

## 7. UI / HUD

### 7.1 In-Game HUD (Unity UI Canvas)
- **Score:** Top-right, "Score: {points}", white, 32px equivalent
- **Timer:** Below score, "Time: {ceil(remaining)}", white, 32px equivalent
- Updated every frame during Playing and Collecting states

### 7.2 Damage Popups
- **Trigger:** On each damage tick hitting an asteroid
- **Text:** "{damage}" (shows "1")
- **Font size:** 16px, white
- **Lifetime:** 1.0 second
- **Animation:**
  - Float upward: 10 px/sec screen-space
  - Fade alpha: 1.0 → 0.0 over lifetime
- **Positioning:** World-to-screen projection of asteroid position (Y + 0.5 offset)
- Destroy when lifetime expires

### 7.3 Game Over Screen
- **Full-screen overlay:** Semi-transparent black (rgba 0,0,0,0.8)
- **Content:** "GAME OVER" heading, final score, RESTART button
- **Style:** Centered panel, rounded corners, subtle border
- **Interaction:** Restart button triggers Restarting state

---

## 8. Audio (New)

The current Bevy version has no audio. The Unity version should include:

### 8.1 Sound Effects
| Event | Sound | Notes |
|-------|-------|-------|
| Mining hit | Impact/crunch | Plays each damage tick on affected asteroids |
| Asteroid destroyed | Explosion/shatter | When HP reaches 0 |
| Mineral collected | Chime/ding | When mineral reaches center, pitch varies by level |
| Game over | Fanfare/jingle | When transitioning to GameOver state |
| UI button click | Click/tap | Restart button |

### 8.2 Ambient / Music
- Space ambient background loop (low, atmospheric)
- Optional: subtle tension music during Playing, calmer during Collecting

### 8.3 Implementation
- Use Unity AudioSource components
- Spatial audio for mining hits (3D positioned at asteroid)
- UI sounds as 2D (non-spatial)
- AudioMixer for volume control (SFX / Music channels)

---

## 9. Constants Reference

| Constant | Value | Description |
|----------|-------|-------------|
| NUM_BODIES | 100 | Asteroids per round |
| ROTATION_SPEED | 0.1 | Base orbital speed (rad/sec) |
| LEVEL_TIME | 20.0 | Round duration (seconds) |
| MINING_RADIUS | 1.5 | Mining circle radius / damage range |
| DAMAGE_INTERVAL | 1.0 | Seconds between damage ticks |
| DAMAGE_PER_TICK | 1 | HP removed per tick |
| MINERAL_COUNT | 10 | Minerals per asteroid |
| MINERAL_ACCELERATION | 2.0 | Pull acceleration (units/sec²) |
| COLLECTION_DISTANCE | 0.3 | Distance from origin to collect |
| MINERAL_SPIN | 2.0 | Detached mineral rotation (rad/sec) |
| POPUP_LIFETIME | 1.0 | Damage popup duration (sec) |
| POPUP_FLOAT_SPEED | 10.0 | Popup upward drift (px/sec) |
| CAMERA_HEIGHT | 20.0 | Camera Y position |
| STAR_HEIGHT | 5.0 | Star Y position |
| BLOOM_INTENSITY | 0.3 | Post-process bloom strength |
| RNG_SEED | 42 | Deterministic spawn seed |

---

## 10. Unity Architecture Recommendations

### 10.1 Project Structure
```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, GameState enum, Constants
│   ├── Asteroids/      # AsteroidSpawner, Asteroid, OrbitalMotion
│   ├── Minerals/       # MineralPiece, DetachedMineral, MineralCollector
│   ├── Mining/         # MiningCircle, CursorProjection, DamageSystem
│   ├── UI/             # HUDManager, DamagePopup, GameOverScreen
│   ├── World/          # StarSetup, CameraSetup
│   └── Audio/          # AudioManager, SFXTriggers
├── Models/
├── Materials/
├── Audio/
├── Prefabs/
├── Scenes/
└── Settings/           # URP asset, post-processing volumes
```

### 10.2 Key MonoBehaviours / Systems

| Script | Responsibility |
|--------|---------------|
| `GameManager` | State machine (Playing/Collecting/GameOver/Restarting), round lifecycle |
| `AsteroidSpawner` | Spawn 100 asteroids with seeded RNG on round start |
| `Asteroid` | Holds Health, MineralLevel, manages child minerals |
| `OrbitalMotion` | Per-asteroid orbital + spin movement |
| `MiningCircle` | Raycast mouse to Y=0 plane, position torus, detect overlap |
| `DamageSystem` | 1-sec timer, apply damage to asteroids within radius |
| `MineralPiece` | Attached mineral data (level, point value) |
| `DetachedMineral` | Pull toward origin with acceleration, spin, collection check |
| `HUDManager` | Update score/timer text |
| `DamagePopup` | Float-up + fade-out animated text |
| `GameOverScreen` | Overlay panel, restart button |
| `AudioManager` | Centralized SFX/music playback |

### 10.3 WebGL Considerations
- Avoid `System.Threading` — single-threaded in WebGL
- Use `AudioSource` carefully (WebGL requires user interaction before audio plays)
- Asset compression for fast loading (texture compression, mesh optimization)
- Test with WebGL 2.0 target
- `Application.ExternalCall` / `jslib` plugins for JS interop if needed

---

## 11. Visual Upgrade Notes (Realistic Space)

Moving from low-poly to realistic space aesthetic:
- **Skybox:** Cubemap or procedural starfield with nebula
- **Asteroids:** Textured models with normal maps, more geometric variety (2-3 mesh variants)
- **Minerals:** Crystalline/ore appearance with specular highlights and level-based glow
- **Star:** Volumetric-style glow, lens flare, animated surface (shader or particle)
- **Mining circle:** Holographic/sci-fi ring with animated shader
- **Particles:** Add particle effects for mining hits, asteroid destruction, mineral collection, and star ambient

---

## 12. Verification / Testing

1. **Spawn validation:** Confirm 100 asteroids spawn with correct orbital distribution (3-7 range), correct HP per level
2. **Orbital motion:** Asteroids orbit smoothly around origin, no drift or clumping
3. **Mining:** Circle follows mouse accurately, damage applies every 1 sec to asteroids within 1.5 radius
4. **Destruction:** Asteroid destroyed at 0 HP, 10 minerals detach with correct world positions
5. **Mineral pull:** Minerals accelerate toward center at 2.0/sec², collected at distance < 0.3
6. **Scoring:** Points match level (1/2/5), score accumulates correctly
7. **Timer:** Counts down from 20, transitions to Collecting at 0
8. **State flow:** Playing → Collecting → GameOver → Restarting → Playing (full loop)
9. **Game Over:** Shows when all minerals collected, restart works cleanly
10. **WebGL build:** Runs in browser, no threading errors, audio plays after user interaction
11. **Desktop build:** Runs natively on Windows, stable framerate with 100 asteroids + 1000 minerals
