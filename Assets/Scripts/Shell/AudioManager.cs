using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;
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
        private float nextStep;
        private bool peaked;

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
            if (noise != subscribedNoise)
            {
                if (subscribedNoise != null) subscribedNoise.OnNoiseEmitted -= OnNoise;
                subscribedNoise = noise;
                if (subscribedNoise != null) subscribedNoise.OnNoiseEmitted += OnNoise;
            }

            float tension = HordeDirector.Instance != null ? HordeDirector.Instance.Tension : 0f;
            float music = SettingsService.Instance != null ? SettingsService.Instance.MusicVolume : 0.7f;
            ambient.volume = music * (0.08f + tension / 500f);
            ambient.pitch = Mathf.Lerp(0.82f, 1.35f, tension / 100f);
            if (tension >= 75f && !peaked)
            {
                peaked = true;
                Play("pulse", 0.35f);
            }
            else if (tension < 40f)
            {
                peaked = false;
            }
            Step();
        }

        public void Play(string id, float volume = 1f)
        {
            var source = Rent();
            source.pitch = Random.Range(0.94f, 1.06f);
            float sfx = SettingsService.Instance != null ? SettingsService.Instance.SfxVolume : 1f;
            source.PlayOneShot(GetClip(id), volume * sfx);
        }

        private void Step()
        {
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var body = player.GetComponent<CharacterController>();
            if (body == null || body.velocity.magnitude < 0.8f) return;
            if (Time.time < nextStep) return;
            float interval = player.IsSprinting ? 0.28f : player.IsCrouching ? 0.55f : 0.42f;
            nextStep = Time.time + interval;
            bool road = false;
            if (Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, out var hit, 2.2f, GameLayers.VisionOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                road = hit.collider.name.Contains("Road") || hit.collider.name.Contains("Street");
            }
            var source = Rent();
            source.pitch = road ? Random.Range(1.05f, 1.2f) : Random.Range(0.85f, 1f);
            float sfx = SettingsService.Instance != null ? SettingsService.Instance.SfxVolume : 1f;
            source.PlayOneShot(GetClip(road ? "step_hard" : "step"), (player.IsCrouching ? 0.12f : 0.28f) * sfx);
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
                float tone = id == "scream" ? Mathf.Sin(t * 90f) : id == "pulse" ? Mathf.Sin(t * 28f) : id == "step" || id == "step_hard" ? noise * Mathf.Sin(t * 18f) : id == "ui" ? Mathf.Sin(t * 40f) : noise;
                data[i] = tone * envelope * (id == "ambient" ? 0.2f : 0.6f);
            }
            clip = AudioClip.Create(id, samples, 1, rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
            return clip;
        }
    }
}
