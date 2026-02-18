using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that applies brief screen shake on critical hit events.
/// Attaches itself to the Main Camera. Uses random XZ offset for 2-3 frames (~0.05s).
/// Self-instantiates via [RuntimeInitializeOnLoadMethod] on the camera GameObject.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 _originalLocalPos;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private bool _shaking;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var cam = Camera.main;
        if (cam != null)
            Instance = cam.gameObject.AddComponent<CameraShake>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        _originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Triggers a brief screen shake. Restarts timer if already shaking but does not
    /// increase magnitude (prevents stacking).
    /// </summary>
    /// <param name="duration">Shake duration in seconds (default ~3 frames at 60fps).</param>
    /// <param name="magnitude">Offset magnitude in world units.</param>
    public void Shake(float duration = 0f, float magnitude = 0f)
    {
        // Use constants as defaults (can't use const in default param)
        if (duration <= 0f) duration = GameConstants.ScreenShakeDuration;
        if (magnitude <= 0f) magnitude = GameConstants.ScreenShakeMagnitude;

        _shakeDuration = duration;
        if (!_shaking)
            _shakeMagnitude = magnitude;
        _shaking = true;
    }

    private void LateUpdate()
    {
        if (_shaking && _shakeDuration > 0f)
        {
            // Random offset in XZ plane (camera looks down from Y=18)
            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * _shakeMagnitude,
                0f,
                Random.Range(-1f, 1f) * _shakeMagnitude
            );
            transform.localPosition = _originalLocalPos + offset;
            _shakeDuration -= Time.deltaTime;
        }
        else if (_shaking)
        {
            transform.localPosition = _originalLocalPos;
            _shaking = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
