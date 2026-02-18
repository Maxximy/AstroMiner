using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Hover tooltip for tech tree nodes showing name, cost, effect description,
    /// and current vs next stat preview. Created by UISetup and managed by TechTreeController.
    /// </summary>
    public class TechTreeTooltip : MonoBehaviour
    {
        private GameObject tooltipRoot;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI descriptionText;
        private TextMeshProUGUI statPreviewText;
        private RectTransform tooltipRect;
        private RectTransform canvasRect;

        /// <summary>Whether the tooltip is currently visible.</summary>
        public bool IsVisible => tooltipRoot != null && tooltipRoot.activeSelf;

        /// <summary>
        /// Creates the tooltip UI hierarchy.
        /// </summary>
        public void Initialize(RectTransform parent)
        {
            // Find the canvas RectTransform for screen bounds clamping
            canvasRect = parent.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

            // Root panel
            var rootGO = new GameObject("TooltipRoot");
            rootGO.transform.SetParent(parent, false);
            tooltipRect = rootGO.AddComponent<RectTransform>();
            tooltipRect.anchorMin = Vector2.zero;
            tooltipRect.anchorMax = Vector2.zero;
            tooltipRect.pivot = new Vector2(0, 1); // Top-left pivot
            tooltipRect.sizeDelta = new Vector2(260, 200);

            // Dark semi-transparent background
            var bgImage = rootGO.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            bgImage.raycastTarget = false;

            // Vertical layout
            var layout = rootGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Content size fitter for auto-height
            var fitter = rootGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Name text (bold, 16pt, white)
            nameText = CreateTooltipText("TooltipName", rootGO.transform, 16, Color.white);

            // Cost text (14pt, gold)
            costText = CreateTooltipText("TooltipCost", rootGO.transform, 14,
                new Color(0.9f, 0.85f, 0.4f, 1f));

            // Separator line
            var separatorGO = new GameObject("Separator");
            separatorGO.transform.SetParent(rootGO.transform, false);
            var sepRect = separatorGO.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(0, 1);
            var sepLayout = separatorGO.AddComponent<LayoutElement>();
            sepLayout.preferredHeight = 1;
            sepLayout.flexibleWidth = 1;
            var sepImage = separatorGO.AddComponent<Image>();
            sepImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            sepImage.raycastTarget = false;

            // Description text (12pt, light gray, word wrap)
            descriptionText = CreateTooltipText("TooltipDesc", rootGO.transform, 12,
                new Color(0.75f, 0.75f, 0.75f, 1f));
            descriptionText.enableWordWrapping = true;

            // Stat preview text (12pt, green)
            statPreviewText = CreateTooltipText("TooltipStats", rootGO.transform, 12,
                new Color(0.3f, 0.9f, 0.3f, 1f));
            statPreviewText.enableWordWrapping = true;

            tooltipRoot = rootGO;
            tooltipRoot.SetActive(false);
        }

        /// <summary>
        /// Show the tooltip for a given node at a screen position.
        /// </summary>
        public void Show(UpgradeNodeSO node, bool isPurchased, Vector2 screenPos, TechTreeController controller)
        {
            if (tooltipRoot == null || node == null) return;

            nameText.text = node.DisplayName;

            if (isPurchased)
            {
                costText.text = "PURCHASED";
                costText.color = new Color(0.204f, 0.596f, 0.859f, 1f); // Blue
                statPreviewText.text = "";
            }
            else
            {
                costText.text = NumberFormatter.Format(node.ActualCost) + " credits";
                costText.color = new Color(0.9f, 0.85f, 0.4f, 1f); // Gold

                // Build stat preview
                if (node.Effects != null && node.Effects.Length > 0 && controller != null)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var effect in node.Effects)
                    {
                        float current = controller.GetCurrentStatValue(effect.Target);
                        float next = ComputeNextValue(effect, current);
                        string statName = GetStatDisplayName(effect.Target);
                        sb.AppendLine($"{statName}: {FormatStatValue(effect.Target, current)} -> {FormatStatValue(effect.Target, next)}");
                    }
                    statPreviewText.text = sb.ToString().TrimEnd();
                }
                else
                {
                    statPreviewText.text = "";
                }
            }

            descriptionText.text = string.IsNullOrEmpty(node.Description) ? "" : node.Description;

            tooltipRoot.SetActive(true);
            UpdatePosition(screenPos);
        }

        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        public void Hide()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        /// <summary>
        /// Update tooltip position to follow the mouse, clamped to screen bounds.
        /// </summary>
        public void UpdatePosition(Vector2 screenPos)
        {
            if (tooltipRect == null || canvasRect == null) return;

            // Convert screen position to local canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPos);

            // Offset slightly from cursor
            localPos += new Vector2(15, -15);

            // Clamp to screen bounds
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            // Right edge clamp
            if (localPos.x + tooltipSize.x > halfW)
                localPos.x = localPos.x - tooltipSize.x - 30;

            // Bottom edge clamp
            if (localPos.y - tooltipSize.y < -halfH)
                localPos.y = -halfH + tooltipSize.y;

            tooltipRect.anchoredPosition = localPos;
        }

        private float ComputeNextValue(StatEffect effect, float current)
        {
            switch (effect.Target)
            {
                case StatTarget.MiningTickInterval:
                    return Mathf.Max(0.05f, current - effect.Value);
                case StatTarget.ResourceMultiplier:
                    return current * (1f + effect.Value);
                case StatTarget.SpawnRateReduction:
                    return Mathf.Max(0.3f, current - effect.Value);
                case StatTarget.LaserCooldown:
                case StatTarget.ChainCooldown:
                case StatTarget.EmpCooldown:
                case StatTarget.OverchargeCooldown:
                    return Mathf.Max(1f, current - effect.Value);
                case StatTarget.ComboMastery:
                    return effect.Value;
                case StatTarget.AdvanceLevel:
                    return current + effect.Value;
                default:
                    return current + effect.Value;
            }
        }

        private string FormatStatValue(StatTarget target, float value)
        {
            switch (target)
            {
                case StatTarget.CritChance:
                case StatTarget.LuckyStrikeChance:
                    return (value * 100f).ToString("F0") + "%";
                case StatTarget.ResourceMultiplier:
                case StatTarget.CritMultiplier:
                case StatTarget.OverchargeDamageMultiplier:
                case StatTarget.ComboMastery:
                    return value.ToString("F2") + "x";
                case StatTarget.MiningTickInterval:
                case StatTarget.SpawnRateReduction:
                    return value.ToString("F2") + "s";
                case StatTarget.RunDuration:
                case StatTarget.LaserCooldown:
                case StatTarget.ChainCooldown:
                case StatTarget.EmpCooldown:
                case StatTarget.OverchargeCooldown:
                case StatTarget.OverchargeDuration:
                case StatTarget.DotDuration:
                    return value.ToString("F1") + "s";
                case StatTarget.AdvanceLevel:
                    return "Lv " + value.ToString("F0");
                case StatTarget.ChainTargets:
                case StatTarget.MaxAsteroidsBonus:
                    return value.ToString("F0");
                default:
                    return value.ToString("F1");
            }
        }

        private string GetStatDisplayName(StatTarget target)
        {
            switch (target)
            {
                case StatTarget.MiningRadius: return "Mining Radius";
                case StatTarget.MiningDamage: return "Damage/Tick";
                case StatTarget.MiningTickInterval: return "Tick Interval";
                case StatTarget.CritChance: return "Crit Chance";
                case StatTarget.CritMultiplier: return "Crit Multiplier";
                case StatTarget.ResourceMultiplier: return "Resource Multi";
                case StatTarget.LuckyStrikeChance: return "Lucky Strike";
                case StatTarget.SpawnRateReduction: return "Spawn Interval";
                case StatTarget.MaxAsteroidsBonus: return "Max Asteroids";
                case StatTarget.SkillUnlock: return "Unlock Skill";
                case StatTarget.LaserDamage: return "Laser Damage";
                case StatTarget.LaserCooldown: return "Laser Cooldown";
                case StatTarget.ChainDamage: return "Chain Damage";
                case StatTarget.ChainCooldown: return "Chain Cooldown";
                case StatTarget.ChainTargets: return "Chain Targets";
                case StatTarget.EmpDamage: return "EMP Damage";
                case StatTarget.EmpCooldown: return "EMP Cooldown";
                case StatTarget.EmpRadius: return "EMP Radius";
                case StatTarget.OverchargeCooldown: return "Overcharge CD";
                case StatTarget.OverchargeDuration: return "Overcharge Dur";
                case StatTarget.OverchargeDamageMultiplier: return "Overcharge Dmg";
                case StatTarget.ComboMastery: return "Combo Mastery";
                case StatTarget.RunDuration: return "Run Duration";
                case StatTarget.AdvanceLevel: return "Level";
                case StatTarget.DotDamage: return "DoT Damage";
                case StatTarget.DotDuration: return "DoT Duration";
                default: return target.ToString();
            }
        }

        private TextMeshProUGUI CreateTooltipText(string name, Transform parent, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(236, 0); // Width minus padding, height auto
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
