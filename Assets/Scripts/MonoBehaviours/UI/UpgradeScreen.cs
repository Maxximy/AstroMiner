using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using TMPro;

/// <summary>
/// Placeholder upgrade screen for Phase 6 tech tree.
/// Shows total credits and a Start Run button.
/// Wired by UISetup at runtime.
/// </summary>
public class UpgradeScreen : MonoBehaviour
{
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _creditsText;
    private Button _startRunButton;
    private GameObject _root;

    /// <summary>
    /// Called by UISetup to wire UI references.
    /// </summary>
    public void Initialize(TextMeshProUGUI titleText, TextMeshProUGUI creditsText, Button startRunButton, GameObject root)
    {
        _titleText = titleText;
        _creditsText = creditsText;
        _startRunButton = startRunButton;
        _root = root;

        // Wire start run button
        if (_startRunButton != null)
        {
            _startRunButton.onClick.AddListener(OnStartRunClicked);
        }

        // Start hidden
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    /// <summary>
    /// Show the upgrade screen with current total credits.
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

        if (_titleText != null)
        {
            _titleText.text = "Upgrades";
        }

        if (_creditsText != null)
        {
            _creditsText.text = NumberFormatter.Format((double)currentCredits) + " credits";
        }
    }

    /// <summary>
    /// Hide the upgrade screen.
    /// </summary>
    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void OnStartRunClicked()
    {
        GameManager.Instance.ResetRun();
        GameManager.Instance.TransitionTo(GamePhase.Playing);
    }
}
