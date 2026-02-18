using System.Collections;
using UnityEngine;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Singleton that manages pooled visual effects for all four skills:
    /// Laser Burst beam, Chain Lightning arcs, EMP Pulse blast, and Overcharge glow.
    /// Called by FeedbackEventBridge when SkillEvents are drained from ECS buffers.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public class SkillVFXManager : MonoBehaviour
    {
        public static SkillVFXManager Instance { get; private set; }

        // Laser beam
        private GameObject laserBeamGO;
        private LineRenderer laserLine;

        // Chain lightning
        private GameObject chainLightningGO;
        private LineRenderer chainLine;

        // EMP blast
        private GameObject empBlastGO;
        private ParticleSystem empParticles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("SkillVFXManager");
                Instance = go.AddComponent<SkillVFXManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateLaserBeam();
            CreateChainLightning();
            CreateEmpBlast();

            Debug.Log("SkillVFXManager: initialized laser beam, chain lightning, and EMP blast VFX.");
        }

        private void CreateLaserBeam()
        {
            laserBeamGO = new GameObject("LaserBeam");
            laserBeamGO.transform.SetParent(transform);

            laserLine = laserBeamGO.AddComponent<LineRenderer>();
            laserLine.positionCount = 2;
            laserLine.startWidth = 0.3f;
            laserLine.endWidth = 0.15f;
            laserLine.useWorldSpace = true;
            laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laserLine.receiveShadows = false;

            // HDR cyan material
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Color hdrCyan = new Color(0f, 1f, 1f) * 6f;
            mat.SetColor("_BaseColor", hdrCyan);
            laserLine.material = mat;

            laserBeamGO.SetActive(false);
        }

        private void CreateChainLightning()
        {
            chainLightningGO = new GameObject("ChainLightning");
            chainLightningGO.transform.SetParent(transform);

            chainLine = chainLightningGO.AddComponent<LineRenderer>();
            chainLine.positionCount = 6; // max: ship + up to 5 targets
            chainLine.startWidth = 0.15f;
            chainLine.endWidth = 0.1f;
            chainLine.useWorldSpace = true;
            chainLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            chainLine.receiveShadows = false;

            // HDR blue-white material
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Color hdrBlueWhite = new Color(0.5f, 0.7f, 1f) * 5f;
            mat.SetColor("_BaseColor", hdrBlueWhite);
            chainLine.material = mat;

            chainLightningGO.SetActive(false);
        }

        private void CreateEmpBlast()
        {
            empBlastGO = new GameObject("EMPBlast");
            empBlastGO.transform.SetParent(transform);

            empParticles = empBlastGO.AddComponent<ParticleSystem>();

            // Main module
            var main = empParticles.main;
            main.startLifetime = 0.4f;
            main.startSpeed = GameConstants.EmpPulseRadius * 2f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.7f, 0.4f, 1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 30;

            // Emission: burst of 30
            var emission = empParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 30)
            });

            // Shape: sphere
            var shape = empParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Size over lifetime: shrink
            var sizeOverLifetime = empParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // Color over lifetime: fade out
            var colorOverLifetime = empParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.7f, 0.4f, 1f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.3f, 1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Renderer: URP particle material
            var renderer = empBlastGO.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", new Color(0.7f, 0.4f, 1f));
                    renderer.material = mat;
                }
            }

            empParticles.Stop();
        }

        /// <summary>
        /// Plays a cyan laser beam from origin to target for 0.15 seconds.
        /// </summary>
        public void PlayLaserBurst(Vector3 origin, Vector3 target)
        {
            laserLine.SetPosition(0, origin);
            laserLine.SetPosition(1, target);
            laserBeamGO.SetActive(true);
            StartCoroutine(DisableAfter(laserBeamGO, 0.15f));
        }

        /// <summary>
        /// Plays a jagged chain lightning line from origin through multiple targets for 0.2 seconds.
        /// </summary>
        public void PlayChainLightning(Vector3 origin, Vector3[] targets, int count)
        {
            // Total points: origin + count targets
            int totalPoints = 1 + count;
            chainLine.positionCount = totalPoints;
            chainLine.SetPosition(0, origin);

            for (int i = 0; i < count; i++)
            {
                // Add slight random perpendicular offset for jagged look
                Vector3 pos = targets[i];
                if (i > 0 && i < count - 1)
                {
                    // Random offset perpendicular to line direction
                    pos.x += Random.Range(-0.15f, 0.15f);
                    pos.z += Random.Range(-0.15f, 0.15f);
                }
                chainLine.SetPosition(i + 1, pos);
            }

            chainLightningGO.SetActive(true);
            StartCoroutine(DisableAfter(chainLightningGO, 0.2f));
        }

        /// <summary>
        /// Plays an expanding purple-blue particle burst at the target position.
        /// </summary>
        public void PlayEmpPulse(Vector3 position)
        {
            empBlastGO.transform.position = position;
            empParticles.Play();
        }

        /// <summary>
        /// Overcharge activation visual -- handled by MiningCircleVisual reading OverchargeBuffData.
        /// </summary>
        public void PlayOverchargeActivation()
        {
            // No-op: Overcharge visual is handled by MiningCircleVisual reading OverchargeBuffData singleton.
        }

        private IEnumerator DisableAfter(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null)
                go.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
