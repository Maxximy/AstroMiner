using System;
using System.Collections;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.4f;

    private Coroutine _activeCoroutine;

    /// <summary>
    /// Fade to black (alpha 0 -> 1). Blocks raycasts immediately.
    /// Calls onComplete when fully black.
    /// </summary>
    public void FadeOut(Action onComplete = null)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _fadeCanvasGroup.blocksRaycasts = true;
        _activeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
    }

    /// <summary>
    /// Fade from black (alpha 1 -> 0). Disables blocksRaycasts when clear.
    /// Calls onComplete when fully clear.
    /// </summary>
    public void FadeIn(Action onComplete = null)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, () =>
        {
            _fadeCanvasGroup.blocksRaycasts = false;
            onComplete?.Invoke();
        }));
    }

    /// <summary>
    /// Immediately set to fully black.
    /// </summary>
    public void SetBlack()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        _fadeCanvasGroup.alpha = 1f;
        _fadeCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Immediately set to fully clear (invisible).
    /// </summary>
    public void SetClear()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeCoroutine(float from, float to, Action onComplete)
    {
        float elapsed = 0f;
        _fadeCanvasGroup.alpha = from;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            // Smooth ease-in-out using SmoothStep
            _fadeCanvasGroup.alpha = Mathf.SmoothStep(from, to, t);
            yield return null;
        }

        _fadeCanvasGroup.alpha = to;
        _activeCoroutine = null;
        onComplete?.Invoke();
    }
}
