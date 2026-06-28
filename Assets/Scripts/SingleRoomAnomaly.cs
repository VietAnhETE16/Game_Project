using UnityEngine;

[System.Serializable]
public class SingleRoomAnomaly
{
    [Header("Basic Info")]
    public string anomalyId;

    [Tooltip("Room ID phải trùng với roomId trong CameraManager.")]
    public string roomId;

    public AnomalyType anomalyType;

    [Header("Spawn Rules")]
    [Min(0f)]
    [Tooltip("Relative chance of selecting this anomaly when it is eligible.")]
    public float spawnWeight = 1f;

    [Min(0f)]
    [Tooltip("Seconds before this anomaly may spawn again after deactivation.")]
    public float cooldownSeconds = 30f;

    [Header("Object Toggle")]
    public GameObject[] normalObjects;
    public GameObject[] anomalyObjects;

    [Header("Movement Anomaly")]
    public Transform movableObject;
    public Transform normalPose;
    public Transform anomalyPose;

    [Header("Optional Effect")]
    public AdvancedAnomalyEffect anomalyEffect;

    [HideInInspector]
    public bool isActive;

    [System.NonSerialized]
    private float cooldownEndsAt;

    [System.NonSerialized]
    private bool hasCachedMovablePose;

    [System.NonSerialized]
    private Transform cachedMovableParent;

    [System.NonSerialized]
    private Vector3 cachedMovableLocalPosition;

    [System.NonSerialized]
    private Quaternion cachedMovableLocalRotation;

    [System.NonSerialized]
    private Vector3 cachedMovableLocalScale;

    public bool IsOnCooldown => Time.time < cooldownEndsAt;

    public float GetSpawnWeight(AnomalyDatabase database)
    {
        if (TryGetDefinition(database, out AnomalyDefinition definition))
            return Mathf.Max(0f, definition.SpawnWeight);

        return spawnWeight > 0f ? spawnWeight : 1f;
    }

    public float GetCooldownSeconds(AnomalyDatabase database)
    {
        if (TryGetDefinition(database, out AnomalyDefinition definition))
            return Mathf.Max(0f, definition.CooldownSeconds);

        return Mathf.Max(0f, cooldownSeconds);
    }

    public AnomalyType GetAnomalyType(AnomalyDatabase database)
    {
        if (TryGetDefinition(database, out AnomalyDefinition definition))
            return definition.AnomalyType;

        return anomalyType;
    }

    public void Activate()
    {
        isActive = true;

        if (normalObjects != null)
        {
            foreach (GameObject obj in normalObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (anomalyObjects != null)
        {
            foreach (GameObject obj in anomalyObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        CacheMovablePose();

        if (movableObject != null && anomalyPose != null)
        {
            MoveToPose(anomalyPose);
        }

        if (anomalyEffect != null)
            anomalyEffect.enabled = true;
    }

    public void Deactivate()
    {
        Deactivate(false, null);
    }

    public void Deactivate(bool startCooldown, AnomalyDatabase database)
    {
        isActive = false;

        if (normalObjects != null)
        {
            foreach (GameObject obj in normalObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (anomalyObjects != null)
        {
            foreach (GameObject obj in anomalyObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (movableObject != null)
        {
            RestoreMovablePose();
        }

        if (anomalyEffect != null)
            anomalyEffect.enabled = false;

        cooldownEndsAt = startCooldown
            ? Time.time + GetCooldownSeconds(database)
            : 0f;
    }

    public void ClearCooldown()
    {
        cooldownEndsAt = 0f;
    }

    private void CacheMovablePose()
    {
        if (movableObject == null || hasCachedMovablePose)
            return;

        cachedMovableParent = movableObject.parent;
        cachedMovableLocalPosition = movableObject.localPosition;
        cachedMovableLocalRotation = movableObject.localRotation;
        cachedMovableLocalScale = movableObject.localScale;
        hasCachedMovablePose = true;
    }

    private void MoveToPose(Transform targetPose)
    {
        if (movableObject.parent == targetPose.parent)
        {
            movableObject.localPosition = targetPose.localPosition;
            movableObject.localRotation = targetPose.localRotation;
            return;
        }

        movableObject.SetPositionAndRotation(
            targetPose.position,
            targetPose.rotation);
    }

    private void RestoreMovablePose()
    {
        if (hasCachedMovablePose)
        {
            if (movableObject.parent == cachedMovableParent)
            {
                movableObject.localPosition = cachedMovableLocalPosition;
                movableObject.localRotation = cachedMovableLocalRotation;
                movableObject.localScale = cachedMovableLocalScale;
                return;
            }

            Vector3 worldPosition = cachedMovableParent != null
                ? cachedMovableParent.TransformPoint(cachedMovableLocalPosition)
                : cachedMovableLocalPosition;
            Quaternion worldRotation = cachedMovableParent != null
                ? cachedMovableParent.rotation * cachedMovableLocalRotation
                : cachedMovableLocalRotation;

            movableObject.SetPositionAndRotation(worldPosition, worldRotation);
            movableObject.localScale = cachedMovableLocalScale;
            return;
        }

        if (normalPose != null)
            MoveToPose(normalPose);
    }

    private bool TryGetDefinition(
        AnomalyDatabase database,
        out AnomalyDefinition definition)
    {
        if (database != null)
            return database.TryGetDefinition(anomalyId, out definition);

        definition = null;
        return false;
    }
}
