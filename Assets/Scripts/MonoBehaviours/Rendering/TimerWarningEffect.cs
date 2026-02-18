using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Entities;

/// <summary>
/// Reads GameStateData.Timer from ECS and controls URP Vignette intensity.
/// Fades in a steady red vignette during the last 10 seconds of a run.
/// Vignette intensity is 0 during normal gameplay (respects clean visual style decision from 01-01).
/// Self-instantiates via [RuntimeInitializeOnLoadMethod].
/// </summary>
public class TimerWarningEffect : MonoBehaviour
{
    public static TimerWarningEffect Instance { get; private set; }

    private Volume _volume;
    private Vignette _vignette;
    private EntityManager _em;
    private EntityQuery _gameStateQuery;
    private bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            var go = new GameObject("TimerWarningEffect");
            Instance = go.AddComponent<TimerWarningEffect>();
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
        // Find the existing Volume component in the scene
        _volume = FindAnyObjectByType<Volume>();
        if (_volume != null && _volume.profile != null)
        {
            if (!_volume.profile.TryGet<Vignette>(out _vignette))
            {
                // Add Vignette override if not present in the profile
                _vignette = _volume.profile.Add<Vignette>(overrideState: true);
            }
            Debug.Log("TimerWarningEffect: Vignette control initialized.");
        }
        else
        {
            Debug.LogWarning("TimerWarningEffect: No Volume or VolumeProfile found. Timer warning vignette disabled.");
        }
    }

    private void Update()
    {
        if (_vignette == null) return;

        if (!_initialized)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            _em = world.EntityManager;
            _gameStateQuery = _em.CreateEntityQuery(typeof(GameStateData));
            _initialized = true;
        }

        if (_gameStateQuery.CalculateEntityCount() == 0) return;
        var gameState = _gameStateQuery.GetSingleton<GameStateData>();

        // Steady red vignette that fades in during last N seconds, stays at constant intensity
        if (gameState.Phase == GamePhase.Playing && gameState.Timer <= GameConstants.TimerWarningThreshold && gameState.Timer > 0f)
        {
            float t = 1f - (gameState.Timer / GameConstants.TimerWarningThreshold); // 0->1 as timer counts down
            _vignette.color.Override(Color.red);
            _vignette.intensity.Override(Mathf.Lerp(0f, GameConstants.TimerWarningMaxIntensity, t));
            _vignette.active = true;
        }
        else
        {
            _vignette.intensity.Override(0f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
