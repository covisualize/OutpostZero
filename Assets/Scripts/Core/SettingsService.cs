using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutpostZero.AI;
using OutpostZero.Graphics;

namespace OutpostZero.Core
{
    public class SettingsService : MonoBehaviour
    {
        public static SettingsService Instance { get; private set; }
        public static float ShakeScale => Instance != null ? Instance.screenShake : 1f;

        [SerializeField] private float screenShake = 1f;
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float textScale = 1f;
        [SerializeField] private bool subtitles = true;
        [SerializeField] private int colorblindMode;
        [SerializeField] private string language = "en";
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float ambienceVolume = 0.8f;
        [SerializeField] private float uiVolume = 1f;
        [SerializeField] private int quality = 1;
        [SerializeField] private int vsync = 1;
        [SerializeField] private float fieldOfView = 55f;
        [SerializeField] private bool merciful;
        [SerializeField] private int nextDifficulty = 2;
        [SerializeField] private int goreLevel = 1;
        [SerializeField] private int hitStop = 1;
        [SerializeField] private int damageNumbers = 1;
        [SerializeField] private float hudOpacity = 1f;
        [SerializeField] private float brightness = 1f;
        [SerializeField] private int motionBlur;
        [SerializeField] private int windowMode;

        public float ScreenShake => screenShake;
        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
        public float MusicVolume => musicVolume;
        public float AmbienceVolume => ambienceVolume;
        public float UiVolume => uiVolume;
        public float TextScale => textScale;
        public bool Subtitles => subtitles;
        public int ColorblindMode => colorblindMode;
        public string Language => language;
        public int Quality => quality;
        public bool VSync => vsync != 0;
        public float FieldOfView => fieldOfView;
        public bool Merciful => merciful;
        public int NextDifficulty => nextDifficulty < 1 ? 2 : nextDifficulty > 3 ? 3 : nextDifficulty;
        public int Gore => Presentation.Gore(goreLevel);
        public bool HitStop => Presentation.HitStop(hitStop);
        public bool DamageNumbers => Presentation.DamageNumbers(damageNumbers);
        public float HudOpacity => Presentation.Opacity(hudOpacity);
        public float Brightness => Presentation.Brightness(brightness);
        public bool MotionBlur => Presentation.MotionBlur(motionBlur);
        public int WindowMode => windowMode;
        public string DiscreteKey => (subtitles ? "1" : "0") + colorblindMode + quality + vsync + (merciful ? "1" : "0") + NextDifficulty + goreLevel + hitStop + damageNumbers + motionBlur + windowMode;
        public bool ShowSettings { get; private set; }

        public event Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            ApplyVolume();
            ApplyDisplay();
            ApplyWindow();
        }

        public void SetShake(float value)
        {
            screenShake = Mathf.Clamp01(value);
            OnChanged?.Invoke();
        }

        public void SetVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolume();
            OnChanged?.Invoke();
        }

        public void SetTextScale(float value)
        {
            textScale = Mathf.Clamp(value, 0.8f, 1.6f);
            OnChanged?.Invoke();
        }

        public void SetSubtitles(bool value)
        {
            subtitles = value;
            OnChanged?.Invoke();
        }

        public void CycleColorblind()
        {
            colorblindMode = (colorblindMode + 1) % 3;
            OnChanged?.Invoke();
        }

        public void SetLanguage(string code)
        {
            language = string.IsNullOrEmpty(code) ? "en" : code;
            OnChanged?.Invoke();
        }

        public void SetSfx(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            OnChanged?.Invoke();
        }

        public void SetMusic(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            OnChanged?.Invoke();
        }

        public void SetAmbience(float value)
        {
            ambienceVolume = Mathf.Clamp01(value);
            OnChanged?.Invoke();
        }

        public void SetUi(float value)
        {
            uiVolume = Mathf.Clamp01(value);
            OnChanged?.Invoke();
        }

        public void SetFieldOfView(float value)
        {
            fieldOfView = Mathf.Clamp(value, 40f, 75f);
            OnChanged?.Invoke();
        }

        public void CycleQuality()
        {
            quality = (quality + 1) % 4;
            ApplyDisplay();
            OnChanged?.Invoke();
        }

        public void ToggleMerciful()
        {
            merciful = !merciful;
            OnChanged?.Invoke();
        }

        public void SetMerciful(bool value)
        {
            merciful = value;
            OnChanged?.Invoke();
        }

        public void CycleDifficulty()
        {
            int current = NextDifficulty;
            nextDifficulty = current >= 3 ? 1 : current + 1;
            OnChanged?.Invoke();
        }

        public void SetNextDifficulty(int value)
        {
            if (value <= 0) return;
            nextDifficulty = value >= 3 ? 3 : value;
            OnChanged?.Invoke();
        }

        public void CycleGore()
        {
            goreLevel = Presentation.NextGore(goreLevel);
            OnChanged?.Invoke();
        }

        public void ToggleHitStop()
        {
            hitStop = Presentation.ToggleOff(hitStop);
            OnChanged?.Invoke();
        }

        public void ToggleDamageNumbers()
        {
            damageNumbers = Presentation.ToggleOff(damageNumbers);
            OnChanged?.Invoke();
        }

        public void SetHudOpacity(float value)
        {
            hudOpacity = Presentation.Opacity(value);
            OnChanged?.Invoke();
        }

        public void SetBrightness(float value)
        {
            brightness = Presentation.Brightness(value);
            OnChanged?.Invoke();
        }

        public void ToggleMotionBlur()
        {
            motionBlur = motionBlur == 1 ? 0 : 1;
            OnChanged?.Invoke();
        }

        public void CycleWindow()
        {
            windowMode = windowMode == 2 ? 1 : 2;
            ApplyWindow();
            OnChanged?.Invoke();
        }

        public void ApplyComfort(int gore, int stop, int numbers, float opacity, float bright, int blur, int window)
        {
            goreLevel = gore <= 0 ? 1 : gore >= 3 ? 3 : gore;
            hitStop = stop == 2 ? 2 : 1;
            damageNumbers = numbers == 2 ? 2 : 1;
            hudOpacity = Presentation.Opacity(opacity);
            brightness = Presentation.Brightness(bright);
            motionBlur = blur == 1 ? 1 : 0;
            windowMode = window == 1 || window == 2 ? window : 0;
            ApplyWindow();
            OnChanged?.Invoke();
        }

        public void ToggleVSync()
        {
            vsync = vsync == 0 ? 1 : 0;
            ApplyDisplay();
            OnChanged?.Invoke();
        }

        public void TogglePanel() => ShowSettings = !ShowSettings;

        public void ApplySnapshot(float shake, float volume, float scale, bool captions, string lang)
        {
            screenShake = Mathf.Clamp01(shake);
            masterVolume = Mathf.Clamp01(volume);
            textScale = Mathf.Clamp(scale <= 0f ? 1f : scale, 0.8f, 1.6f);
            subtitles = captions;
            language = string.IsNullOrEmpty(lang) ? "en" : lang;
            ApplyVolume();
            OnChanged?.Invoke();
        }

        public void ApplyPresentation(float sfx, float music, int tier, int sync, float fov, string bindings, float ambience = 0f, float ui = 0f)
        {
            sfxVolume = Mathf.Clamp01(sfx <= 0f ? 1f : sfx);
            musicVolume = Mathf.Clamp01(music <= 0f ? 0.7f : music);
            ambienceVolume = Mathf.Clamp01(ambience <= 0f ? 0.8f : ambience);
            uiVolume = Mathf.Clamp01(ui <= 0f ? 1f : ui);
            quality = Mathf.Clamp(tier, 0, 3);
            vsync = sync == 0 ? 0 : 1;
            fieldOfView = Mathf.Clamp(fov < 40f ? 55f : fov, 40f, 75f);
            OutpostZero.Player.ControlBindings.Unpack(bindings);
            ApplyVolume();
            ApplyDisplay();
            OnChanged?.Invoke();
        }

        private void ApplyVolume()
        {
            AudioListener.volume = masterVolume;
        }

        private void ApplyDisplay()
        {
            var tier = QualityProfile.For(quality);
            QualitySettings.vSyncCount = vsync;
            Application.targetFrameRate = vsync == 0 ? 60 : -1;
            QualitySettings.shadowDistance = tier.ShadowDistance;
            QualitySettings.antiAliasing = tier.Msaa;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale = tier.RenderScale;
                pipeline.msaaSampleCount = tier.Msaa;
            }
            var spawners = FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++) spawners[i].ApplyCap(tier.Zombies);
            WeatherController.Instance?.ApplyBudget(tier.Particles);
        }

        private void ApplyWindow()
        {
            if (windowMode == 2) Screen.fullScreen = true;
            else if (windowMode == 1) Screen.fullScreen = false;
        }
    }
}
