using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PrototypeGameManager gameManager;

    [SerializeField]
    private GameObject menuPanel;

    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button quitButton;

    [Header("Optional Gameplay UI")]
    [Tooltip("UI objects hidden while the start menu is visible.")]
    [SerializeField]
    private GameObject[] gameplayUiObjects;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GetComponent<PrototypeGameManager>();

        if (menuPanel == null)
            BuildRuntimeMenu();

        BindButtons();
        ShowMenu();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    public void ShowMenu()
    {
        if (gameManager != null)
            gameManager.PrepareForMenu();

        SetGameplayUiVisible(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void StartGame()
    {
        if (gameManager == null)
        {
            Debug.LogError("StartMenuController requires a PrototypeGameManager.", this);
            return;
        }

        if (menuPanel != null)
            menuPanel.SetActive(false);

        SetGameplayUiVisible(true);
        gameManager.StartGame();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void SetGameplayUiVisible(bool isVisible)
    {
        if (gameplayUiObjects == null)
            return;

        foreach (GameObject gameplayUiObject in gameplayUiObjects)
        {
            if (gameplayUiObject != null)
                gameplayUiObject.SetActive(isVisible);
        }
    }

    private void BuildRuntimeMenu()
    {
        Canvas gameplayCanvas = FindGameplayCanvas();
        if (gameplayUiObjects == null || gameplayUiObjects.Length == 0)
        {
            gameplayUiObjects = gameplayCanvas != null
                ? new[] { gameplayCanvas.gameObject }
                : new GameObject[0];
        }

        GameObject canvasObject = new GameObject(
            "StartMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuPanel = CreatePanel(canvasObject.transform);
        CreateText(
            menuPanel.transform,
            "Title",
            "BACH KHOA AFTER DARK",
            82f,
            FontStyles.Bold,
            new Color(0.75f, 0.95f, 0.78f),
            new Vector2(0.5f, 0.73f),
            new Vector2(900f, 120f));
        CreateText(
            menuPanel.transform,
            "Subtitle",
            "OBSERVATION SHIFT  |  00:00 - 06:00",
            28f,
            FontStyles.Normal,
            new Color(0.56f, 0.72f, 0.6f),
            new Vector2(0.5f, 0.62f),
            new Vector2(900f, 60f));

        startButton = CreateButton(
            menuPanel.transform,
            "StartButton",
            "START SHIFT",
            new Vector2(0.5f, 0.43f));
        quitButton = CreateButton(
            menuPanel.transform,
            "QuitButton",
            "QUIT",
            new Vector2(0.5f, 0.31f));

        CreateText(
            menuPanel.transform,
            "Hint",
            "Monitor every room. Report changes before the system becomes unstable.",
            22f,
            FontStyles.Italic,
            new Color(0.48f, 0.58f, 0.5f),
            new Vector2(0.5f, 0.14f),
            new Vector2(1100f, 50f));
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject(
            "StartMenuPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.02f, 0.97f);
        return panel;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchor)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 82f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.16f, 0.11f, 0.96f);
        colors.highlightedColor = new Color(0.13f, 0.3f, 0.19f, 1f);
        colors.pressedColor = new Color(0.05f, 0.11f, 0.07f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        buttonObject.GetComponent<Image>().color = colors.normalColor;

        TMP_Text text = CreateText(
            buttonObject.transform,
            "Label",
            label,
            32f,
            FontStyles.Bold,
            new Color(0.76f, 1f, 0.8f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        Vector2 anchor,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Canvas FindGameplayCanvas()
    {
        Canvas[] canvases =
            UnityObjectFinder.FindAllIncludingInactive<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "Canvas")
                return canvas;
        }

        return null;
    }
}
