using ECS.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Reads GameStateData.Timer from ECS and controls URP Vignette intensity.
    /// Fades in a steady red vignette during the last 10 seconds of a run.
    /// Vignette intensity is 0 during normal gameplay (respects clean visual style decision from 01-01).
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public class TimerWarningEffect : MonoBehaviour
    {
        public static TimerWarningEffect Instance { get; private set; }

        private Volume volume;
        private Vignette vignette;
        private EntityManager em;
        private EntityQuery gameStateQuery;
        private bool initialized;

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
            volume = FindAnyObjectByType<Volume>();
            if (volume != null && volume.profile != null)
            {
                if (!volume.profile.TryGet<Vignette>(out vignette))
                {
                    // Add Vignette override if not present in the profile
                    vignette = volume.profile.Add<Vignette>();
                    vignette.intensity.overrideState = true;
                    vignette.color.overrideState = true;
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
            if (vignette == null) return;

            if (!initialized)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                em = world.EntityManager;
                gameStateQuery = em.CreateEntityQuery(typeof(GameStateData));
                initialized = true;
            }

            if (gameStateQuery.CalculateEntityCount() == 0) return;
            var gameState = gameStateQuery.GetSingleton<GameStateData>();

            // Steady red vignette that fades in during last N seconds, stays at constant intensity
            if (gameState.Phase == GamePhase.Playing && gameState.Timer <= GameConstants.TimerWarningThreshold && gameState.Timer > 0f)
            {
                float t = 1f - (gameState.Timer / GameConstants.TimerWarningThreshold); // 0->1 as timer counts down
                vignette.color.Override(Color.red);
                vignette.intensity.Override(Mathf.Lerp(0f, GameConstants.TimerWarningMaxIntensity, t));
                vignette.active = true;
            }
            else
            {
                vignette.intensity.Override(0f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
