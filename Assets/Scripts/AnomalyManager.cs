using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    private const string TooManyUnreportedAnomaliesReason =
        "TOO MANY ANOMALIES WERE LEFT UNREPORTED";

    private const string TooManyWrongReportsReason =
        "TOO MANY FALSE REPORTS WERE SUBMITTED";

    [Header("References")]
    public CameraManager cameraManager;

    [Tooltip("Optional data overrides matched by anomaly ID.")]
    [SerializeField]
    private AnomalyDatabase anomalyDatabase;

    [Tooltip("Optional difficulty curves evaluated from 00:00 to 06:00.")]
    [SerializeField]
    private AnomalyDifficultyProfile difficultyProfile;

    [Header("Anomaly List")]
    public List<SingleRoomAnomaly> anomalies = new List<SingleRoomAnomaly>();

    [Header("Spawn Scheduler")]
    [Min(0.01f)]
    public float minSpawnInterval = 10f;

    [Min(0.01f)]
    public float maxSpawnInterval = 20f;

    [Tooltip("Prevents anomalies from spawning in the currently observed room.")]
    public bool preventSpawnInCurrentRoom = true;

    [Header("Active Limits")]
    [Min(1)]
    public int maxActiveAnomalies = 4;

    [Header("Type Weight Scaling")]
    [Tooltip("Multiplier applied to Intruder spawn weights from 00:00 to 06:00.")]
    [SerializeField]
    private AnimationCurve intruderWeightMultiplier =
        new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.5f, 0.8f),
            new Keyframe(1f, 2.1f));

    [Header("Lose Conditions")]
    [Min(0)]
    [Tooltip("Game over countdown starts while active anomalies are above this count.")]
    [SerializeField]
    private int unresolvedAnomalyThreshold = 3;

    [Min(0f)]
    [Tooltip("Game minutes allowed while anomaly count stays above the threshold.")]
    [SerializeField]
    private float unresolvedAnomalyGraceGameMinutes = 30f;

    [Min(0)]
    [Tooltip("Game over triggers when wrong reports exceed this amount.")]
    [SerializeField]
    private int maxWrongReports = 10;

    [Header("Debug")]
    public bool enableDebugLog = true;

    public event Action<int> OnActiveAnomalyCountChanged;
    public event Action<string> OnLoseConditionTriggered;
    public event Action OnTooManyAnomalies;

    private readonly List<SingleRoomAnomaly> activeAnomalies =
        new List<SingleRoomAnomaly>();

    private readonly List<WeightedCandidate> candidates =
        new List<WeightedCandidate>();

    private Coroutine spawnCoroutine;
    private bool isRunning;
    private float difficultyProgress;
    private float currentGameMinutes;
    private float unresolvedPressureStartedAt = -1f;
    private int wrongReportCount;
    private bool loseConditionTriggered;

    public void Begin()
    {
        ResetAllAnomalies();
        difficultyProgress = 0f;
        wrongReportCount = 0;
        unresolvedPressureStartedAt = -1f;
        loseConditionTriggered = false;
        isRunning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void Stop()
    {
        isRunning = false;

        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    public void SetDifficultyProgress(float normalizedProgress)
    {
        difficultyProgress = Mathf.Clamp01(normalizedProgress);
    }

    public void SetGameElapsedMinutes(float gameMinutes)
    {
        currentGameMinutes = Mathf.Max(0f, gameMinutes);
        UpdateUnresolvedAnomalyPressure();
    }

    public void ResetAllAnomalies()
    {
        activeAnomalies.Clear();
        wrongReportCount = 0;
        unresolvedPressureStartedAt = -1f;
        loseConditionTriggered = false;

        foreach (SingleRoomAnomaly anomaly in anomalies)
        {
            if (anomaly == null)
                continue;

            anomaly.Deactivate(false, anomalyDatabase);
            anomaly.ClearCooldown();
        }

        OnActiveAnomalyCountChanged?.Invoke(0);
    }

    public int GetActiveAnomalyCount()
    {
        return activeAnomalies.Count;
    }

    public bool ReportAnomaly(string reportedRoomId, AnomalyType reportedType)
    {
        SingleRoomAnomaly matchedAnomaly = null;

        foreach (SingleRoomAnomaly anomaly in activeAnomalies)
        {
            if (anomaly.roomId != reportedRoomId)
                continue;

            if (anomaly.GetAnomalyType(anomalyDatabase) != reportedType)
                continue;

            matchedAnomaly = anomaly;
            break;
        }

        if (matchedAnomaly == null)
        {
            wrongReportCount++;
            Log(
                "Wrong report. Room: " + reportedRoomId +
                " | Type: " + reportedType +
                " | Wrong reports: " + wrongReportCount);

            if (wrongReportCount > maxWrongReports)
            {
                TriggerLoseCondition(TooManyWrongReportsReason);
            }

            return false;
        }

        matchedAnomaly.Deactivate(true, anomalyDatabase);
        activeAnomalies.Remove(matchedAnomaly);

        Log(
            "Correct report. Room: " + reportedRoomId +
            " | Type: " + reportedType);

        unresolvedPressureStartedAt = -1f;
        OnActiveAnomalyCountChanged?.Invoke(activeAnomalies.Count);
        UpdateUnresolvedAnomalyPressure();
        return true;
    }

    private IEnumerator SpawnLoop()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(GetNextSpawnInterval());

            if (isRunning)
                TrySpawnRandomAnomaly();
        }
    }

    private float GetNextSpawnInterval()
    {
        float minimum = Mathf.Max(0.01f, minSpawnInterval);
        float maximum = Mathf.Max(minimum, maxSpawnInterval);
        float interval = UnityEngine.Random.Range(minimum, maximum);

        if (difficultyProfile != null)
        {
            interval *= difficultyProfile.GetSpawnIntervalMultiplier(
                difficultyProgress);
        }

        return Mathf.Max(0.01f, interval);
    }

    private void TrySpawnRandomAnomaly()
    {
        int activeLimit = GetCurrentActiveLimit();

        if (activeAnomalies.Count >= activeLimit)
        {
            UpdateUnresolvedAnomalyPressure();
            return;
        }

        string currentRoomId = cameraManager != null
            ? cameraManager.CurrentRoomId
            : string.Empty;

        float totalWeight = BuildCandidateList(currentRoomId);

        if (candidates.Count == 0 || totalWeight <= 0f)
        {
            Log(
                "No available anomaly to spawn. Current observed room: " +
                currentRoomId);
            return;
        }

        SingleRoomAnomaly selectedAnomaly = SelectWeightedCandidate(totalWeight);

        if (selectedAnomaly == null)
            return;

        selectedAnomaly.Activate();
        activeAnomalies.Add(selectedAnomaly);

        Log(
            "Spawned anomaly: " + selectedAnomaly.anomalyId +
            " | Room: " + selectedAnomaly.roomId +
            " | Type: " + selectedAnomaly.GetAnomalyType(anomalyDatabase) +
            " | Current observed room: " + currentRoomId);

        OnActiveAnomalyCountChanged?.Invoke(activeAnomalies.Count);
        UpdateUnresolvedAnomalyPressure();
    }

    private float BuildCandidateList(string currentRoomId)
    {
        candidates.Clear();
        float totalWeight = 0f;

        foreach (SingleRoomAnomaly anomaly in anomalies)
        {
            if (!IsEligible(anomaly, currentRoomId))
                continue;

            float weight = anomaly.GetSpawnWeight(anomalyDatabase) *
                           GetTypeWeightMultiplier(
                               anomaly.GetAnomalyType(anomalyDatabase));

            if (weight <= 0f)
                continue;

            totalWeight += weight;
            candidates.Add(new WeightedCandidate(anomaly, totalWeight));
        }

        return totalWeight;
    }

    private bool IsEligible(SingleRoomAnomaly anomaly, string currentRoomId)
    {
        if (anomaly == null || anomaly.isActive || anomaly.IsOnCooldown)
            return false;

        if (string.IsNullOrWhiteSpace(anomaly.anomalyId) ||
            string.IsNullOrWhiteSpace(anomaly.roomId))
        {
            return false;
        }

        if (HasActiveAnomalyInRoom(anomaly.roomId))
            return false;

        return !preventSpawnInCurrentRoom ||
               !string.Equals(
                   anomaly.roomId,
                   currentRoomId,
                   StringComparison.Ordinal);
    }

    private bool HasActiveAnomalyInRoom(string roomId)
    {
        foreach (SingleRoomAnomaly activeAnomaly in activeAnomalies)
        {
            if (activeAnomaly == null)
                continue;

            if (string.Equals(
                    activeAnomaly.roomId,
                    roomId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private float GetTypeWeightMultiplier(AnomalyType anomalyType)
    {
        if (anomalyType != AnomalyType.Intruder ||
            intruderWeightMultiplier == null)
        {
            return 1f;
        }

        return Mathf.Max(
            0f,
            intruderWeightMultiplier.Evaluate(
                Mathf.Clamp01(difficultyProgress)));
    }

    private SingleRoomAnomaly SelectWeightedCandidate(float totalWeight)
    {
        float roll = UnityEngine.Random.value * totalWeight;

        foreach (WeightedCandidate candidate in candidates)
        {
            if (roll <= candidate.CumulativeWeight)
                return candidate.Anomaly;
        }

        return candidates[candidates.Count - 1].Anomaly;
    }

    private int GetCurrentActiveLimit()
    {
        int limit = Mathf.Max(1, maxActiveAnomalies);

        if (difficultyProfile != null)
        {
            limit += difficultyProfile.GetAdditionalActiveAnomalies(
                difficultyProgress);
        }

        return limit;
    }

    private void UpdateUnresolvedAnomalyPressure()
    {
        if (!isRunning || loseConditionTriggered)
            return;

        int threshold = Mathf.Max(0, unresolvedAnomalyThreshold);
        if (activeAnomalies.Count <= threshold)
        {
            unresolvedPressureStartedAt = -1f;
            return;
        }

        if (unresolvedPressureStartedAt < 0f)
        {
            unresolvedPressureStartedAt = currentGameMinutes;
        }

        float elapsedPressureMinutes =
            currentGameMinutes - unresolvedPressureStartedAt;

        if (elapsedPressureMinutes >= unresolvedAnomalyGraceGameMinutes)
        {
            TriggerLoseCondition(TooManyUnreportedAnomaliesReason);
        }
    }

    private void TriggerLoseCondition(string reason)
    {
        if (loseConditionTriggered)
            return;

        loseConditionTriggered = true;
        OnLoseConditionTriggered?.Invoke(reason);

        if (reason == TooManyUnreportedAnomaliesReason)
            OnTooManyAnomalies?.Invoke();
    }

    private void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log(message, this);
    }

    private readonly struct WeightedCandidate
    {
        public WeightedCandidate(
            SingleRoomAnomaly anomaly,
            float cumulativeWeight)
        {
            Anomaly = anomaly;
            CumulativeWeight = cumulativeWeight;
        }

        public SingleRoomAnomaly Anomaly { get; }
        public float CumulativeWeight { get; }
    }
}
