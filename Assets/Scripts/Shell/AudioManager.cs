using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Sensory;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Which one-shots stay in the ears and which sit in the world.
    /// </summary>
    public static class AudioSpace
    {
        public static float SpatialBlend(string id)
        {
            if (id == "ambient" || id == "pulse" || id == "ui" || id == "rain" || id == "wind" || id == "heart" || id == "breath") return 0f;
            if (id != null && id.StartsWith("step")) return 0.35f;
            return 1f;
        }

        public static float MaxDistance(string id)
        {
            if (id == "boom" || id == "boom_far") return 48f;
            if (id == "scream") return 36f;
            if (id == "gun" || id == "shotgun" || id == "gun_far") return 32f;
            return 18f;
        }
    }

    /// <summary>
    /// Procedural one-shots so the expedition is not silent before authored clips exist.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private AudioSource ambient;
        private AudioSource percussion;
        private AudioSource combat;
        private AudioSource weather;
        private string weatherId = "";
        private NoiseManager subscribedNoise;
        private float nextStep;
        private bool peaked;
        private int streak;
        private float streakAt;
        private float nextHeart;
        private float nextBreath;

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
            weather = gameObject.AddComponent<AudioSource>();
            weather.loop = true;
            weather.spatialBlend = 0f;
            weather.playOnAwake = false;
            percussion = AddBed("stem_perc");
            combat = AddBed("stem_combat");
        }

        private AudioSource AddBed(string id)
        {
            var bed = gameObject.AddComponent<AudioSource>();
            bed.loop = true;
            bed.spatialBlend = 0f;
            bed.playOnAwake = false;
            bed.clip = GetClip(id);
            bed.volume = 0f;
            bed.Play();
            return bed;
        }

        public void Sting(string moment)
        {
            string id = MusicStem.Cue(moment);
            if (id.Length == 0) return;
            Play(id, 0.6f);
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
            var snapshot = CurrentSnapshot();
            Levels(out float music, out float sfx, out float ambience, out float ui);
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            MusicStem.Gains(MusicStem.Theme(state), tension, out float drone, out float perc, out float fight);
            // The listener already carries master, so each bus is scaled on its own.
            ambient.volume = AudioMix.Gain("ambient", drone, 1f, music, sfx, ambience, ui, snapshot);
            ambient.pitch = Mathf.Lerp(0.82f, 1.35f, tension / 100f);
            if (percussion != null) percussion.volume = AudioMix.Gain("ambient", perc * 0.45f, 1f, music, sfx, ambience, ui, snapshot);
            if (combat != null) combat.volume = AudioMix.Gain("ambient", fight * 0.5f, 1f, music, sfx, ambience, ui, snapshot);
            ApplyLowpass(ambient, snapshot);
            if (percussion != null) ApplyLowpass(percussion, snapshot);
            if (combat != null) ApplyLowpass(combat, snapshot);
            UpdateWeather(snapshot, music, sfx, ambience, ui);
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
            Body();
        }

        public void Play(string id, float volume = 1f)
        {
            PlayAt(id, transform.position, volume);
        }

        public void Play(string id, float volume, float pitch)
        {
            PlayAt(id, transform.position, volume, pitch);
        }

        public void PlayAt(string id, Vector3 position, float volume = 1f, float pitch = 0f)
        {
            var source = Rent();
            source.transform.position = position;
            source.spatialBlend = AudioSpace.SpatialBlend(id);
            source.minDistance = 1.5f;
            source.maxDistance = AudioSpace.MaxDistance(id);
            source.rolloffMode = AudioRolloffMode.Linear;
            source.pitch = pitch > 0f ? pitch : Random.Range(0.94f, 1.06f);
            bool wall = BehindWall(id, position);
            float heard = EarWall.Gain(volume, wall, id == "scream");
            if (heard <= 0.001f) return;
            var snapshot = CurrentSnapshot();
            Levels(out float music, out float sfx, out float ambience, out float ui);
            ApplyLowpass(source, snapshot, wall);
            source.PlayOneShot(GetClip(id), AudioMix.Gain(id, heard, 1f, music, sfx, ambience, ui, snapshot));
        }

        private static bool BehindWall(string id, Vector3 position)
        {
            if (!EarWall.InWorld(AudioSpace.SpatialBlend(id))) return false;
            var listener = PlayerRegistry.Current;
            if (listener == null) return false;
            Vector3 ear = listener.transform.position + Vector3.up * 1.5f;
            Vector3 to = position - ear;
            float distance = to.magnitude;
            if (distance <= EarWall.Clear) return false;
            Vector3 direction = to / distance;
            return Physics.Raycast(ear, direction, distance - 0.4f, GameLayers.VisionOcclusionMask, QueryTriggerInteraction.Ignore);
        }

        private void UpdateWeather(MixSnapshot snapshot, float music, float sfx, float ambience, float ui)
        {
            var kind = WeatherController.Instance != null ? WeatherController.Instance.Kind : WeatherKind.Clear;
            string id = kind == WeatherKind.Rain ? "rain" : kind == WeatherKind.Fog ? "wind" : "";
            if (id != weatherId)
            {
                weatherId = id;
                if (string.IsNullOrEmpty(id))
                {
                    weather.Stop();
                    weather.volume = 0f;
                }
                else
                {
                    weather.clip = GetClip(id);
                    weather.Play();
                }
            }
            if (!string.IsNullOrEmpty(weatherId))
                weather.volume = AudioMix.Gain(weatherId, 0.35f, 1f, music, sfx, ambience, ui, snapshot);
            ApplyLowpass(weather, snapshot);
        }

        private static MixSnapshot CurrentSnapshot()
        {
            bool toxic = false;
            var player = PlayerRegistry.Current;
            if (player != null)
            {
                var effects = player.GetComponent<StatusEffectController>();
                toxic = effects != null && effects.IsPoisoned;
            }
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            return AudioMix.SnapshotFor(state, toxic);
        }

        private static void Levels(out float music, out float sfx, out float ambience, out float ui)
        {
            var settings = SettingsService.Instance;
            music = settings != null ? settings.MusicVolume : 0.7f;
            sfx = settings != null ? settings.SfxVolume : 1f;
            ambience = settings != null ? settings.AmbienceVolume : 0.8f;
            ui = settings != null ? settings.UiVolume : 1f;
        }

        private static void ApplyLowpass(AudioSource source, MixSnapshot snapshot)
        {
            ApplyLowpass(source, snapshot, false);
        }

        private static void ApplyLowpass(AudioSource source, MixSnapshot snapshot, bool wall)
        {
            var filter = source.GetComponent<AudioLowPassFilter>();
            if (filter == null) filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            float hz = AudioMix.LowpassHz(snapshot);
            filter.cutoffFrequency = wall ? EarWall.Muffle(hz) : hz;
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
            string surface = "";
            if (Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, out var hit, 2.2f, GameLayers.VisionOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                surface = hit.collider.name;
            }
            string step = AudioMix.StepId(surface);
            bool hard = step == "step_hard" || step == "step_metal";
            float pitch = hard ? Random.Range(1.05f, 1.2f) : Random.Range(0.85f, 1f);
            PlayAt(step, player.transform.position, player.IsCrouching ? 0.12f : 0.28f, pitch);
        }

        private void Body()
        {
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var life = player.GetComponent<HealthSystem>();
            float hp = life != null && life.MaxHealth > 0f ? life.CurrentHealth / life.MaxHealth : 1f;
            float air = player.MaxStamina > 0f ? player.CurrentStamina / player.MaxStamina : 1f;
            float now = Time.time;
            if (!BodyCue.Heart(hp)) nextHeart = 0f;
            else if (BodyCue.Due(now, nextHeart, BodyCue.HeartGap(hp)))
            {
                nextHeart = now;
                Play("heart", 0.45f, BodyCue.HeartPitch(hp));
            }
            if (!BodyCue.Breath(air)) nextBreath = 0f;
            else if (BodyCue.Due(now, nextBreath, BodyCue.BreathGap))
            {
                nextBreath = now;
                Play("breath", 0.22f, 0.9f);
            }
        }

        private void OnShot(Vector3 muzzle, WeaponBase weapon)
        {
            if (weapon == null) return;
            string id = weapon.Type == WeaponType.Shotgun ? "shotgun" : weapon.Type == WeaponType.Melee ? "swing" : "gun";
            PlayAt(id, muzzle, 0.8f);
            if (id == "swing") return;
            float distance = HearDistance(muzzle);
            float tail = SoundTail.Gun(distance);
            if (tail > 0.001f) PlayAt("gun_far", muzzle, tail, SoundTail.Pitch(distance));
        }

        private void OnHit(Vector3 point, Vector3 normal, GameObject target) => PlayAt("hit", point, 0.45f);

        private void OnKill(GameObject victim, GameObject killer)
        {
            Vector3 at = victim != null ? victim.transform.position : transform.position;
            PlayAt("kill", at, 0.5f);
            float now = Time.time;
            streak = MusicStem.Tally(streak, streakAt, now, MusicStem.Window);
            streakAt = now;
            if (MusicStem.Streak(streak)) Sting("kill");
        }

        private void OnNoise(Vector3 origin, float radius, NoiseType type)
        {
            if (type == NoiseType.Explosion)
            {
                PlayAt("boom", origin, 0.9f);
                float distance = HearDistance(origin);
                float echo = SoundTail.Echo(distance);
                if (echo > 0.001f) PlayAt("boom_far", origin, echo, SoundTail.Pitch(distance));
            }
            else if (type == NoiseType.ZombieScream) PlayAt("scream", origin, 0.55f);
        }

        private static float HearDistance(Vector3 position)
        {
            var listener = PlayerRegistry.Current;
            if (listener == null) return 0f;
            return Vector3.Distance(listener.transform.position, position);
        }

        private AudioSource Rent()
        {
            foreach (var source in pool)
            {
                if (!source.isPlaying) return source;
            }
            var voice = new GameObject("Voice");
            voice.transform.SetParent(transform, false);
            var extra = voice.AddComponent<AudioSource>();
            extra.spatialBlend = 0f;
            extra.playOnAwake = false;
            pool.Add(extra);
            return extra;
        }

        private static bool StepTone(string id)
        {
            return id == "step" || id == "step_hard" || id == "step_metal" || id == "step_wood" || id == "step_water";
        }

        private static float Tone(string id, float t, float noise)
        {
            if (id == "scream") return Mathf.Sin(t * 90f);
            if (id == "pulse") return Mathf.Sin(t * 28f);
            if (id == "rain") return noise;
            if (id == "wind") return noise * Mathf.Sin(t * 6f);
            if (StepTone(id)) return noise * Mathf.Sin(t * 18f);
            if (id == "ui") return Mathf.Sin(t * 40f);
            if (id == "stem_perc") return Mathf.Sin(t * 48f) > 0.65f ? noise : 0f;
            if (id == "stem_combat") return Mathf.Sin(t * 16f);
            if (id == "stinger_kill") return Mathf.Sin(t * 55f);
            if (id == "stinger_death") return Mathf.Sin(t * 8f);
            if (id == "stinger_extract") return Mathf.Sin(t * 32f);
            if (id == "stinger_raid") return noise * Mathf.Sin(t * 12f);
            if (id == "stinger_dawn") return Mathf.Sin(t * 22f);
            if (id == "gun_far") return Mathf.Sin(t * 9f);
            if (id == "boom_far") return noise * Mathf.Sin(t * 4f);
            if (id == "heart") return Mathf.Sin(t * 7f);
            if (id == "breath") return noise * Mathf.Sin(t * 3f);
            if (id == "dry") return Mathf.Sin(t * 90f);
            if (id == "mag_out" || id == "mag_in") return noise * Mathf.Sin(t * 14f);
            if (id == "rack") return Mathf.Sin(t * 28f);
            return noise;
        }

        private AudioClip GetClip(string id)
        {
            if (clips.TryGetValue(id, out var clip)) return clip;
            int rate = 22050;
            bool loop = id == "ambient" || id == "rain" || id == "wind" || id == "stem_perc" || id == "stem_combat";
            float seconds = loop ? 2f : id == "boom_far" ? 0.7f : id == "boom" ? 0.45f : id == "gun_far" ? 0.42f : id == "breath" ? 0.5f : id == "heart" ? 0.22f : id == "dry" ? 0.07f : 0.18f;
            int samples = Mathf.CeilToInt(rate * seconds);
            var data = new float[samples];
            var random = new System.Random(id.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float decay = id == "boom_far" || id == "gun_far" ? 2.4f : id == "boom" ? 4f : id == "heart" || id == "breath" ? 5f : 10f;
                float envelope = loop ? 0.25f : Mathf.Exp(-t * decay);
                float tone = Tone(id, t, noise);
                data[i] = tone * envelope * (loop ? 0.2f : 0.6f);
            }
            clip = AudioClip.Create(id, samples, 1, rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
            return clip;
        }
    }
}
