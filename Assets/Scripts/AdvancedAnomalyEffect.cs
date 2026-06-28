using System.Collections;
using UnityEngine;

public enum AdvancedAnomalyEffectMode
{
    None,
    Rotate,
    FlickerLights,
    PulseRenderers,
    CameraStatic,
    TintRed,
    TintBlack
}

[DisallowMultipleComponent]
public sealed class AdvancedAnomalyEffect : MonoBehaviour
{
    private AdvancedAnomalyEffectMode mode;
    private float speed;
    private float interval;
    private Vector3 rotationAxis;
    private Quaternion initialRotation;
    private Light[] lights;
    private Renderer[] renderers;
    private CameraStaticOverlay staticOverlay;
    private CameraManager cameraManager;
    private string roomId;
    private Coroutine effectCoroutine;

    public void Configure(
        AdvancedAnomalyEffectMode effectMode,
        float effectSpeed,
        float effectInterval,
        Vector3 effectRotationAxis,
        string effectRoomId)
    {
        mode = effectMode;
        speed = Mathf.Max(0f, effectSpeed);
        interval = Mathf.Max(0.05f, effectInterval);
        rotationAxis = effectRotationAxis.sqrMagnitude > 0f
            ? effectRotationAxis.normalized
            : Vector3.up;
        roomId = effectRoomId;

        initialRotation = transform.localRotation;
        lights = GetComponentsInChildren<Light>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        if (mode == AdvancedAnomalyEffectMode.None)
            return;

        if (mode == AdvancedAnomalyEffectMode.CameraStatic)
        {
            staticOverlay =
                UnityObjectFinder.FindAnyActive<CameraStaticOverlay>();
            cameraManager = UnityObjectFinder.FindAnyActive<CameraManager>();
        }

        if (mode == AdvancedAnomalyEffectMode.TintRed)
            ApplyTint(new Color(0.45f, 0.01f, 0.01f, 1f));

        if (mode == AdvancedAnomalyEffectMode.TintBlack)
            ApplyTint(new Color(0.005f, 0.005f, 0.005f, 1f));

        effectCoroutine = StartCoroutine(RunEffect());
    }

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        transform.localRotation = initialRotation;
        SetLights(true);
        SetRenderers(true);
        ClearTint();
    }

    private IEnumerator RunEffect()
    {
        switch (mode)
        {
            case AdvancedAnomalyEffectMode.Rotate:
                yield return RotateContinuously();
                break;

            case AdvancedAnomalyEffectMode.FlickerLights:
                yield return FlickerLights();
                break;

            case AdvancedAnomalyEffectMode.PulseRenderers:
                yield return PulseRenderers();
                break;

            case AdvancedAnomalyEffectMode.CameraStatic:
                yield return PlayCameraStatic();
                break;

            case AdvancedAnomalyEffectMode.TintRed:
            case AdvancedAnomalyEffectMode.TintBlack:
                yield break;
        }
    }

    private IEnumerator RotateContinuously()
    {
        while (true)
        {
            transform.Rotate(rotationAxis, speed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    private IEnumerator FlickerLights()
    {
        WaitForSeconds delay = new WaitForSeconds(interval);
        bool enabledState = false;

        while (true)
        {
            SetLights(enabledState);
            enabledState = !enabledState;
            yield return delay;
        }
    }

    private IEnumerator PulseRenderers()
    {
        WaitForSeconds delay = new WaitForSeconds(interval);
        bool enabledState = false;

        while (true)
        {
            SetRenderers(enabledState);
            enabledState = !enabledState;
            yield return delay;
        }
    }

    private IEnumerator PlayCameraStatic()
    {
        WaitForSeconds delay = new WaitForSeconds(interval);

        while (true)
        {
            if (staticOverlay != null &&
                (cameraManager == null || cameraManager.CurrentRoomId == roomId))
            {
                staticOverlay.Play(Mathf.Max(interval * 0.8f, 0.1f));
            }

            yield return delay;
        }
    }

    private void SetLights(bool enabledState)
    {
        if (lights == null)
            return;

        foreach (Light targetLight in lights)
        {
            if (targetLight != null)
                targetLight.enabled = enabledState;
        }
    }

    private void SetRenderers(bool enabledState)
    {
        if (renderers == null)
            return;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
                targetRenderer.enabled = enabledState;
        }
    }

    private void ApplyTint(Color color)
    {
        if (renderers == null)
            return;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
                targetRenderer.SetPropertyBlock(properties);
        }
    }

    private void ClearTint()
    {
        if (renderers == null)
            return;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
                targetRenderer.SetPropertyBlock(null);
        }
    }
}
