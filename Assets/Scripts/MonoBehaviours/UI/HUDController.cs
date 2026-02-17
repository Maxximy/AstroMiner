using UnityEngine;
using Unity.Entities;
using TMPro;

/// <summary>
/// Displays running credit total and countdown timer during gameplay.
/// Shows HUD only during Playing and Collecting phases.
/// Wired by UISetup at runtime.
/// </summary>
public class HUDController : MonoBehaviour
{
    private TextMeshProUGUI _creditsText;
    private TextMeshProUGUI _timerText;
    private GameObject _hudRoot;

    private EntityManager _em;
    private EntityQuery _gameStateQuery;
    private bool _initialized;

    /// <summary>
    /// Called by UISetup to wire text references and root object.
    /// </summary>
    public void Initialize(TextMeshProUGUI creditsText, TextMeshProUGUI timerText, GameObject hudRoot)
    {
        _creditsText = creditsText;
        _timerText = timerText;
        _hudRoot = hudRoot;
    }

    void LateUpdate()
    {
        if (!_initialized)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _em = world.EntityManager;
            _gameStateQuery = _em.CreateEntityQuery(typeof(GameStateData));
            _initialized = true;
        }

        if (_gameStateQuery.CalculateEntityCount() == 0) return;

        var gameState = _gameStateQuery.GetSingleton<GameStateData>();

        // Show HUD only during Playing and Collecting phases
        bool showHUD = gameState.Phase == GamePhase.Playing || gameState.Phase == GamePhase.Collecting;
        if (_hudRoot != null && _hudRoot.activeSelf != showHUD)
        {
            _hudRoot.SetActive(showHUD);
        }

        if (!showHUD) return;

        // Credits display with K/M/B/T suffix formatting
        if (_creditsText != null)
        {
            _creditsText.text = NumberFormatter.Format((double)gameState.Credits);
        }

        // Timer display as MM:SS
        if (_timerText != null)
        {
            float timer = Mathf.Max(0f, gameState.Timer);
            int minutes = (int)(timer / 60f);
            int seconds = (int)(timer % 60f);
            _timerText.text = $"{minutes}:{seconds:D2}";
        }
    }
}
