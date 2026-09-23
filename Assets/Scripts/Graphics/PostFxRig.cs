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
        private LiftGammaGain night;
        private ChromaticAberration fringe;

        public const string ProfilePath = "PostFX/OutpostZero_PostFX";
        public const string AimProfilePath = "PostFX/OutpostZero_AimDepth";

        public const float BloomIntensity = 0.35f;
        public const float BloomThreshold = 1.1f;
        public const float VignetteIntensity = 0.28f;
        public const float VignetteSmoothness = 0.4f;
        public const float Exposure = 0.2f;
        public static readonly Color Filter = new Color(1f, 0.96f, 0.9f);
        public const float GrainIntensity = 0.25f;
        public const float BlurIntensity = 0.35f;
        public const float DepthStart = 6f;
        public const float DepthEnd = 18f;

        private void Start()
        {
            volume = Attach.Ensure<Volume>(gameObject);
            volume.isGlobal = true;
            volume.priority = 20f;
            var profile = Profile(volume, ProfilePath);

            bloom = Take<Bloom>(profile, b =>
            {
                b.intensity.Override(BloomIntensity);
                b.threshold.Override(BloomThreshold);
            });
            vignette = Take<Vignette>(profile, v =>
            {
                v.intensity.Override(VignetteIntensity);
                v.smoothness.Override(VignetteSmoothness);
            });
            Take<Tonemapping>(profile, tone => tone.mode.Override(TonemappingMode.ACES));
            color = Take<ColorAdjustments>(profile, c =>
            {
                c.postExposure.Override(Exposure);
                c.contrast.Override(ScreenGrade.Contrast);
                c.saturation.Override(ScreenGrade.BaseSaturation);
                c.colorFilter.Override(Filter);
            });
            Take<ShadowsMidtonesHighlights>(profile, tones =>
            {
                tones.shadows.Override(ScreenGrade.Shadows);
                tones.midtones.Override(ScreenGrade.Midtones);
                tones.highlights.Override(ScreenGrade.Highlights);
            });
            night = Take<LiftGammaGain>(profile, _ => { });
            fringe = Take<ChromaticAberration>(profile, f => f.intensity.Override(ScreenGrade.Aberration));
            grain = Take<FilmGrain>(profile, g =>
            {
                g.active = false;
                g.intensity.Override(GrainIntensity);
                g.type.Override(FilmGrainLookup.Thin1);
            });
            blur = Take<MotionBlur>(profile, m =>
            {
                m.active = false;
                m.intensity.Override(BlurIntensity);
            });

            var aimHost = new GameObject("AimDepthVolume");
            aimHost.transform.SetParent(transform, false);
            aimDepth = aimHost.AddComponent<Volume>();
            aimDepth.isGlobal = true;
            aimDepth.priority = 21f;
            aimDepth.weight = 0f;
            var aimProfile = Profile(aimDepth, AimProfilePath);
            depth = Take<DepthOfField>(aimProfile, d =>
            {
                d.mode.Override(DepthOfFieldMode.Gaussian);
                d.gaussianStart.Override(DepthStart);
                d.gaussianEnd.Override(DepthEnd);
            });
            ApplyTier(1, false);
        }

        /// <summary>
        /// The committed profile under Resources, cloned per volume so the per-frame overrides never touch the
        /// asset; an empty profile when it is missing, which <see cref="Take"/> then fills from the constants.
        /// </summary>
        private static VolumeProfile Profile(Volume host, string path)
        {
            var asset = Resources.Load<VolumeProfile>(path);
            if (asset != null)
            {
                host.sharedProfile = asset;
                return host.profile;
            }
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            host.sharedProfile = profile;
            return profile;
        }

        private static T Take<T>(VolumeProfile profile, System.Action<T> defaults) where T : VolumeComponent
        {
            if (profile.TryGet(out T found)) return found;
            var added = profile.Add<T>();
            added.active = true;
            defaults(added);
            return added;
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
            bool camp = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.CampManagement;
            float hurt = 0f;
            if (PlayerRegistry.Current != null && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ExpeditionActive)
            {
                var health = PlayerRegistry.Current.GetComponent<OutpostZero.Combat.HealthSystem>();
                if (health != null && health.MaxHealth > 0f && !health.IsDead) hurt = ScreenGrade.Hurt(health.CurrentHealth / health.MaxHealth);
            }
            float dark = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightFactor : 0f;
            bloom.intensity.Override(budget.Bloom);
            vignette.intensity.Override(ScreenGrade.Vignette(PoisonVeil.Shade(RaidGrade.Vignette(raid, tier), poisoned), hurt));
            vignette.color.Override(ScreenGrade.VignetteColor(hurt));
            if (night != null)
            {
                night.lift.Override(ScreenGrade.Lift(dark));
                night.gamma.Override(ScreenGrade.Gamma(dark));
                night.gain.Override(ScreenGrade.Gain(dark));
            }
            if (fringe != null) fringe.active = ScreenGrade.AberrationOn(tier);
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
                ScreenGrade.Warm(camp, red, green, blue, out red, out green, out blue);
                color.colorFilter.Override(new Color(red, green, blue));
                color.saturation.Override(ScreenGrade.Saturation(hurt, ExpeditionCameraRig.DeathWeight));
            }
            if (blur != null)
            {
                blur.active = PoisonVeil.Soft(poisoned, motion);
                blur.intensity.Override(PoisonVeil.BlurOf(poisoned, motion));
            }
        }
    }
}
