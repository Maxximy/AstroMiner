using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    void Awake()
    {
        CreateFadeCanvas();
        CreateDebugCanvas();
        CreateHUDCanvas();
        CreateResultsCanvas();
        CreateUpgradeCanvas();
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
        var fadeField = typeof(FadeController).GetField("_fadeCanvasGroup",
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
        WireTMPField(DebugOverlay, "_stateText", stateTextGO.GetComponent<TextMeshProUGUI>());
        WireTMPField(DebugOverlay, "_fpsText", fpsTextGO.GetComponent<TextMeshProUGUI>());
        WireTMPField(DebugOverlay, "_entityCountText", entityCountTextGO.GetComponent<TextMeshProUGUI>());
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

        // Title text (top center)
        var titleTextGO = CreateTMPText("TitleText", "Upgrades", bgPanelGO.transform);
        var titleRect = titleTextGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(400, 50);
        var titleTMP = titleTextGO.GetComponent<TextMeshProUGUI>();
        titleTMP.fontSize = 40;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;

        // Credits text (top-right)
        var creditsTextGO = CreateTMPText("CreditsText", "0 credits", bgPanelGO.transform);
        var creditsRect = creditsTextGO.GetComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(1, 1);
        creditsRect.anchorMax = new Vector2(1, 1);
        creditsRect.pivot = new Vector2(1, 1);
        creditsRect.anchoredPosition = new Vector2(-20, -35);
        creditsRect.sizeDelta = new Vector2(200, 30);
        var creditsTMP = creditsTextGO.GetComponent<TextMeshProUGUI>();
        creditsTMP.fontSize = 24;
        creditsTMP.color = Color.white;
        creditsTMP.alignment = TextAlignmentOptions.Right;

        // Placeholder text (center)
        var placeholderGO = CreateTMPText("PlaceholderText", "Tech tree coming in Phase 6", bgPanelGO.transform);
        var placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
        placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
        placeholderRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.sizeDelta = new Vector2(400, 30);
        var placeholderTMP = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderTMP.fontSize = 20;
        placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderTMP.alignment = TextAlignmentOptions.Center;

        // Start Run button (center bottom)
        var startRunButton = CreateButton("StartRunButton", "Start Run", bgPanelGO.transform, new Vector2(200, 50));
        var startRunRect = startRunButton.GetComponent<RectTransform>();
        startRunRect.anchorMin = new Vector2(0.5f, 0);
        startRunRect.anchorMax = new Vector2(0.5f, 0);
        startRunRect.pivot = new Vector2(0.5f, 0);
        startRunRect.anchoredPosition = new Vector2(0, 60);
        // Green-ish background for start button
        var startRunImage = startRunButton.GetComponent<Image>();
        if (startRunImage != null)
        {
            startRunImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);
        }

        // UpgradeScreen component
        UpgradeScreen = upgradeCanvasGO.AddComponent<UpgradeScreen>();
        UpgradeScreen.Initialize(titleTMP, creditsTMP, startRunButton.GetComponent<Button>(), upgradeCanvasGO);
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
