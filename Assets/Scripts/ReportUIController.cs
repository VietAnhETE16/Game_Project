using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReportUIController : MonoBehaviour
{
    [Header("References")]
    public AnomalyManager anomalyManager;
    public CameraManager cameraManager;

    [Header("UI")]
    public GameObject reportPanel;

    [Header("Dropdowns")]
    public TMP_Dropdown roomDropdown;
    public TMP_Dropdown anomalyTypeDropdown;

    [Header("Option Lists")]
    [SerializeField]
    private Transform roomOptionRoot;

    [SerializeField]
    private Transform anomalyTypeOptionRoot;

    [Header("Buttons")]
    public Button reportButton;
    public Button submitButton;
    public Button closeButton;

    [Header("Report Processing")]
    [Min(0f)]
    [SerializeField]
    private float processingDuration = 3f;

    [Min(0f)]
    [SerializeField]
    private float resultDisplayDuration = 2f;

    [SerializeField]
    private string processingMessage = "PROCESSING REPORT...";

    [SerializeField]
    private string successMessage = "REPORT SUCCESSFUL";

    [SerializeField]
    private string failureMessage = "REPORT FAILED";

    private readonly List<AnomalyType> reportableTypes =
        new List<AnomalyType>();
    private readonly List<Button> roomOptionButtons = new List<Button>();
    private readonly List<Button> anomalyTypeOptionButtons = new List<Button>();
    private GameObject processingIndicator;
    private TMP_Text processingText;
    private GameObject resultOverlay;
    private TMP_Text overlayText;
    private Transform reportContentRoot;
    private Coroutine reportCoroutine;
    private bool reportInProgress;
    private int selectedRoomIndex;
    private int selectedAnomalyTypeIndex;

    private void Start()
    {
        if (reportPanel != null)
            reportPanel.SetActive(false);

        BuildReportLayout();
        HideLegacyDropdowns();
        SetupRoomOptions();
        SetupAnomalyTypeOptions();
        SetupButtons();
        BuildProcessingIndicator();
        BuildResultOverlay();
    }

    private void OnDestroy()
    {
        if (reportButton != null)
            reportButton.onClick.RemoveListener(OpenReportPanel);

        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitReport);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseReportPanel);

    }

    private void SetupButtons()
    {
        if (reportButton != null)
        {
            reportButton.onClick.RemoveListener(OpenReportPanel);
            reportButton.onClick.AddListener(OpenReportPanel);
        }
        else
            Debug.LogError("ReportUIController: Report Button is missing.");

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitReport);
            submitButton.onClick.AddListener(SubmitReport);
        }
        else
            Debug.LogError("ReportUIController: Submit Button is missing.");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseReportPanel);
            closeButton.onClick.AddListener(CloseReportPanel);
        }
        else
            Debug.LogWarning("ReportUIController: Close Button is missing.");
    }

    private void SetupRoomOptions()
    {
        if (cameraManager == null)
        {
            Debug.LogError("ReportUIController: CameraManager is missing.");
            return;
        }

        if (cameraManager.RoomCount == 0)
        {
            Debug.LogError("ReportUIController: CameraManager has no rooms.");
            return;
        }

        Transform root = GetOrCreateOptionRoot(
            ref roomOptionRoot,
            "RoomOptionList",
            new Vector2(0.06f, 0.63f),
            new Vector2(0.94f, 0.82f),
            new Vector2(210f, 76f));
        ClearOptions(root);
        roomOptionButtons.Clear();

        for (int i = 0; i < cameraManager.Rooms.Count; i++)
        {
            int optionIndex = i;
            Button button = CreateOptionButton(
                root,
                cameraManager.Rooms[i].DisplayName,
                () => SelectRoom(optionIndex));
            roomOptionButtons.Add(button);
        }

        SelectRoom(Mathf.Clamp(selectedRoomIndex, 0, cameraManager.RoomCount - 1));
    }

    private void SetupAnomalyTypeOptions()
    {
        reportableTypes.Clear();
        Transform root = GetOrCreateOptionRoot(
            ref anomalyTypeOptionRoot,
            "AnomalyTypeOptionList",
            new Vector2(0.06f, 0.18f),
            new Vector2(0.94f, 0.55f),
            new Vector2(240f, 82f));
        ClearOptions(root);
        anomalyTypeOptionButtons.Clear();

        foreach (AnomalyType anomalyType in Enum.GetValues(typeof(AnomalyType)))
        {
            int optionIndex = reportableTypes.Count;
            reportableTypes.Add(anomalyType);
            Button button = CreateOptionButton(
                root,
                GetDisplayName(anomalyType),
                () => SelectAnomalyType(optionIndex));
            anomalyTypeOptionButtons.Add(button);
        }

        SelectAnomalyType(
            Mathf.Clamp(selectedAnomalyTypeIndex, 0, reportableTypes.Count - 1));
    }

    private void OpenReportPanel()
    {
        if (reportInProgress)
            return;

        if (reportPanel == null)
        {
            Debug.LogError("ReportUIController: Report Panel is missing.");
            return;
        }

        reportPanel.SetActive(true);

        Debug.Log("Report panel opened.");
    }

    private void CloseReportPanel()
    {
        if (reportInProgress)
            return;

        if (reportPanel != null)
            reportPanel.SetActive(false);
    }

    private void SubmitReport()
    {
        if (reportInProgress)
            return;

        if (anomalyManager == null)
        {
            Debug.LogError("ReportUIController: AnomalyManager is missing.");
            return;
        }

        if (cameraManager == null)
        {
            Debug.LogError("ReportUIController: CameraManager is missing.");
            return;
        }

        string selectedRoomId = GetSelectedRoomId();
        AnomalyType selectedType = GetSelectedAnomalyType();

        Debug.Log(
            "Submitting report. Selected Room: " +
            selectedRoomId +
            " | Selected Type: " +
            selectedType
        );

        if (reportPanel != null)
            reportPanel.SetActive(false);

        reportCoroutine = StartCoroutine(
            ProcessReport(selectedRoomId, selectedType));
    }

    private IEnumerator ProcessReport(
        string selectedRoomId,
        AnomalyType selectedType)
    {
        reportInProgress = true;
        SetReportControlsInteractable(false);
        ShowProcessingIndicator();

        if (processingDuration > 0f)
            yield return new WaitForSecondsRealtime(processingDuration);

        HideProcessingIndicator();

        bool success =
            anomalyManager.ReportAnomaly(selectedRoomId, selectedType);

        string message = success ? successMessage : failureMessage;
        Color color = success
            ? new Color(0.2f, 1f, 0.35f, 1f)
            : new Color(1f, 0.2f, 0.2f, 1f);

        ShowResultOverlay(message, color);

        if (resultDisplayDuration > 0f)
            yield return new WaitForSecondsRealtime(resultDisplayDuration);

        HideResultOverlay();
        SetReportControlsInteractable(true);
        reportInProgress = false;
        reportCoroutine = null;
    }

    private void BuildProcessingIndicator()
    {
        if (processingIndicator != null)
            return;

        processingIndicator = new GameObject(
            "ReportProcessingIndicator",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = processingIndicator.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 94;

        CanvasScaler scaler =
            processingIndicator.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject(
            "Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(processingIndicator.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-35f, -35f);
        panelRect.sizeDelta = new Vector2(430f, 90f);

        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.82f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Message",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        processingText = textObject.GetComponent<TextMeshProUGUI>();
        processingText.alignment = TextAlignmentOptions.Center;
        processingText.fontSize = 28f;
        processingText.fontStyle = FontStyles.Bold;
        processingText.color = Color.white;
        processingText.raycastTarget = false;
        processingText.text = processingMessage;

        processingIndicator.SetActive(false);
    }

    private void ShowProcessingIndicator()
    {
        if (processingIndicator == null)
            BuildProcessingIndicator();

        if (processingText != null)
            processingText.text = processingMessage;

        if (processingIndicator != null)
            processingIndicator.SetActive(true);
    }

    private void HideProcessingIndicator()
    {
        if (processingIndicator != null)
            processingIndicator.SetActive(false);
    }

    private void BuildResultOverlay()
    {
        if (resultOverlay != null)
            return;

        resultOverlay = new GameObject(
            "ReportResultOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(Image));

        Canvas canvas = resultOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = resultOverlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image background = resultOverlay.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.9f);
        background.raycastTarget = true;

        GameObject textObject = new GameObject(
            "Message",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(resultOverlay.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        overlayText = textObject.GetComponent<TextMeshProUGUI>();
        overlayText.alignment = TextAlignmentOptions.Center;
        overlayText.fontSize = 52f;
        overlayText.fontStyle = FontStyles.Bold;
        overlayText.enableAutoSizing = true;
        overlayText.fontSizeMin = 28f;
        overlayText.fontSizeMax = 52f;
        overlayText.raycastTarget = false;

        resultOverlay.SetActive(false);
    }

    private void ShowResultOverlay(string message, Color color)
    {
        if (resultOverlay == null)
            BuildResultOverlay();

        if (overlayText != null)
        {
            overlayText.text = message;
            overlayText.color = color;
        }

        if (resultOverlay != null)
            resultOverlay.SetActive(true);
    }

    private void HideResultOverlay()
    {
        if (resultOverlay != null)
            resultOverlay.SetActive(false);
    }

    private void SetReportControlsInteractable(bool interactable)
    {
        if (reportButton != null)
            reportButton.interactable = interactable;

        if (submitButton != null)
            submitButton.interactable = interactable;

        if (closeButton != null)
            closeButton.interactable = interactable;

        if (roomDropdown != null)
            roomDropdown.interactable = interactable;

        if (anomalyTypeDropdown != null)
            anomalyTypeDropdown.interactable = interactable;

        SetButtonsInteractable(roomOptionButtons, interactable);
        SetButtonsInteractable(anomalyTypeOptionButtons, interactable);
    }

    private string GetSelectedRoomId()
    {
        if (cameraManager.RoomCount == 0)
            return "";

        if (selectedRoomIndex < 0 || selectedRoomIndex >= cameraManager.Rooms.Count)
            return "";

        return cameraManager.Rooms[selectedRoomIndex].RoomId;
    }

    private AnomalyType GetSelectedAnomalyType()
    {
        if (selectedAnomalyTypeIndex < 0 ||
            selectedAnomalyTypeIndex >= reportableTypes.Count)
        {
            return AnomalyType.ObjectMovement;
        }

        return reportableTypes[selectedAnomalyTypeIndex];
    }

    private void SelectRoom(int index)
    {
        selectedRoomIndex = Mathf.Clamp(
            index,
            0,
            Mathf.Max(0, cameraManager.RoomCount - 1));
        RefreshOptionButtons(roomOptionButtons, selectedRoomIndex);
    }

    private void SelectAnomalyType(int index)
    {
        selectedAnomalyTypeIndex = Mathf.Clamp(
            index,
            0,
            Mathf.Max(0, reportableTypes.Count - 1));
        RefreshOptionButtons(
            anomalyTypeOptionButtons,
            selectedAnomalyTypeIndex);
    }

    private void HideLegacyDropdowns()
    {
        if (roomDropdown != null)
            roomDropdown.gameObject.SetActive(false);

        if (anomalyTypeDropdown != null)
            anomalyTypeDropdown.gameObject.SetActive(false);
    }

    private void BuildReportLayout()
    {
        if (reportPanel == null)
            return;

        RectTransform panelRect = reportPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        Image background = reportPanel.GetComponent<Image>();
        if (background == null)
            background = reportPanel.AddComponent<Image>();

        background.color = new Color(0f, 0f, 0f, 0.82f);
        background.raycastTarget = true;

        for (int i = 0; i < reportPanel.transform.childCount; i++)
            reportPanel.transform.GetChild(i).gameObject.SetActive(false);

        reportContentRoot = reportPanel.transform.Find("ObservationReportLayout");
        if (reportContentRoot == null)
        {
            GameObject rootObject = new GameObject(
                "ObservationReportLayout",
                typeof(RectTransform));
            rootObject.transform.SetParent(reportPanel.transform, false);
            reportContentRoot = rootObject.transform;
        }

        reportContentRoot.gameObject.SetActive(true);
        RectTransform rootRect = reportContentRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CreateHeading(
            reportContentRoot,
            "RoomHeading",
            "Anomaly spotted in:",
            new Vector2(0.25f, 0.82f),
            new Vector2(0.75f, 0.94f));
        CreateHeading(
            reportContentRoot,
            "TypeHeading",
            "Anomaly type:",
            new Vector2(0.25f, 0.55f),
            new Vector2(0.75f, 0.67f));

        roomOptionRoot = null;
        anomalyTypeOptionRoot = null;
        closeButton = CreateCommandButton(
            "CancelButton",
            "Cancel",
            new Vector2(0.02f, 0.02f),
            new Vector2(0.24f, 0.12f),
            TextAlignmentOptions.Left);
        submitButton = CreateCommandButton(
            "SendButton",
            "Send",
            new Vector2(0.76f, 0.02f),
            new Vector2(0.98f, 0.12f),
            TextAlignmentOptions.Right);
    }

    private static TMP_Text CreateHeading(
        Transform parent,
        string objectName,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 34f;
        label.fontStyle = FontStyles.Normal;
        label.color = GetNeonTextColor();
        label.enableAutoSizing = true;
        label.fontSizeMin = 20f;
        label.fontSizeMax = 34f;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateCommandButton(
        string objectName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAlignmentOptions alignment)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(reportContentRoot, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.clear;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.alignment = alignment;
        text.fontSize = 34f;
        text.color = GetNeonTextColor();
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = 34f;
        text.raycastTarget = false;
        return button;
    }

    private Transform GetOrCreateOptionRoot(
        ref Transform root,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 cellSize)
    {
        Transform parent = reportContentRoot != null
            ? reportContentRoot
            : reportPanel != null
                ? reportPanel.transform
                : transform;

        if (root == null)
            root = parent.Find(objectName);

        if (root == null)
        {
            GameObject rootObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GridLayoutGroup layout = root.GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = root.gameObject.AddComponent<GridLayoutGroup>();

        layout.cellSize = cellSize;
        layout.spacing = new Vector2(28f, 18f);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.constraint = GridLayoutGroup.Constraint.Flexible;

        return root;
    }

    private static void ClearOptions(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static Button CreateOptionButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            label + " Option",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = Color.clear;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = Color.clear;
        colors.pressedColor = Color.clear;
        colors.selectedColor = Color.clear;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject glowObject = new GameObject(
            "Glow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        glowObject.transform.SetParent(buttonObject.transform, false);

        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(122f, 122f);

        Image glow = glowObject.GetComponent<Image>();
        glow.sprite = CreateGlowSprite();
        glow.color = Color.clear;
        glow.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Normal;
        text.color = GetNeonTextColor();
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 24f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;

        return button;
    }

    private static void RefreshOptionButtons(
        IReadOnlyList<Button> buttons,
        int selectedIndex)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button == null || button.targetGraphic == null)
                continue;

            ColorBlock colors = button.colors;
            bool selected = i == selectedIndex;
            colors.normalColor = Color.clear;
            button.colors = colors;
            button.targetGraphic.color = Color.clear;

            Transform glow = button.transform.Find("Glow");
            Image glowImage = glow != null ? glow.GetComponent<Image>() : null;
            if (glowImage != null)
            {
                glowImage.color = selected
                    ? GetOptionSelectedColor()
                    : Color.clear;
            }
        }
    }

    private static void SetButtonsInteractable(
        IReadOnlyList<Button> buttons,
        bool interactable)
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private static Color GetNeonTextColor()
    {
        return new Color(0.58f, 1f, 0.38f, 1f);
    }

    private static Color GetOptionSelectedColor()
    {
        return new Color(0.35f, 0.95f, 0.16f, 0.72f);
    }

    private static Sprite glowSprite;

    private static Sprite CreateGlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;

        const int textureSize = 96;
        Texture2D texture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (textureSize - 1) * 0.5f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, 1.7f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        glowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        return glowSprite;
    }

    private static string GetDisplayName(AnomalyType anomalyType)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            anomalyType.ToString(),
            "([a-z])([A-Z])",
            "$1 $2");
    }

}
