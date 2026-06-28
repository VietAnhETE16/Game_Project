using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class AdvancedAnomalyInstaller : MonoBehaviour
{
    [SerializeField]
    private AnomalyManager anomalyManager;

    [SerializeField]
    private List<RoomAnomalySet> rooms = new List<RoomAnomalySet>();

    private bool installed;

    private void Awake()
    {
        Install();
    }

    public void Install()
    {
        if (installed)
            return;

        if (anomalyManager == null)
            anomalyManager = GetComponent<AnomalyManager>();

        if (anomalyManager == null)
        {
            Debug.LogError(
                "AdvancedAnomalyInstaller requires an AnomalyManager.",
                this);
            return;
        }

        foreach (RoomAnomalySet room in rooms)
            InstallRoom(room);

        installed = true;
    }

    private void InstallRoom(RoomAnomalySet room)
    {
        if (room == null ||
            room.parent == null ||
            string.IsNullOrWhiteSpace(room.roomId))
        {
            return;
        }

        foreach (AdvancedAnomalyDefinition definition in room.anomalies)
        {
            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.anomalyId))
            {
                continue;
            }

            if (ContainsAnomaly(definition.anomalyId))
                continue;

            GameObject instance = GetOrCreateInstance(definition, room.parent);
            AdvancedAnomalyBehavior behavior = GetBehavior(definition);

            GameObject[] normalObjects =
                GetNormalObjects(definition, instance, behavior);
            GameObject[] anomalyObjects =
                GetAnomalyObjects(definition, instance, behavior);

            SetInitialObjectStates(normalObjects, anomalyObjects);

            AdvancedAnomalyEffect effect =
                ConfigureEffect(
                    definition,
                    instance,
                    room.parent,
                    room.roomId);

            Transform movableObject = definition.movableObject;
            if (movableObject == null &&
                behavior == AdvancedAnomalyBehavior.MoveObject &&
                instance != null)
            {
                movableObject = instance.transform;
            }

            anomalyManager.anomalies.Add(new SingleRoomAnomaly
            {
                anomalyId = definition.anomalyId,
                roomId = room.roomId,
                anomalyType = definition.anomalyType,
                spawnWeight = definition.spawnWeight,
                cooldownSeconds = definition.cooldownSeconds,
                normalObjects = normalObjects,
                anomalyObjects = anomalyObjects,
                movableObject = movableObject,
                normalPose = definition.normalPose,
                anomalyPose = definition.anomalyPose,
                anomalyEffect = effect
            });
        }
    }

    private static AdvancedAnomalyBehavior GetBehavior(
        AdvancedAnomalyDefinition definition)
    {
        if (definition.behavior != AdvancedAnomalyBehavior.Automatic)
            return definition.behavior;

        switch (definition.anomalyType)
        {
            case AnomalyType.ObjectMovement:
                return AdvancedAnomalyBehavior.MoveObject;

            case AnomalyType.ObjectDisappearance:
                return AdvancedAnomalyBehavior.Disappear;

            case AnomalyType.PaintingChange:
                return HasObjects(definition.normalObjects) &&
                       HasObjects(definition.anomalyObjects)
                    ? AdvancedAnomalyBehavior.SwapObjects
                    : AdvancedAnomalyBehavior.Appear;

            default:
                return definition.disappearOnActivation
                    ? AdvancedAnomalyBehavior.Disappear
                    : AdvancedAnomalyBehavior.Appear;
        }
    }

    private static GameObject[] GetNormalObjects(
        AdvancedAnomalyDefinition definition,
        GameObject instance,
        AdvancedAnomalyBehavior behavior)
    {
        if (HasObjects(definition.normalObjects))
            return definition.normalObjects;

        return behavior == AdvancedAnomalyBehavior.Disappear &&
               instance != null
            ? new[] { instance }
            : null;
    }

    private static GameObject[] GetAnomalyObjects(
        AdvancedAnomalyDefinition definition,
        GameObject instance,
        AdvancedAnomalyBehavior behavior)
    {
        if (HasObjects(definition.anomalyObjects))
            return definition.anomalyObjects;

        return behavior == AdvancedAnomalyBehavior.Appear &&
               instance != null
            ? new[] { instance }
            : null;
    }

    private static bool HasObjects(GameObject[] objects)
    {
        return objects != null && objects.Length > 0;
    }

    private static void SetInitialObjectStates(
        GameObject[] normalObjects,
        GameObject[] anomalyObjects)
    {
        SetActive(normalObjects, true);
        SetActive(anomalyObjects, false);
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject target in objects)
        {
            if (target != null)
                target.SetActive(active);
        }
    }

    private static AdvancedAnomalyEffect ConfigureEffect(
        AdvancedAnomalyDefinition definition,
        GameObject instance,
        Transform parent,
        string roomId)
    {
        if (definition.effectMode == AdvancedAnomalyEffectMode.None)
            return null;

        GameObject effectHost = definition.effectHost != null
            ? definition.effectHost
            : instance;

        if (effectHost == null)
            effectHost = CreateRuntimeEffectHost(definition, parent);

        AdvancedAnomalyEffect effect =
            effectHost.GetComponent<AdvancedAnomalyEffect>();
        if (effect == null)
            effect = effectHost.AddComponent<AdvancedAnomalyEffect>();

        effect.Configure(
            definition.effectMode,
            definition.effectSpeed,
            definition.effectInterval,
            definition.rotationAxis,
            roomId);
        effect.enabled = false;
        return effect;
    }

    private static GameObject CreateRuntimeEffectHost(
        AdvancedAnomalyDefinition definition,
        Transform parent)
    {
        string objectName = string.IsNullOrWhiteSpace(definition.anomalyId)
            ? "RuntimeAnomalyEffect"
            : definition.anomalyId + "_EffectHost";

        GameObject effectHost = new GameObject(objectName);
        effectHost.hideFlags = HideFlags.DontSave;

        if (parent != null)
            effectHost.transform.SetParent(parent, false);

        effectHost.transform.localPosition = Vector3.zero;
        effectHost.transform.localRotation = Quaternion.identity;
        effectHost.transform.localScale = Vector3.one;
        return effectHost;
    }

    private static GameObject GetOrCreateInstance(
        AdvancedAnomalyDefinition definition,
        Transform parent)
    {
        if (definition.sceneObject != null)
            return definition.sceneObject;

        if (definition.prefab == null)
            return null;

        GameObject instance = Instantiate(definition.prefab, parent, false);
        instance.name = definition.anomalyId;
        instance.hideFlags = HideFlags.DontSave;
        instance.transform.localPosition = definition.localPosition;
        instance.transform.localEulerAngles = definition.localEulerAngles;
        instance.transform.localScale = definition.localScale;
        return instance;
    }

    private bool ContainsAnomaly(string anomalyId)
    {
        foreach (SingleRoomAnomaly anomaly in anomalyManager.anomalies)
        {
            if (anomaly != null &&
                string.Equals(
                    anomaly.anomalyId,
                    anomalyId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public sealed class RoomAnomalySet
{
    public string roomId;
    public Transform parent;
    public List<AdvancedAnomalyDefinition> anomalies =
        new List<AdvancedAnomalyDefinition>();
}

[Serializable]
public sealed class AdvancedAnomalyDefinition
{
    public string anomalyId;
    public AnomalyType anomalyType;

    [Tooltip("Existing anomaly object stored in the scene hierarchy.")]
    public GameObject sceneObject;

    [Tooltip("Fallback prefab used when no scene object is assigned.")]
    public GameObject prefab;

    [Min(0f)]
    public float spawnWeight = 1f;

    [Min(0f)]
    public float cooldownSeconds = 30f;

    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    [Header("Behavior")]
    [Tooltip("Automatic selects behavior from Anomaly Type.")]
    public AdvancedAnomalyBehavior behavior;

    [Tooltip("Objects visible normally and hidden during the anomaly.")]
    public GameObject[] normalObjects;

    [Tooltip("Objects hidden normally and visible during the anomaly.")]
    public GameObject[] anomalyObjects;

    [Tooltip("Object moved by Object Movement.")]
    public Transform movableObject;

    [Tooltip("Transform used when the movement anomaly is inactive.")]
    public Transform normalPose;

    [Tooltip("Transform used when the movement anomaly is active.")]
    public Transform anomalyPose;

    [Tooltip("Legacy shortcut. Prefer Behavior = Disappear.")]
    public bool disappearOnActivation;

    [Header("Optional Effect")]
    [Tooltip("Optional object that receives the effect component.")]
    public GameObject effectHost;

    public AdvancedAnomalyEffectMode effectMode;

    [Min(0f)]
    public float effectSpeed = 30f;

    [Min(0.05f)]
    public float effectInterval = 0.2f;

    public Vector3 rotationAxis = Vector3.up;
}

public enum AdvancedAnomalyBehavior
{
    Automatic,
    Appear,
    Disappear,
    SwapObjects,
    MoveObject
}
