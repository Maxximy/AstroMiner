using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using TMPro;

/// <summary>
/// Displays run results showing credits earned this run.
/// Shown during GameOver phase, hidden otherwise.
/// Continue button transitions to Upgrading phase.
/// Wired by UISetup at runtime.
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _creditsEarnedText;
    private Button _continueButton;
    private GameObject _root;

    /// <summary>
    /// Called by UISetup to wire UI references.
    /// </summary>
    public void Initialize(TextMeshProUGUI titleText, TextMeshProUGUI creditsEarnedText, Button continueButton, GameObject root)
    {
        _titleText = titleText;
        _creditsEarnedText = creditsEarnedText;
        _continueButton = continueButton;
        _root = root;

        // Wire continue button
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        // Start hidden
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    /// <summary>
    /// Show the results screen with credits earned this run.
    /// Reads current credits from ECS and computes delta from run start.
    /// </summary>
    public void Show()
    {
        if (_root != null)
        {
            _root.SetActive(true);
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

        if (_titleText != null)
        {
            _titleText.text = "Run Complete!";
        }

        if (_creditsEarnedText != null)
        {
            _creditsEarnedText.text = NumberFormatter.Format((double)creditsThisRun) + " credits earned";
        }
    }

    /// <summary>
    /// Hide the results screen.
    /// </summary>
    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void OnContinueClicked()
    {
        Debug.Log("continue");
        GameManager.Instance.TransitionTo(GamePhase.Upgrading);
    }
}
