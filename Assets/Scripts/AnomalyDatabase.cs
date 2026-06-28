using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AnomalyDatabase",
    menuName = "Observation Duty/Anomaly Database")]
public sealed class AnomalyDatabase : ScriptableObject
{
    [SerializeField]
    private List<AnomalyDefinition> anomalies = new List<AnomalyDefinition>();

    private readonly Dictionary<string, AnomalyDefinition> definitionsById =
        new Dictionary<string, AnomalyDefinition>(StringComparer.Ordinal);

    private bool lookupInitialized;

    public bool TryGetDefinition(string anomalyId, out AnomalyDefinition definition)
    {
        if (!lookupInitialized)
            RebuildLookup();

        if (string.IsNullOrWhiteSpace(anomalyId))
        {
            definition = null;
            return false;
        }

        return definitionsById.TryGetValue(anomalyId, out definition);
    }

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    private void RebuildLookup()
    {
        definitionsById.Clear();

        foreach (AnomalyDefinition definition in anomalies)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.AnomalyId))
                continue;

            if (definitionsById.ContainsKey(definition.AnomalyId))
            {
                Debug.LogWarning(
                    "AnomalyDatabase contains duplicate ID: " + definition.AnomalyId,
                    this);
                continue;
            }

            definitionsById.Add(definition.AnomalyId, definition);
        }

        lookupInitialized = true;
    }
}

[Serializable]
public sealed class AnomalyDefinition
{
    [SerializeField]
    private string anomalyId;

    [SerializeField]
    private AnomalyType anomalyType;

    [Min(0f)]
    [SerializeField]
    private float spawnWeight = 1f;

    [Min(0f)]
    [SerializeField]
    private float cooldownSeconds = 30f;

    public string AnomalyId => anomalyId;
    public AnomalyType AnomalyType => anomalyType;
    public float SpawnWeight => spawnWeight;
    public float CooldownSeconds => cooldownSeconds;
}
