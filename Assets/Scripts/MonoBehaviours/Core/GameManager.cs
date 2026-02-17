using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private FadeController _fadeController;

    private IGameState _currentState;
    private GamePhase _currentPhase;
    private Dictionary<GamePhase, IGameState> _states;
    private bool _isTransitioning;

    /// <summary>
    /// The current game phase. Read-only from outside.
    /// </summary>
    public GamePhase CurrentPhase => _currentPhase;

    /// <summary>
    /// Credits snapshot at the start of the current run.
    /// Used by HUDController and ResultsScreen to compute "credits this run".
    /// </summary>
    public long CreditsAtRunStart { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _states = new Dictionary<GamePhase, IGameState>
        {
            { GamePhase.Playing, new PlayingState() },
            { GamePhase.Collecting, new CollectingState() },
            { GamePhase.GameOver, new GameOverState() },
            { GamePhase.Upgrading, new UpgradingState() },
        };
    }

    void Start()
    {
        // Find FadeController created by UISetup (runs in Awake)
        _fadeController = FindAnyObjectByType<FadeController>();
        if (_fadeController == null)
            Debug.LogError("GameManager: FadeController not found. Ensure UISetup runs before GameManager.Start.");

        // Set initial state to Playing without fade (immediate)
        _currentPhase = GamePhase.Playing;
        _currentState = _states[GamePhase.Playing];
        _currentState.Enter(this);
        _fadeController?.SetClear();

        // Write initial phase to ECS singleton
        WritePhaseToECS(GamePhase.Playing);
    }

    void Update()
    {
        _currentState?.Execute(this);
    }

    /// <summary>
    /// Transition to a new game phase with fade-to-black (unless Playing->Collecting).
    /// </summary>
    public void TransitionTo(GamePhase newPhase)
    {
        // Guard: prevent overlapping transitions
        if (_isTransitioning) return;

        // Guard: prevent transition to same state
        if (_currentPhase == newPhase) return;

        _isTransitioning = true;

        // Special case: Playing -> Collecting has NO fade (per user decision: gameplay stays visible)
        if (_currentPhase == GamePhase.Playing && newPhase == GamePhase.Collecting)
        {
            _currentState?.Exit(this);
            _currentPhase = newPhase;
            WritePhaseToECS(newPhase);
            _currentState = _states[newPhase];
            _currentState.Enter(this);
            _isTransitioning = false;
            return;
        }

        // Normal case: fade out -> switch state -> fade in
        var previousState = _currentState;
        previousState?.Exit(this);

        _fadeController.FadeOut(() =>
        {
            _currentPhase = newPhase;
            WritePhaseToECS(newPhase);
            _currentState = _states[newPhase];
            _currentState.Enter(this);

            _fadeController.FadeIn(() =>
            {
                _isTransitioning = false;
            });
        });
    }

    /// <summary>
    /// Resets the game state for a new run. Called before transitioning to Playing from Upgrading.
    /// Destroys all leftover asteroid and mineral entities, resets the timer.
    /// Credits are persistent across runs (only timer resets).
    /// </summary>
    public void ResetRun()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        var em = world.EntityManager;

        // Destroy all remaining asteroids
        var asteroidQuery = em.CreateEntityQuery(typeof(AsteroidTag));
        em.DestroyEntity(asteroidQuery);

        // Destroy all remaining minerals
        var mineralQuery = em.CreateEntityQuery(typeof(MineralTag));
        em.DestroyEntity(mineralQuery);

        // Reset timer (credits stay persistent)
        var gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
        if (gameStateQuery.CalculateEntityCount() > 0)
        {
            var entity = gameStateQuery.GetSingletonEntity();
            var data = em.GetComponentData<GameStateData>(entity);
            data.Timer = GameConstants.DefaultRunDuration;
            em.SetComponentData(entity, data);
        }

        Debug.Log("ResetRun: Cleared entities and reset timer for new run");
    }

    private void WritePhaseToECS(GamePhase phase)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        var em = world.EntityManager;
        var query = em.CreateEntityQuery(typeof(GameStateData));
        if (query.CalculateEntityCount() == 0) return;

        var entity = query.GetSingletonEntity();
        var data = em.GetComponentData<GameStateData>(entity);
        data.Phase = phase;
        em.SetComponentData(entity, data);
    }
}
