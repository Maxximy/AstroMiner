using ECS.Components;
using MonoBehaviours.Core;
using MonoBehaviours.Save;
using Unity.Entities;
using UnityEngine;

namespace States
{
    public class PlayingState : IGameState
    {
        private EntityManager em;
        private Entity gameStateEntity;
        private bool resolved;

        /// <summary>
        /// Static flag ensuring saved credits are loaded into ECS only once per session.
        /// Persists across state re-entries within the same application lifetime.
        /// </summary>
        private static bool saveLoaded = false;

        public void Enter(GameManager manager)
        {
            Debug.Log("Entering Playing state");

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("PlayingState: ECS world not available");
                return;
            }

            em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(GameStateData));
            if (query.CalculateEntityCount() == 0)
            {
                Debug.LogError("PlayingState: GameStateData singleton not found");
                return;
            }

            gameStateEntity = query.GetSingletonEntity();
            resolved = true;

            // On first run of the session, load saved credits from prior session
            if (!saveLoaded)
            {
                SaveManager.Instance?.LoadIntoECS();
                saveLoaded = true;
            }

            // Initialize timer for this run
            var data = em.GetComponentData<GameStateData>(gameStateEntity);
            data.Timer = GameConstants.DefaultRunDuration;
            em.SetComponentData(gameStateEntity, data);

            // Snapshot credits at run start for "credits this run" calculation
            manager.CreditsAtRunStart = data.Credits;
        }

        public void Execute(GameManager manager)
        {
            if (!resolved) return;

            var data = em.GetComponentData<GameStateData>(gameStateEntity);
            data.Timer -= Time.deltaTime;

            if (data.Timer <= 0f)
            {
                data.Timer = 0f;
                em.SetComponentData(gameStateEntity, data);
                manager.TransitionTo(GamePhase.Collecting);
                return;
            }

            em.SetComponentData(gameStateEntity, data);
        }

        public void Exit(GameManager manager)
        {
            Debug.Log("Exiting Playing state");
        }
    }
}
