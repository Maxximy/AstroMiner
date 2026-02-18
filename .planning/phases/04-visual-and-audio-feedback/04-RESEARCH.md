# Phase 4: Visual and Audio Feedback - Research

**Researched:** 2026-02-18
**Domain:** Unity damage popups, particle effects, AudioMixer, screen effects, WebGL audio
**Confidence:** HIGH

## Summary

Phase 4 transforms a functional game into a satisfying one by adding four categories of feedback: damage numbers, particle effects, screen effects, and audio. The existing codebase already has the architectural scaffolding for this -- a CollectionEvent DynamicBuffer entity is created by ECSBootstrap but never written to, the GameObjectPool class supports pooling for damage popups, and the renderer pattern (AsteroidRenderer/MineralRenderer) demonstrates entity-to-GameObject syncing that particle spawning can follow.

The primary technical challenge is the ECS-to-MonoBehaviour event pipeline. Currently MiningDamageSystem applies damage silently without producing events, and MineralCollectionSystem directly increments credits without writing CollectionEvents. Phase 4 must add DynamicBuffer event components (DamageEvent, DestructionEvent) and modify existing systems to populate them, then create MonoBehaviour bridges that drain these buffers each frame to trigger popups, particles, and audio. The AudioMixer asset must be created in the Unity Editor (it cannot be created at runtime via script), then referenced by the AudioManager MonoBehaviour.

**Primary recommendation:** Build the DynamicBuffer event pipeline first (damage, destruction, collection events), then layer feedback consumers (popup manager, particle spawner, audio manager, screen effects) on top. AudioMixer and audio clip assets are editor-only creation -- the user must create these manually or via CoPlay MCP.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Damage numbers: Float upward + fade animation (classic RPG style, ~1 second duration)
- Small & subtle size -- visible but not distracting, focus stays on asteroids and mining circle
- Show every number even when overlapping -- no aggregation or batching of rapid ticks
- Critical hits get scale boost (noticeably larger than normal) + brief white flash/glow at the hit point
- Color coding per roadmap: white (normal), yellow "CRIT!" (critical), orange italic (DoT), skill-colored (skills)
- Asteroid explosions: chunky debris -- rock fragments fly outward, physical and weighty feel
- Mineral trails: simple color-matched glowing streak behind each mineral as it flies to ship
- Overall visual density: moderate -- noticeable effects that build up but don't overwhelm, screen stays readable in peak chaos
- Mining circle: no additional VFX when actively damaging -- existing glow is enough
- Screen shake: light intensity, clearly noticeable but brief (2-3 frames) on critical hits and skill impacts
- Timer warning: steady red vignette that fades in at 10 seconds remaining and stays -- constant pressure, no pulsing
- Mineral collection feedback: credit counter briefly scales up or changes color when credits are added (no ship flash)
- No HP indicators on asteroids -- no health bars, no color tinting, damage numbers are the only HP feedback
- Soundscape: mix of electronic + weighty -- electronic bleeps/tones for UI, heavy thuds for mining/destruction
- Background music: ambient/chill spacey synths -- meditative mining mood
- Separate SFX and Music volume controls via AudioMixer (per roadmap)
- Audio assets: free CC0/royalty-free sound libraries and music packs -- no procedural generation

### Claude's Discretion
- Mineral collection chime handling at scale (individual per-mineral vs batched for nearby collections) -- optimize for what sounds good at 1000+ minerals
- Exact particle counts and lifetimes for debris explosions
- Damage number font choice and exact fade timing
- Music loop length and crossfade behavior
- AudioMixer group structure and default volume levels

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FEED-01 | Floating damage numbers on each damage tick (white, small, float up and fade) | DamagePopupManager pools TMPro world-space text, DamageEvent buffer from MiningDamageSystem triggers spawn |
| FEED-02 | Critical hits show yellow "CRIT!" text with scale-up pop animation | DamageEvent includes DamageType enum; popup manager applies scale boost + yellow color for crit type |
| FEED-03 | DoT ticks show orange italicized damage numbers | DamageEvent with DoT type; popup manager applies orange color + italic font style (Phase 5 produces DoT events) |
| FEED-04 | Skill damage shows skill-colored medium damage numbers | DamageEvent with Skill type + color field; popup scales up for medium size (Phase 5 produces skill events) |
| FEED-05 | Particle effects on asteroid destruction (explosion into collectible particles) | DestructionEvent buffer; MonoBehaviour spawns pooled ParticleSystem at position with chunky debris config |
| FEED-06 | Particle trail when minerals fly toward ship | TrailRenderer on mineral pool GameObjects with HDR emissive material matching mineral color |
| FEED-07 | Subtle screen shake on critical hits and skill impacts | CameraShake MonoBehaviour on main camera reads shake events, applies 2-3 frame random offset |
| FEED-08 | Edge glow warning when timer is in last 10 seconds | TimerWarningEffect reads GameStateData.Timer, fades in URP Vignette (red, intensity 0.4) |
| AUDI-01 | Mining hit SFX plays on each damage tick (spatial 3D at asteroid position) | AudioManager plays from pooled AudioSource; 2D audio only (WebGL spatial blend broken) with volume rolloff |
| AUDI-02 | Asteroid destruction SFX (explosion/shatter) | DestructionEvent triggers AudioManager.PlaySFX with explosion clip at position |
| AUDI-03 | Mineral collection SFX (chime, pitch varies by tier) | CollectionEvent triggers chime; batch nearby collections within 50ms window to avoid audio spam |
| AUDI-04 | Skill activation SFX (unique per skill) | Stub event/API for Phase 5 skill system to call AudioManager.PlaySkillSFX(skillType) |
| AUDI-05 | Game over fanfare | GameOverState.Enter calls AudioManager.PlayOneShot with fanfare clip |
| AUDI-06 | Space ambient background loop | AudioManager plays looping music clip via dedicated Music AudioSource |
| AUDI-07 | UI button click SFX | Button click handler calls AudioManager.PlayUI with click clip |
| AUDI-08 | AudioMixer with separate SFX and Music volume channels | AudioMixer asset (editor-created) with Master > SFX + Music groups, exposed volume parameters |
| VISL-03 | PBR materials on asteroids with per-resource-type appearance | MaterialPropertyBlock with distinct base colors per resource tier (extends existing AsteroidRenderer) |
| VISL-04 | Emissive/glow materials on rare minerals and star | HDR emissive color via MaterialPropertyBlock on mineral GameObjects (extends MineralRenderer) |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity ParticleSystem | Built-in (com.unity.modules.particlesystem) | Asteroid explosion debris | Already in manifest; proven WebGL-compatible; Shuriken system is the standard for legacy particle effects |
| Unity AudioMixer | Built-in (com.unity.modules.audio) | SFX/Music volume routing | Already in manifest; only way to get separate volume channels in Unity |
| TextMeshPro (TMPro) | Part of com.unity.ugui 2.0.0 | Damage number text rendering | Already used by HUDController, UISetup; standard for all text in Unity |
| URP VolumeProfile | Part of com.unity.render-pipelines.universal 17.3.0 | Vignette timer warning effect | Already installed; Vignette component has Color/Intensity properties accessible at runtime |
| TrailRenderer | Built-in | Mineral flight trails | Lightweight, WebGL-compatible, simple color-matched glow streaks |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| GameObjectPool | Custom (existing) | Pool damage popup GameObjects and explosion ParticleSystems | All popup/VFX spawning during gameplay |
| DynamicBuffer | com.unity.entities 1.4.4 | ECS-to-MonoBehaviour event pipeline | Damage events, destruction events, collection events |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ParticleSystem (Shuriken) | VFX Graph | VFX Graph uses compute shaders -- not available on WebGL. Shuriken is the only option. |
| TrailRenderer for mineral trails | ParticleSystem trails module | TrailRenderer is simpler, lighter weight, and directly attached to existing mineral pool GameObjects |
| World-space TMPro Canvas for popups | 3D TextMeshPro (non-UI) | World-space Canvas with TMPro is simpler, more reliable, and already used in the project; 3D TextMesh has rendering order issues |
| Separate AudioSources per sound | PlayOneShot on single source | PlayOneShot cannot be stopped/controlled; pooled AudioSources allow priority, volume, and pitch control per-sound |

**Installation:**
No new packages needed. All required modules are already in `Packages/manifest.json`:
- `com.unity.modules.particlesystem` (particle effects)
- `com.unity.modules.audio` (AudioMixer, AudioSource)
- `com.unity.ugui` 2.0.0 (TMPro for damage numbers)
- `com.unity.render-pipelines.universal` 17.3.0 (Volume/Vignette)

**Editor-created assets required (cannot be scripted):**
- AudioMixer asset (`Assets/Audio/GameAudioMixer.mixer`)
- Audio clip files (`Assets/Audio/SFX/*.wav`, `Assets/Audio/Music/*.ogg`)

## Architecture Patterns

### Recommended Project Structure (New Files)
```
Assets/
├── Scripts/
│   ├── ECS/
│   │   ├── Components/
│   │   │   └── FeedbackComponents.cs      # DamageEvent, DestructionEvent buffers + DamageType enum
│   │   └── Systems/
│   │       (MiningDamageSystem.cs modified to emit DamageEvents)
│   │       (MineralCollectionSystem.cs modified to emit CollectionEvents)
│   │       (MineralSpawnSystem.cs modified to emit DestructionEvents)
│   ├── MonoBehaviours/
│   │   ├── Bridge/
│   │   │   └── FeedbackEventBridge.cs     # Drains DamageEvent, DestructionEvent, CollectionEvent buffers
│   │   ├── Audio/
│   │   │   └── AudioManager.cs            # Singleton: AudioSource pool, mixer routing, PlaySFX/PlayMusic
│   │   ├── Rendering/
│   │   │   ├── DamagePopupManager.cs      # Pools world-space TMPro popups, animates float-up-fade
│   │   │   ├── ExplosionManager.cs        # Pools ParticleSystems for asteroid destruction debris
│   │   │   ├── CameraShake.cs             # Coroutine-based screen shake on main camera
│   │   │   └── TimerWarningEffect.cs      # Red vignette fade-in during last 10 seconds
│   │   └── UI/
│   │       (HUDController.cs modified for credit counter pop animation)
│   └── Shared/
│       └── GameConstants.cs               # New constants for feedback tuning
├── Audio/
│   ├── SFX/                               # .wav files: hit, destroy, collect, click, fanfare
│   ├── Music/                             # .ogg files: ambient space loop
│   └── GameAudioMixer.mixer               # Editor-created AudioMixer asset
└── Materials/
    └── DamagePopupMaterial.mat             # (optional) if needed for popup text rendering
```

### Pattern 1: DynamicBuffer Event Pipeline (ECS -> MonoBehaviour)
**What:** ECS systems append unmanaged event structs to DynamicBuffers. A MonoBehaviour bridge drains these buffers every LateUpdate frame and dispatches to feedback managers (popups, audio, particles).
**When to use:** Any time ECS produces discrete events that the GameObject layer must react to.
**Why this over alternatives:** The project already uses this pattern -- CollectionEvent is defined and the buffer entity is created in ECSBootstrap. This phase extends it to DamageEvent and DestructionEvent.

```csharp
// ECS/Components/FeedbackComponents.cs
public enum DamageType : byte
{
    Normal = 0,
    Critical = 1,
    DoT = 2,
    Skill = 3
}

public struct DamageEvent : IBufferElementData
{
    public float3 Position;
    public float Amount;
    public DamageType Type;
    // Skill color encoded as 3 bytes (R,G,B) for Burst compatibility
    public byte ColorR;
    public byte ColorG;
    public byte ColorB;
}

public struct DestructionEvent : IBufferElementData
{
    public float3 Position;
    public float Scale; // asteroid size for particle count scaling
    public int ResourceTier;
}

// MonoBehaviours/Bridge/FeedbackEventBridge.cs
public class FeedbackEventBridge : MonoBehaviour
{
    private EntityManager _em;
    private EntityQuery _damageBufferQuery;
    private EntityQuery _destructionBufferQuery;
    private EntityQuery _collectionBufferQuery;

    void LateUpdate()
    {
        // Drain DamageEvent buffer
        var damageBuffer = /* get buffer */;
        for (int i = 0; i < damageBuffer.Length; i++)
        {
            var evt = damageBuffer[i];
            DamagePopupManager.Instance.Spawn(evt);
            AudioManager.Instance.PlayDamageHit(evt.Position);
            if (evt.Type == DamageType.Critical)
                CameraShake.Instance.Shake();
        }
        damageBuffer.Clear();

        // Drain DestructionEvent buffer
        var destructionBuffer = /* get buffer */;
        for (int i = 0; i < destructionBuffer.Length; i++)
        {
            var evt = destructionBuffer[i];
            ExplosionManager.Instance.PlayExplosion(evt.Position, evt.Scale);
            AudioManager.Instance.PlayDestruction(evt.Position);
        }
        destructionBuffer.Clear();

        // Drain CollectionEvent buffer
        var collectionBuffer = /* get buffer */;
        for (int i = 0; i < collectionBuffer.Length; i++)
        {
            var evt = collectionBuffer[i];
            AudioManager.Instance.PlayCollection(evt.ResourceTier);
        }
        collectionBuffer.Clear();
    }
}
```
**Confidence:** HIGH -- CollectionEvent and the DynamicBuffer pattern are already established in the codebase.

### Pattern 2: Pooled World-Space TMPro Damage Popups
**What:** Pre-instantiate a pool of small world-space Canvas + TMPro GameObjects. On each DamageEvent, get one from the pool, position it at the damage location, animate it floating upward while fading out, then return to pool.
**When to use:** Every damage tick produces a popup. At max DPS with 50 asteroids in mining circle, this could mean 50 popups per 0.25s tick = 200 popups/second. Pool must handle this throughput.
**Implementation details:**
- Each popup: a small Canvas (RenderMode.WorldSpace) with a single TMPro text child
- Canvas worldCamera set to main camera (required for world-space canvas rendering)
- Animation: translate Y += speed * dt, alpha -= dt / duration, scale for crits
- Pool size: ~100 pre-warmed, 300 max (popups last ~1 second, so steady state at 200/sec needs ~200 active)
- Return to pool when alpha reaches 0

```csharp
// DamagePopupManager.cs (simplified)
public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    private GameObjectPool _popupPool;
    private List<ActivePopup> _activePopups = new List<ActivePopup>();

    private struct ActivePopup
    {
        public GameObject GO;
        public TextMeshProUGUI Text;
        public CanvasGroup Group;
        public float Elapsed;
        public float Duration;
        public float RiseSpeed;
    }

    void Update()
    {
        // Animate all active popups: rise + fade
        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            var popup = _activePopups[i];
            popup.Elapsed += Time.deltaTime;
            float t = popup.Elapsed / popup.Duration;

            // Rise
            var pos = popup.GO.transform.position;
            pos.y += popup.RiseSpeed * Time.deltaTime;
            popup.GO.transform.position = pos;

            // Fade
            popup.Group.alpha = 1f - t;

            if (t >= 1f)
            {
                _popupPool.Release(popup.GO);
                _activePopups.RemoveAt(i);
            }
            else
            {
                _activePopups[i] = popup;
            }
        }
    }

    public void Spawn(DamageEvent evt)
    {
        var go = _popupPool.Get();
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        var group = go.GetComponent<CanvasGroup>();

        // Position at damage location (XZ plane, slight Y offset)
        go.transform.position = new Vector3(evt.Position.x, 0.5f, evt.Position.z);

        // Configure text and style based on damage type
        text.text = Mathf.RoundToInt(evt.Amount).ToString();
        text.color = GetDamageColor(evt.Type, evt.ColorR, evt.ColorG, evt.ColorB);
        text.fontStyle = evt.Type == DamageType.DoT ? FontStyles.Italic : FontStyles.Normal;
        float scale = evt.Type == DamageType.Critical ? 1.5f : 1f;
        go.transform.localScale = Vector3.one * scale;

        group.alpha = 1f;
        _activePopups.Add(new ActivePopup { GO = go, Text = text, Group = group, Elapsed = 0, Duration = 1f, RiseSpeed = 1.5f });
    }
}
```
**Confidence:** HIGH -- ObjectPool + TMPro is the standard Unity pattern for damage popups.

### Pattern 3: Pooled ParticleSystem for Explosions
**What:** Pre-instantiate a pool of ParticleSystem GameObjects configured for chunky debris. On DestructionEvent, position one at the asteroid location, call Play(), and return to pool when emission completes.
**When to use:** Asteroid destruction events.
**Implementation details:**
- ParticleSystem configured: Burst emission (15-25 particles), short lifetime (0.5-1s), outward velocity, gravity, random rotation
- Renderer: small cubes or default sprites with rock-brown colors
- Pool size: ~20 pre-warmed (max asteroids dying simultaneously is bounded by MaxActiveAsteroids = 50)
- Return to pool after ParticleSystem.IsAlive() returns false

**Confidence:** HIGH -- Standard Unity ParticleSystem pooling pattern.

### Pattern 4: AudioSource Pool for SFX
**What:** Pre-instantiate a pool of GameObjects with AudioSource components. When a sound needs to play, get a source from the pool, assign the clip, set pitch/volume, and play. Return to pool when clip finishes.
**When to use:** All SFX playback. Multiple sounds may play simultaneously (mining hits on different asteroids, collection chimes overlapping).
**Implementation details:**
- AudioSource.spatialBlend = 0 (2D audio only -- WebGL spatial blend is broken, see Pitfall 2)
- Volume attenuation based on distance from camera center (manual rolloff) for pseudo-spatial feel
- Pool size: ~20 AudioSources (cap simultaneous sounds to avoid audio mud)
- Mining hit SFX: cooldown/throttle to max ~4 per second (at 200 damage ticks/sec, playing every tick would be audio noise)
- Collection chime: batch within 50ms window -- play one chime per batch, pitch raised proportional to batch size (more minerals = higher pitch = excitement)

```csharp
// AudioManager.cs (simplified)
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _musicGroup;

    private AudioSource _musicSource;
    private Queue<AudioSource> _sfxPool;

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (_sfxPool.Count == 0) return; // all sources busy
        var source = _sfxPool.Dequeue();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.outputAudioMixerGroup = _sfxGroup;
        source.Play();
        StartCoroutine(ReturnToPoolWhenDone(source));
    }
}
```
**Confidence:** HIGH -- Standard Unity audio pooling pattern.

### Pattern 5: Camera Shake via Coroutine
**What:** On shake trigger, start a coroutine that applies small random position offsets to the camera for 2-3 frames, then restores original position.
**When to use:** Critical hits and skill impacts.
**Implementation details:**
- Store original camera position before shake
- Each frame: apply Random.insideUnitSphere * magnitude as offset
- Duration: 2-3 frames (~0.05s at 60fps)
- Magnitude: small (0.1-0.2 world units -- noticeable but brief)
- Prevent stacking: if already shaking, restart timer but don't increase magnitude

```csharp
// CameraShake.cs
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private Vector3 _originalPos;
    private float _shakeDuration;
    private float _shakeMagnitude = 0.15f;

    public void Shake(float duration = 0.05f, float magnitude = 0.15f)
    {
        _shakeDuration = duration;
        _shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (_shakeDuration > 0)
        {
            transform.localPosition = _originalPos + Random.insideUnitSphere * _shakeMagnitude;
            _shakeDuration -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = _originalPos;
        }
    }
}
```
**Confidence:** HIGH -- Widely documented, simple, WebGL-compatible.

### Pattern 6: URP Vignette Timer Warning
**What:** When GameStateData.Timer drops below 10 seconds, smoothly increase the Vignette effect intensity with a red color. Stays at full intensity until run ends.
**When to use:** Last 10 seconds of a run.
**Implementation details:**
- Get VolumeProfile from Volume component on camera or global volume
- Use `profile.TryGet<Vignette>(out var vignette)` to access Vignette override
- Set `vignette.color.Override(Color.red)` and `vignette.intensity.Override(Mathf.Lerp(0, 0.4f, t))`
- Fade in over 1-2 seconds (smooth ramp from 0 to 0.4 intensity)
- Reset to 0 when run ends (Playing state exits)

```csharp
// TimerWarningEffect.cs
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimerWarningEffect : MonoBehaviour
{
    private Volume _volume;
    private Vignette _vignette;

    void Start()
    {
        _volume = FindAnyObjectByType<Volume>();
        if (_volume != null && _volume.profile.TryGet<Vignette>(out var v))
            _vignette = v;
    }

    void Update()
    {
        // Read timer from ECS
        float timer = /* GameStateData.Timer */;
        if (timer <= 10f && timer > 0f)
        {
            float t = 1f - (timer / 10f); // 0->1 as timer goes 10->0
            _vignette.color.Override(Color.red);
            _vignette.intensity.Override(Mathf.Lerp(0f, 0.4f, t));
        }
        else
        {
            _vignette.intensity.Override(0f);
        }
    }
}
```
**Confidence:** HIGH -- URP Vignette with Color property confirmed in official docs for Unity 6.

### Anti-Patterns to Avoid
- **Instantiate/Destroy for popups:** Never call Instantiate or Destroy at runtime for damage numbers, particles, or audio sources. Always use the GameObjectPool. At 200 damage ticks/sec, GC from Instantiate would cause frame drops.
- **One AudioSource per entity:** Do not attach AudioSources to asteroid or mineral GameObjects. Pool AudioSources centrally in AudioManager.
- **Spatial audio on WebGL:** Do not set spatialBlend > 0 on AudioSources. WebGL has a confirmed bug where any spatialBlend > 0 snaps to 1.0, breaking volume attenuation. Use 2D audio with manual distance-based volume.
- **Particle systems on ECS entities:** Do not create companion GameObjects or attach ParticleSystems to ECS entities. Spawn pooled particle effects at event positions from the MonoBehaviour bridge.
- **TMPro Rebuild batching issues:** Do not parent all damage popups under one Canvas. Each popup should have its own small world-space Canvas to avoid TMPro rebuild cascading across all active popups when one changes.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Object pooling | Custom linked list pool | Existing `GameObjectPool` wrapper around `UnityEngine.Pool.ObjectPool` | Already built, pre-warm support, proven in asteroid/mineral renderers |
| Text rendering | Custom mesh text generation | `TextMeshProUGUI` on world-space Canvas | TMPro handles font atlases, scaling, styles (italic, bold), alpha, all correctly |
| Audio mixing/routing | Manual volume multiplication on each AudioSource | `AudioMixer` with exposed parameters | AudioMixer handles decibel curves, ducking, snapshots correctly |
| Particle effects | Custom sprite spawning + manual animation | Unity `ParticleSystem` (Shuriken) | Built-in burst emission, lifetime, gravity, size-over-lifetime, color-over-lifetime |
| Timer-based color interpolation | Custom lerp in Update | `Mathf.Lerp` / `Color.Lerp` with `t = elapsed / duration` | Standard Unity math, no edge cases |
| Vignette effect | Custom full-screen shader/overlay | URP `Vignette` VolumeOverride | Already part of URP post-processing stack, runtime-modifiable via C# API |

**Key insight:** Every feedback system in this phase has a built-in Unity solution. The complexity is in wiring the ECS event pipeline to trigger these systems, not in building the feedback systems themselves.

## Common Pitfalls

### Pitfall 1: TMPro Canvas Rebuild Performance at Scale
**What goes wrong:** All damage popups parented under one shared Canvas causes TMPro to rebuild the entire canvas mesh whenever any single popup text changes. At 200 popups/sec, this dominates frame time.
**Why it happens:** Unity Canvas batching rebuilds all child UI elements when any child is marked dirty.
**How to avoid:** Each popup gets its own tiny world-space Canvas (1 TMPro child). Canvas rebuilds are isolated to individual popups. Alternatively, use CanvasGroup.alpha for fading (does not trigger rebuild) instead of modifying text color alpha.
**Warning signs:** Profiler shows Canvas.BuildBatch or Canvas.SendWillRenderCanvases taking > 2ms.

### Pitfall 2: WebGL Spatial Audio is Broken
**What goes wrong:** Setting AudioSource.spatialBlend to any value > 0 on WebGL causes the blend to snap to 1.0, producing pure 3D audio with incorrect attenuation. Sounds become silent or wrong volume.
**Why it happens:** Known Unity bug (tracked in Issue Tracker). WebGL audio implementation uses Web Audio API which handles spatial audio differently than FMOD on desktop.
**How to avoid:** Use 2D audio only (spatialBlend = 0) for all AudioSources. Implement manual distance-based volume attenuation: `volume = Mathf.Clamp01(1f - dist / maxDist)` to simulate spatial rolloff.
**Warning signs:** SFX sounds correct in Editor but wrong in WebGL build.

### Pitfall 3: AudioMixer Cannot Be Created at Runtime
**What goes wrong:** Attempting to create an AudioMixer asset via script at runtime fails. AudioMixer is an editor-only asset type.
**Why it happens:** AudioMixer is designed to be created and configured in the Unity Editor window. The runtime API only supports reading/modifying existing mixer assets.
**How to avoid:** Create the AudioMixer asset in the editor before implementing AudioManager. Use CoPlay MCP or have the user create `Assets/Audio/GameAudioMixer.mixer` with Master > SFX + Music groups. Expose "SFXVolume" and "MusicVolume" parameters.
**Warning signs:** NullReferenceException on AudioManager._mixer at runtime.

### Pitfall 4: Audio Not Playing on WebGL First Load
**What goes wrong:** No audio plays when the WebGL game first loads, even though AudioSources are playing.
**Why it happens:** Browser security policy requires user interaction (click/touch/keypress) before Web Audio API can create an AudioContext. Unity's WebGL audio implementation defers playback until this happens.
**How to avoid:** The game already requires a mouse click to interact (mining circle follows mouse), so audio will naturally start after the first interaction. However, background music started in Awake/Start may be silent. Gate music playback on first frame with valid mouse input, or accept that music starts after first click.
**Warning signs:** Music plays in Editor but is silent in WebGL until user clicks.

### Pitfall 5: Particle System Draw Calls on WebGL
**What goes wrong:** Many active ParticleSystems each produce separate draw calls, impacting WebGL CPU-side dispatch performance.
**Why it happens:** Each ParticleSystem with a unique material creates a separate draw batch. WebGL has higher per-draw-call overhead than desktop.
**How to avoid:** Use a shared material across all explosion ParticleSystems (same texture, same shader). Keep particle counts moderate (15-25 per explosion). Pool and reuse ParticleSystems. Max concurrent explosions bounded by asteroid kill rate.
**Warning signs:** Profiler shows > 50 extra draw calls during heavy destruction phase.

### Pitfall 6: Damage Number Spam During Peak Mining
**What goes wrong:** With 50 asteroids in the mining circle, MiningDamageSystem fires 50 damage ticks per interval (0.25s). That is 200 damage numbers per second, all showing "10" damage. The screen becomes unreadable.
**Why it happens:** User decision requires showing every number without aggregation. But the current tick interval (0.25s) with potentially 50 asteroids creates extreme volume.
**How to avoid:** This is acceptable per user decision ("show every number even when overlapping"). The pool must handle this throughput (200 popups/sec * 1s duration = 200 active popups). Size the pool to 300 to handle burst peaks. Use small font size (per user decision) so overlapping numbers don't fully obscure gameplay.
**Warning signs:** Frame drops during heavy mining. Monitor pool exhaustion.

### Pitfall 7: MineralCollectionSystem Modifying for Events Breaks Burst
**What goes wrong:** MineralCollectionSystem is currently a BurstCompiled ISystem. Adding DynamicBuffer writes for CollectionEvent requires accessing the buffer entity, which is fine for Burst but requires careful entity lookup.
**Why it happens:** The CollectionEvent buffer lives on a separate entity from the minerals being processed.
**How to avoid:** Look up the buffer entity once at the start of OnUpdate using `SystemAPI.GetSingletonBuffer<CollectionEvent>()`. This works in Burst-compiled ISystem because DynamicBuffer is unmanaged. Write to it in the collection loop.
**Warning signs:** Compilation error about managed types in Burst.

## Code Examples

### Modifying MiningDamageSystem to Emit DamageEvents
```csharp
// Inside MiningDamageSystem.OnUpdate, after damage is applied:
// MiningDamageSystem needs to be modified to write DamageEvents
// The DamageEvent buffer must be looked up once per frame

var damageBuffer = SystemAPI.GetSingletonBuffer<DamageEvent>();

// Inside the damage application block:
if (tickTimer.ValueRO.Elapsed >= config.TickInterval)
{
    health.ValueRW.CurrentHP -= config.DamagePerTick;
    tickTimer.ValueRW.Elapsed -= config.TickInterval;

    // Emit damage event for feedback systems
    damageBuffer.Add(new DamageEvent
    {
        Position = transform.ValueRO.Position,
        Amount = config.DamagePerTick,
        Type = DamageType.Normal, // Phase 5 adds crit/DoT logic
        ColorR = 255, ColorG = 255, ColorB = 255
    });
}
```

### Modifying MineralCollectionSystem to Emit CollectionEvents
```csharp
// Inside MineralCollectionSystem.OnUpdate:
var collectionBuffer = SystemAPI.GetSingletonBuffer<CollectionEvent>();

// Inside collection check:
if (dist <= GameConstants.MineralCollectionRadius)
{
    gameStateRW.ValueRW.Credits += mineralData.ValueRO.CreditValue;

    collectionBuffer.Add(new CollectionEvent
    {
        ResourceTier = mineralData.ValueRO.ResourceTier,
        CreditValue = mineralData.ValueRO.CreditValue,
        Position = transform.ValueRO.Position
    });

    ecb.DestroyEntity(entity);
}
```

### Adding TrailRenderer to Mineral Pool GameObjects
```csharp
// In MineralRenderer.ConfigureMineralVisual, add TrailRenderer:
var trail = go.GetComponent<TrailRenderer>();
if (trail == null) trail = go.AddComponent<TrailRenderer>();

trail.time = 0.3f; // trail duration
trail.startWidth = 0.15f;
trail.endWidth = 0.0f;
trail.minVertexDistance = 0.1f;
trail.shadowCastingMode = ShadowCastingMode.Off;
trail.receiveShadows = false;

// HDR emissive material matching mineral color
var trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
Color hdrColor = DefaultMineralColor * 2f; // HDR multiplier for glow
trailMaterial.SetColor("_BaseColor", hdrColor);
trail.material = trailMaterial;
```

### AudioManager Volume Control via AudioMixer
```csharp
// Volume slider callbacks
public void SetSFXVolume(float sliderValue)
{
    // Convert linear 0-1 to decibels (-80 to 0)
    float db = sliderValue > 0.001f ? Mathf.Log10(sliderValue) * 20f : -80f;
    _mixer.SetFloat("SFXVolume", db);
}

public void SetMusicVolume(float sliderValue)
{
    float db = sliderValue > 0.001f ? Mathf.Log10(sliderValue) * 20f : -80f;
    _mixer.SetFloat("MusicVolume", db);
}
```

### Credit Counter Pop Animation (HUDController)
```csharp
// In HUDController, track previous credits to detect changes
private long _previousCredits;
private float _popTimer;
private Vector3 _creditsOriginalScale;
private Color _creditsOriginalColor;

// In LateUpdate, after updating credits text:
if (gameState.Credits != _previousCredits)
{
    _previousCredits = gameState.Credits;
    _popTimer = 0.2f; // pop duration
}

if (_popTimer > 0)
{
    _popTimer -= Time.deltaTime;
    float t = _popTimer / 0.2f;
    // Scale pop: 1.0 -> 1.3 -> 1.0
    float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
    _creditsText.transform.localScale = _creditsOriginalScale * scale;
    _creditsText.color = Color.Lerp(_creditsOriginalColor, new Color(1f, 0.9f, 0.3f), t); // gold flash
}
```

## Discretion Recommendations

### Mineral Collection Chime Batching
**Recommendation:** Batch collection chimes within a 50ms window. When the first mineral is collected, start a 50ms timer. Count all collections in that window. Play one chime with pitch proportional to batch size: `basePitch + 0.05f * Mathf.Min(batchCount, 10)`. This creates a satisfying "cascade" effect as waves of minerals arrive without producing ear-splitting per-mineral chime spam at 1000+ minerals.

### Explosion Particle Counts
**Recommendation:** 15-25 particles per explosion. Use Burst emission mode (all at once). Lifetime: 0.5-0.8 seconds. Speed: 3-5 units/sec outward. Gravity: 2-3 (pulls debris down for weighty feel). Size: 0.1-0.3 (small chunky pieces). Rotation: random. Color: rock brown/gray matching asteroid palette. Shape: small cubes or default particle sprites.

### Damage Number Font and Timing
**Recommendation:** Use the default TMPro font (LiberationSans SDF -- already included). Font size: 2-3 world units for normal hits, 4-5 for crits. Rise speed: 1.5 units/sec. Fade duration: 0.8 seconds. Start fading at 0.3 seconds (visible for 0.3s at full alpha, then 0.5s fade out). This gives enough time to read numbers without cluttering the screen.

### Music Loop and Crossfade
**Recommendation:** Use a single ambient space music track, 2-4 minutes long, looping seamlessly. No crossfade needed for a single loop -- set AudioSource.loop = true. If multiple tracks are added later, implement a 2-second crossfade by running two music AudioSources simultaneously with inverse volume ramps. For now, one loop is sufficient.

### AudioMixer Group Structure
**Recommendation:**
```
Master (exposed: "MasterVolume")
├── SFX (exposed: "SFXVolume")
│   -- All gameplay sounds route here
└── Music (exposed: "MusicVolume")
    -- Background music routes here
```
Default volumes: Master 0dB, SFX 0dB, Music -6dB (music quieter than SFX by default). Volume controls use the logarithmic slider-to-dB conversion pattern shown in Code Examples.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| VFX Graph for particles | Shuriken ParticleSystem | N/A for WebGL (VFX Graph never supported) | Must use legacy Shuriken; VFX Graph requires compute shaders |
| 3D spatial audio on WebGL | 2D audio with manual rolloff | Ongoing bug since Unity 2019 | Cannot use spatialBlend > 0 on WebGL |
| PostProcessing v2 stack | URP VolumeProfile + Volume Overrides | Unity 2019.3 | Use `profile.TryGet<Vignette>()` not old PostProcessingProfile API |
| Instantiate/Destroy for transient objects | ObjectPool<T> pattern | Unity 2021 | Built-in ObjectPool<T> in UnityEngine.Pool namespace |
| Legacy GUI.Label | TextMeshPro on Canvas | Unity 2018+ | TMPro is standard for all text rendering, GPU-efficient SDF |

**Deprecated/outdated:**
- `OnGUI()` for damage numbers: Do not use. Legacy immediate-mode GUI is not compatible with Canvas rendering pipeline.
- `PostProcessingBehaviour` (old PPv2): Replaced by URP Volume system. Use `UnityEngine.Rendering.Universal.Vignette`, not `UnityEngine.Rendering.PostProcessing.Vignette`.
- `GUIText` / `3DText`: Replaced by TextMeshPro.

## Audio Asset Sources

The user decided on free CC0/royalty-free sound libraries. Recommended sources for the required audio clips:

| Clip Category | Recommended Sources |
|--------------|---------------------|
| Mining hits, impacts | [SONNISS GameAudioGDC](https://sonniss.com/gameaudiogdc/) -- royalty-free, no attribution |
| Explosions, destruction | [Freesound.org](https://freesound.org/) (filter by CC0) |
| Collection chimes, UI clicks | [OpenGameArt.org](https://opengameart.org/content/cc0-sound-effects) -- CC0 collection |
| Ambient space music | [itch.io CC0 music](https://itch.io/game-assets/assets-cc0/tag-music) or [Soundimage.org](https://soundimage.org/looping-music/) |
| Game over fanfare | [Mixkit](https://mixkit.co/free-sound-effects/game/) -- royalty-free game sounds |

Audio formats: `.wav` for SFX (uncompressed, small files), `.ogg` for music (compressed, larger files). Unity WebGL compresses all audio to AAC format in the build regardless of source format.

## Open Questions

1. **World-space popup billboard orientation**
   - What we know: Camera is at Y=18 with 60-degree angle looking down at XZ plane. Popups need to face the camera.
   - What's unclear: Whether world-space Canvas automatically billboards, or if manual billboard rotation is needed.
   - Recommendation: Add `transform.LookAt(Camera.main.transform)` or use `transform.rotation = Camera.main.transform.rotation` on each popup every frame. Test both approaches.

2. **FEED-02/03/04 dependencies on Phase 5**
   - What we know: Critical hits, DoT, and skill damage types are Phase 5 features. Phase 4 must build the infrastructure to display them but cannot fully test them.
   - What's unclear: Whether to add placeholder crit logic in Phase 4 for testing, or defer entirely.
   - Recommendation: Build the DamageType enum and popup styling in Phase 4. Add a debug key (e.g., hold Shift while mining = crit) for visual testing. Phase 5 replaces this with real crit logic.

3. **FEED-07 screen shake without crit system**
   - What we know: Screen shake triggers on critical hits and skill impacts (both Phase 5 features).
   - What's unclear: How to test screen shake in Phase 4 without crit events.
   - Recommendation: Build CameraShake with a public Shake() method. Wire it to DamageType.Critical events from the bridge (even though no crits occur yet). Add debug key for testing. Phase 5 will produce real crit events.

4. **AudioMixer creation workflow**
   - What we know: AudioMixer assets cannot be created via script. Must be created in Unity Editor.
   - What's unclear: Whether CoPlay MCP can create AudioMixer assets, or if user must do it manually.
   - Recommendation: Plan step creates the AudioMixer via CoPlay MCP if supported, otherwise documents manual steps for user. AudioManager script references it via serialized field.

## Sources

### Primary (HIGH confidence)
- Existing codebase analysis: `GameObjectPool.cs`, `AsteroidRenderer.cs`, `MineralRenderer.cs`, `MiningDamageSystem.cs`, `MineralCollectionSystem.cs`, `MineralSpawnSystem.cs`, `ECSBootstrap.cs`, `HUDController.cs`, `UISetup.cs` -- all read and analyzed
- [Unity 6.3 Technical limitations for Web](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html) -- WebGL constraints
- [Unity AudioMixer manual](https://docs.unity3d.com/Manual/AudioMixer.html) -- Mixer creation and exposed parameters
- [Unity Vignette Volume Override for URP](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/post-processing-vignette.html) -- Vignette properties (Color, Intensity, Smoothness, Center, Rounded)
- [Unity AudioMixer.SetFloat API](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.SetFloat.html) -- Runtime parameter control
- [Unity Volumes in URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/Volumes.html) -- Volume system documentation

### Secondary (MEDIUM confidence)
- [Unity Issue Tracker: AudioSource Spatial Blend WebGL bug](https://issuetracker.unity3d.com/issues/audio-source-spatial-blend-value-gets-set-to-1-in-the-webgl-player-when-the-spatial-blend-value-is-bigger-than-0) -- Confirmed spatial audio broken on WebGL
- [Unity Discussions: TMPro rebuild performance](https://forum.unity.com/threads/textmeshprougui-rebuild-performance-issue.743951/) -- Canvas rebuild performance issue
- [Unity Discussions: UI optimization for hundreds of floating damage text](https://discussions.unity.com/t/ui-optimization-hundreds-of-floating-damage-text/250517) -- Pooling and performance strategies
- [Unity Discussions: AudioMixer via script](https://discussions.unity.com/t/can-i-create-or-load-an-audiomixer-via-script/573285) -- Confirmed AudioMixer must be editor-created
- [Unity Learn: Create a burst particle](https://learn.unity.com/pathway/creative-core/unit/creative-core-vfx/tutorial/create-a-burst-particle-3) -- Burst emission configuration

### Tertiary (LOW confidence)
- [SONNISS GameAudioGDC](https://sonniss.com/gameaudiogdc/) -- Audio asset source (availability may change)
- [OpenGameArt.org CC0 sound effects](https://opengameart.org/content/cc0-sound-effects) -- Audio asset source

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- All required tools are already installed Unity packages; no new dependencies
- Architecture: HIGH -- DynamicBuffer event pattern already exists in codebase (CollectionEvent); extending it is well-understood
- Pitfalls: HIGH -- WebGL spatial audio bug is documented in Unity Issue Tracker; TMPro rebuild issues are well-known; AudioMixer limitation is confirmed
- Audio sourcing: MEDIUM -- CC0 asset availability depends on external websites; specific clips need user selection

**Research date:** 2026-02-18
**Valid until:** 2026-03-18 (30 days -- all APIs are stable Unity features)
