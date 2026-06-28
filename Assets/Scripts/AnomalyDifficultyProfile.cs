using UnityEngine;

[CreateAssetMenu(
    fileName = "AnomalyDifficultyProfile",
    menuName = "Observation Duty/Anomaly Difficulty Profile")]
public sealed class AnomalyDifficultyProfile : ScriptableObject
{
    [Header("Spawn Timing")]
    [Tooltip("Multiplier applied to spawn intervals over normalized game progress.")]
    [SerializeField]
    private AnimationCurve spawnIntervalMultiplier =
        AnimationCurve.Linear(0f, 1f, 1f, 0.55f);

    [Header("Spawn Pressure")]
    [Tooltip("Additional active anomaly capacity unlocked over game progress.")]
    [SerializeField]
    private AnimationCurve additionalActiveAnomalies =
        AnimationCurve.Linear(0f, 0f, 1f, 0f);

    public float GetSpawnIntervalMultiplier(float progress)
    {
        return Mathf.Max(0.01f, spawnIntervalMultiplier.Evaluate(Mathf.Clamp01(progress)));
    }

    public int GetAdditionalActiveAnomalies(float progress)
    {
        float value = additionalActiveAnomalies.Evaluate(Mathf.Clamp01(progress));
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
