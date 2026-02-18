---
phase: 04-visual-and-audio-feedback
plan: 02
subsystem: audio
tags: [audio-manager, audio-mixer, sfx-pool, camera-shake, vignette, urp-volume, feedback-bridge, coroutine-pool]

# Dependency graph
requires:
  - phase: 04-visual-and-audio-feedback/01
    provides: DamageEvent, DestructionEvent, CollectionEvent buffers, DamagePopupManager, ExplosionManager
  - phase: 03-collection-economy-and-session
    provides: MineralCollectionSystem CollectionEvent, GameStateData timer, UISetup buttons, GameOverState
provides:
  - AudioManager singleton with pooled AudioSource SFX playback and AudioMixer routing
  - FeedbackEventBridge that drains all three ECS event buffers and dispatches to 5 consumer systems
  - CameraShake for screen shake on critical hit events
  - TimerWarningEffect red vignette during last 10 seconds via URP Volume Vignette override
  - AudioMixer asset with Master > SFX + Music groups and exposed volume parameters
  - UI button click SFX on all programmatic buttons
  - Game over fanfare playback
  - Collection chime batching with pitch variation
  - Stub PlaySkillSFX method ready for Phase 5
affects: [05-ship-skills-advanced-damage]

# Tech tracking
tech-stack:
  added: [AudioMixer, AudioSource pooling, URP Vignette override]
  patterns: [coroutine-based AudioSource pool return, manual 2D distance attenuation, 50ms collection chime batching, mixer exposed parameter volume control]

key-files:
  created:
    - Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs
    - Assets/Scripts/MonoBehaviours/Bridge/FeedbackEventBridge.cs
    - Assets/Scripts/MonoBehaviours/Rendering/CameraShake.cs
    - Assets/Scripts/MonoBehaviours/Rendering/TimerWarningEffect.cs
    - Assets/Resources/Audio/GameAudioMixer.mixer
    - Assets/Resources/Audio/SFX/DamageHit.wav
    - Assets/Resources/Audio/SFX/Destruction.wav
    - Assets/Resources/Audio/SFX/CollectionChime.wav
    - Assets/Resources/Audio/SFX/UIClick.wav
    - Assets/Resources/Audio/SFX/Fanfare.wav
    - Assets/Resources/Audio/Music/AmbientSpace.ogg
  modified:
    - Assets/Scripts/MonoBehaviours/Core/UISetup.cs
    - Assets/Scripts/States/GameOverState.cs
    - Assets/Scripts/Shared/GameConstants.cs
    - Assets/Scenes/Game.unity

key-decisions:
  - "AudioMixer placed at Assets/Resources/Audio/GameAudioMixer -- loaded via Resources.Load with graceful degradation if missing"
  - "All AudioSources use spatialBlend=0 (2D) with manual distance-based volume attenuation -- WebGL spatial audio is broken per research"
  - "SFX pool of 20 AudioSources with coroutine-based return after clip finishes playing"
  - "Mining hit SFX throttled to max 4/sec via cooldown timer to prevent audio overload"
  - "Collection chimes batch within 50ms window with pitch increasing by batch count for satisfying rapid-collect feel"
  - "CameraShake attaches to Main Camera GameObject directly (not a separate object) for correct transform offset"
  - "TimerWarningEffect manually sets Vignette override state per-property (intensity, color) -- URP Add<T>(overrideState) parameter does not work as expected"
  - "FeedbackEventBridge is the single central dispatcher for all ECS event buffers to all visual and audio consumer systems"

patterns-established:
  - "Central event bridge pattern: single MonoBehaviour drains all ECS event buffers in LateUpdate and dispatches to multiple consumer managers"
  - "AudioSource pool with coroutine return: dequeue from Queue, play clip, StartCoroutine to re-enqueue after clip.length seconds"
  - "Manual 2D distance attenuation: volume *= Clamp01(1 - dist/maxDist) since WebGL spatial blend is broken"
  - "Audio clip Resources.Load with null-safe playback: all methods silently return if clip is null for graceful degradation"

requirements-completed: [FEED-07, FEED-08, AUDI-01, AUDI-02, AUDI-03, AUDI-04, AUDI-05, AUDI-06, AUDI-07, AUDI-08]

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 4 Plan 02: Audio & Feedback Bridge Summary

**Pooled AudioManager with AudioMixer SFX/Music routing, FeedbackEventBridge wiring all ECS events to 5 consumer systems, CameraShake on crits, and TimerWarningEffect red vignette**

## Performance

- **Duration:** 8 min (including checkpoint verification)
- **Started:** 2026-02-18T06:45:00Z
- **Completed:** 2026-02-18T06:59:00Z
- **Tasks:** 2
- **Files modified:** 15 (7 scripts created, 1 mixer asset, 6 audio clips, 1 scene modified)

## Accomplishments
- Built AudioManager singleton with 20-source AudioSource pool, AudioMixer integration (Master > SFX + Music groups), and specialized playback methods for damage hits, destruction, collection chimes, UI clicks, fanfare, and background music
- Created FeedbackEventBridge as the central dispatcher that drains DamageEvent, DestructionEvent, and CollectionEvent ECS buffers every frame and routes to DamagePopupManager, ExplosionManager, AudioManager, and CameraShake
- Implemented CameraShake (brief XZ offset on critical hits) and TimerWarningEffect (red URP Vignette fade-in during last 10 seconds)
- Wired UI button click SFX and game over fanfare into existing UISetup and GameOverState code
- Created and verified all audio assets: AudioMixer, 5 SFX clips, 1 ambient music loop

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AudioManager, CameraShake, TimerWarningEffect, and FeedbackEventBridge** - `06c7f9b` (feat)
2. **Task 2: Create AudioMixer asset, source audio clips, and verify full feedback pipeline** - `f4c7d69` (feat)

## Files Created/Modified
- `Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs` - Singleton audio system with AudioSource pool, mixer routing, SFX/music playback
- `Assets/Scripts/MonoBehaviours/Bridge/FeedbackEventBridge.cs` - Central ECS event buffer drain and dispatch to all feedback managers
- `Assets/Scripts/MonoBehaviours/Rendering/CameraShake.cs` - Brief screen shake on critical hit events (attached to Main Camera)
- `Assets/Scripts/MonoBehaviours/Rendering/TimerWarningEffect.cs` - Red URP Vignette override during last 10 seconds of run
- `Assets/Scripts/MonoBehaviours/Core/UISetup.cs` - Added click SFX listener to all programmatic buttons
- `Assets/Scripts/States/GameOverState.cs` - Added game over fanfare playback
- `Assets/Scripts/Shared/GameConstants.cs` - Added 8 audio/feedback tuning constants
- `Assets/Resources/Audio/GameAudioMixer.mixer` - AudioMixer with Master > SFX + Music groups, exposed SFXVolume/MusicVolume parameters
- `Assets/Resources/Audio/SFX/DamageHit.wav` - Mining hit sound (short metallic impact)
- `Assets/Resources/Audio/SFX/Destruction.wav` - Asteroid destruction explosion sound
- `Assets/Resources/Audio/SFX/CollectionChime.wav` - Mineral collection chime
- `Assets/Resources/Audio/SFX/UIClick.wav` - UI button click sound
- `Assets/Resources/Audio/SFX/Fanfare.wav` - Game over fanfare jingle
- `Assets/Resources/Audio/Music/AmbientSpace.ogg` - Space ambient music loop
- `Assets/Scenes/Game.unity` - Scene updates for feedback systems

## Decisions Made
- AudioMixer placed at `Assets/Resources/Audio/GameAudioMixer` rather than root Resources -- cleaner organization, required resource path fix in AudioManager.cs
- All AudioSources use spatialBlend=0 (2D) with manual distance-based volume attenuation because WebGL spatial audio is unreliable
- SFX pool of 20 AudioSources with coroutine-based return after clip completes playing
- Mining hit SFX throttled to max 4/sec to prevent audio overload during rapid mining
- Collection chimes batch within 50ms window with pitch increasing per batch count for satisfying rapid-collect feedback
- CameraShake attaches directly to Main Camera GameObject for correct transform offset
- TimerWarningEffect manually sets Vignette intensity and color override states individually (URP `Add<Vignette>(overrideState: true)` parameter does not apply to individual properties as expected)
- FeedbackEventBridge serves as the single central dispatcher -- all ECS event consumers receive events through this one bridge

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed AudioMixer resource path**
- **Found during:** Task 2 (checkpoint verification)
- **Issue:** AudioManager loaded mixer from `Resources.Load("GameAudioMixer")` but user placed mixer at `Assets/Resources/Audio/GameAudioMixer.mixer`
- **Fix:** Changed resource path to `"Audio/GameAudioMixer"` to match actual asset location
- **Files modified:** Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs
- **Verification:** AudioMixer loads correctly in Play Mode
- **Committed in:** f4c7d69

**2. [Rule 1 - Bug] Fixed URP Vignette override state initialization**
- **Found during:** Task 2 (checkpoint verification)
- **Issue:** `_volume.profile.Add<Vignette>(overrideState: true)` does not set per-property override states in URP -- Vignette intensity and color remained inactive
- **Fix:** Changed to `Add<Vignette>()` followed by explicit `_vignette.intensity.overrideState = true` and `_vignette.color.overrideState = true`
- **Files modified:** Assets/Scripts/MonoBehaviours/Rendering/TimerWarningEffect.cs
- **Verification:** Red vignette correctly fades in during last 10 seconds
- **Committed in:** f4c7d69

**3. [Rule 1 - Bug] Removed debug log from PlayDamageHit**
- **Found during:** Post-checkpoint cleanup
- **Issue:** `Debug.Log("play damage hit")` left in from user testing -- would spam console during gameplay
- **Fix:** Removed the debug log line
- **Files modified:** Assets/Scripts/MonoBehaviours/Audio/AudioManager.cs
- **Committed in:** f4c7d69

---

**Total deviations:** 3 auto-fixed (3 bug fixes)
**Impact on plan:** All auto-fixes necessary for correctness. No scope creep.

## Issues Encountered

None beyond the auto-fixed items above.

## User Setup Required

None - all managers self-instantiate via RuntimeInitializeOnLoadMethod. Audio assets are loaded via Resources.Load with graceful degradation.

## Next Phase Readiness
- Phase 4 is now complete -- all visual and audio feedback systems are operational
- Phase 5 (Ship Skills and Advanced Damage) can emit DamageType.Critical and DamageType.Skill events and they will automatically trigger camera shake and styled damage popups through the existing pipeline
- PlaySkillSFX stub method exists in AudioManager ready for Phase 5 to implement per-skill sounds
- AudioMixer volume controls are ready for a settings UI (deferred to future work)

## Self-Check: PASSED

- All 4 created script files verified on disk (AudioManager.cs, FeedbackEventBridge.cs, CameraShake.cs, TimerWarningEffect.cs)
- All 6 audio asset files verified on disk (GameAudioMixer.mixer, DamageHit.wav, Destruction.wav, CollectionChime.wav, UIClick.wav, Fanfare.wav, AmbientSpace.ogg)
- Commit 06c7f9b (Task 1) verified in git log
- Commit f4c7d69 (Task 2) verified in git log

---
*Phase: 04-visual-and-audio-feedback*
*Completed: 2026-02-18*
