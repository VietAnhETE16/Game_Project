using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrototypeGameManager : MonoBehaviour
{
    [Header("References")]
    public AnomalyManager anomalyManager;

    [Header("UI")]
    public TMP_Text clockText;
    public TMP_Text statusText;
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Time Settings")]
    [Tooltip("Thời lượng thật của một ván chơi, tính bằng giây.")]
    public float realGameDuration = 360f;

    [Header("Anomaly Start")]
    [Range(0f, 6f)]
    [Tooltip("Number of in-game hours without anomalies after 00:00.")]
    [SerializeField]
    private float safeGameHours = 1f;

    [Min(0f)]
    [SerializeField]
    private float anomalyStartMessageDuration = 4f;

    [SerializeField]
    private string anomalyStartMessage =
        "WARNING: ANOMALIES HAVE STARTED TO APPEAR";

    [Header("Startup")]
    [Tooltip("Enable for scenes that do not use a start menu.")]
    [SerializeField]
    private bool startAutomatically;

    private float elapsedTime;
    private bool gameRunning;
    private bool anomalyPhaseStarted;
    private bool anomalyStartMessageVisible;
    private Coroutine anomalyStartMessageCoroutine;
    private GameObject endGameOverlay;
    private TMP_Text endGameTitle;
    private TMP_Text endGameSubtitle;
    private Button playAgainButton;

    public bool IsGameRunning => gameRunning;

    private void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        BuildEndGameOverlay();

        if (anomalyManager != null)
        {
            anomalyManager.OnActiveAnomalyCountChanged += UpdateStatusText;
            anomalyManager.OnLoseConditionTriggered += GameOver;
        }

        if (startAutomatically)
            StartGame();
        else
            PrepareForMenu();
    }

    private void OnDestroy()
    {
        if (anomalyManager != null)
        {
            anomalyManager.OnActiveAnomalyCountChanged -= UpdateStatusText;
            anomalyManager.OnLoseConditionTriggered -= GameOver;
        }

        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(StartGame);
    }

    private void Update()
    {
        if (!gameRunning)
            return;

        elapsedTime += Time.deltaTime;

        if (anomalyManager != null)
        {
            if (realGameDuration > 0f)
                anomalyManager.SetDifficultyProgress(elapsedTime / realGameDuration);

            anomalyManager.SetGameElapsedMinutes(GetElapsedGameMinutes());
        }

        TryStartAnomalyPhase();
        UpdateClock();

        if (elapsedTime >= realGameDuration)
        {
            WinGame();
        }
    }

    public void StartGame()
    {
        elapsedTime = 0f;
        gameRunning = true;
        anomalyPhaseStarted = false;
        StopAnomalyStartMessage();
        HideEndGameOverlay();

        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (anomalyManager != null)
        {
            anomalyManager.Stop();
            anomalyManager.ResetAllAnomalies();
            anomalyManager.SetGameElapsedMinutes(0f);
        }

        UpdateClock();
        UpdateStatusText(0);
        TryStartAnomalyPhase();
    }

    public void PrepareForMenu()
    {
        gameRunning = false;
        elapsedTime = 0f;
        anomalyPhaseStarted = false;
        StopAnomalyStartMessage();
        HideEndGameOverlay();

        if (anomalyManager != null)
        {
            anomalyManager.Stop();
            anomalyManager.ResetAllAnomalies();
            anomalyManager.SetGameElapsedMinutes(0f);
        }

        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateClock();
        UpdateStatusText(0);
    }

    private void UpdateClock()
    {
        int totalGameMinutes = Mathf.FloorToInt(GetElapsedGameMinutes());

        int hour = totalGameMinutes / 60;
        int minute = totalGameMinutes % 60;

        if (clockText != null)
        {
            clockText.text = hour.ToString("00") + ":" + minute.ToString("00");
        }
    }

    private void UpdateStatusText(int activeAnomalyCount)
    {
        if (statusText == null || anomalyStartMessageVisible)
            return;

        if (activeAnomalyCount <= 0)
        {
            statusText.text = "System Status: Stable";
        }
        else if (activeAnomalyCount == 1)
        {
            statusText.text = "System Status: Minor Disturbance";
        }
        else if (activeAnomalyCount == 2)
        {
            statusText.text = "System Status: Multiple Anomalies Detected";
        }
        else
        {
            statusText.text = "System Status: Critical";
        }
    }

    private void WinGame()
    {
        if (!gameRunning)
            return;

        gameRunning = false;
        StopAnomalyStartMessage();

        if (anomalyManager != null)
            anomalyManager.Stop();

        ShowEndGameOverlay(
            "SHIFT COMPLETE",
            "YOU SURVIVED UNTIL 06:00",
            new Color(0.25f, 1f, 0.4f, 1f));

        Debug.Log("YOU WIN");
    }

    private void GameOver()
    {
        GameOver("TOO MANY ANOMALIES WERE LEFT UNREPORTED");
    }

    private void GameOver(string subtitle)
    {
        if (!gameRunning)
            return;

        gameRunning = false;
        StopAnomalyStartMessage();

        if (anomalyManager != null)
            anomalyManager.Stop();

        ShowEndGameOverlay(
            "GAME OVER",
            string.IsNullOrWhiteSpace(subtitle)
                ? "TOO MANY ANOMALIES WERE LEFT UNREPORTED"
                : subtitle,
            new Color(1f, 0.2f, 0.2f, 1f));

        Debug.Log("GAME OVER");
    }

    private void TryStartAnomalyPhase()
    {
        if (!gameRunning || anomalyPhaseStarted)
            return;

        float safeDuration = realGameDuration *
                             (Mathf.Clamp(safeGameHours, 0f, 6f) / 6f);

        if (elapsedTime < safeDuration)
            return;

        anomalyPhaseStarted = true;

        if (anomalyManager != null)
            anomalyManager.Begin();

        ShowAnomalyStartMessage();
    }

    private float GetElapsedGameMinutes()
    {
        if (realGameDuration <= 0f)
            return 360f;

        return Mathf.Clamp01(elapsedTime / realGameDuration) * 360f;
    }

    private void ShowAnomalyStartMessage()
    {
        StopAnomalyStartMessage();

        if (statusText == null)
            return;

        anomalyStartMessageVisible = true;
        statusText.text = anomalyStartMessage;

        if (anomalyStartMessageDuration <= 0f)
        {
            anomalyStartMessageVisible = false;
            RefreshStatusText();
            return;
        }

        anomalyStartMessageCoroutine =
            StartCoroutine(HideAnomalyStartMessage());
    }

    private IEnumerator HideAnomalyStartMessage()
    {
        yield return new WaitForSeconds(anomalyStartMessageDuration);

        anomalyStartMessageCoroutine = null;
        anomalyStartMessageVisible = false;
        RefreshStatusText();
    }

    private void StopAnomalyStartMessage()
    {
        if (anomalyStartMessageCoroutine != null)
        {
            StopCoroutine(anomalyStartMessageCoroutine);
            anomalyStartMessageCoroutine = null;
        }

        anomalyStartMessageVisible = false;
    }

    private void RefreshStatusText()
    {
        UpdateStatusText(
            anomalyManager != null
                ? anomalyManager.GetActiveAnomalyCount()
                : 0);
    }

    private void BuildEndGameOverlay()
    {
        if (endGameOverlay != null)
            return;

        endGameOverlay = new GameObject(
            "EndGameOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(Image));

        Canvas canvas = endGameOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = endGameOverlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image background = endGameOverlay.GetComponent<Image>();
        background.color = new Color(0.01f, 0.015f, 0.012f, 0.97f);
        background.raycastTarget = true;

        endGameTitle = CreateEndGameText(
            endGameOverlay.transform,
            "Title",
            new Vector2(0.1f, 0.55f),
            new Vector2(0.9f, 0.75f),
            72f,
            FontStyles.Bold);

        endGameSubtitle = CreateEndGameText(
            endGameOverlay.transform,
            "Subtitle",
            new Vector2(0.15f, 0.43f),
            new Vector2(0.85f, 0.55f),
            30f,
            FontStyles.Normal);

        playAgainButton = CreatePlayAgainButton(endGameOverlay.transform);
        playAgainButton.onClick.AddListener(StartGame);
        endGameOverlay.SetActive(false);
    }

    private static TMP_Text CreateEndGameText(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        FontStyles style)
    {
        GameObject textObject = new GameObject(
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

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreatePlayAgainButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "PlayAgainButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.31f);
        rect.anchorMax = new Vector2(0.5f, 0.31f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 84f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.25f, 0.13f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.48f, 0.24f, 1f);
        colors.pressedColor = new Color(0.05f, 0.16f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        buttonObject.GetComponent<Image>().color = colors.normalColor;

        TMP_Text label = CreateEndGameText(
            buttonObject.transform,
            "Label",
            Vector2.zero,
            Vector2.one,
            32f,
            FontStyles.Bold);
        label.text = "PLAY AGAIN";
        label.color = Color.white;
        return button;
    }

    private void ShowEndGameOverlay(
        string title,
        string subtitle,
        Color titleColor)
    {
        if (endGameOverlay == null)
            BuildEndGameOverlay();

        if (endGameTitle != null)
        {
            endGameTitle.text = title;
            endGameTitle.color = titleColor;
        }

        if (endGameSubtitle != null)
        {
            endGameSubtitle.text = subtitle;
            endGameSubtitle.color = Color.white;
        }

        if (endGameOverlay != null)
            endGameOverlay.SetActive(true);
    }

    private void HideEndGameOverlay()
    {
        if (endGameOverlay != null)
            endGameOverlay.SetActive(false);
    }
}
