using UnityEngine;
using Unity.Entities;

public class CollectingState : IGameState
{
    private EntityManager _em;
    private EntityQuery _mineralQuery;
    private EntityQuery _asteroidQuery;
    private float _gracePeriodTimer;
    private bool _resolved;

    public void Enter(GameManager manager)
    {
        Debug.Log("Entering Collecting state");

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("CollectingState: ECS world not available");
            return;
        }

        _em = world.EntityManager;
        _mineralQuery = _em.CreateEntityQuery(typeof(MineralTag));
        _asteroidQuery = _em.CreateEntityQuery(typeof(AsteroidTag));
        _gracePeriodTimer = 0f;
        _resolved = true;
    }

    public void Execute(GameManager manager)
    {
        if (!_resolved) return;

        int mineralCount = _mineralQuery.CalculateEntityCount();
        int asteroidCount = _asteroidQuery.CalculateEntityCount();

        if (mineralCount == 0 && asteroidCount == 0)
        {
            _gracePeriodTimer += Time.deltaTime;
            if (_gracePeriodTimer >= GameConstants.CollectingGracePeriod)
            {
                manager.TransitionTo(GamePhase.GameOver);
            }
        }
        else
        {
            // Entities reappeared (ECB edge case) -- reset grace timer
            _gracePeriodTimer = 0f;
        }
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Collecting state");
    }
}
