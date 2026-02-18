using ECS.Components;
using MonoBehaviours.Core;
using Unity.Entities;
using UnityEngine;

namespace States
{
    public class CollectingState : IGameState
    {
        private EntityManager em;
        private EntityQuery mineralQuery;
        private float gracePeriodTimer;
        private float totalTimer;
        private bool resolved;

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

            em = world.EntityManager;
            mineralQuery = em.CreateEntityQuery(typeof(MineralTag));
            gracePeriodTimer = 0f;
            totalTimer = 0f;
            resolved = true;

            // Run is over — destroy all remaining asteroids so only minerals remain
            DestroyAllAsteroids();
        }

        public void Execute(GameManager manager)
        {
            if (!resolved) return;

            totalTimer += Time.deltaTime;

            // Safety timeout — never stay in Collecting forever
            if (totalTimer >= MaxCollectingDuration)
            {
                manager.TransitionTo(GamePhase.GameOver);
                return;
            }

            int mineralCount = mineralQuery.CalculateEntityCount();

            if (mineralCount == 0)
            {
                gracePeriodTimer += Time.deltaTime;
                if (gracePeriodTimer >= GameConstants.CollectingGracePeriod)
                {
                    manager.TransitionTo(GamePhase.GameOver);
                }
            }
            else
            {
                gracePeriodTimer = 0f;
            }
        }

        private void DestroyAllAsteroids()
        {
            var asteroidQuery = em.CreateEntityQuery(typeof(AsteroidTag));
            var asteroids = asteroidQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < asteroids.Length; i++)
            {
                em.DestroyEntity(asteroids[i]);
            }
            asteroids.Dispose();
            asteroidQuery.Dispose();
        }

        public void Exit(GameManager manager)
        {
            Debug.Log("Exiting Collecting state");
        }
    }
}
