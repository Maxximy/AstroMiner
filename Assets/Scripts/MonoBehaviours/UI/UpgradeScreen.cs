using ECS.Components;
using MonoBehaviours.Core;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Upgrade screen controller for the tech tree view between runs.
    /// Shows the tech tree graph with pan/zoom navigation and a Start Run button.
    /// Wired by UISetup at runtime.
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI creditsText;
        private Button startRunButton;
        private GameObject root;

        /// <summary>
        /// Reference to the tech tree controller for refreshing state on show.
        /// </summary>
        public TechTreeController TechTreeController { get; set; }

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
        /// Refreshes tech tree node states to reflect latest credit balance.
        /// </summary>
        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            // Refresh tech tree node states with latest credits
            if (TechTreeController != null)
            {
                TechTreeController.RefreshAllNodeStates();
            }

            // Read current credits from ECS for header display
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
                titleText.text = "Tech Tree";
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
