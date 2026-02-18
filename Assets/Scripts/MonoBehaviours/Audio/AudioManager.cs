using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
    private AudioMixer _mixer;
    private AudioMixerGroup _sfxGroup;
    private AudioMixerGroup _musicGroup;

    // -- AudioSource pool for SFX --------------------------------------------
    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

    // -- Dedicated music source ----------------------------------------------
    private AudioSource _musicSource;

    // -- Audio clip references (loaded from Resources) -----------------------
    private AudioClip _damageHitClip;
    private AudioClip _destructionClip;
    private AudioClip _collectionChimeClip;
    private AudioClip _fanfareClip;
    private AudioClip _uiClickClip;
    private AudioClip _musicClip;

    // -- Damage hit throttle -------------------------------------------------
    private float _lastDamageHitTime;

    // -- Collection chime batching -------------------------------------------
    private int _collectionBatchCount;
    private float _collectionBatchTimer;
    private int _collectionBatchTier;

    // -- Music auto-start ----------------------------------------------------
    private bool _musicStarted;

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
        _mixer = Resources.Load<AudioMixer>("GameAudioMixer");
        if (_mixer != null)
        {
            var sfxGroups = _mixer.FindMatchingGroups("SFX");
            if (sfxGroups != null && sfxGroups.Length > 0)
                _sfxGroup = sfxGroups[0];

            var musicGroups = _mixer.FindMatchingGroups("Music");
            if (musicGroups != null && musicGroups.Length > 0)
                _musicGroup = musicGroups[0];

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

            if (_sfxGroup != null)
                source.outputAudioMixerGroup = _sfxGroup;

            _sfxPool.Enqueue(source);
        }

        // Create dedicated Music AudioSource
        var musicGO = new GameObject("MusicSource");
        musicGO.transform.SetParent(transform);
        _musicSource = musicGO.AddComponent<AudioSource>();
        _musicSource.spatialBlend = 0f;
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;

        if (_musicGroup != null)
            _musicSource.outputAudioMixerGroup = _musicGroup;

        // Load audio clips from Resources (null is fine -- graceful degradation)
        _damageHitClip = Resources.Load<AudioClip>("Audio/SFX/DamageHit");
        _destructionClip = Resources.Load<AudioClip>("Audio/SFX/Destruction");
        _collectionChimeClip = Resources.Load<AudioClip>("Audio/SFX/CollectionChime");
        _fanfareClip = Resources.Load<AudioClip>("Audio/SFX/Fanfare");
        _uiClickClip = Resources.Load<AudioClip>("Audio/SFX/UIClick");
        _musicClip = Resources.Load<AudioClip>("Audio/Music/AmbientSpace");

        Debug.Log("AudioManager: initialized with SFX pool of " + GameConstants.AudioSFXPoolSize + " sources.");
    }

    private void Update()
    {
        // Collection chime batching: play after 50ms window
        if (_collectionBatchTimer > 0f)
        {
            _collectionBatchTimer -= Time.deltaTime;
            if (_collectionBatchTimer <= 0f && _collectionBatchCount > 0)
            {
                float pitch = 1.0f + 0.05f * Mathf.Min(_collectionBatchCount, 10);
                PlaySFX(_collectionChimeClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f, pitch);
                _collectionBatchCount = 0;
            }
        }

        // Auto-start music on first available frame
        if (!_musicStarted && _musicClip != null)
        {
            PlayMusic(_musicClip);
            _musicStarted = true;
        }
    }

    // =========================================================================
    // SFX Playback
    // =========================================================================

    /// <summary>
    /// Plays a one-shot SFX clip from the pool with distance-based volume attenuation.
    /// </summary>
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || _sfxPool.Count == 0) return;

        var source = _sfxPool.Dequeue();
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
            _sfxPool.Enqueue(source);
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
        if (Time.time - _lastDamageHitTime < GameConstants.DamageHitSFXCooldown) return;
        _lastDamageHitTime = Time.time;

        PlaySFX(_damageHitClip, position, 0.5f, Random.Range(0.9f, 1.1f));
    }

    /// <summary>
    /// Plays asteroid destruction SFX (AUDI-02).
    /// </summary>
    public void PlayDestruction(Vector3 position)
    {
        PlaySFX(_destructionClip, position, 0.8f);
    }

    /// <summary>
    /// Queues a collection chime for batching within 50ms window (AUDI-03).
    /// </summary>
    public void QueueCollectionChime(int resourceTier)
    {
        _collectionBatchCount++;
        _collectionBatchTier = resourceTier;
        if (_collectionBatchTimer <= 0f)
        {
            _collectionBatchTimer = GameConstants.CollectionChimeBatchWindow;
        }
    }

    /// <summary>
    /// Plays game over fanfare SFX (AUDI-05).
    /// </summary>
    public void PlayGameOverFanfare()
    {
        var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        PlaySFX(_fanfareClip, camPos, 1.0f);
    }

    /// <summary>
    /// Plays UI button click SFX (AUDI-07).
    /// </summary>
    public void PlayUIClick()
    {
        var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        PlaySFX(_uiClickClip, camPos, 0.6f);
    }

    /// <summary>
    /// Stub for skill activation SFX (AUDI-04). Phase 5 will implement real skill sounds.
    /// </summary>
    public void PlaySkillSFX(int skillType)
    {
        // Phase 5 placeholder -- no-op until skill system exists
    }

    // =========================================================================
    // Music Playback
    // =========================================================================

    /// <summary>
    /// Starts background music loop (AUDI-06).
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource == null) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    /// <summary>
    /// Stops background music.
    /// </summary>
    public void StopMusic()
    {
        if (_musicSource != null)
            _musicSource.Stop();
    }

    // =========================================================================
    // Volume Control
    // =========================================================================

    /// <summary>
    /// Sets SFX volume (0-1 linear) via AudioMixer exposed parameter (AUDI-08).
    /// </summary>
    public void SetSFXVolume(float linearValue)
    {
        if (_mixer == null) return;
        float db = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
        _mixer.SetFloat("SFXVolume", db);
    }

    /// <summary>
    /// Sets Music volume (0-1 linear) via AudioMixer exposed parameter (AUDI-08).
    /// </summary>
    public void SetMusicVolume(float linearValue)
    {
        if (_mixer == null) return;
        float db = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
        _mixer.SetFloat("MusicVolume", db);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
