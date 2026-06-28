using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private TMP_Text cameraNameText;

    [SerializeField]
    private Button previousCameraButton;

    [SerializeField]
    private Button nextCameraButton;

    [Header("Discovery")]
    [Tooltip("When enabled, every active RoomCameraPoint in the scene is discovered automatically.")]
    [SerializeField]
    private bool autoDiscoverRooms = true;

    [Tooltip("Optional explicit room list used when automatic discovery is disabled.")]
    [SerializeField]
    private List<RoomCameraPoint> configuredRooms = new List<RoomCameraPoint>();

    [Header("Transition")]
    [SerializeField]
    private bool useSmoothTransition = true;

    [Min(0.01f)]
    [SerializeField]
    private float transitionDuration = 0.35f;

    [SerializeField]
    private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private bool switchOnStart = true;

    [Header("Visual Effects")]
    [SerializeField]
    private CameraStaticOverlay staticOverlay;

    [Tooltip("Extra time the static remains visible after the camera transition.")]
    [Min(0f)]
    [SerializeField]
    private float staticTailDuration = 0.12f;

    [SerializeField]
    private MonochromePostProcessing monochromePostProcessing;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip cameraSwitchClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float cameraSwitchVolume = 0.75f;

    [Tooltip("Creates a short electronic click when no switch clip is assigned.")]
    [SerializeField]
    private bool useGeneratedFallbackSound = true;

    private readonly List<RoomCameraPoint> rooms = new List<RoomCameraPoint>();
    private int currentRoomIndex = -1;
    private Coroutine transitionCoroutine;
    private AudioClip generatedSwitchClip;

    public event Action<RoomCameraPoint, int> RoomChanged;

    public IReadOnlyList<RoomCameraPoint> Rooms => rooms;
    public int RoomCount => rooms.Count;
    public RoomCameraPoint CurrentRoom =>
        currentRoomIndex >= 0 && currentRoomIndex < rooms.Count ? rooms[currentRoomIndex] : null;
    public string CurrentRoomId => CurrentRoom != null ? CurrentRoom.RoomId : string.Empty;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (staticOverlay == null)
            staticOverlay = GetComponent<CameraStaticOverlay>();

        if (staticOverlay == null)
            staticOverlay = gameObject.AddComponent<CameraStaticOverlay>();

        if (monochromePostProcessing == null)
            monochromePostProcessing = GetComponent<MonochromePostProcessing>();

        if (monochromePostProcessing == null)
            monochromePostProcessing = gameObject.AddComponent<MonochromePostProcessing>();

        BindButtons();
        RefreshRooms();
    }

    private void Start()
    {
        if (!ValidateSetup())
            return;

        if (switchOnStart)
            SwitchToRoom(0, true);
    }

    private void OnDestroy()
    {
        if (previousCameraButton != null)
            previousCameraButton.onClick.RemoveListener(PreviousCamera);

        if (nextCameraButton != null)
            nextCameraButton.onClick.RemoveListener(NextCamera);
    }

    public void RefreshRooms()
    {
        rooms.Clear();

        if (autoDiscoverRooms)
        {
            RoomCameraPoint[] discoveredRooms =
                UnityObjectFinder.FindAllActive<RoomCameraPoint>();
            rooms.AddRange(discoveredRooms);
        }
        else
        {
            foreach (RoomCameraPoint room in configuredRooms)
            {
                if (room != null && !rooms.Contains(room))
                    rooms.Add(room);
            }
        }

        rooms.Sort(CompareRooms);
        ValidateRoomIds();

        if (currentRoomIndex >= rooms.Count)
            currentRoomIndex = rooms.Count - 1;
    }

    public void NextCamera()
    {
        if (rooms.Count == 0)
            return;

        int nextIndex = currentRoomIndex < 0 ? 0 : (currentRoomIndex + 1) % rooms.Count;
        SwitchToRoom(nextIndex);
    }

    public void PreviousCamera()
    {
        if (rooms.Count == 0)
            return;

        int previousIndex = currentRoomIndex <= 0 ? rooms.Count - 1 : currentRoomIndex - 1;
        SwitchToRoom(previousIndex);
    }

    public void SwitchToRoom(int index)
    {
        SwitchToRoom(index, false);
    }

    public bool SwitchToRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            return false;

        int index = rooms.FindIndex(room =>
            string.Equals(room.RoomId, roomId, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return false;

        SwitchToRoom(index);
        return true;
    }

    private void SwitchToRoom(int index, bool instant)
    {
        if (mainCamera == null || index < 0 || index >= rooms.Count)
            return;

        RoomCameraPoint selectedRoom = rooms[index];
        if (selectedRoom == null || selectedRoom.CameraTransform == null)
            return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        currentRoomIndex = index;
        Transform target = selectedRoom.CameraTransform;

        if (!instant)
            staticOverlay.Play(transitionDuration + staticTailDuration);

        if (instant || !useSmoothTransition || transitionDuration <= 0f)
        {
            mainCamera.transform.SetPositionAndRotation(target.position, target.rotation);
            CompleteSwitch(selectedRoom, index, !instant);
            return;
        }

        PlaySwitchSound();
        transitionCoroutine = StartCoroutine(TransitionToRoom(selectedRoom, index));
    }

    private IEnumerator TransitionToRoom(RoomCameraPoint room, int index)
    {
        Transform cameraTransform = mainCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        Transform target = room.CameraTransform;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / transitionDuration);
            float curvedTime = transitionCurve.Evaluate(normalizedTime);

            cameraTransform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPosition, target.position, curvedTime),
                Quaternion.SlerpUnclamped(startRotation, target.rotation, curvedTime));

            yield return null;
        }

        cameraTransform.SetPositionAndRotation(target.position, target.rotation);
        transitionCoroutine = null;
        CompleteSwitch(room, index, false);
    }

    private void CompleteSwitch(RoomCameraPoint room, int index, bool playSound)
    {
        if (playSound)
            PlaySwitchSound();

        if (cameraNameText != null)
            cameraNameText.text = room.DisplayName;

        RoomChanged?.Invoke(room, index);
    }

    private void PlaySwitchSound()
    {
        if (audioSource == null || cameraSwitchVolume <= 0f)
            return;

        AudioClip clip = cameraSwitchClip;
        if (clip == null && useGeneratedFallbackSound)
        {
            if (generatedSwitchClip == null)
                generatedSwitchClip = CreateFallbackSwitchClip();

            clip = generatedSwitchClip;
        }

        if (clip != null)
            audioSource.PlayOneShot(clip, cameraSwitchVolume);
    }

    private AudioClip CreateFallbackSwitchClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.08f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float envelope = 1f - (time / duration);
            float tone = Mathf.Sin(2f * Mathf.PI * 1150f * time);
            samples[i] = tone * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("Generated Camera Switch", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void BindButtons()
    {
        if (previousCameraButton != null)
        {
            previousCameraButton.onClick.RemoveListener(PreviousCamera);
            previousCameraButton.onClick.AddListener(PreviousCamera);
        }

        if (nextCameraButton != null)
        {
            nextCameraButton.onClick.RemoveListener(NextCamera);
            nextCameraButton.onClick.AddListener(NextCamera);
        }
    }

    private bool ValidateSetup()
    {
        if (mainCamera == null)
        {
            Debug.LogError("CameraManager requires one Main Camera.", this);
            return false;
        }

        if (rooms.Count == 0)
        {
            Debug.LogError("CameraManager did not discover any RoomCameraPoint components.", this);
            return false;
        }

        return true;
    }

    private void ValidateRoomIds()
    {
        HashSet<string> roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (RoomCameraPoint room in rooms)
        {
            if (room == null)
                continue;

            if (string.IsNullOrWhiteSpace(room.RoomId))
            {
                Debug.LogWarning($"Room camera point '{room.name}' has an empty Room ID.", room);
                continue;
            }

            if (!roomIds.Add(room.RoomId))
                Debug.LogError($"Duplicate Room ID detected: '{room.RoomId}'.", room);
        }
    }

    private static int CompareRooms(RoomCameraPoint left, RoomCameraPoint right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;

        int orderComparison = left.Order.CompareTo(right.Order);
        return orderComparison != 0
            ? orderComparison
            : string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
}
