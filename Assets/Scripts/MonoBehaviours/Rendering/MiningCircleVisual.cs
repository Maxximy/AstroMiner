using System.Collections;
using ECS.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// MonoBehaviour that renders a glowing cyan ring at the mouse world position.
    /// Uses a LineRenderer with HDR emissive material for bloom effect.
    /// Reads InputData and MiningConfigData from ECS singletons each frame.
    /// </summary>
    public class MiningCircleVisual : MonoBehaviour
    {
        [SerializeField] private int Segments = 64;
        [SerializeField] private float LineWidth = 0.08f;
        [SerializeField] private float HDRIntensity = 4f;

        private LineRenderer lineRenderer;
        private Material circleMaterial;
        private EntityManager em;
        private Entity inputEntity;
        private Entity miningConfigEntity;
        private bool initialized;

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

            em = world.EntityManager;

            // Get singleton entities
            inputEntity = em.CreateEntityQuery(typeof(InputData)).GetSingletonEntity();
            miningConfigEntity = em.CreateEntityQuery(typeof(MiningConfigData)).GetSingletonEntity();

            // Add LineRenderer component
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = Segments;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            // Generate unit circle points on XZ plane (Y=0 relative)
            // Parent transform's scale controls actual radius
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                lineRenderer.SetPosition(i, new Vector3(x, 0f, z));
            }

            // Create HDR emissive material for bloom glow
            circleMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Color hdrCyan = new Color(0f, 1f, 1f) * HDRIntensity;
            circleMaterial.SetColor("_BaseColor", hdrCyan);
            lineRenderer.material = circleMaterial;

            // Read initial radius and set scale
            var config = em.GetComponentData<MiningConfigData>(miningConfigEntity);
            transform.localScale = Vector3.one * config.Radius;

            initialized = true;
            Debug.Log($"MiningCircleVisual initialized: radius={config.Radius}, segments={Segments}, HDR intensity={HDRIntensity}");
        }

        void Update()
        {
            if (!initialized)
                return;

            // Read mouse position from ECS
            var input = em.GetComponentData<InputData>(inputEntity);

            if (input.MouseValid)
            {
                // Position circle at mouse world pos on XZ plane
                // InputData.MouseWorldPos is float2(x, z), so .x -> world X, .y -> world Z
                // Slight Y offset (0.05f) to avoid z-fighting with ground plane
                transform.position = new Vector3(input.MouseWorldPos.x, 0.05f, input.MouseWorldPos.y);

                // Update radius from config (in case it changes at runtime)
                var config = em.GetComponentData<MiningConfigData>(miningConfigEntity);
                transform.localScale = Vector3.one * config.Radius;

                // Ensure visible
                if (!lineRenderer.enabled)
                    lineRenderer.enabled = true;
            }
            else
            {
                // Hide circle when mouse is not valid
                lineRenderer.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (circleMaterial != null)
                Destroy(circleMaterial);
        }
    }
}
