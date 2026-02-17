using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        // TODO: Remove in Phase 2 -- temporary testing shortcuts
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) TransitionTo(GamePhase.Playing);
            if (kb.digit2Key.wasPressedThisFrame) TransitionTo(GamePhase.Collecting);
            if (kb.digit3Key.wasPressedThisFrame) TransitionTo(GamePhase.GameOver);
            if (kb.digit4Key.wasPressedThisFrame) TransitionTo(GamePhase.Upgrading);
        }
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
