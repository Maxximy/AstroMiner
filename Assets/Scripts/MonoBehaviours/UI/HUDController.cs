using ECS.Components;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Displays running credit total and countdown timer during gameplay.
    /// Shows HUD only during Playing and Collecting phases.
    /// Wired by UISetup at runtime.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private TextMeshProUGUI creditsText;
        private TextMeshProUGUI timerText;
        private GameObject hudRoot;

        private EntityManager em;
        private EntityQuery gameStateQuery;
        private bool initialized;

        // Credit counter pop animation state
        private long previousCredits;
        private float popTimer;
        private Vector3 creditsOriginalScale = Vector3.one;
        private Color creditsOriginalColor = Color.white;

        /// <summary>
        /// Called by UISetup to wire text references and root object.
        /// </summary>
        public void Initialize(TextMeshProUGUI creditsText, TextMeshProUGUI timerText, GameObject hudRoot)
        {
            this.creditsText = creditsText;
            this.timerText = timerText;
            this.hudRoot = hudRoot;
        }

        void LateUpdate()
        {
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

            // Show HUD only during Playing and Collecting phases
            bool showHUD = gameState.Phase == GamePhase.Playing || gameState.Phase == GamePhase.Collecting;
            if (hudRoot != null && hudRoot.activeSelf != showHUD)
            {
                hudRoot.SetActive(showHUD);
            }

            if (!showHUD) return;

            // Credits display with K/M/B/T suffix formatting
            if (creditsText != null)
            {
                creditsText.text = NumberFormatter.Format((double)gameState.Credits);

                // Detect credit change and trigger pop animation
                if (gameState.Credits != previousCredits && previousCredits != 0)
                {
                    popTimer = GameConstants.CreditPopDuration;
                }
                previousCredits = gameState.Credits;

                // Animate credit counter pop (scale up + gold flash)
                if (popTimer > 0)
                {
                    popTimer -= Time.deltaTime;
                    float t = Mathf.Clamp01(popTimer / GameConstants.CreditPopDuration);
                    float scale = 1f + (GameConstants.CreditPopScale - 1f) * Mathf.Sin(t * Mathf.PI);
                    creditsText.transform.localScale = creditsOriginalScale * scale;
                    creditsText.color = Color.Lerp(creditsOriginalColor, new Color(1f, 0.9f, 0.3f), t);
                }
                else
                {
                    creditsText.transform.localScale = creditsOriginalScale;
                    creditsText.color = creditsOriginalColor;
                }
            }

            // Timer display as MM:SS
            if (timerText != null)
            {
                float timer = Mathf.Max(0f, gameState.Timer);
                int minutes = (int)(timer / 60f);
                int seconds = (int)(timer % 60f);
                timerText.text = $"{minutes}:{seconds:D2}";
            }
        }
    }
}
