using UnityEngine;

[System.Serializable]
public class SingleRoomAnomaly
{
    [Header("Basic Info")]
    public string anomalyId;
    public AnomalyType anomalyType;

    [Header("Object Toggle")]
    public GameObject[] normalObjects;
    public GameObject[] anomalyObjects;

    [Header("Movement Anomaly")]
    public Transform movableObject;
    public Transform normalPose;
    public Transform anomalyPose;

    [HideInInspector]
    public bool isActive;

    public void Activate()
    {
        isActive = true;

        foreach (GameObject obj in normalObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in anomalyObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        if (movableObject != null && anomalyPose != null)
        {
            movableObject.position = anomalyPose.position;
            movableObject.rotation = anomalyPose.rotation;
        }
    }

    public void Deactivate()
    {
        isActive = false;

        foreach (GameObject obj in normalObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        foreach (GameObject obj in anomalyObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        if (movableObject != null && normalPose != null)
        {
            movableObject.position = normalPose.position;
            movableObject.rotation = normalPose.rotation;
        }
    }
}