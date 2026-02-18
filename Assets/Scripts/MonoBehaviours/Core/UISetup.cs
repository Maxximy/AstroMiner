using Data;
using MonoBehaviours.Audio;
using MonoBehaviours.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MonoBehaviours.Core
{
    /// <summary>
    /// Creates all UI canvases and wires references at runtime.
    /// Runs in Awake before GameManager.Start uses the FadeController.
    /// </summary>
    public class UISetup : MonoBehaviour
    {
        [Header("References (auto-wired at runtime)")]
        public FadeController FadeController { get; private set; }
        public DebugOverlay DebugOverlay { get; private set; }
        public HUDController HUDController { get; private set; }
        public ResultsScreen ResultsScreen { get; private set; }
        public UpgradeScreen UpgradeScreen { get; private set; }
        public SkillBarController SkillBarController { get; private set; }

        void Awake()
        {
            EnsureEventSystem();
            CreateFadeCanvas();
            CreateDebugCanvas();
            CreateHUDCanvas();
            CreateResultsCanvas();
            CreateUpgradeCanvas();
            CreateSkillBarCanvas();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        private void CreateFadeCanvas()
        {
            // FadeCanvas (ScreenSpace-Overlay, Sort Order 100 -- always on top)
            var fadeCanvasGO = new GameObject("FadeCanvas");
            fadeCanvasGO.transform.SetParent(transform);

            var fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 100;

            fadeCanvasGO.AddComponent<CanvasScaler>();
            fadeCanvasGO.AddComponent<GraphicRaycaster>();

            // FadePanel (full-screen black Image with CanvasGroup)
            var fadePanelGO = new GameObject("FadePanel");
            fadePanelGO.transform.SetParent(fadeCanvasGO.transform, false);

            var fadePanelRect = fadePanelGO.AddComponent<RectTransform>();
            fadePanelRect.anchorMin = Vector2.zero;
            fadePanelRect.anchorMax = Vector2.one;
            fadePanelRect.offsetMin = Vector2.zero;
            fadePanelRect.offsetMax = Vector2.zero;

            var fadePanelImage = fadePanelGO.AddComponent<Image>();
            fadePanelImage.color = Color.black;

            var fadeCanvasGroup = fadePanelGO.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;

            // FadeController on the canvas root
            FadeController = fadeCanvasGO.AddComponent<FadeController>();

            // Wire the CanvasGroup reference via reflection (SerializeField)
            var fadeField = typeof(FadeController).GetField("FadeCanvasGroup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fadeField != null)
                fadeField.SetValue(FadeController, fadeCanvasGroup);
        }

        private void CreateDebugCanvas()
        {
            // DebugCanvas (ScreenSpace-Overlay, Sort Order 99)
            var debugCanvasGO = new GameObject("DebugCanvas");
            debugCanvasGO.transform.SetParent(transform);

            var debugCanvas = debugCanvasGO.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            debugCanvas.sortingOrder = 99;

            var scaler = debugCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            debugCanvasGO.AddComponent<GraphicRaycaster>();

            // DebugPanel (top-left corner, semi-transparent black background)
            var debugPanelGO = new GameObject("DebugPanel");
            debugPanelGO.transform.SetParent(debugCanvasGO.transform, false);

            var panelRect = debugPanelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);

            var panelImage = debugPanelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.6f);

            var layout = debugPanelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = debugPanelGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // State text
            var stateTextGO = CreateTMPText("StateText", "Playing", debugPanelGO.transform);

            // FPS text
            var fpsTextGO = CreateTMPText("FPSText", "FPS: --", debugPanelGO.transform);

            // Entity count text
            var entityCountTextGO = CreateTMPText("EntityCountText", "Entities: 0", debugPanelGO.transform);

            // DebugOverlay component on the canvas root
            DebugOverlay = debugCanvasGO.AddComponent<DebugOverlay>();

            // Wire TextMeshProUGUI references via reflection
            WireTMPField(DebugOverlay, "StateText", stateTextGO.GetComponent<TextMeshProUGUI>());
            WireTMPField(DebugOverlay, "FPSText", fpsTextGO.GetComponent<TextMeshProUGUI>());
            WireTMPField(DebugOverlay, "EntityCountText", entityCountTextGO.GetComponent<TextMeshProUGUI>());
        }

        private void CreateHUDCanvas()
        {
            // HUDCanvas (ScreenSpace-Overlay, Sort Order 10)
            var hudCanvasGO = new GameObject("HUDCanvas");
            hudCanvasGO.transform.SetParent(transform);

            var hudCanvas = hudCanvasGO.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 10;

            var scaler = hudCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            hudCanvasGO.AddComponent<GraphicRaycaster>();

            // Credits label (top-right, small)
            var creditsLabelGO = CreateTMPText("CreditsLabel", "CREDITS", hudCanvasGO.transform);
            var creditsLabelRect = creditsLabelGO.GetComponent<RectTransform>();
            creditsLabelRect.anchorMin = new Vector2(1, 1);
            creditsLabelRect.anchorMax = new Vector2(1, 1);
            creditsLabelRect.pivot = new Vector2(1, 1);
            creditsLabelRect.anchoredPosition = new Vector2(-20, -10);
            creditsLabelRect.sizeDelta = new Vector2(200, 20);
            var creditsLabelTMP = creditsLabelGO.GetComponent<TextMeshProUGUI>();
            creditsLabelTMP.fontSize = 12;
            creditsLabelTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            creditsLabelTMP.alignment = TextAlignmentOptions.Right;

            // Credits text (top-right, below label)
            var creditsTextGO = CreateTMPText("CreditsText", "0", hudCanvasGO.transform);
            var creditsTextRect = creditsTextGO.GetComponent<RectTransform>();
            creditsTextRect.anchorMin = new Vector2(1, 1);
            creditsTextRect.anchorMax = new Vector2(1, 1);
            creditsTextRect.pivot = new Vector2(1, 1);
            creditsTextRect.anchoredPosition = new Vector2(-20, -28);
            creditsTextRect.sizeDelta = new Vector2(200, 30);
            var creditsTextTMP = creditsTextGO.GetComponent<TextMeshProUGUI>();
            creditsTextTMP.fontSize = 24;
            creditsTextTMP.color = Color.white;
            creditsTextTMP.alignment = TextAlignmentOptions.Right;

            // Timer text (top-center)
            var timerTextGO = CreateTMPText("TimerText", "1:00", hudCanvasGO.transform);
            var timerTextRect = timerTextGO.GetComponent<RectTransform>();
            timerTextRect.anchorMin = new Vector2(0.5f, 1);
            timerTextRect.anchorMax = new Vector2(0.5f, 1);
            timerTextRect.pivot = new Vector2(0.5f, 1);
            timerTextRect.anchoredPosition = new Vector2(0, -10);
            timerTextRect.sizeDelta = new Vector2(200, 40);
            var timerTextTMP = timerTextGO.GetComponent<TextMeshProUGUI>();
            timerTextTMP.fontSize = 32;
            timerTextTMP.color = Color.white;
            timerTextTMP.alignment = TextAlignmentOptions.Center;

            // HUDController component
            HUDController = hudCanvasGO.AddComponent<HUDController>();
            HUDController.Initialize(creditsTextTMP, timerTextTMP, hudCanvasGO);
        }

        private void CreateResultsCanvas()
        {
            // ResultsCanvas (ScreenSpace-Overlay, Sort Order 50)
            var resultsCanvasGO = new GameObject("ResultsCanvas");
            resultsCanvasGO.transform.SetParent(transform);

            var resultsCanvas = resultsCanvasGO.AddComponent<Canvas>();
            resultsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            resultsCanvas.sortingOrder = 50;

            var scaler = resultsCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            resultsCanvasGO.AddComponent<GraphicRaycaster>();

            // Semi-transparent dark background panel
            var bgPanelGO = new GameObject("BackgroundPanel");
            bgPanelGO.transform.SetParent(resultsCanvasGO.transform, false);
            var bgRect = bgPanelGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgPanelGO.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.85f);

            // Title text
            var titleTextGO = CreateTMPText("TitleText", "Run Complete!", bgPanelGO.transform);
            var titleRect = titleTextGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 80);
            titleRect.sizeDelta = new Vector2(600, 60);
            var titleTMP = titleTextGO.GetComponent<TextMeshProUGUI>();
            titleTMP.fontSize = 48;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Credits earned text
            var creditsTextGO = CreateTMPText("CreditsEarnedText", "0 credits earned", bgPanelGO.transform);
            var creditsRect = creditsTextGO.GetComponent<RectTransform>();
            creditsRect.anchorMin = new Vector2(0.5f, 0.5f);
            creditsRect.anchorMax = new Vector2(0.5f, 0.5f);
            creditsRect.pivot = new Vector2(0.5f, 0.5f);
            creditsRect.anchoredPosition = new Vector2(0, 10);
            creditsRect.sizeDelta = new Vector2(600, 50);
            var creditsTMP = creditsTextGO.GetComponent<TextMeshProUGUI>();
            creditsTMP.fontSize = 36;
            creditsTMP.color = new Color(0.9f, 0.75f, 0.3f, 1f); // Gold color
            creditsTMP.alignment = TextAlignmentOptions.Center;

            // Continue button
            var continueButton = CreateButton("ContinueButton", "Continue", bgPanelGO.transform, new Vector2(200, 50));
            var continueBtnRect = continueButton.GetComponent<RectTransform>();
            continueBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            continueBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            continueBtnRect.pivot = new Vector2(0.5f, 0.5f);
            continueBtnRect.anchoredPosition = new Vector2(0, -60);

            // ResultsScreen component
            ResultsScreen = resultsCanvasGO.AddComponent<ResultsScreen>();
            ResultsScreen.Initialize(titleTMP, creditsTMP, continueButton.GetComponent<Button>(), resultsCanvasGO);
        }

        private void CreateUpgradeCanvas()
        {
            // UpgradeCanvas (ScreenSpace-Overlay, Sort Order 50)
            var upgradeCanvasGO = new GameObject("UpgradeCanvas");
            upgradeCanvasGO.transform.SetParent(transform);

            var upgradeCanvas = upgradeCanvasGO.AddComponent<Canvas>();
            upgradeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            upgradeCanvas.sortingOrder = 50;

            var scaler = upgradeCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            upgradeCanvasGO.AddComponent<GraphicRaycaster>();

            // Solid dark background panel
            var bgPanelGO = new GameObject("BackgroundPanel");
            bgPanelGO.transform.SetParent(upgradeCanvasGO.transform, false);
            var bgRect = bgPanelGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgPanelGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Title text (top center) -- "Tech Tree"
            var titleTextGO = CreateTMPText("TitleText", "Tech Tree", bgPanelGO.transform);
            var titleRect = titleTextGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(400, 50);
            var titleTMP = titleTextGO.GetComponent<TextMeshProUGUI>();
            titleTMP.fontSize = 40;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Credits text (top-right) -- always visible
            var creditsTextGO = CreateTMPText("CreditsText", "0 credits", bgPanelGO.transform);
            var creditsRect = creditsTextGO.GetComponent<RectTransform>();
            creditsRect.anchorMin = new Vector2(1, 1);
            creditsRect.anchorMax = new Vector2(1, 1);
            creditsRect.pivot = new Vector2(1, 1);
            creditsRect.anchoredPosition = new Vector2(-20, -20);
            creditsRect.sizeDelta = new Vector2(250, 30);
            var creditsTMP = creditsTextGO.GetComponent<TextMeshProUGUI>();
            creditsTMP.fontSize = 24;
            creditsTMP.color = new Color(0.9f, 0.85f, 0.4f, 1f); // Gold
            creditsTMP.alignment = TextAlignmentOptions.Right;

            // Viewport panel: full-screen area minus top bar and bottom button
            // Uses RectMask2D for clipping the pannable/zoomable content
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(bgPanelGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(0, 70);   // Above Start Run button
            viewportRect.offsetMax = new Vector2(0, -60);   // Below title bar
            viewportGO.AddComponent<RectMask2D>();
            // Need an Image for raycast detection (transparent)
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.01f); // Near-transparent for raycast

            // Content panel: large pannable/zoomable surface (child of viewport)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(4000, 4000);

            // Start Run button (bottom center, green)
            var startRunButton = CreateButton("StartRunButton", "Start Run", bgPanelGO.transform, new Vector2(200, 50));
            var startRunRect = startRunButton.GetComponent<RectTransform>();
            startRunRect.anchorMin = new Vector2(0.5f, 0);
            startRunRect.anchorMax = new Vector2(0.5f, 0);
            startRunRect.pivot = new Vector2(0.5f, 0);
            startRunRect.anchoredPosition = new Vector2(0, 10);
            var startRunImage = startRunButton.GetComponent<Image>();
            if (startRunImage != null)
            {
                startRunImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);
            }

            // Create TechTreeController on the canvas
            var techTreeController = upgradeCanvasGO.AddComponent<TechTreeController>();

            // Build node data from TechTreeDefinitions (programmatic, Option A)
            var treeData = TechTreeDefinitions.BuildTree();
            techTreeController.Initialize(
                treeData.AllNodes, treeData.StartNodeIndex,
                contentRect, viewportRect, creditsTMP
            );

            // UpgradeScreen component
            UpgradeScreen = upgradeCanvasGO.AddComponent<UpgradeScreen>();
            UpgradeScreen.Initialize(titleTMP, creditsTMP, startRunButton.GetComponent<Button>(), upgradeCanvasGO);
            UpgradeScreen.TechTreeController = techTreeController;
        }

        private void CreateSkillBarCanvas()
        {
            // SkillBarCanvas (ScreenSpace-Overlay, Sort Order 15 -- above HUD)
            var skillBarCanvasGO = new GameObject("SkillBarCanvas");
            skillBarCanvasGO.transform.SetParent(transform);

            var skillBarCanvas = skillBarCanvasGO.AddComponent<Canvas>();
            skillBarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            skillBarCanvas.sortingOrder = 15;

            var scaler = skillBarCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            skillBarCanvasGO.AddComponent<GraphicRaycaster>();

            // Horizontal panel anchored bottom-center
            var panelGO = new GameObject("SkillBarPanel");
            panelGO.transform.SetParent(skillBarCanvasGO.transform, false);

            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 20);
            panelRect.sizeDelta = new Vector2(320, 70);

            var layoutGroup = panelGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 8;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Skill icon colors and keybinds
            Color[] iconColors =
            {
                new Color(0f, 1f, 1f, 0.8f),     // Slot 0 (Laser): Cyan
                new Color(0.5f, 0.7f, 1f, 0.8f),  // Slot 1 (Chain): Blue
                new Color(0.7f, 0.4f, 1f, 0.8f),  // Slot 2 (EMP): Purple
                new Color(1f, 0.8f, 0f, 0.8f)      // Slot 3 (Overcharge): Gold
            };
            string[] keybinds = { "1", "2", "3", "4" };

            var overlays = new Image[4];
            var cooldownTexts = new TextMeshProUGUI[4];
            var buttons = new Button[4];
            var slotRoots = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                // Slot root with Button
                var slotGO = new GameObject("SkillSlot_" + i);
                slotGO.transform.SetParent(panelGO.transform, false);

                var slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(70, 70);

                // Background image
                var bgImage = slotGO.AddComponent<Image>();
                bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

                // Button component
                var button = slotGO.AddComponent<Button>();
                button.targetGraphic = bgImage;
                button.onClick.AddListener(() => AudioManager.Instance?.PlayUIClick());
                buttons[i] = button;
                slotRoots[i] = slotGO;

                // Icon child
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(slotGO.transform, false);

                var iconRect = iconGO.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;

                var iconImage = iconGO.AddComponent<Image>();
                iconImage.color = iconColors[i];
                iconImage.raycastTarget = false;

                // Skill-specific icon shapes
                switch (i)
                {
                    case 0: // Laser: horizontal beam line
                        iconRect.sizeDelta = new Vector2(50, 8);
                        break;
                    case 1: // Chain: blue filled square
                        iconRect.sizeDelta = new Vector2(30, 30);
                        break;
                    case 2: // EMP: filled circle (using Image.Type.Simple for now)
                        iconRect.sizeDelta = new Vector2(35, 35);
                        break;
                    case 3: // Overcharge: small gold rect
                        iconRect.sizeDelta = new Vector2(25, 40);
                        break;
                }

                // Cooldown overlay child (radial fill)
                var overlayGO = new GameObject("CooldownOverlay");
                overlayGO.transform.SetParent(slotGO.transform, false);

                var overlayRect = overlayGO.AddComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var overlayImage = overlayGO.AddComponent<Image>();
                overlayImage.color = new Color(0f, 0f, 0f, 0.7f);
                overlayImage.type = Image.Type.Filled;
                overlayImage.fillMethod = Image.FillMethod.Radial360;
                overlayImage.fillOrigin = (int)Image.Origin360.Top;
                overlayImage.fillClockwise = true;
                overlayImage.fillAmount = 0f;
                overlayImage.raycastTarget = false;
                overlays[i] = overlayImage;

                // Cooldown text child (centered)
                var cdTextGO = CreateTMPText("CooldownText", "", slotGO.transform);
                var cdTextRect = cdTextGO.GetComponent<RectTransform>();
                cdTextRect.anchorMin = Vector2.zero;
                cdTextRect.anchorMax = Vector2.one;
                cdTextRect.offsetMin = Vector2.zero;
                cdTextRect.offsetMax = Vector2.zero;
                var cdTMP = cdTextGO.GetComponent<TextMeshProUGUI>();
                cdTMP.fontSize = 16;
                cdTMP.color = Color.white;
                cdTMP.alignment = TextAlignmentOptions.Center;
                cdTMP.raycastTarget = false;
                cooldownTexts[i] = cdTMP;

                // Keybind badge child (bottom-right corner)
                var badgeGO = CreateTMPText("KeybindBadge", keybinds[i], slotGO.transform);
                var badgeRect = badgeGO.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1, 0);
                badgeRect.anchorMax = new Vector2(1, 0);
                badgeRect.pivot = new Vector2(1, 0);
                badgeRect.anchoredPosition = new Vector2(-2, 2);
                badgeRect.sizeDelta = new Vector2(20, 16);
                var badgeTMP = badgeGO.GetComponent<TextMeshProUGUI>();
                badgeTMP.fontSize = 10;
                badgeTMP.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                badgeTMP.alignment = TextAlignmentOptions.BottomRight;
                badgeTMP.raycastTarget = false;
            }

            // Add SkillBarController and initialize
            SkillBarController = skillBarCanvasGO.AddComponent<SkillBarController>();
            SkillBarController.Initialize(overlays, cooldownTexts, buttons, panelGO, slotRoots);
        }

        private GameObject CreateTMPText(string name, string defaultText, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 20);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;

            return go;
        }

        /// <summary>
        /// Creates a UI Button with Image background and TMPro text child.
        /// </summary>
        private GameObject CreateButton(string name, string label, Transform parent, Vector2 size)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = size;

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f); // Dark gray default

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Wire click SFX for all buttons (AUDI-07)
            button.onClick.AddListener(() => AudioManager.Instance?.PlayUIClick());

            // Button label text
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 18;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.enableWordWrapping = false;

            return buttonGO;
        }

        private void WireTMPField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }
    }
}
