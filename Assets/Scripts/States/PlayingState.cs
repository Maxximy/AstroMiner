using UnityEngine;
using Unity.Entities;

public class PlayingState : IGameState
{
    private EntityManager _em;
    private Entity _gameStateEntity;
    private bool _resolved;

    /// <summary>
    /// Static flag ensuring saved credits are loaded into ECS only once per session.
    /// Persists across state re-entries within the same application lifetime.
    /// </summary>
    private static bool _saveLoaded = false;

    public void Enter(GameManager manager)
    {
        Debug.Log("Entering Playing state");

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("PlayingState: ECS world not available");
            return;
        }

        _em = world.EntityManager;
        var query = _em.CreateEntityQuery(typeof(GameStateData));
        if (query.CalculateEntityCount() == 0)
        {
            Debug.LogError("PlayingState: GameStateData singleton not found");
            return;
        }

        _gameStateEntity = query.GetSingletonEntity();
        _resolved = true;

        // On first run of the session, load saved credits from prior session
        if (!_saveLoaded)
        {
            SaveManager.Instance?.LoadIntoECS();
            _saveLoaded = true;
        }

        // Initialize timer for this run
        var data = _em.GetComponentData<GameStateData>(_gameStateEntity);
        data.Timer = GameConstants.DefaultRunDuration;
        _em.SetComponentData(_gameStateEntity, data);

        // Snapshot credits at run start for "credits this run" calculation
        manager.CreditsAtRunStart = data.Credits;
    }

    public void Execute(GameManager manager)
    {
        if (!_resolved) return;

        var data = _em.GetComponentData<GameStateData>(_gameStateEntity);
        data.Timer -= Time.deltaTime;

        if (data.Timer <= 0f)
        {
            data.Timer = 0f;
            _em.SetComponentData(_gameStateEntity, data);
            manager.TransitionTo(GamePhase.Collecting);
            return;
        }

        _em.SetComponentData(_gameStateEntity, data);
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Playing state");
    }
}
