using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class MonochromePostProcessing : MonoBehaviour
{
    [Header("Monochrome")]
    [Range(-100f, 0f)]
    [SerializeField]
    private float saturation = -100f;

    [Tooltip("Optional volume. The first global Volume in the scene is used when empty.")]
    [SerializeField]
    private Volume targetVolume;

    private VolumeProfile runtimeProfile;

    private void Awake()
    {
        ApplyMonochrome();
    }

    public void ApplyMonochrome()
    {
        if (targetVolume == null)
            targetVolume = FindGlobalVolume();

        if (targetVolume == null)
        {
            Debug.LogWarning("MonochromePostProcessing could not find a global Volume.", this);
            return;
        }

        // Clone the profile so Play Mode never modifies the project asset.
        runtimeProfile = Instantiate(targetVolume.sharedProfile);
        runtimeProfile.name = targetVolume.sharedProfile.name + " (Runtime Monochrome)";
        targetVolume.profile = runtimeProfile;

        if (!runtimeProfile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);

        colorAdjustments.active = true;
        colorAdjustments.saturation.Override(saturation);
    }

    private static Volume FindGlobalVolume()
    {
        Volume[] volumes = UnityObjectFinder.FindAllActive<Volume>();

        foreach (Volume volume in volumes)
        {
            if (volume.isGlobal && volume.sharedProfile != null)
                return volume;
        }

        return null;
    }
}
