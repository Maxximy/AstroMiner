using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Individual node UI element in the tech tree graph.
    /// Handles visual state (color), cost display, click handling, and purchase animation.
    /// Created and managed by TechTreeController.
    /// </summary>
    public class TechTreeNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum NodeState
        {
            Hidden,
            Locked,
            Available,
            TooExpensive,
            Purchased
        }

        // Colors matching locked decisions in 06-CONTEXT.md
        private static readonly Color PurchasedColor = new Color(0.204f, 0.596f, 0.859f, 1f); // #3498DB blue
        private static readonly Color AvailableColor = new Color(0.180f, 0.800f, 0.443f, 1f); // #2ECC71 green
        private static readonly Color TooExpensiveColor = new Color(0.906f, 0.298f, 0.235f, 1f); // #E74C3C red

        private Image backgroundImage;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI nameText;
        private Button button;
        private RectTransform rectTransform;

        private int nodeIndex;
        private NodeState currentState = NodeState.Hidden;

        /// <summary>The node index in the TechTreeController's node array.</summary>
        public int NodeIndex => nodeIndex;

        /// <summary>The current visual state of this node.</summary>
        public NodeState CurrentNodeState => currentState;

        /// <summary>Reference to the parent controller for tooltip callbacks.</summary>
        public TechTreeController Controller { get; set; }

        /// <summary>
        /// Creates the node's visual hierarchy and wires click handling.
        /// </summary>
        public void Initialize(int index, string displayName, int actualCost, Vector2 position,
            RectTransform parent, System.Action<int> onClicked)
        {
            nodeIndex = index;
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();

            transform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(60, 60);

            // Background image (circle-like colored square)
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = AvailableColor;
            backgroundImage.raycastTarget = true;

            // Button component
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = backgroundImage;
            int capturedIndex = index;
            button.onClick.AddListener(() => onClicked?.Invoke(capturedIndex));

            // Name text above node
            var nameGO = new GameObject("NodeName");
            nameGO.transform.SetParent(transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0, 4);
            nameRect.sizeDelta = new Vector2(120, 20);
            nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = displayName;
            nameText.fontSize = 10;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableWordWrapping = false;
            nameText.raycastTarget = false;

            // Cost text below node
            var costGO = new GameObject("NodeCost");
            costGO.transform.SetParent(transform, false);
            var costRect = costGO.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0.5f, 0f);
            costRect.anchorMax = new Vector2(0.5f, 0f);
            costRect.pivot = new Vector2(0.5f, 1f);
            costRect.anchoredPosition = new Vector2(0, -4);
            costRect.sizeDelta = new Vector2(120, 18);
            costText = costGO.AddComponent<TextMeshProUGUI>();
            costText.text = NumberFormatter.Format(actualCost);
            costText.fontSize = 11;
            costText.color = new Color(0.9f, 0.85f, 0.4f, 1f); // Gold
            costText.alignment = TextAlignmentOptions.Center;
            costText.enableWordWrapping = false;
            costText.raycastTarget = false;

            // Start hidden
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the node's visual appearance based on its current state.
        /// </summary>
        public void UpdateState(NodeState state, long playerCredits)
        {
            currentState = state;

            switch (state)
            {
                case NodeState.Hidden:
                case NodeState.Locked:
                    gameObject.SetActive(false);
                    return;

                case NodeState.Purchased:
                    gameObject.SetActive(true);
                    backgroundImage.color = PurchasedColor;
                    if (costText != null) costText.gameObject.SetActive(false);
                    if (button != null) button.interactable = false;
                    break;

                case NodeState.Available:
                    gameObject.SetActive(true);
                    backgroundImage.color = AvailableColor;
                    if (costText != null) costText.gameObject.SetActive(true);
                    if (button != null) button.interactable = true;
                    break;

                case NodeState.TooExpensive:
                    gameObject.SetActive(true);
                    backgroundImage.color = TooExpensiveColor;
                    if (costText != null) costText.gameObject.SetActive(true);
                    if (button != null) button.interactable = true;
                    break;
            }
        }

        /// <summary>
        /// Brief scale punch + color lerp from green to blue on purchase.
        /// </summary>
        public void PlayPurchaseEffect()
        {
            StartCoroutine(PurchaseAnimationCoroutine());
        }

        private IEnumerator PurchaseAnimationCoroutine()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 originalScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale punch: 1.0 -> 1.2 -> 1.0
                float scaleFactor = t < 0.5f
                    ? Mathf.Lerp(1f, 1.2f, t * 2f)
                    : Mathf.Lerp(1.2f, 1f, (t - 0.5f) * 2f);
                transform.localScale = originalScale * scaleFactor;

                // Color lerp: green -> blue
                backgroundImage.color = Color.Lerp(AvailableColor, PurchasedColor, t);

                yield return null;
            }

            transform.localScale = originalScale;
            backgroundImage.color = PurchasedColor;
        }

        // -- IPointerEnterHandler / IPointerExitHandler for tooltip --

        public void OnPointerEnter(PointerEventData eventData)
        {
            Controller?.ShowTooltip(nodeIndex, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Controller?.HideTooltip();
        }
    }
}
