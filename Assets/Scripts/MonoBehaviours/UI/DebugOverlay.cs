using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private TextMeshProUGUI _entityCountText;

    private float _fpsTimer;
    private int _frameCount;
    private EntityManager _em;
    private bool _ecsReady;
    private EntityQuery _gameStateQuery;
    private EntityQuery _entityCountQuery;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            _em = world.EntityManager;
            _gameStateQuery = _em.CreateEntityQuery(typeof(GameStateData));
            _entityCountQuery = _em.CreateEntityQuery(typeof(LocalTransform));
            _ecsReady = true;
        }
    }

    void LateUpdate()
    {
        UpdateFPS();

        if (!_ecsReady) return;

        UpdateState();
        UpdateEntityCount();
    }

    private void UpdateFPS()
    {
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= 0.5f)
        {
            float fps = _frameCount / _fpsTimer;
            _fpsText.SetText("FPS: {0:0}", fps);
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    private void UpdateState()
    {
        if (_gameStateQuery.CalculateEntityCount() > 0)
        {
            var gameState = _gameStateQuery.GetSingleton<GameStateData>();
            _stateText.SetText(gameState.Phase.ToString());
        }
    }

    private void UpdateEntityCount()
    {
        int count = _entityCountQuery.CalculateEntityCount();
        _entityCountText.SetText("Entities: {0}", count);
    }
}
