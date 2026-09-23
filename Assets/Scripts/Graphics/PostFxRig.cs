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
        private Volume sanctuary;
        private ColorAdjustments sanctuaryColor;
        private Volume toxic;
        private ColorAdjustments toxicColor;
        private Vignette toxicEdge;
        private MotionBlur toxicBlur;
        private Volume damage;
        private Vignette damageEdge;
        private ColorAdjustments damageColor;

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

            sanctuary = Layer("SanctuaryVolume", GradeLayers.SanctuaryPriority);
            var sanctuaryProfile = Profile(sanctuary, GradeLayers.SanctuaryPath);
            sanctuaryColor = Take<ColorAdjustments>(sanctuaryProfile, c => c.colorFilter.Override(GradeLayers.SanctuaryFilter(Filter)));

            toxic = Layer("ToxicVolume", GradeLayers.ToxicPriority);
            var toxicProfile = Profile(toxic, GradeLayers.ToxicPath);
            toxicColor = Take<ColorAdjustments>(toxicProfile, c => c.colorFilter.Override(GradeLayers.ToxicFilter(Filter)));
            toxicEdge = Take<Vignette>(toxicProfile, v => v.intensity.Override(GradeLayers.ToxicVignette(VignetteIntensity)));
            toxicBlur = Take<MotionBlur>(toxicProfile, m => m.intensity.Override(PoisonVeil.Blur));

            damage = Layer("DamageVolume", GradeLayers.DamagePriority);
            var damageProfile = Profile(damage, GradeLayers.DamagePath);
            damageEdge = Take<Vignette>(damageProfile, v =>
            {
                v.intensity.Override(GradeLayers.DamageVignette(VignetteIntensity));
                v.color.Override(GradeLayers.DamageEdge);
            });
            damageColor = Take<ColorAdjustments>(damageProfile, c => c.saturation.Override(GradeLayers.DamageSaturation(0f)));
            ApplyTier(1, false);
        }

        /// <summary>A global volume for one look, starting unseen; its weight is set every frame.</summary>
        private Volume Layer(string name, float priority)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            var layer = host.AddComponent<Volume>();
            layer.isGlobal = true;
            layer.priority = priority;
            layer.weight = 0f;
            return layer;
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
            float dt = Time.unscaledDeltaTime;
            bloom.intensity.Override(budget.Bloom);
            float edge = RaidGrade.Vignette(raid, tier);
            vignette.intensity.Override(edge);
            vignette.color.Override(Color.black);
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
                BoltGlare.Wash(flash, red, green, blue, out red, out green, out blue);
                bool ash = WeatherController.Instance != null && AshFall.Falls(WeatherController.Instance.District);
                AshVeil.Grit(ash, red, green, blue, out red, out green, out blue);
                var filter = new Color(red, green, blue);
                color.colorFilter.Override(filter);
                color.saturation.Override(ScreenGrade.Saturation(0f, ExpeditionCameraRig.DeathWeight));

                toxic.weight = GradeLayers.Fade(toxic.weight, poisoned, dt);
                toxicColor.colorFilter.Override(GradeLayers.ToxicFilter(filter));
                var tinted = GradeLayers.Blend(filter, GradeLayers.ToxicFilter(filter), toxic.weight);
                sanctuary.weight = GradeLayers.Fade(sanctuary.weight, camp, dt);
                sanctuaryColor.colorFilter.Override(GradeLayers.SanctuaryFilter(tinted));
            }
            if (blur != null)
            {
                blur.active = motion;
                blur.intensity.Override(PoisonVeil.BlurOf(false, motion));
            }

            if (color == null) toxic.weight = GradeLayers.Fade(toxic.weight, poisoned, dt);
            toxicEdge.intensity.Override(GradeLayers.ToxicVignette(edge));
            toxicBlur.intensity.Override(PoisonVeil.Blur);
            float closed = GradeLayers.Blend(edge, GradeLayers.ToxicVignette(edge), toxic.weight);

            damage.weight = hurt;
            damageEdge.intensity.Override(GradeLayers.DamageVignette(closed));
            damageEdge.color.Override(GradeLayers.DamageEdge);
            damageColor.saturation.Override(GradeLayers.DamageSaturation(ExpeditionCameraRig.DeathWeight));
        }
    }
}
