using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Sensory;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Procedural one-shots so the expedition is not silent before authored clips exist.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private AudioSource ambient;
        private NoiseManager subscribedNoise;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.loop = true;
            ambient.spatialBlend = 0f;
            ambient.volume = 0.12f;
            ambient.clip = GetClip("ambient");
            ambient.Play();
        }

        private void OnEnable()
        {
            CombatEvents.OnShotFired += OnShot;
            CombatEvents.OnHit += OnHit;
            CombatEvents.OnKill += OnKill;
        }

        private void OnDisable()
        {
            CombatEvents.OnShotFired -= OnShot;
            CombatEvents.OnHit -= OnHit;
            CombatEvents.OnKill -= OnKill;
            if (subscribedNoise != null) subscribedNoise.OnNoiseEmitted -= OnNoise;
        }

        private void Update()
        {
            var noise = NoiseManager.Instance;
            if (noise == subscribedNoise) return;
            if (subscribedNoise != null) subscribedNoise.OnNoiseEmitted -= OnNoise;
            subscribedNoise = noise;
            if (subscribedNoise != null) subscribedNoise.OnNoiseEmitted += OnNoise;
        }

        public void Play(string id, float volume = 1f)
        {
            var source = Rent();
            source.pitch = Random.Range(0.94f, 1.06f);
            source.PlayOneShot(GetClip(id), volume * (SettingsService.Instance != null ? SettingsService.Instance.MasterVolume : 1f));
        }

        private void OnShot(Vector3 muzzle, WeaponBase weapon)
        {
            if (weapon == null) return;
            Play(weapon.Type == WeaponType.Shotgun ? "shotgun" : weapon.Type == WeaponType.Melee ? "swing" : "gun", 0.8f);
        }

        private void OnHit(Vector3 point, Vector3 normal, GameObject target) => Play("hit", 0.45f);

        private void OnKill(GameObject victim, GameObject killer) => Play("kill", 0.5f);

        private void OnNoise(Vector3 origin, float radius, NoiseType type)
        {
            if (type == NoiseType.Explosion) Play("boom", 0.9f);
            else if (type == NoiseType.ZombieScream) Play("scream", 0.55f);
        }

        private AudioSource Rent()
        {
            foreach (var source in pool)
            {
                if (!source.isPlaying) return source;
            }
            var extra = gameObject.AddComponent<AudioSource>();
            extra.spatialBlend = 0f;
            extra.playOnAwake = false;
            pool.Add(extra);
            return extra;
        }

        private AudioClip GetClip(string id)
        {
            if (clips.TryGetValue(id, out var clip)) return clip;
            int rate = 22050;
            float seconds = id == "ambient" ? 2f : id == "boom" ? 0.45f : 0.18f;
            int samples = Mathf.CeilToInt(rate * seconds);
            var data = new float[samples];
            var random = new System.Random(id.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float envelope = id == "ambient" ? 0.25f : Mathf.Exp(-t * (id == "boom" ? 4f : 10f));
                float tone = id == "scream" ? Mathf.Sin(t * 90f) : id == "ui" ? Mathf.Sin(t * 40f) : noise;
                data[i] = tone * envelope * (id == "ambient" ? 0.2f : 0.6f);
            }
            clip = AudioClip.Create(id, samples, 1, rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
            return clip;
        }
    }
}
