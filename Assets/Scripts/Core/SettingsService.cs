using System;
using UnityEngine;

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

        public float ScreenShake => screenShake;
        public float MasterVolume => masterVolume;
        public float TextScale => textScale;
        public bool Subtitles => subtitles;
        public int ColorblindMode => colorblindMode;
        public string Language => language;
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

        private void ApplyVolume()
        {
            AudioListener.volume = masterVolume;
        }
    }
}
