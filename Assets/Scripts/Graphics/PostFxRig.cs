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
        private Volume aimDepth;
        private ColorAdjustments color;
        private MotionBlur blur;

        private void Start()
        {
            volume = Attach.Ensure<Volume>(gameObject);
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

            color = profile.Add<ColorAdjustments>();
            color.active = true;
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));

            grain = profile.Add<FilmGrain>();
            grain.active = false;
            grain.intensity.Override(0.18f);
            grain.type.Override(FilmGrainLookup.Medium1);

            blur = profile.Add<MotionBlur>();
            blur.active = false;
            blur.intensity.Override(0.35f);

            var aimHost = new GameObject("AimDepthVolume");
            aimHost.transform.SetParent(transform, false);
            aimDepth = aimHost.AddComponent<Volume>();
            aimDepth.isGlobal = true;
            aimDepth.priority = 21f;
            aimDepth.weight = 0f;
            var aimProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            aimDepth.sharedProfile = aimProfile;
            depth = aimProfile.Add<DepthOfField>();
            depth.active = true;
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

        /// <summary>Depth-of-field volume weight: follows the ADS camera blend, or snaps with aiming when no rig runs.</summary>
        public static float AimDepth(bool tierAllows, bool aiming, bool rig, float blend)
        {
            if (!tierAllows) return 0f;
            if (!rig) return aiming ? 1f : 0f;
            return blend < 0f ? 0f : blend > 1f ? 1f : blend;
        }

        private void ApplyTier(int tier, bool aiming)
        {
            if (bloom == null) return;
            var budget = QualityProfile.For(tier);
            bool raid = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.RaidActive;
            bool poisoned = false;
            if (PlayerRegistry.Current != null)
            {
                var effects = PlayerRegistry.Current.GetComponent<OutpostZero.Player.StatusEffectController>();
                poisoned = effects != null && effects.IsPoisoned;
            }
            bool motion = SettingsService.Instance != null && SettingsService.Instance.MotionBlur;
            bool flash = BoltGlare.Live(Sensory.StormCover.Bolt, Time.time);
            bloom.intensity.Override(budget.Bloom);
            vignette.intensity.Override(PoisonVeil.Shade(RaidGrade.Vignette(raid, tier), poisoned));
            grain.active = raid || budget.Grain;
            if (aimDepth != null) aimDepth.weight = AimDepth(budget.DepthOfField, aiming, ExpeditionCameraRig.Instance != null, ExpeditionCameraRig.AimWeight);
            if (color != null)
            {
                float bright = SettingsService.Instance != null ? SettingsService.Instance.Brightness : 1f;
                color.postExposure.Override(BoltGlare.Bright(RaidGrade.Exposure(bright, raid), flash));
                RaidGrade.Filter(raid, out float red, out float green, out float blue);
                PoisonVeil.Tint(poisoned, red, green, blue, out red, out green, out blue);
                BoltGlare.Wash(flash, red, green, blue, out red, out green, out blue);
                bool ash = WeatherController.Instance != null && AshFall.Falls(WeatherController.Instance.District);
                AshVeil.Grit(ash, red, green, blue, out red, out green, out blue);
                color.colorFilter.Override(new Color(red, green, blue));
            }
            if (blur != null)
            {
                blur.active = PoisonVeil.Soft(poisoned, motion);
                blur.intensity.Override(PoisonVeil.BlurOf(poisoned, motion));
            }
        }
    }
}
