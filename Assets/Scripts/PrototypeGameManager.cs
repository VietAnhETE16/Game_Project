using TMPro;
using UnityEngine;

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

    private float elapsedTime;
    private bool gameRunning;

    private void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (anomalyManager != null)
        {
            anomalyManager.OnActiveAnomalyCountChanged += UpdateStatusText;
            anomalyManager.OnTooManyAnomalies += GameOver;
        }

        StartGame();
    }

    private void OnDestroy()
    {
        if (anomalyManager != null)
        {
            anomalyManager.OnActiveAnomalyCountChanged -= UpdateStatusText;
            anomalyManager.OnTooManyAnomalies -= GameOver;
        }
    }

    private void Update()
    {
        if (!gameRunning)
            return;

        elapsedTime += Time.deltaTime;
        UpdateClock();

        if (elapsedTime >= realGameDuration)
        {
            WinGame();
        }
    }

    private void StartGame()
    {
        elapsedTime = 0f;
        gameRunning = true;

        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (anomalyManager != null)
            anomalyManager.Begin();

        UpdateClock();
        UpdateStatusText(0);
    }

    private void UpdateClock()
    {
        float progress = Mathf.Clamp01(elapsedTime / realGameDuration);

        int totalGameMinutes = Mathf.FloorToInt(progress * 360f);

        int hour = totalGameMinutes / 60;
        int minute = totalGameMinutes % 60;

        if (clockText != null)
        {
            clockText.text = hour.ToString("00") + ":" + minute.ToString("00") + " AM";
        }
    }

    private void UpdateStatusText(int activeAnomalyCount)
    {
        if (statusText == null)
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
        gameRunning = false;

        if (anomalyManager != null)
            anomalyManager.Stop();

        if (winPanel != null)
            winPanel.SetActive(true);

        Debug.Log("YOU WIN");
    }

    private void GameOver()
    {
        if (!gameRunning)
            return;

        gameRunning = false;

        if (anomalyManager != null)
            anomalyManager.Stop();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("GAME OVER");
    }
}