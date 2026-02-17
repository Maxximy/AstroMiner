using UnityEngine;
using Unity.Entities;

public class CollectingState : IGameState
{
    private EntityManager _em;
    private EntityQuery _mineralQuery;
    private float _gracePeriodTimer;
    private float _totalTimer;
    private bool _resolved;

    private const float MaxCollectingDuration = 10f;

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
        _gracePeriodTimer = 0f;
        _totalTimer = 0f;
        _resolved = true;

        // Run is over — destroy all remaining asteroids so only minerals remain
        DestroyAllAsteroids();
    }

    public void Execute(GameManager manager)
    {
        if (!_resolved) return;

        _totalTimer += Time.deltaTime;

        // Safety timeout — never stay in Collecting forever
        if (_totalTimer >= MaxCollectingDuration)
        {
            manager.TransitionTo(GamePhase.GameOver);
            return;
        }

        int mineralCount = _mineralQuery.CalculateEntityCount();

        if (mineralCount == 0)
        {
            _gracePeriodTimer += Time.deltaTime;
            if (_gracePeriodTimer >= GameConstants.CollectingGracePeriod)
            {
                manager.TransitionTo(GamePhase.GameOver);
            }
        }
        else
        {
            _gracePeriodTimer = 0f;
        }
    }

    private void DestroyAllAsteroids()
    {
        var asteroidQuery = _em.CreateEntityQuery(typeof(AsteroidTag));
        var asteroids = asteroidQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < asteroids.Length; i++)
        {
            _em.DestroyEntity(asteroids[i]);
        }
        asteroids.Dispose();
        asteroidQuery.Dispose();
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Collecting state");
    }
}
