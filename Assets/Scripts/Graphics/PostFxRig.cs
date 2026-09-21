using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    public class PostFxRig : MonoBehaviour
    {
        private Volume volume;
        private Bloom bloom;
        private Vignette vignette;
        private FilmGrain grain;
        private DepthOfField depth;

        private void Start()
        {
            volume = gameObject.GetComponent<Volume>() ?? gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.1f);

            vignette = profile.Add<Vignette>();
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

            grain = profile.Add<FilmGrain>();
            grain.active = false;
            grain.intensity.Override(0.18f);
            grain.type.Override(FilmGrainLookup.Medium1);

            depth = profile.Add<DepthOfField>();
            depth.active = false;
            depth.mode.Override(DepthOfFieldMode.Gaussian);
            depth.gaussianStart.Override(6f);
            depth.gaussianEnd.Override(18f);
            ApplyTier(1, false);
        }

        private void Update()
        {
            int tier = SettingsService.Instance != null ? SettingsService.Instance.Quality : 1;
            bool aiming = PlayerRegistry.Current != null && PlayerRegistry.Current.IsAimingDownSights;
            ApplyTier(tier, aiming);
        }

        private void ApplyTier(int tier, bool aiming)
        {
            if (bloom == null) return;
            var budget = QualityProfile.For(tier);
            bloom.intensity.Override(budget.Bloom);
            vignette.intensity.Override(tier <= 0 ? 0.16f : 0.28f);
            grain.active = budget.Grain;
            depth.active = aiming && budget.DepthOfField;
        }
    }
}
