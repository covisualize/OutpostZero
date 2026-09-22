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
            if (id == "ambient" || id == "pulse" || id == "ui" || id == "rain" || id == "storm" || id == "wind" || id == "ash" || id == "heart" || id == "breath" || id == "pained") return 0f;
            if (id != null && id.StartsWith("step")) return 0.35f;
            return 1f;
        }

        public static float MaxDistance(string id)
        {
            if (id == "boom" || id == "boom_far" || id == "thunder") return 48f;
            if (id == "scream") return 36f;
            if (id == "shriek" || id == "roar" || id == "stomp") return 40f;
            if (id == "gun" || id == "shotgun" || id == "rifle" || id == "smg" || id == "gun_far") return 32f;
            if (id == "groan" || id == "snarl" || id == "grunt") return 22f;
            if (id == "hum" || id == "crackle" || id == "buzz") return Colony.YardBed.Reach;
            if (id == "flies") return FlyBed.Reach;
            if (id == "hiss") return 20f;
            if (id == "clink" || id == "clack") return 6f;
            if (id == "spark") return 16f;
            if (id == "splinter" || id == "spray") return 12f;
            if (id == "dust") return 8f;
            if (id == "mist") return 10f;
            if (id == "burn") return 14f;
            if (id == "cloud") return 12f;
            if (id == "splash") return 8f;
            if (id == "spit") return 8f;
            if (id == "drip") return 6f;
            if (id == "creak") return 12f;
            if (id == "bite") return 6f;
            if (id == "cough") return 10f;
            if (id == "whoosh") return 14f;
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
        private float stepEvent;
        private bool peaked;
        private int streak;
        private float streakAt;
        private float nextHeart;
        private float nextBreath;
        private readonly float[] groanSeats = new float[ZombieVoice.Cap];
        private AudioSource hum;
        private AudioSource crackle;
        private AudioSource buzz;
        private AudioSource flies;
        private float[] flyDistances = System.Array.Empty<float>();

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
            hum = AddWorld("hum");
            crackle = AddWorld("crackle");
            buzz = AddWorld("buzz");
            flies = AddWorld("flies");
            flies.maxDistance = FlyBed.Reach;
        }

        private AudioSource AddWorld(string id)
        {
            var go = new GameObject(id);
            go.transform.SetParent(transform, false);
            var bed = go.AddComponent<AudioSource>();
            bed.loop = true;
            bed.spatialBlend = 1f;
            bed.playOnAwake = false;
            bed.clip = GetClip(id);
            bed.volume = 0f;
            bed.minDistance = 2f;
            bed.maxDistance = Colony.YardBed.Reach;
            bed.rolloffMode = AudioRolloffMode.Linear;
            return bed;
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
            UpdateYard(snapshot, music, sfx, ambience, ui);
            UpdateFlies(snapshot, music, sfx, ambience, ui);
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

        private float lastOpen;

        public void PlayAt(string id, Vector3 position, float volume = 1f, float pitch = 0f)
        {
            var source = Rent();
            source.transform.position = position;
            source.spatialBlend = AudioSpace.SpatialBlend(id);
            source.minDistance = 1.5f;
            source.maxDistance = AudioSpace.MaxDistance(id);
            source.rolloffMode = AudioRolloffMode.Linear;
            if (pitch > 0f) source.pitch = pitch;
            else
            {
                lastOpen = PitchGate.Next(lastOpen, Random.value);
                source.pitch = lastOpen;
            }
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
            string district = WeatherController.Instance != null ? WeatherController.Instance.District : "";
            string id = AshFall.Bed(kind, district);
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

        private void UpdateYard(MixSnapshot snapshot, float music, float sfx, float ambience, float ui)
        {
            var grid = Colony.GridBuilder.Instance;
            var modules = grid != null ? grid.Placed : null;
            bool fire = Colony.YardBed.Spot(modules, "Campfire", out float fireX, out float fireZ);
            bool lamp = Colony.YardBed.Spot(modules, "Lamp", out float lampX, out float lampZ);
            bool generator = Colony.YardBed.Spot(modules, "Generator", out float genX, out float genZ);
            var camp = Colony.CampServices.Instance;
            if (camp != null && !camp.GeneratorOnline) generator = false;
            if (camp != null && camp.GeneratorOnline && !generator)
            {
                generator = true;
                genX = camp.transform.position.x;
                genZ = camp.transform.position.z;
            }
            Colony.YardBed.Mix(generator, fire, lamp, out float humGain, out float crackleGain, out float buzzGain);
            Hold(hum, "hum", genX, genZ, humGain, snapshot, music, sfx, ambience, ui);
            Hold(crackle, "crackle", fireX, fireZ, crackleGain, snapshot, music, sfx, ambience, ui);
            Hold(buzz, "buzz", lampX, lampZ, buzzGain, snapshot, music, sfx, ambience, ui);
        }

        private void UpdateFlies(MixSnapshot snapshot, float music, float sfx, float ambience, float ui)
        {
            var live = FlyMark.Live;
            var listener = PlayerRegistry.Current;
            int count = live.Count;
            if (listener == null || count == 0)
            {
                Hold(flies, "flies", 0f, 0f, 0f, snapshot, music, sfx, ambience, ui);
                return;
            }
            if (flyDistances.Length != count) flyDistances = new float[count];
            Vector3 ear = listener.transform.position;
            for (int i = 0; i < count; i++)
            {
                var mark = live[i];
                if (mark == null)
                {
                    flyDistances[i] = FlyBed.Reach + 1f;
                    continue;
                }
                Vector3 at = mark.transform.position;
                float dx = at.x - ear.x;
                float dz = at.z - ear.z;
                flyDistances[i] = Mathf.Sqrt(dx * dx + dz * dz);
            }
            int best = FlyBed.Nearest(flyDistances);
            if (best < 0 || live[best] == null)
            {
                Hold(flies, "flies", 0f, 0f, 0f, snapshot, music, sfx, ambience, ui);
                return;
            }
            Vector3 bin = live[best].transform.position;
            Hold(flies, "flies", bin.x, bin.z, FlyBed.Gain(flyDistances[best]), snapshot, music, sfx, ambience, ui);
        }

        private void Hold(AudioSource source, string id, float x, float z, float gain, MixSnapshot snapshot, float music, float sfx, float ambience, float ui)
        {
            if (source == null) return;
            var at = new Vector3(x, 0f, z);
            source.transform.position = at;
            bool wall = gain > 0f && BehindWall(id, at);
            float heard = gain <= 0f ? 0f : EarWall.Gain(gain, wall, false);
            source.volume = AudioMix.Gain(id, heard, 1f, music, sfx, ambience, ui, snapshot);
            ApplyLowpass(source, snapshot, wall);
            if (source.volume > 0.001f)
            {
                if (!source.isPlaying) source.Play();
            }
            else if (source.isPlaying)
            {
                source.Stop();
            }
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

        public void Footfall()
        {
            stepEvent = Time.time;
            PlayStep();
        }

        private void Step()
        {
            if (!Player.StepGate.AllowTimer(Time.time, stepEvent)) return;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var body = player.GetComponent<CharacterController>();
            if (body == null || body.velocity.magnitude < 0.8f) return;
            if (Time.time < nextStep) return;
            float interval = player.IsSprinting ? 0.28f : player.IsCrouching ? 0.55f : 0.42f;
            nextStep = Time.time + interval;
            PlayStep();
        }

        private void PlayStep()
        {
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var body = player.GetComponent<CharacterController>();
            if (body == null || body.velocity.magnitude < 0.8f) return;
            string step = AudioMix.StepId("");
            if (Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, out var hit, 2.2f, GameLayers.VisionOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                var tag = SurfaceTag.Of(hit.collider);
                step = tag != null && tag.Kind != SurfaceKind.Default ? SurfaceTag.StepId(tag.Kind) : AudioMix.StepId(hit.collider.name);
            }
            if (OutpostZero.Expedition.GlassShard.Covers(player.transform.position.x, player.transform.position.z))
                step = "step_glass";
            bool hard = step == "step_hard" || step == "step_metal";
            float pitch = hard ? Random.Range(1.05f, 1.2f) : Random.Range(0.85f, 1f);
            PlayAt(step, player.transform.position, player.IsCrouching ? 0.12f : 0.28f, pitch);
            var weather = WeatherController.Instance;
            float wetness = weather != null ? WeatherSurface.Wetness(weather.Kind) : 0f;
            bool soaked = Combat.WoundShow.Soaked(wetness);
            int puffs = Combat.WoundShow.Puffs(player.IsSprinting, player.IsCrouching, soaked);
            if (puffs > 0) Combat.CombatVfx.Puff(player.transform.position, puffs, soaked);
            if (soaked && !player.IsCrouching) PlayAt("splash", player.transform.position, player.IsSprinting ? 0.3f : Combat.WoundShow.Splash);
            ImpactDecalPool.Instance?.StampBoot(player.transform.position, player.transform.right);
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
            string id = ClipBook.Fire(weapon.Type);
            PlayAt(id, muzzle, 0.8f);
            if (id == "swing") return;
            float distance = HearDistance(muzzle);
            float tail = SoundTail.Gun(distance);
            if (tail > 0.001f) PlayAt("gun_far", muzzle, tail, SoundTail.Pitch(distance));
        }

        private void OnHit(Vector3 point, Vector3 normal, GameObject target)
        {
            bool melee = CombatEvents.FromWeapon && CombatEvents.LastWeapon == WeaponType.Melee;
            bool barrel = target != null && target.GetComponentInParent<DestructibleHazard>() != null;
            if (melee)
            {
                PlayAt(barrel ? "clang" : "chop", point, 0.5f);
                return;
            }
            string face = Combat.StrikeFace.Of(target);
            PlayAt(Combat.StrikeFace.Sound(face), point, Combat.StrikeFace.Volume(face));
        }

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
            else if (type == NoiseType.BleedDrip) PlayAt("drip", origin, 0.2f);
            else if (type == NoiseType.DoorSwing) PlayAt("creak", origin, 0.45f);
            else if (type == NoiseType.RationBite) PlayAt("bite", origin, 0.24f);
        }

        public void Groan(string breed, Vector3 at, float now, float last, out float next)
        {
            next = last;
            if (PlayerRegistry.Current == null) return;
            float distance = HearDistance(at);
            if (!ZombieVoice.IdleDue(distance, ZombieVoice.Live(now, groanSeats), now, last)) return;
            ZombieVoice.Seat(groanSeats, now);
            next = now;
            float pitch = breed == "brute" ? 0.55f : breed == "runner" ? 1.12f : 0f;
            PlayAt(ZombieVoice.Idle(breed), at, breed == "brute" ? 0.7f : 0.4f, pitch);
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

        private static float Tone(string id, float t, float noise)
        {
            return ClipBook.Tone(id, t, noise);
        }

        private AudioClip GetClip(string id)
        {
            if (clips.TryGetValue(id, out var clip)) return clip;
            int rate = 22050;
            bool loop = id == "ambient" || id == "rain" || id == "storm" || id == "wind" || id == "ash" || id == "stem_perc" || id == "stem_combat" || id == "hum" || id == "crackle" || id == "buzz" || id == "flies";
            float seconds = loop ? 2f : id == "boom_far" || id == "thunder" ? 0.7f : id == "roar" || id == "stomp" ? 0.5f : id == "boom" ? 0.45f : id == "gun_far" ? 0.42f : id == "breath" || id == "groan" ? 0.5f : id == "shriek" ? 0.28f : id == "heart" || id == "hiss" ? 0.22f : id == "dry" || id == "take_soft" || id == "take_box" || id == "take_metal" || id == "clink" || id == "clack" || id == "spit" ? 0.08f : id == "burn" || id == "cloud" ? 0.5f : 0.18f;
            int samples = Mathf.CeilToInt(rate * seconds);
            var data = new float[samples];
            var random = new System.Random(id.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float decay = id == "boom_far" || id == "gun_far" || id == "roar" || id == "stomp" ? 2.4f : id == "boom" ? 4f : id == "heart" || id == "breath" ? 5f : 10f;
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
