using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OutpostZero.Graphics
{
    public class PostFxRig : MonoBehaviour
    {
        private Volume volume;

        private void Start()
        {
            volume = gameObject.GetComponent<Volume>() ?? gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.1f);

            var vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.4f);

            var tone = profile.Add<Tonemapping>();
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>();
            color.active = true;
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));
        }
    }
}
