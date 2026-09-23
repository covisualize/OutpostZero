using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutpostZero.AI;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Core
{
    public class SettingsService : MonoBehaviour
    {
        public static SettingsService Instance { get; private set; }
        public static float ShakeScale => Instance != null ? Instance.screenShake : 1f;

        [SerializeField] private float screenShake = 1f;
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float textScale = 1f;
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private bool subtitles = true;
        [SerializeField] private bool quietFlash;
        [SerializeField] private int colorblindMode;
        [SerializeField] private bool enemyOutline;
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
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private int motionBlur;
        [SerializeField] private int windowMode;
        [SerializeField] private int aimAssist;
        [SerializeField] private int invertLook;
        [SerializeField] private int crouchMode;
        [SerializeField] private int sprintMode;
        [SerializeField] private int aimMode;
        [SerializeField] private int frameCap;
        [SerializeField] private int resolution;
        [SerializeField] private int renderScale;

        public float ScreenShake => screenShake;
        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
        public float MusicVolume => musicVolume;
        public float AmbienceVolume => ambienceVolume;
        public float UiVolume => uiVolume;
        public float TextScale => textScale;
        public float UiScale => uiScale;
        public bool Subtitles => subtitles;
        public bool QuietFlash => quietFlash;
        public int ColorblindMode => colorblindMode;
        public bool EnemyOutline => enemyOutline;
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
        public int AimAssist => aimAssist <= 0 ? 0 : aimAssist >= 2 ? 2 : 1;
        public bool InvertLook => invertLook == 1;
        public float Sensitivity => PlayOptions.Sensitivity(sensitivity);
        public int CrouchMode => crouchMode == 1 ? 1 : 0;
        public int SprintMode => sprintMode == 1 ? 1 : 0;
        public int AimMode => aimMode == 1 ? 1 : 0;
        public int FrameCap => frameCap < 0 || frameCap > 4 ? 0 : frameCap;
        public int Resolution => resolution < 0 || resolution >= DisplayModes.Count ? 0 : resolution;
        public int RenderScaleStep => PlayOptions.ScaleStep(renderScale);
        public string DiscreteKey => (subtitles ? "1" : "0") + colorblindMode + quality + vsync + (merciful ? "1" : "0") + NextDifficulty + goreLevel + hitStop + damageNumbers + motionBlur + windowMode + AimAssist + (InvertLook ? 1 : 0) + CrouchMode + SprintMode + AimMode + FrameCap + Resolution + (quietFlash ? "1" : "0") + RenderScaleStep + (enemyOutline ? "1" : "0");
        public bool ShowSettings { get; private set; }

        public event Action OnChanged;
        private bool suppressWrite;
        private bool hasFocus = true;
        private string opened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            LoadLanguagePacks();
            LoadFile();
            ApplyVolume();
            ApplyDisplay();
            ApplyWindow();
            Application.focusChanged += OnFocus;
        }

        private static void LoadLanguagePacks()
        {
            var problems = new System.Collections.Generic.List<string>();
            Loc.ClearPacks();
            int added = LocPacks.Load(Path.Combine(Application.streamingAssetsPath, LocPacks.Folder), problems);
            if (added > 0) Debug.Log("[Loc] Loaded " + added + " translated language(s): " + string.Join(", ", Loc.Packs));
            for (int i = 0; i < problems.Count && i < 20; i++) Debug.LogWarning("[Loc] " + problems[i]);
            if (problems.Count > 20) Debug.LogWarning("[Loc] " + (problems.Count - 20) + " more translation problems.");
        }

        private void OnDestroy()
        {
            Application.focusChanged -= OnFocus;
        }

        private void OnFocus(bool focused)
        {
            hasFocus = focused;
            ApplyVolume();
        }

        /// <summary>
        /// Every change applies and saves at once. Opening the panel marks where a revert returns to.
        /// </summary>
        public void BeginEdit() => opened = ExportSettings();

        public bool HasUnsaved => SettingsDraft.Dirty(opened, ExportSettings());

        public void KeepEdits() => opened = null;

        public void RevertEdits()
        {
            string back = opened;
            opened = null;
            if (!string.IsNullOrEmpty(back)) ImportSettings(back);
        }

        public void SetShake(float value)
        {
            screenShake = Mathf.Clamp01(value);
            Raise();
        }

        public void SetVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolume();
            Raise();
        }

        public void SetUiScale(float value)
        {
            uiScale = PlayOptions.UiScale(value);
            Raise();
        }

        public void SetTextScale(float value)
        {
            textScale = Mathf.Clamp(value, 0.8f, 1.6f);
            Raise();
        }

        public void SetSubtitles(bool value)
        {
            subtitles = value;
            Raise();
        }

        public void ToggleQuietFlash()
        {
            quietFlash = !quietFlash;
            Raise();
        }

        public void CycleColorblind()
        {
            colorblindMode = HudPalette.Next(colorblindMode);
            Raise();
        }

        public void ToggleEnemyOutline()
        {
            enemyOutline = !enemyOutline;
            Raise();
        }

        public void SetLanguage(string code)
        {
            language = string.IsNullOrEmpty(code) ? "en" : code;
            Raise();
        }

        public void SetSfx(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            Raise();
        }

        public void SetMusic(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            Raise();
        }

        public void SetAmbience(float value)
        {
            ambienceVolume = Mathf.Clamp01(value);
            Raise();
        }

        public void SetUi(float value)
        {
            uiVolume = Mathf.Clamp01(value);
            Raise();
        }

        public void SetSensitivity(float value)
        {
            sensitivity = PlayOptions.Sensitivity(value);
            Raise();
        }

        public void SetFieldOfView(float value)
        {
            fieldOfView = Mathf.Clamp(value, 40f, 75f);
            Raise();
        }

        public void CycleQuality()
        {
            quality = (quality + 1) % 4;
            ApplyDisplay();
            Raise();
        }

        public void ToggleMerciful()
        {
            merciful = !merciful;
            Raise();
        }

        public void SetMerciful(bool value)
        {
            merciful = value;
            Raise();
        }

        public void CycleDifficulty()
        {
            int current = NextDifficulty;
            nextDifficulty = current >= 3 ? 1 : current + 1;
            Raise();
        }

        public void SetNextDifficulty(int value)
        {
            if (value <= 0) return;
            nextDifficulty = value >= 3 ? 3 : value;
            Raise();
        }

        public void CycleGore()
        {
            goreLevel = Presentation.NextGore(goreLevel);
            Raise();
        }

        public void ToggleHitStop()
        {
            hitStop = Presentation.ToggleOff(hitStop);
            Raise();
        }

        public void ToggleDamageNumbers()
        {
            damageNumbers = Presentation.ToggleOff(damageNumbers);
            Raise();
        }

        public void SetHudOpacity(float value)
        {
            hudOpacity = Presentation.Opacity(value);
            Raise();
        }

        public void SetBrightness(float value)
        {
            brightness = Presentation.Brightness(value);
            Raise();
        }

        public void ToggleMotionBlur()
        {
            motionBlur = motionBlur == 1 ? 0 : 1;
            Raise();
        }

        public void CycleWindow()
        {
            windowMode = windowMode == 2 ? 1 : 2;
            ApplyWindow();
            ApplyResolution();
            Raise();
        }

        public void CycleResolution()
        {
            resolution = DisplayModes.Next(Resolution);
            ApplyResolution();
            Raise();
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
            Raise();
        }

        public void CycleAim()
        {
            aimAssist = AimAssist >= 2 ? 0 : AimAssist + 1;
            Raise();
        }

        public void ToggleInvert()
        {
            invertLook = InvertLook ? 0 : 1;
            Raise();
        }

        public void ToggleCrouchMode()
        {
            crouchMode = CrouchMode == 1 ? 0 : 1;
            Raise();
        }

        public void ToggleAimMode()
        {
            aimMode = AimMode == 1 ? 0 : 1;
            Raise();
        }

        public void ToggleSprintMode()
        {
            sprintMode = SprintMode == 1 ? 0 : 1;
            Raise();
        }

        public void CycleFrameCap()
        {
            frameCap = PlayOptions.NextFrame(FrameCap);
            ApplyDisplay();
            Raise();
        }

        public void ApplyPlay(int assist, int invert, int crouch, int sprint, int cap, int display = 0, int ads = 0)
        {
            aimAssist = assist <= 0 ? 0 : assist >= 2 ? 2 : 1;
            invertLook = invert == 1 ? 1 : 0;
            crouchMode = crouch == 1 ? 1 : 0;
            sprintMode = sprint == 1 ? 1 : 0;
            aimMode = ads == 1 ? 1 : 0;
            frameCap = cap < 0 || cap > 4 ? 0 : cap;
            resolution = display < 0 || display >= DisplayModes.Count ? 0 : display;
            ApplyDisplay();
            Raise();
        }

        public void CycleRenderScale()
        {
            renderScale = PlayOptions.NextScale(renderScale);
            ApplyDisplay();
            Raise();
        }

        public void ToggleVSync()
        {
            vsync = vsync == 0 ? 1 : 0;
            ApplyDisplay();
            Raise();
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
            Raise();
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
            Raise();
        }

        private void ApplyVolume()
        {
            if (MixerRig.Live)
            {
                MixerRig.Master(masterVolume);
                AudioListener.volume = SettingsDraft.Heard(1f, hasFocus);
                return;
            }
            AudioListener.volume = SettingsDraft.Heard(masterVolume, hasFocus);
        }

        private void ApplyDisplay()
        {
            var tier = QualityProfile.For(quality);
            if (QualitySettings.count == QualityProfile.Count && QualitySettings.GetQualityLevel() != quality)
                QualitySettings.SetQualityLevel(quality, true);
            QualitySettings.lodBias = tier.LodBias;
            QualitySettings.vSyncCount = vsync;
            Application.targetFrameRate = PlayOptions.FrameTarget(FrameCap, vsync != 0);
            QualitySettings.shadowDistance = tier.ShadowDistance;
            QualitySettings.shadowCascades = ShadowRig.Cascades;
            QualitySettings.antiAliasing = tier.Msaa;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale = PlayOptions.RenderScale(renderScale, tier.RenderScale);
                pipeline.msaaSampleCount = tier.Msaa;
            }
            var spawners = FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++) spawners[i].ApplyCap(OutpostZero.AI.DifficultyProfile.AliveCap(quality, OutpostZero.AI.DifficultyProfile.Active));
            WeatherController.Instance?.ApplyBudget(tier.Particles);
            ApplyResolution();
        }

        private void ApplyResolution()
        {
            if (!DisplayModes.Size(Resolution, out int width, out int height)) return;
            var mode = windowMode == 1
                ? FullScreenMode.Windowed
                : windowMode == 2 ? FullScreenMode.ExclusiveFullScreen : Screen.fullScreenMode;
            Screen.SetResolution(width, height, mode);
        }

        private void ApplyWindow()
        {
            if (windowMode == 2) Screen.fullScreen = true;
            else if (windowMode == 1) Screen.fullScreen = false;
        }

        public string ExportSettings() => SettingsFile.ToJson(CaptureFile());

        public void ImportSettings(string json)
        {
            if (!SettingsFile.TryFromJson(json, out var snap)) return;
            suppressWrite = true;
            ApplyImported(snap);
            suppressWrite = false;
            Raise();
        }

        public void NoteBindings() => Raise();

        private void Raise()
        {
            if (!suppressWrite) WriteFile();
            OnChanged?.Invoke();
        }

        private void LoadFile()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SettingsFile.FileName);
                if (!File.Exists(path)) return;
                if (!SettingsFile.TryFromJson(File.ReadAllText(path), out var snap)) return;
                suppressWrite = true;
                ApplyImported(snap);
                suppressWrite = false;
            }
            catch (Exception ex)
            {
                suppressWrite = false;
                Debug.LogWarning("[Settings] " + ex.Message);
            }
        }

        private void WriteFile()
        {
            try
            {
                string dir = Application.persistentDataPath;
                if (string.IsNullOrEmpty(dir)) return;
                File.WriteAllText(Path.Combine(dir, SettingsFile.FileName), ExportSettings());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Settings] " + ex.Message);
            }
        }

        private SettingsFile.Snapshot CaptureFile()
        {
            return new SettingsFile.Snapshot
            {
                shake = screenShake,
                master = masterVolume,
                sfx = sfxVolume,
                music = musicVolume,
                ambience = ambienceVolume,
                ui = uiVolume,
                text = textScale,
                uiScale = uiScale,
                fov = fieldOfView,
                opacity = hudOpacity,
                brightness = brightness,
                sensitivity = Sensitivity,
                colorblind = colorblindMode,
                outline = enemyOutline ? 1 : 0,
                quality = quality,
                vsync = vsync,
                difficulty = NextDifficulty,
                gore = Gore == 0 ? 3 : Gore,
                hitStop = HitStop ? 1 : 2,
                numbers = DamageNumbers ? 1 : 2,
                blur = motionBlur,
                window = windowMode,
                aim = AimAssist,
                invert = invertLook,
                crouch = CrouchMode,
                sprint = SprintMode,
                ads = AimMode,
                frame = FrameCap,
                resolution = Resolution,
                render = RenderScaleStep,
                subtitles = subtitles,
                merciful = merciful,
                quietFlash = quietFlash,
                language = string.IsNullOrEmpty(language) ? "en" : language,
                keys = OutpostZero.Player.ControlBindings.Pack(),
                pad = OutpostZero.Player.PadBindings.Pack()
            };
        }

        private void ApplyImported(SettingsFile.Snapshot snap)
        {
            screenShake = Mathf.Clamp01(snap.shake);
            masterVolume = Mathf.Clamp01(snap.master);
            sfxVolume = Mathf.Clamp01(snap.sfx);
            musicVolume = Mathf.Clamp01(snap.music);
            ambienceVolume = Mathf.Clamp01(snap.ambience);
            uiVolume = Mathf.Clamp01(snap.ui);
            textScale = Mathf.Clamp(snap.text <= 0f ? 1f : snap.text, 0.8f, 1.6f);
            uiScale = PlayOptions.UiScale(snap.uiScale);
            fieldOfView = Mathf.Clamp(snap.fov < 40f ? 55f : snap.fov, 40f, 75f);
            hudOpacity = Presentation.Opacity(snap.opacity);
            brightness = Presentation.Brightness(snap.brightness);
            sensitivity = PlayOptions.Sensitivity(snap.sensitivity);
            colorblindMode = HudPalette.Clamp(snap.colorblind);
            enemyOutline = snap.outline == 1;
            quality = Mathf.Clamp(snap.quality, 0, 3);
            vsync = snap.vsync == 0 ? 0 : 1;
            nextDifficulty = snap.difficulty <= 0 ? 2 : snap.difficulty >= 3 ? 3 : snap.difficulty;
            goreLevel = snap.gore <= 0 ? 1 : snap.gore >= 3 ? 3 : snap.gore;
            hitStop = snap.hitStop == 2 ? 2 : 1;
            damageNumbers = snap.numbers == 2 ? 2 : 1;
            motionBlur = snap.blur == 1 ? 1 : 0;
            windowMode = snap.window == 1 || snap.window == 2 ? snap.window : 0;
            aimAssist = snap.aim <= 0 ? 0 : snap.aim >= 2 ? 2 : 1;
            invertLook = snap.invert == 1 ? 1 : 0;
            crouchMode = snap.crouch == 1 ? 1 : 0;
            sprintMode = snap.sprint == 1 ? 1 : 0;
            aimMode = snap.ads == 1 ? 1 : 0;
            frameCap = snap.frame < 0 || snap.frame > 4 ? 0 : snap.frame;
            resolution = snap.resolution < 0 || snap.resolution >= DisplayModes.Count ? 0 : snap.resolution;
            renderScale = PlayOptions.ScaleStep(snap.render);
            subtitles = snap.subtitles;
            merciful = snap.merciful;
            quietFlash = snap.quietFlash;
            language = PseudoLoc.Keep(snap.language, DevCheats.Allowed(Application.isEditor, Debug.isDebugBuild));
            OutpostZero.Player.ControlBindings.Unpack(snap.keys);
            OutpostZero.Player.PadBindings.Unpack(snap.pad);
            ApplyVolume();
            ApplyDisplay();
            ApplyWindow();
        }
    }
}
