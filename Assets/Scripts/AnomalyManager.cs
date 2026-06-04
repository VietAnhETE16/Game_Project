using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    [Header("Anomaly List")]
    public List<SingleRoomAnomaly> anomalies = new List<SingleRoomAnomaly>();

    [Header("Spawn Settings")]
    public float minSpawnInterval = 10f;
    public float maxSpawnInterval = 20f;

    [Header("Lose Condition")]
    public int maxActiveAnomalies = 4;

    [Header("Debug")]
    public bool enableDebugLog = true;

    public event Action<int> OnActiveAnomalyCountChanged;
    public event Action OnTooManyAnomalies;

    private readonly List<SingleRoomAnomaly> activeAnomalies = new List<SingleRoomAnomaly>();
    private Coroutine spawnCoroutine;
    private bool isRunning;

    public void Begin()
    {
        ResetAllAnomalies();

        isRunning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void Stop()
    {
        isRunning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void ResetAllAnomalies()
    {
        activeAnomalies.Clear();

        foreach (SingleRoomAnomaly anomaly in anomalies)
        {
            if (anomaly != null)
                anomaly.Deactivate();
        }

        OnActiveAnomalyCountChanged?.Invoke(activeAnomalies.Count);
    }

    private IEnumerator SpawnLoop()
    {
        while (isRunning)
        {
            float waitTime = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            if (isRunning)
                TrySpawnRandomAnomaly();
        }
    }

    private void TrySpawnRandomAnomaly()
    {
        List<SingleRoomAnomaly> candidates = new List<SingleRoomAnomaly>();

        foreach (SingleRoomAnomaly anomaly in anomalies)
        {
            if (anomaly != null && !anomaly.isActive)
            {
                candidates.Add(anomaly);
            }
        }

        if (candidates.Count == 0)
        {
            if (enableDebugLog)
                Debug.Log("No available anomaly to spawn.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        SingleRoomAnomaly selectedAnomaly = candidates[randomIndex];

        selectedAnomaly.Activate();
        activeAnomalies.Add(selectedAnomaly);

        if (enableDebugLog)
        {
            Debug.Log("Spawned anomaly: " + selectedAnomaly.anomalyId 
                + " | Type: " + selectedAnomaly.anomalyType);
        }

        OnActiveAnomalyCountChanged?.Invoke(activeAnomalies.Count);

        if (activeAnomalies.Count >= maxActiveAnomalies)
        {
            OnTooManyAnomalies?.Invoke();
        }
    }

    public bool ReportAnomaly(AnomalyType reportedType)
    {
        SingleRoomAnomaly matchedAnomaly = null;

        foreach (SingleRoomAnomaly anomaly in activeAnomalies)
        {
            if (anomaly.anomalyType == reportedType)
            {
                matchedAnomaly = anomaly;
                break;
            }
        }

        if (matchedAnomaly == null)
        {
            if (enableDebugLog)
                Debug.Log("Wrong report: " + reportedType);

            return false;
        }

        matchedAnomaly.Deactivate();
        activeAnomalies.Remove(matchedAnomaly);

        if (enableDebugLog)
            Debug.Log("Correct report: " + reportedType);

        OnActiveAnomalyCountChanged?.Invoke(activeAnomalies.Count);

        return true;
    }

    public int GetActiveAnomalyCount()
    {
        return activeAnomalies.Count;
    }
}