using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace MonoBehaviours.Audio
{
    /// <summary>
    /// Singleton audio system with pooled AudioSource playback, AudioMixer routing,
    /// and specialized methods for each game event type.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// Audio clips are loaded from Resources; null clips are handled gracefully (silent).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // -- AudioMixer integration -----------------------------------------------
        private AudioMixer mixer;
        private AudioMixerGroup sfxGroup;
        private AudioMixerGroup musicGroup;

        // -- AudioSource pool for SFX --------------------------------------------
        private readonly Queue<AudioSource> sfxPool = new Queue<AudioSource>();

        // -- Dedicated music source ----------------------------------------------
        private AudioSource musicSource;

        // -- Audio clip references (loaded from Resources) -----------------------
        private AudioClip damageHitClip;
        private AudioClip destructionClip;
        private AudioClip collectionChimeClip;
        private AudioClip fanfareClip;
        private AudioClip uiClickClip;
        private AudioClip musicClip;
        private AudioClip critHitClip;
        private AudioClip[] skillClips;

        // -- Damage hit throttle -------------------------------------------------
        private float lastDamageHitTime;

        // -- Collection chime batching -------------------------------------------
        private int collectionBatchCount;
        private float collectionBatchTimer;
        private int collectionBatchTier;

        // -- Music auto-start ----------------------------------------------------
        private bool musicStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("AudioManager");
                Instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Load AudioMixer from Resources (graceful degradation if not found)
            mixer = Resources.Load<AudioMixer>("Audio/GameAudioMixer");
            if (mixer != null)
            {
                var sfxGroups = mixer.FindMatchingGroups("SFX");
                if (sfxGroups != null && sfxGroups.Length > 0)
                    sfxGroup = sfxGroups[0];

                var musicGroups = mixer.FindMatchingGroups("Music");
                if (musicGroups != null && musicGroups.Length > 0)
                    musicGroup = musicGroups[0];

                Debug.Log("AudioManager: AudioMixer loaded with SFX and Music groups.");
            }
            else
            {
                Debug.LogWarning("AudioManager: GameAudioMixer not found in Resources. Audio will play without mixer routing.");
            }

            // Create SFX AudioSource pool
            for (int i = 0; i < GameConstants.AudioSFXPoolSize; i++)
            {
                var child = new GameObject($"SFXSource_{i}");
                child.transform.SetParent(transform);

                var source = child.AddComponent<AudioSource>();
                source.spatialBlend = 0f; // 2D only -- WebGL spatial blend is broken
                source.playOnAwake = false;

                if (sfxGroup != null)
                    source.outputAudioMixerGroup = sfxGroup;

                sfxPool.Enqueue(source);
            }

            // Create dedicated Music AudioSource
            var musicGo = new GameObject("MusicSource");
            musicGo.transform.SetParent(transform);
            musicSource = musicGo.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            if (musicGroup != null)
                musicSource.outputAudioMixerGroup = musicGroup;

            // Load audio clips from Resources (null is fine -- graceful degradation)
            damageHitClip = Resources.Load<AudioClip>("Audio/SFX/DamageHit");
            destructionClip = Resources.Load<AudioClip>("Audio/SFX/Destruction");
            collectionChimeClip = Resources.Load<AudioClip>("Audio/SFX/CollectionChime");
            fanfareClip = Resources.Load<AudioClip>("Audio/SFX/Fanfare");
            uiClickClip = Resources.Load<AudioClip>("Audio/SFX/UIClick");
            musicClip = Resources.Load<AudioClip>("Audio/Music/AmbientSpace");

            // Phase 5: skill and crit audio clips
            critHitClip = Resources.Load<AudioClip>("Audio/SFX/CritHit");
            skillClips = new AudioClip[]
            {
                Resources.Load<AudioClip>("Audio/SFX/SkillLaser"),
                Resources.Load<AudioClip>("Audio/SFX/SkillChain"),
                Resources.Load<AudioClip>("Audio/SFX/SkillEMP"),
                Resources.Load<AudioClip>("Audio/SFX/SkillOvercharge")
            };

            Debug.Log("AudioManager: initialized with SFX pool of " + GameConstants.AudioSFXPoolSize + " sources.");
        }

        private void Update()
        {
            // Collection chime batching: play after 50ms window
            if (collectionBatchTimer > 0f)
            {
                collectionBatchTimer -= Time.deltaTime;
                if (collectionBatchTimer <= 0f && collectionBatchCount > 0)
                {
                    float pitch = 1.0f + 0.05f * Mathf.Min(collectionBatchCount, 10);
                    PlaySfx(collectionChimeClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f, pitch);
                    collectionBatchCount = 0;
                }
            }

            // Auto-start music on first available frame
            if (!musicStarted && musicClip != null)
            {
                PlayMusic(musicClip);
                musicStarted = true;
            }
        }

        // =========================================================================
        // SFX Playback
        // =========================================================================

        /// <summary>
        /// Plays a one-shot SFX clip from the pool with distance-based volume attenuation.
        /// </summary>
        public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sfxPool.Count == 0) return;

            var source = sfxPool.Dequeue();
            source.clip = clip;
            source.pitch = pitch;

            // Manual distance-based volume attenuation (2D audio, no Unity spatial)
            if (Camera.main != null)
            {
                float dist = Vector3.Distance(position, Camera.main.transform.position);
                float attenuation = Mathf.Clamp01(1f - dist / GameConstants.SFXMaxDistance);
                source.volume = volume * attenuation;
            }
            else
            {
                source.volume = volume;
            }

            source.Play();
            StartCoroutine(ReturnToPool(source, Mathf.Min(clip.length / Mathf.Abs(pitch), 3f)));
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                sfxPool.Enqueue(source);
            }
        }

        // =========================================================================
        // Specialized Playback Methods
        // =========================================================================

        /// <summary>
        /// Plays mining hit SFX, throttled to max 4 per second (AUDI-01).
        /// </summary>
        public void PlayDamageHit(Vector3 position)
        {
            if (Time.time - lastDamageHitTime < GameConstants.DamageHitSFXCooldown) return;
            lastDamageHitTime = Time.time;

            PlaySfx(damageHitClip, position, 0.5f, Random.Range(0.9f, 1.1f));
        }

        /// <summary>
        /// Plays asteroid destruction SFX (AUDI-02).
        /// </summary>
        public void PlayDestruction(Vector3 position)
        {
            PlaySfx(destructionClip, position, 0.8f);
        }

        /// <summary>
        /// Queues a collection chime for batching within 50ms window (AUDI-03).
        /// </summary>
        public void QueueCollectionChime(int resourceTier)
        {
            collectionBatchCount++;
            collectionBatchTier = resourceTier;
            if (collectionBatchTimer <= 0f)
            {
                collectionBatchTimer = GameConstants.CollectionChimeBatchWindow;
            }
        }

        /// <summary>
        /// Plays game over fanfare SFX (AUDI-05).
        /// </summary>
        public void PlayGameOverFanfare()
        {
            var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            PlaySfx(fanfareClip, camPos, 1.0f);
        }

        /// <summary>
        /// Plays UI button click SFX (AUDI-07).
        /// </summary>
        public void PlayUIClick()
        {
            var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            PlaySfx(uiClickClip, camPos, 0.6f);
        }

        /// <summary>
        /// Plays skill activation SFX for the given skill type (AUDI-04).
        /// Skill types: 0=Laser, 1=Chain, 2=EMP, 3=Overcharge.
        /// </summary>
        public void PlaySkillSfx(int skillType)
        {
            if (skillType < 0 || skillClips == null || skillType >= skillClips.Length) return;
            var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            PlaySfx(skillClips[skillType], camPos, 0.7f);
        }

        /// <summary>
        /// Plays critical hit SFX at the given position (DMGS-03).
        /// Slightly higher pitch for crit distinction.
        /// </summary>
        public void PlayCritHit(Vector3 position)
        {
            PlaySfx(critHitClip, position, 0.6f, 1.2f);
        }

        // =========================================================================
        // Music Playback
        // =========================================================================

        /// <summary>
        /// Starts background music loop (AUDI-06).
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        /// <summary>
        /// Stops background music.
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        // =========================================================================
        // Volume Control
        // =========================================================================

        /// <summary>
        /// Sets SFX volume (0-1 linear) via AudioMixer exposed parameter (AUDI-08).
        /// </summary>
        public void SetSfxVolume(float linearValue)
        {
            if (mixer == null) return;
            float db = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
            mixer.SetFloat("SFXVolume", db);
        }

        /// <summary>
        /// Sets Music volume (0-1 linear) via AudioMixer exposed parameter (AUDI-08).
        /// </summary>
        public void SetMusicVolume(float linearValue)
        {
            if (mixer == null) return;
            float db = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
            mixer.SetFloat("MusicVolume", db);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
