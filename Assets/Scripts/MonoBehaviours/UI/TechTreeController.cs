using System.Collections.Generic;
using Data;
using ECS.Components;
using MonoBehaviours.Audio;
using MonoBehaviours.Save;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MonoBehaviours.UI
{
    /// <summary>
    /// Main tech tree controller: builds the node-and-line graph from data,
    /// handles pan/zoom navigation, purchase logic with immediate stat application
    /// to ECS singletons, progressive node revelation, and save integration.
    /// </summary>
    public class TechTreeController : MonoBehaviour
    {
        // -- Data --
        private UpgradeNodeSO[] allNodes;
        private int startNodeIndex;

        // -- UI references --
        private RectTransform contentPanel;
        private RectTransform viewportPanel;
        private TextMeshProUGUI creditsText;

        // -- Node UI --
        private TechTreeNode[] nodeUIs;
        private bool[] purchasedNodes;
        private Dictionary<string, int> nodeIdToIndex;

        // -- Connection lines --
        private List<Image> connectionLines = new List<Image>();
        private List<int> lineFromIndex = new List<int>();
        private List<int> lineToIndex = new List<int>();

        // -- Zoom/Pan --
        private float currentZoom = 1f;
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 2f;
        private const float ZoomSpeed = 1.0f;
        private bool isDragging;
        private Vector2 lastDragPos;

        // -- ECS lazy init --
        private EntityManager em;
        private bool ecsInitialized;
        private Entity gameStateEntity;
        private Entity miningConfigEntity;
        private Entity critConfigEntity;
        private Entity playerBonusEntity;
        private Entity runConfigEntity;
        private Entity skillUnlockEntity;
        private Entity skillStatsEntity;

        // -- Tooltip reference (set by Task 2) --
        private TechTreeTooltip tooltip;

        /// <summary>
        /// Spacing multiplier: 1 graph unit = this many UI pixels.
        /// </summary>
        private const float NodeSpacing = 150f;

        /// <summary>
        /// Set the tooltip reference (called after tooltip is created).
        /// </summary>
        public void SetTooltip(TechTreeTooltip tooltipRef)
        {
            tooltip = tooltipRef;
        }

        /// <summary>
        /// Returns the node data array for external access (e.g., tooltip).
        /// </summary>
        public UpgradeNodeSO[] AllNodes => allNodes;

        /// <summary>
        /// Returns the purchased state array for external access.
        /// </summary>
        public bool[] PurchasedNodes => purchasedNodes;

        /// <summary>
        /// Initialize the tech tree from data, building the full node graph.
        /// </summary>
        public void Initialize(UpgradeNodeSO[] nodes, int startIndex,
            RectTransform content, RectTransform viewport, TextMeshProUGUI credits)
        {
            allNodes = nodes;
            startNodeIndex = startIndex;
            contentPanel = content;
            viewportPanel = viewport;
            creditsText = credits;

            // Build lookup dictionary
            nodeIdToIndex = new Dictionary<string, int>();
            for (int i = 0; i < allNodes.Length; i++)
            {
                if (allNodes[i] != null && !string.IsNullOrEmpty(allNodes[i].NodeId))
                    nodeIdToIndex[allNodes[i].NodeId] = i;
            }

            // Load purchased state from save
            purchasedNodes = new bool[allNodes.Length];
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null && save.TechTreeUnlocks != null && save.TechTreeUnlocks.Length == allNodes.Length)
            {
                System.Array.Copy(save.TechTreeUnlocks, purchasedNodes, allNodes.Length);
            }

            // START node is always purchased
            purchasedNodes[startNodeIndex] = true;

            BuildNodeGraph();
            BuildConnectionLines();
            RefreshAllNodeStates();
        }

        private void BuildNodeGraph()
        {
            nodeUIs = new TechTreeNode[allNodes.Length];

            for (int i = 0; i < allNodes.Length; i++)
            {
                var nodeData = allNodes[i];
                if (nodeData == null) continue;

                var nodeGO = new GameObject($"Node_{nodeData.NodeId}");
                var nodeUI = nodeGO.AddComponent<TechTreeNode>();

                Vector2 position = nodeData.GraphPosition * NodeSpacing;
                nodeUI.Initialize(i, nodeData.DisplayName, nodeData.ActualCost, position,
                    contentPanel, OnNodeClicked);
                nodeUI.Controller = this;
                nodeUIs[i] = nodeUI;
            }
        }

        private void BuildConnectionLines()
        {
            for (int i = 0; i < allNodes.Length; i++)
            {
                var nodeData = allNodes[i];
                if (nodeData == null || nodeData.Prerequisites == null) continue;

                foreach (var prereq in nodeData.Prerequisites)
                {
                    if (prereq == null) continue;
                    int prereqIndex = -1;
                    if (nodeIdToIndex.TryGetValue(prereq.NodeId, out int idx))
                        prereqIndex = idx;
                    if (prereqIndex < 0) continue;

                    CreateLine(prereqIndex, i);
                }
            }
        }

        private void CreateLine(int fromIndex, int toIndex)
        {
            var fromPos = allNodes[fromIndex].GraphPosition * NodeSpacing;
            var toPos = allNodes[toIndex].GraphPosition * NodeSpacing;

            var lineGO = new GameObject($"Line_{fromIndex}_{toIndex}");
            lineGO.transform.SetParent(contentPanel, false);
            // Send behind nodes
            lineGO.transform.SetAsFirstSibling();

            var lineRect = lineGO.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);

            // Position at midpoint
            Vector2 midpoint = (fromPos + toPos) * 0.5f;
            lineRect.anchoredPosition = midpoint;

            // Width = distance between nodes, height = 3px
            float distance = Vector2.Distance(fromPos, toPos);
            lineRect.sizeDelta = new Vector2(distance, 3f);

            // Rotation = angle between positions
            Vector2 direction = toPos - fromPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            var lineImage = lineGO.AddComponent<Image>();
            lineImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            lineImage.raycastTarget = false;

            connectionLines.Add(lineImage);
            this.lineFromIndex.Add(fromIndex);
            this.lineToIndex.Add(toIndex);
        }

        /// <summary>
        /// Refresh all node visual states based on current purchase and credit state.
        /// Also updates connection line colors and credit display.
        /// </summary>
        public void RefreshAllNodeStates()
        {
            long credits = GetCurrentCredits();

            for (int i = 0; i < allNodes.Length; i++)
            {
                if (allNodes[i] == null || nodeUIs[i] == null) continue;

                TechTreeNode.NodeState state;
                if (purchasedNodes[i])
                {
                    state = TechTreeNode.NodeState.Purchased;
                }
                else if (IsRevealed(i))
                {
                    if (CanAfford(i, credits))
                        state = TechTreeNode.NodeState.Available;
                    else
                        state = TechTreeNode.NodeState.TooExpensive;
                }
                else
                {
                    state = TechTreeNode.NodeState.Hidden;
                }

                nodeUIs[i].UpdateState(state, credits);
            }

            // Update connection line colors
            for (int i = 0; i < connectionLines.Count; i++)
            {
                bool fromPurchased = purchasedNodes[lineFromIndex[i]];
                bool toPurchased = purchasedNodes[lineToIndex[i]];
                bool toRevealed = IsRevealed(lineToIndex[i]);

                if (fromPurchased && toPurchased)
                    connectionLines[i].color = new Color(0.204f, 0.596f, 0.859f, 0.8f); // Blue
                else if (toRevealed)
                    connectionLines[i].color = new Color(0.6f, 0.6f, 0.6f, 0.6f); // Lighter gray
                else
                    connectionLines[i].color = new Color(0.4f, 0.4f, 0.4f, 0.0f); // Hidden

                connectionLines[i].gameObject.SetActive(toRevealed || (fromPurchased && toPurchased));
            }

            // Update credit display
            if (creditsText != null)
                creditsText.text = NumberFormatter.Format((double)credits) + " credits";
        }

        /// <summary>
        /// A node is revealed if at least one of its prerequisites is purchased,
        /// or if the node has no prerequisites (connected to START implicitly).
        /// </summary>
        private bool IsRevealed(int nodeIndex)
        {
            var nodeData = allNodes[nodeIndex];
            if (nodeData == null) return false;

            // START node is always revealed
            if (nodeIndex == startNodeIndex) return true;

            // A node with no prerequisites is revealed only if it's the start node
            if (nodeData.Prerequisites == null || nodeData.Prerequisites.Length == 0)
                return false;

            // Revealed if at least one prerequisite is purchased
            foreach (var prereq in nodeData.Prerequisites)
            {
                if (prereq == null) continue;
                if (nodeIdToIndex.TryGetValue(prereq.NodeId, out int prereqIdx))
                {
                    if (purchasedNodes[prereqIdx])
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Can afford: credits >= ActualCost AND all prerequisites purchased (TECH-07).
        /// </summary>
        private bool CanAfford(int nodeIndex, long credits)
        {
            var nodeData = allNodes[nodeIndex];
            if (nodeData == null) return false;

            if (credits < nodeData.ActualCost) return false;

            // All prerequisites must be purchased
            if (nodeData.Prerequisites != null)
            {
                foreach (var prereq in nodeData.Prerequisites)
                {
                    if (prereq == null) continue;
                    if (nodeIdToIndex.TryGetValue(prereq.NodeId, out int prereqIdx))
                    {
                        if (!purchasedNodes[prereqIdx])
                            return false;
                    }
                }
            }

            return true;
        }

        private void OnNodeClicked(int nodeIndex)
        {
            // Defense: already purchased
            if (purchasedNodes[nodeIndex]) return;

            long credits = GetCurrentCredits();
            if (!CanAfford(nodeIndex, credits)) return;

            // Deduct credits via ECS
            if (!TryInitECS()) return;
            var gameState = em.GetComponentData<GameStateData>(gameStateEntity);
            gameState.Credits -= allNodes[nodeIndex].ActualCost;
            em.SetComponentData(gameStateEntity, gameState);

            // Mark purchased
            purchasedNodes[nodeIndex] = true;

            // Apply stat effects
            var nodeData = allNodes[nodeIndex];
            if (nodeData.Effects != null)
            {
                foreach (var effect in nodeData.Effects)
                {
                    ApplyStatEffect(effect, nodeData.SkillIndex);
                }
            }

            // Purchase VFX
            if (nodeUIs[nodeIndex] != null)
                nodeUIs[nodeIndex].PlayPurchaseEffect();

            // Purchase SFX
            AudioManager.Instance?.PlayPurchaseSFX();

            // Save immediately
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null)
            {
                save.TechTreeUnlocks = (bool[])purchasedNodes.Clone();
                // Also update save stats
                UpdateSaveStats(save);
                SaveManager.Instance.Save(save);
            }

            // Refresh all nodes (reveals newly connected nodes)
            RefreshAllNodeStates();
        }

        private void ApplyStatEffect(StatEffect effect, int skillIndex)
        {
            if (!TryInitECS()) return;

            switch (effect.Target)
            {
                case StatTarget.MiningRadius:
                {
                    var data = em.GetComponentData<MiningConfigData>(miningConfigEntity);
                    data.Radius += effect.Value;
                    em.SetComponentData(miningConfigEntity, data);
                    break;
                }
                case StatTarget.MiningDamage:
                {
                    var data = em.GetComponentData<MiningConfigData>(miningConfigEntity);
                    data.DamagePerTick += effect.Value;
                    em.SetComponentData(miningConfigEntity, data);
                    break;
                }
                case StatTarget.MiningTickInterval:
                {
                    var data = em.GetComponentData<MiningConfigData>(miningConfigEntity);
                    data.TickInterval = Mathf.Max(0.05f, data.TickInterval - effect.Value);
                    em.SetComponentData(miningConfigEntity, data);
                    break;
                }
                case StatTarget.CritChance:
                {
                    var data = em.GetComponentData<CritConfigData>(critConfigEntity);
                    data.CritChance += effect.Value;
                    em.SetComponentData(critConfigEntity, data);
                    break;
                }
                case StatTarget.CritMultiplier:
                {
                    var data = em.GetComponentData<CritConfigData>(critConfigEntity);
                    data.CritMultiplier += effect.Value;
                    em.SetComponentData(critConfigEntity, data);
                    break;
                }
                case StatTarget.ResourceMultiplier:
                {
                    var data = em.GetComponentData<PlayerBonusData>(playerBonusEntity);
                    data.ResourceMultiplier *= (1f + effect.Value);
                    em.SetComponentData(playerBonusEntity, data);
                    break;
                }
                case StatTarget.LuckyStrikeChance:
                {
                    var data = em.GetComponentData<PlayerBonusData>(playerBonusEntity);
                    data.LuckyStrikeChance += effect.Value;
                    em.SetComponentData(playerBonusEntity, data);
                    break;
                }
                case StatTarget.SpawnRateReduction:
                {
                    var data = em.GetComponentData<RunConfigData>(runConfigEntity);
                    data.SpawnInterval = Mathf.Max(0.3f, data.SpawnInterval - effect.Value);
                    em.SetComponentData(runConfigEntity, data);
                    break;
                }
                case StatTarget.MaxAsteroidsBonus:
                {
                    var data = em.GetComponentData<RunConfigData>(runConfigEntity);
                    data.MaxActiveAsteroids += (int)effect.Value;
                    em.SetComponentData(runConfigEntity, data);
                    break;
                }
                case StatTarget.SkillUnlock:
                {
                    var data = em.GetComponentData<SkillUnlockData>(skillUnlockEntity);
                    switch (skillIndex)
                    {
                        case 1: data.Skill1Unlocked = true; break;
                        case 2: data.Skill2Unlocked = true; break;
                        case 3: data.Skill3Unlocked = true; break;
                        case 4: data.Skill4Unlocked = true; break;
                    }
                    em.SetComponentData(skillUnlockEntity, data);

                    // Update save SkillUnlocks
                    var save = SaveManager.Instance?.CurrentSave;
                    if (save != null && save.SkillUnlocks != null && skillIndex >= 1 && skillIndex <= 4)
                        save.SkillUnlocks[skillIndex - 1] = true;
                    break;
                }
                case StatTarget.LaserDamage:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.LaserDamage += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.LaserCooldown:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.LaserCooldown = Mathf.Max(1f, data.LaserCooldown - effect.Value);
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.ChainDamage:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.ChainDamage += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.ChainCooldown:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.ChainCooldown = Mathf.Max(1f, data.ChainCooldown - effect.Value);
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.ChainTargets:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.ChainMaxTargets += (int)effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.EmpDamage:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.EmpDamage += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.EmpCooldown:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.EmpCooldown = Mathf.Max(1f, data.EmpCooldown - effect.Value);
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.EmpRadius:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.EmpRadius += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.OverchargeCooldown:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.OverchargeCooldown = Mathf.Max(1f, data.OverchargeCooldown - effect.Value);
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.OverchargeDuration:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.OverchargeDuration += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.OverchargeDamageMultiplier:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.OverchargeDamageMultiplier += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.MineralDropCount:
                {
                    var data = em.GetComponentData<PlayerBonusData>(playerBonusEntity);
                    data.MineralDropCount += (int)effect.Value;
                    em.SetComponentData(playerBonusEntity, data);
                    break;
                }
                case StatTarget.ComboMastery:
                {
                    var data = em.GetComponentData<PlayerBonusData>(playerBonusEntity);
                    data.ComboMasteryMultiplier = effect.Value;
                    em.SetComponentData(playerBonusEntity, data);
                    break;
                }
                case StatTarget.RunDuration:
                {
                    var data = em.GetComponentData<RunConfigData>(runConfigEntity);
                    data.RunDuration += effect.Value;
                    em.SetComponentData(runConfigEntity, data);
                    break;
                }
                case StatTarget.AdvanceLevel:
                {
                    var data = em.GetComponentData<RunConfigData>(runConfigEntity);
                    data.CurrentLevel += (int)effect.Value;
                    em.SetComponentData(runConfigEntity, data);

                    // Update save level
                    var save = SaveManager.Instance?.CurrentSave;
                    if (save != null)
                        save.CurrentLevel = data.CurrentLevel;
                    break;
                }
                case StatTarget.DotDamage:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.LaserDotDamagePerTick += effect.Value;
                    data.EmpDotDamagePerTick += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
                case StatTarget.DotDuration:
                {
                    var data = em.GetComponentData<SkillStatsData>(skillStatsEntity);
                    data.LaserDotDuration += effect.Value;
                    data.EmpDotDuration += effect.Value;
                    em.SetComponentData(skillStatsEntity, data);
                    break;
                }
            }
        }

        /// <summary>
        /// Updates SaveData stats to match current ECS singleton values.
        /// </summary>
        private void UpdateSaveStats(SaveData save)
        {
            if (!ecsInitialized) return;

            var mining = em.GetComponentData<MiningConfigData>(miningConfigEntity);
            save.Stats.MiningRadius = mining.Radius;
            save.Stats.DamagePerTick = mining.DamagePerTick;
            save.Stats.TickInterval = mining.TickInterval;

            var crit = em.GetComponentData<CritConfigData>(critConfigEntity);
            save.Stats.CritChance = crit.CritChance;
            save.Stats.CritMultiplier = crit.CritMultiplier;

            var bonus = em.GetComponentData<PlayerBonusData>(playerBonusEntity);
            save.Stats.ResourceMultiplier = bonus.ResourceMultiplier;
            save.Stats.LuckyStrikeChance = bonus.LuckyStrikeChance;
            save.Stats.ComboMasteryMultiplier = bonus.ComboMasteryMultiplier;
            save.Stats.MineralDropCount = bonus.MineralDropCount;

            var runConfig = em.GetComponentData<RunConfigData>(runConfigEntity);
            save.Stats.RunDuration = runConfig.RunDuration;
            save.CurrentLevel = runConfig.CurrentLevel;

            var skills = em.GetComponentData<SkillStatsData>(skillStatsEntity);
            save.Stats.LaserDamage = skills.LaserDamage;
            save.Stats.LaserCooldown = skills.LaserCooldown;
            save.Stats.ChainDamage = skills.ChainDamage;
            save.Stats.ChainCooldown = skills.ChainCooldown;
            save.Stats.ChainMaxTargets = skills.ChainMaxTargets;
            save.Stats.ChainMaxDist = skills.ChainMaxDist;
            save.Stats.EmpDamage = skills.EmpDamage;
            save.Stats.EmpCooldown = skills.EmpCooldown;
            save.Stats.EmpRadius = skills.EmpRadius;
            save.Stats.OverchargeCooldown = skills.OverchargeCooldown;
            save.Stats.OverchargeDuration = skills.OverchargeDuration;
            save.Stats.OverchargeDamageMultiplier = skills.OverchargeDamageMultiplier;
            save.Stats.LaserDotDamagePerTick = skills.LaserDotDamagePerTick;
            save.Stats.LaserDotTickInterval = skills.LaserDotTickInterval;
            save.Stats.LaserDotDuration = skills.LaserDotDuration;
            save.Stats.EmpDotDamagePerTick = skills.EmpDotDamagePerTick;
            save.Stats.EmpDotTickInterval = skills.EmpDotTickInterval;
            save.Stats.EmpDotDuration = skills.EmpDotDuration;
        }

        private long GetCurrentCredits()
        {
            if (!TryInitECS()) return 0;
            var gameState = em.GetComponentData<GameStateData>(gameStateEntity);
            return gameState.Credits;
        }

        // =====================================================================
        // Pan/Zoom (New Input System -- Mouse.current)
        // =====================================================================

        private void Update()
        {
            if (contentPanel == null || viewportPanel == null) return;
            if (Mouse.current == null) return;

            HandleZoom();
            HandlePan();

            // Update tooltip position if visible
            if (tooltip != null && tooltip.IsVisible)
            {
                tooltip.UpdatePosition(Mouse.current.position.ReadValue());
            }
        }

        private void HandleZoom()
        {
            var scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            float newZoom = Mathf.Clamp(currentZoom + (scroll / 120f) * ZoomSpeed, MinZoom, MaxZoom);
            if (Mathf.Approximately(newZoom, currentZoom)) return;

            // Zoom toward mouse pivot
            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewportPanel, Mouse.current.position.ReadValue(), null, out localMousePos);

            // Calculate content offset to maintain mouse-relative position
            Vector2 contentLocalMouse = (localMousePos - contentPanel.anchoredPosition) / currentZoom;
            currentZoom = newZoom;
            contentPanel.localScale = Vector3.one * currentZoom;
            contentPanel.anchoredPosition = localMousePos - contentLocalMouse * currentZoom;
        }

        private void HandlePan()
        {
            // Right-click or middle-mouse drag for pan
            bool rightPressed = Mouse.current.rightButton.isPressed;
            bool middlePressed = Mouse.current.middleButton.isPressed;
            bool panActive = rightPressed || middlePressed;

            if (panActive)
            {
                Vector2 currentPos = Mouse.current.position.ReadValue();
                if (!isDragging)
                {
                    isDragging = true;
                    lastDragPos = currentPos;
                }
                else
                {
                    Vector2 delta = currentPos - lastDragPos;
                    contentPanel.anchoredPosition += delta;
                    lastDragPos = currentPos;
                }
            }
            else
            {
                isDragging = false;
            }
        }

        // =====================================================================
        // Tooltip interface (called by TechTreeNode hover events)
        // =====================================================================

        /// <summary>
        /// Show tooltip for the given node at screen position.
        /// </summary>
        public void ShowTooltip(int nodeIndex, Vector2 screenPos)
        {
            if (tooltip == null || nodeIndex < 0 || nodeIndex >= allNodes.Length) return;
            tooltip.Show(allNodes[nodeIndex], purchasedNodes[nodeIndex], screenPos, this);
        }

        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        public void HideTooltip()
        {
            tooltip?.Hide();
        }

        // =====================================================================
        // ECS lazy init (same pattern as SkillBarController)
        // =====================================================================

        private bool TryInitECS()
        {
            if (ecsInitialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            em = world.EntityManager;

            if (!TryGetSingletonEntity<GameStateData>(out gameStateEntity)) return false;
            if (!TryGetSingletonEntity<MiningConfigData>(out miningConfigEntity)) return false;
            if (!TryGetSingletonEntity<CritConfigData>(out critConfigEntity)) return false;
            if (!TryGetSingletonEntity<PlayerBonusData>(out playerBonusEntity)) return false;
            if (!TryGetSingletonEntity<RunConfigData>(out runConfigEntity)) return false;
            if (!TryGetSingletonEntity<SkillUnlockData>(out skillUnlockEntity)) return false;
            if (!TryGetSingletonEntity<SkillStatsData>(out skillStatsEntity)) return false;

            ecsInitialized = true;
            return true;
        }

        private bool TryGetSingletonEntity<T>(out Entity entity) where T : unmanaged, IComponentData
        {
            var query = em.CreateEntityQuery(typeof(T));
            if (query.CalculateEntityCount() == 0)
            {
                entity = Entity.Null;
                return false;
            }
            entity = query.GetSingletonEntity();
            return true;
        }

        /// <summary>
        /// Read a current stat value from ECS for tooltip preview.
        /// Returns the current value for the given stat target.
        /// </summary>
        public float GetCurrentStatValue(StatTarget target)
        {
            if (!TryInitECS()) return 0f;

            switch (target)
            {
                case StatTarget.MiningRadius:
                    return em.GetComponentData<MiningConfigData>(miningConfigEntity).Radius;
                case StatTarget.MiningDamage:
                    return em.GetComponentData<MiningConfigData>(miningConfigEntity).DamagePerTick;
                case StatTarget.MiningTickInterval:
                    return em.GetComponentData<MiningConfigData>(miningConfigEntity).TickInterval;
                case StatTarget.CritChance:
                    return em.GetComponentData<CritConfigData>(critConfigEntity).CritChance;
                case StatTarget.CritMultiplier:
                    return em.GetComponentData<CritConfigData>(critConfigEntity).CritMultiplier;
                case StatTarget.ResourceMultiplier:
                    return em.GetComponentData<PlayerBonusData>(playerBonusEntity).ResourceMultiplier;
                case StatTarget.LuckyStrikeChance:
                    return em.GetComponentData<PlayerBonusData>(playerBonusEntity).LuckyStrikeChance;
                case StatTarget.RunDuration:
                    return em.GetComponentData<RunConfigData>(runConfigEntity).RunDuration;
                case StatTarget.LaserDamage:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).LaserDamage;
                case StatTarget.LaserCooldown:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).LaserCooldown;
                case StatTarget.ChainDamage:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).ChainDamage;
                case StatTarget.ChainCooldown:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).ChainCooldown;
                case StatTarget.ChainTargets:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).ChainMaxTargets;
                case StatTarget.EmpDamage:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).EmpDamage;
                case StatTarget.EmpCooldown:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).EmpCooldown;
                case StatTarget.EmpRadius:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).EmpRadius;
                case StatTarget.OverchargeCooldown:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).OverchargeCooldown;
                case StatTarget.OverchargeDuration:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).OverchargeDuration;
                case StatTarget.OverchargeDamageMultiplier:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).OverchargeDamageMultiplier;
                case StatTarget.MineralDropCount:
                    return em.GetComponentData<PlayerBonusData>(playerBonusEntity).MineralDropCount;
                case StatTarget.ComboMastery:
                    return em.GetComponentData<PlayerBonusData>(playerBonusEntity).ComboMasteryMultiplier;
                case StatTarget.DotDamage:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).LaserDotDamagePerTick;
                case StatTarget.DotDuration:
                    return em.GetComponentData<SkillStatsData>(skillStatsEntity).LaserDotDuration;
                default:
                    return 0f;
            }
        }
    }
}
