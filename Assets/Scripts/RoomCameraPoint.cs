using UnityEngine;

[DisallowMultipleComponent]
public class RoomCameraPoint : MonoBehaviour
{
    [Header("Room Identity")]
    [Tooltip("Stable identifier used by anomaly and report systems.")]
    [SerializeField]
    private string roomId;

    [SerializeField]
    private string displayName;

    [Tooltip("Rooms are displayed and navigated in ascending order.")]
    [SerializeField]
    private int order;

    [Header("Camera")]
    [Tooltip("Optional camera pose. This object's transform is used when empty.")]
    [SerializeField]
    private Transform cameraTransform;

    public string RoomId => roomId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public int Order => order;
    public Transform CameraTransform => cameraTransform != null ? cameraTransform : transform;

    public void Configure(string id, string roomDisplayName, int roomOrder, Transform cameraPose = null)
    {
        roomId = id?.Trim();
        displayName = roomDisplayName?.Trim();
        order = roomOrder;
        cameraTransform = cameraPose != null ? cameraPose : transform;
    }

    private void OnValidate()
    {
        roomId = roomId?.Trim();
        displayName = displayName?.Trim();
    }
}
