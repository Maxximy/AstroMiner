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

    void Awake()
    {
        CreateFadeCanvas();
        CreateDebugCanvas();
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

    private void WireTMPField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
    }
}
