using System;
using System.Collections;
using UnityEngine;

namespace MonoBehaviours.UI
{
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup FadeCanvasGroup;
        [SerializeField] private float FadeDuration = 0.4f;

        private Coroutine activeCoroutine;

        /// <summary>
        /// Fade to black (alpha 0 -> 1). Blocks raycasts immediately.
        /// Calls onComplete when fully black.
        /// </summary>
        public void FadeOut(Action onComplete = null)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            FadeCanvasGroup.blocksRaycasts = true;
            activeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
        }

        /// <summary>
        /// Fade from black (alpha 1 -> 0). Disables blocksRaycasts when clear.
        /// Calls onComplete when fully clear.
        /// </summary>
        public void FadeIn(Action onComplete = null)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            activeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, () =>
            {
                FadeCanvasGroup.blocksRaycasts = false;
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Immediately set to fully black.
        /// </summary>
        public void SetBlack()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            FadeCanvasGroup.alpha = 1f;
            FadeCanvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Immediately set to fully clear (invisible).
        /// </summary>
        public void SetClear()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            FadeCanvasGroup.alpha = 0f;
            FadeCanvasGroup.blocksRaycasts = false;
        }

        private IEnumerator FadeCoroutine(float from, float to, Action onComplete)
        {
            float elapsed = 0f;
            FadeCanvasGroup.alpha = from;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeDuration);
                // Smooth ease-in-out using SmoothStep
                FadeCanvasGroup.alpha = Mathf.SmoothStep(from, to, t);
                yield return null;
            }

            FadeCanvasGroup.alpha = to;
            activeCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
