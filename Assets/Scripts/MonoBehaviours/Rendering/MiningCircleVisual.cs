using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;

/// <summary>
/// MonoBehaviour that renders a glowing cyan ring at the mouse world position.
/// Uses a LineRenderer with HDR emissive material for bloom effect.
/// Reads InputData and MiningConfigData from ECS singletons each frame.
/// </summary>
public class MiningCircleVisual : MonoBehaviour
{
    [SerializeField] private int _segments = 64;
    [SerializeField] private float _lineWidth = 0.08f;
    [SerializeField] private float _hdrIntensity = 4f;

    private LineRenderer _lineRenderer;
    private Material _circleMaterial;
    private EntityManager _em;
    private Entity _inputEntity;
    private Entity _miningConfigEntity;
    private bool _initialized;

    void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // Wait one frame for ECS singletons to be created by ECSBootstrap
        yield return null;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("MiningCircleVisual: ECS World not available.");
            yield break;
        }

        _em = world.EntityManager;

        // Get singleton entities
        _inputEntity = _em.CreateEntityQuery(typeof(InputData)).GetSingletonEntity();
        _miningConfigEntity = _em.CreateEntityQuery(typeof(MiningConfigData)).GetSingletonEntity();

        // Add LineRenderer component
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = _segments;
        _lineRenderer.loop = true;
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        // Generate unit circle points on XZ plane (Y=0 relative)
        // Parent transform's scale controls actual radius
        for (int i = 0; i < _segments; i++)
        {
            float angle = (float)i / _segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            _lineRenderer.SetPosition(i, new Vector3(x, 0f, z));
        }

        // Create HDR emissive material for bloom glow
        _circleMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color hdrCyan = new Color(0f, 1f, 1f) * _hdrIntensity;
        _circleMaterial.SetColor("_BaseColor", hdrCyan);
        _lineRenderer.material = _circleMaterial;

        // Read initial radius and set scale
        var config = _em.GetComponentData<MiningConfigData>(_miningConfigEntity);
        transform.localScale = Vector3.one * config.Radius;

        _initialized = true;
        Debug.Log($"MiningCircleVisual initialized: radius={config.Radius}, segments={_segments}, HDR intensity={_hdrIntensity}");
    }

    void Update()
    {
        if (!_initialized)
            return;

        // Read mouse position from ECS
        var input = _em.GetComponentData<InputData>(_inputEntity);

        if (input.MouseValid)
        {
            // Position circle at mouse world pos on XZ plane
            // InputData.MouseWorldPos is float2(x, z), so .x -> world X, .y -> world Z
            // Slight Y offset (0.05f) to avoid z-fighting with ground plane
            transform.position = new Vector3(input.MouseWorldPos.x, 0.05f, input.MouseWorldPos.y);

            // Update radius from config (in case it changes at runtime)
            var config = _em.GetComponentData<MiningConfigData>(_miningConfigEntity);
            transform.localScale = Vector3.one * config.Radius;

            // Ensure visible
            if (!_lineRenderer.enabled)
                _lineRenderer.enabled = true;
        }
        else
        {
            // Hide circle when mouse is not valid
            _lineRenderer.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (_circleMaterial != null)
            Destroy(_circleMaterial);
    }
}
