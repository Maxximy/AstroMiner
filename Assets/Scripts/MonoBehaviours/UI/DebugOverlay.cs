using ECS.Components;
using TMPro;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MonoBehaviours.UI
{
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI StateText;
        [SerializeField] private TextMeshProUGUI FPSText;
        [SerializeField] private TextMeshProUGUI EntityCountText;

        private float fpsTimer;
        private int frameCount;
        private EntityManager em;
        private bool ecsReady;
        private EntityQuery gameStateQuery;
        private EntityQuery entityCountQuery;

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                em = world.EntityManager;
                gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
                entityCountQuery = em.CreateEntityQuery(typeof(LocalTransform));
                ecsReady = true;
            }
        }

        void LateUpdate()
        {
            UpdateFPS();

            if (!ecsReady) return;

            UpdateState();
            UpdateEntityCount();
        }

        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= 0.5f)
            {
                float fps = frameCount / fpsTimer;
                FPSText.SetText("FPS: {0:0}", fps);
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        private void UpdateState()
        {
            if (gameStateQuery.CalculateEntityCount() > 0)
            {
                var gameState = gameStateQuery.GetSingleton<GameStateData>();
                StateText.SetText(gameState.Phase.ToString());
            }
        }

        private void UpdateEntityCount()
        {
            int count = entityCountQuery.CalculateEntityCount();
            EntityCountText.SetText("Entities: {0}", count);
        }
    }
}
