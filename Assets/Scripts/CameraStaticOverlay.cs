using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CameraStaticOverlay : MonoBehaviour
{
    [Header("Noise")]
    [Min(32)]
    [SerializeField]
    private int textureSize = 192;

    [Range(0f, 1f)]
    [SerializeField]
    private float opacity = 0.92f;

    [Min(0.01f)]
    [SerializeField]
    private float frameInterval = 0.035f;

    [Range(0f, 1f)]
    [SerializeField]
    private float brightPixelChance = 0.12f;

    [Header("Presentation")]
    [SerializeField]
    [Tooltip("Keep camera noise behind HUD and report canvases.")]
    private int sortingOrder = -10;

    private Canvas overlayCanvas;
    private RawImage noiseImage;
    private Texture2D noiseTexture;
    private Color32[] pixels;
    private Coroutine playbackCoroutine;

    private void Awake()
    {
        BuildOverlay();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (noiseTexture != null)
            Destroy(noiseTexture);
    }

    public void Play(float duration)
    {
        if (duration <= 0f)
            return;

        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        playbackCoroutine = StartCoroutine(PlayNoise(duration));
    }

    private IEnumerator PlayNoise(float duration)
    {
        SetVisible(true);
        float elapsed = 0f;
        WaitForSecondsRealtime frameDelay = new WaitForSecondsRealtime(frameInterval);

        while (elapsed < duration)
        {
            UpdateNoiseTexture();
            yield return frameDelay;
            elapsed += frameInterval;
        }

        SetVisible(false);
        playbackCoroutine = null;
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject(
            "CameraStaticCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject(
            "StaticNoise",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        noiseImage = imageObject.GetComponent<RawImage>();
        noiseImage.raycastTarget = false;
        noiseImage.color = new Color(1f, 1f, 1f, opacity);

        noiseTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Runtime Camera Static",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        pixels = new Color32[textureSize * textureSize];
        noiseImage.texture = noiseTexture;
    }

    private void UpdateNoiseTexture()
    {
        byte horizontalBand = (byte)Random.Range(15, 70);
        int bandStart = Random.Range(0, textureSize);
        int bandHeight = Random.Range(1, Mathf.Max(2, textureSize / 18));

        for (int y = 0; y < textureSize; y++)
        {
            bool isBand = y >= bandStart && y < bandStart + bandHeight;

            for (int x = 0; x < textureSize; x++)
            {
                byte value = Random.value < brightPixelChance
                    ? (byte)Random.Range(190, 256)
                    : (byte)Random.Range(0, 150);

                if (isBand)
                    value = (byte)Mathf.Min(255, value + horizontalBand);

                pixels[(y * textureSize) + x] = new Color32(value, value, value, 255);
            }
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false, false);
        noiseImage.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
    }

    private void SetVisible(bool isVisible)
    {
        if (overlayCanvas != null)
            overlayCanvas.gameObject.SetActive(isVisible);
    }
}
