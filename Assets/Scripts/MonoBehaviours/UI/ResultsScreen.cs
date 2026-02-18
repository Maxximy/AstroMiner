using ECS.Components;
using MonoBehaviours.Core;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Displays run results showing credits earned this run.
    /// Shown during GameOver phase, hidden otherwise.
    /// Continue button transitions to Upgrading phase.
    /// Wired by UISetup at runtime.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI creditsEarnedText;
        private Button continueButton;
        private GameObject root;

        /// <summary>
        /// Called by UISetup to wire UI references.
        /// </summary>
        public void Initialize(TextMeshProUGUI titleText, TextMeshProUGUI creditsEarnedText, Button continueButton, GameObject root)
        {
            this.titleText = titleText;
            this.creditsEarnedText = creditsEarnedText;
            this.continueButton = continueButton;
            this.root = root;

            // Wire continue button
            if (this.continueButton != null)
            {
                this.continueButton.onClick.AddListener(OnContinueClicked);
            }

            // Start hidden
            if (this.root != null)
            {
                this.root.SetActive(false);
            }
        }

        /// <summary>
        /// Show the results screen with credits earned this run.
        /// Reads current credits from ECS and computes delta from run start.
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

            long creditsThisRun = currentCredits - GameManager.Instance.CreditsAtRunStart;

            if (titleText != null)
            {
                titleText.text = "Run Complete!";
            }

            if (creditsEarnedText != null)
            {
                creditsEarnedText.text = NumberFormatter.Format((double)creditsThisRun) + " credits earned";
            }
        }

        /// <summary>
        /// Hide the results screen.
        /// </summary>
        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnContinueClicked()
        {
            Debug.Log("continue");
            GameManager.Instance.TransitionTo(GamePhase.Upgrading);
        }
    }
}
