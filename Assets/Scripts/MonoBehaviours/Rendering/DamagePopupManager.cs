using System.Collections.Generic;
using ECS.Components;
using MonoBehaviours.Pool;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace MonoBehaviours.Rendering
{
    /// <summary>
    /// Singleton MonoBehaviour that spawns pooled world-space TMPro damage popups.
    /// Called by FeedbackEventBridge when DamageEvents are drained from ECS buffers.
    /// Creates floating damage numbers that rise upward, fade out, and billboard toward the camera.
    /// Self-instantiates via [RuntimeInitializeOnLoadMethod] -- no manual scene setup required.
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        /// <summary>Tracks an active popup's state for animation.</summary>
        private struct ActivePopup
        {
            public GameObject GO;
            public TextMeshProUGUI Text;
            public CanvasGroup Group;
            public float Elapsed;
        }

        private GameObjectPool _popupPool;
        private GameObject _popupPrefab;
        private readonly List<ActivePopup> _activePopups = new List<ActivePopup>(128);

        // Note: ECS event draining is handled centrally by FeedbackEventBridge.
        // DamagePopupManager only exposes the public Spawn() method for dispatching.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("DamagePopupManager");
                Instance = go.AddComponent<DamagePopupManager>();
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
            // Create popup prefab programmatically
            _popupPrefab = CreatePopupPrefab();
            _popupPrefab.SetActive(false);
            _popupPrefab.transform.SetParent(transform);

            // Create pool: pre-warm 100, max 300
            var poolParent = new GameObject("PopupPool").transform;
            poolParent.SetParent(transform);
            _popupPool = new GameObjectPool(_popupPrefab, poolParent, 100, 300);

            Debug.Log("DamagePopupManager: initialized with pool of 100 popups.");
        }

        private GameObject CreatePopupPrefab()
        {
            // Root GameObject
            var root = new GameObject("DamagePopup");

            // World-space Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            // Canvas sizing: 2x1 world units
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1f);
            rect.localScale = Vector3.one * 0.02f; // Scale down to world units

            // CanvasGroup for alpha fading
            root.AddComponent<CanvasGroup>();

            // TMPro text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(root.transform, false);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = GameConstants.DamagePopupFontSizeNormal / 0.02f; // Compensate for canvas scale
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            // Size the text RectTransform to fill the canvas
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return root;
        }

        /// <summary>
        /// Spawns a floating damage number at the given world position.
        /// </summary>
        public void Spawn(float3 position, float amount, DamageType type, byte colorR, byte colorG, byte colorB)
        {
            if (_popupPool == null) return;

            var go = _popupPool.Get();

            // Position slightly above the XZ plane
            go.transform.position = new Vector3(position.x, 0.5f, position.z);

            // Get references
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            var group = go.GetComponent<CanvasGroup>();

            if (tmp == null || group == null)
            {
                _popupPool.Release(go);
                return;
            }

            // Set text based on damage type
            string text = Mathf.RoundToInt(amount).ToString();
            if (type == DamageType.Critical)
                text = "CRIT!\n" + text;

            tmp.text = text;

            // Set color by DamageType
            switch (type)
            {
                case DamageType.Normal:
                    tmp.color = Color.white;
                    break;
                case DamageType.Critical:
                    tmp.color = new Color(1f, 0.9f, 0.2f);
                    break;
                case DamageType.DoT:
                    tmp.color = new Color(1f, 0.6f, 0.1f);
                    break;
                case DamageType.Skill:
                    tmp.color = new Color(colorR / 255f, colorG / 255f, colorB / 255f);
                    break;
            }

            // Set font style
            tmp.fontStyle = type == DamageType.DoT ? FontStyles.Italic : FontStyles.Bold;

            // Set scale (critical gets boost)
            go.transform.localScale = type == DamageType.Critical
                ? Vector3.one * 0.02f * GameConstants.DamagePopupCritScale
                : Vector3.one * 0.02f;

            // Reset alpha
            group.alpha = 1f;

            // Billboard: face camera immediately
            if (Camera.main != null)
                go.transform.rotation = Camera.main.transform.rotation;

            // Add to active list
            _activePopups.Add(new ActivePopup
            {
                GO = go,
                Text = tmp,
                Group = group,
                Elapsed = 0f
            });
        }

        private void Update()
        {
            // Animate active popups
            float dt = Time.deltaTime;
            var cam = Camera.main;

            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.Elapsed += dt;

                // Rise upward
                var pos = popup.GO.transform.position;
                pos.y += GameConstants.DamagePopupRiseSpeed * dt;
                popup.GO.transform.position = pos;

                // Compute normalized time
                float t = popup.Elapsed / GameConstants.DamagePopupDuration;

                // Fade after delay
                if (popup.Elapsed > GameConstants.DamagePopupFadeDelay)
                {
                    float fadeT = (popup.Elapsed - GameConstants.DamagePopupFadeDelay) /
                                  (GameConstants.DamagePopupDuration - GameConstants.DamagePopupFadeDelay);
                    popup.Group.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeT));
                }

                // Billboard: face camera
                if (cam != null)
                    popup.GO.transform.rotation = cam.transform.rotation;

                // Write back modified struct
                _activePopups[i] = popup;

                // Remove when done
                if (t >= 1f)
                {
                    _popupPool.Release(popup.GO);
                    _activePopups.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}