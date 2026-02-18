using ECS.Components;
using MonoBehaviours.Core;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Placeholder upgrade screen for Phase 6 tech tree.
    /// Shows total credits and a Start Run button.
    /// Wired by UISetup at runtime.
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI creditsText;
        private Button startRunButton;
        private GameObject root;

        /// <summary>
        /// Called by UISetup to wire UI references.
        /// </summary>
        public void Initialize(TextMeshProUGUI titleText, TextMeshProUGUI creditsText, Button startRunButton, GameObject root)
        {
            this.titleText = titleText;
            this.creditsText = creditsText;
            this.startRunButton = startRunButton;
            this.root = root;

            // Wire start run button
            if (this.startRunButton != null)
            {
                this.startRunButton.onClick.AddListener(OnStartRunClicked);
            }

            // Start hidden
            if (this.root != null)
            {
                this.root.SetActive(false);
            }
        }

        /// <summary>
        /// Show the upgrade screen with current total credits.
        /// </summary>
        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            // Read current credits from ECS
            long currentCredits = 0;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                var query = em.CreateEntityQuery(typeof(GameStateData));
                if (query.CalculateEntityCount() > 0)
                {
                    var gameState = query.GetSingleton<GameStateData>();
                    currentCredits = gameState.Credits;
                }
            }

            if (titleText != null)
            {
                titleText.text = "Upgrades";
            }

            if (creditsText != null)
            {
                creditsText.text = NumberFormatter.Format((double)currentCredits) + " credits";
            }
        }

        /// <summary>
        /// Hide the upgrade screen.
        /// </summary>
        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnStartRunClicked()
        {
            GameManager.Instance.ResetRun();
            GameManager.Instance.TransitionTo(GamePhase.Playing);
        }
    }
}
