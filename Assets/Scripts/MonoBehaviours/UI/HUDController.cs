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

    // Credit counter pop animation state
    private long _previousCredits;
    private float _popTimer;
    private Vector3 _creditsOriginalScale = Vector3.one;
    private Color _creditsOriginalColor = Color.white;

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

            // Detect credit change and trigger pop animation
            if (gameState.Credits != _previousCredits && _previousCredits != 0)
            {
                _popTimer = GameConstants.CreditPopDuration;
            }
            _previousCredits = gameState.Credits;

            // Animate credit counter pop (scale up + gold flash)
            if (_popTimer > 0)
            {
                _popTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_popTimer / GameConstants.CreditPopDuration);
                float scale = 1f + (GameConstants.CreditPopScale - 1f) * Mathf.Sin(t * Mathf.PI);
                _creditsText.transform.localScale = _creditsOriginalScale * scale;
                _creditsText.color = Color.Lerp(_creditsOriginalColor, new Color(1f, 0.9f, 0.3f), t);
            }
            else
            {
                _creditsText.transform.localScale = _creditsOriginalScale;
                _creditsText.color = _creditsOriginalColor;
            }
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
