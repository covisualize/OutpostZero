using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    public enum WeatherKind
    {
        Clear,
        Fog,
        Rain,
        Overcast,
        Storm
    }

    public static class WeatherSurface
    {
        public static float Wetness(WeatherKind kind)
        {
            if (kind == WeatherKind.Rain) return 0.65f;
            if (kind == WeatherKind.Storm) return 0.85f;
            if (kind == WeatherKind.Fog) return 0.2f;
            if (kind == WeatherKind.Overcast) return 0.1f;
            return 0f;
        }

        public static float Sight(WeatherKind kind)
        {
            if (kind == WeatherKind.Fog) return 0.62f;
            if (kind == WeatherKind.Storm) return 0.7f;
            if (kind == WeatherKind.Rain) return 0.8f;
            if (kind == WeatherKind.Overcast) return 0.9f;
            return 1f;
        }
    }

    /// <summary>
    /// Rain leaves four puddles on the avenue. Fog and a dry street leave them hidden.
    /// </summary>
    public static class RainPuddle
    {
        public const int Count = 4;
        public const float Y = 0.03f;

        public struct Spot
        {
            public float X;
            public float Z;
            public float W;
            public float D;
        }

        public static bool Shows(float wetness)
        {
            return wetness >= 0.65f;
        }

        public static Spot At(int index)
        {
            if (index == 1) return new Spot { X = -2.4f, Z = 9f, W = 1.6f, D = 1.05f };
            if (index == 2) return new Spot { X = 3.1f, Z = 14f, W = 1.2f, D = 0.85f };
            if (index == 3) return new Spot { X = -1.2f, Z = 17f, W = 1.8f, D = 1.1f };
            return new Spot { X = 2.2f, Z = 6f, W = 1.4f, D = 0.9f };
        }
    }

    /// <summary>
    /// Three low banks of mist on the avenue. They sit under the eye line.
    /// Fog, a storm, a dark overcast, and deep night raise them. A clear day leaves the street open.
    /// </summary>
    public static class MistBank
    {
        public const int Count = 3;
        public const float Y = 0.45f;
        public const float Tall = 0.7f;
        public const float NightAt = 0.7f;
        public const float OvercastAt = 0.5f;

        public struct Spot
        {
            public float X;
            public float Z;
            public float W;
            public float D;
        }

        public static float Top => Y + Tall * 0.5f;

        public static bool Shows(WeatherKind kind, float night)
        {
            if (night < 0f) night = 0f;
            if (night > 1f) night = 1f;
            if (kind == WeatherKind.Fog || kind == WeatherKind.Storm) return true;
            if (kind == WeatherKind.Overcast) return night >= OvercastAt;
            return night >= NightAt;
        }

        public static Spot At(int index)
        {
            if (index == 1) return new Spot { X = 1.2f, Z = 11f, W = 4.2f, D = 2.4f };
            if (index == 2) return new Spot { X = -2.8f, Z = 15.5f, W = 3.6f, D = 2.1f };
            return new Spot { X = -3.4f, Z = 4.5f, W = 3.8f, D = 2.2f };
        }
    }

    public class WeatherController : MonoBehaviour
    {
        public static WeatherController Instance { get; private set; }
        public static float SightMultiplier => Instance != null ? Instance.multiplier : 1f;

        [SerializeField] private WeatherKind kind = WeatherKind.Clear;
        [SerializeField] private float multiplier = 1f;
        private ParticleSystem rain;
        private ParticleSystem debris;
        private ParticleSystem ash;
        private GameObject puddles;
        private GameObject mist;
        private Material mistMat;
        private string district = "";
        private WeatherKind applied = (WeatherKind)(-1);
        private float nextShift;
        private float lastBolt;
        private bool thunderSent;

        public WeatherKind Kind => kind;
        public string District => district ?? "";

        public void SetDistrict(string id)
        {
            district = id ?? "";
            Apply();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            nextShift = Time.time + 80f;
        }

        private void Update()
        {
            if (Time.time >= nextShift)
            {
                nextShift = Time.time + 90f;
                kind = (WeatherKind)(((int)kind + 1) % 3);
            }
            Apply();
            bool bolt = FlashCap.Due(kind == WeatherKind.Rain, lastBolt, Time.time) || SkyBand.BoltDue(kind, lastBolt, Time.time);
            if (bolt && CombatVfx.Bolt(transform.position + Vector3.up * 18f))
            {
                lastBolt = Time.time;
                thunderSent = false;
                Sensory.StormCover.Strike(lastBolt);
            }
            if (Sensory.StormCover.ThunderDue(lastBolt, Time.time, thunderSent))
            {
                thunderSent = true;
                Vector3 at = transform.position;
                Shell.AudioManager.Instance?.PlayAt("thunder", at, Sensory.StormCover.Volume);
                if (Sensory.NoiseManager.Instance != null)
                    Sensory.NoiseManager.Instance.EmitNoise(at, Sensory.StormCover.Radius, 1f, NoiseType.Thunder);
            }
        }

        public void Set(WeatherKind weather)
        {
            kind = weather;
            Apply();
        }

        public void SetFor(WeatherKind weather, float holdSeconds)
        {
            kind = weather;
            nextShift = Time.time + Mathf.Max(1f, holdSeconds);
            Apply();
        }

        private void Apply()
        {
            float night = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightFactor : 0f;
            float eye = 1.7f;
            var cam = Camera.main;
            if (cam != null) eye = cam.transform.position.y;
            float density = GroundMist.Density(kind, night, eye);
            RenderSettings.fog = density > 0.001f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = GroundMist.Tint(kind, night);
            RenderSettings.fogDensity = density;
            Shader.SetGlobalFloat("_WindStrength", GroundMist.Wind(kind));
            Shader.SetGlobalFloat("_OutpostWet", WeatherSurface.Wetness(kind));
            HoldPuddles(WeatherSurface.Wetness(kind));
            HoldMist(kind, night);
            multiplier = WeatherSurface.Sight(kind);
            if (SkyBand.Rains(kind)) EnsureRain();
            if (rain != null)
            {
                rain.gameObject.SetActive(SkyBand.Rains(kind));
                var emission = rain.emission;
                emission.rateOverTime = kind == WeatherKind.Storm ? 160f : 80f;
            }
            if (kind != WeatherKind.Clear) EnsureDebris();
            if (debris != null) debris.gameObject.SetActive(kind != WeatherKind.Clear);
            if (AshFall.Falls(district)) EnsureAsh();
            if (ash != null) ash.gameObject.SetActive(AshFall.Falls(district));
            if (applied != kind)
            {
                applied = kind;
                PushWetness(WeatherSurface.Wetness(kind));
            }
            ApplyBudget(QualityProfile.For(SettingsService.Instance != null ? SettingsService.Instance.Quality : 1).Particles);
        }

        private static void PushWetness(float wetness)
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Wetness", wetness);
                renderer.SetPropertyBlock(block);
            }
        }

        public void ApplyBudget(int particles)
        {
            int rainCap = Mathf.Max(40, particles);
            int debrisCap = Mathf.Max(20, particles / 4);
            if (rain != null)
            {
                var main = rain.main;
                main.maxParticles = rainCap;
            }
            if (debris != null)
            {
                var main = debris.main;
                main.maxParticles = debrisCap;
            }
        }

        private void HoldPuddles(float wetness)
        {
            if (puddles == null)
            {
                puddles = new GameObject("RainPuddles");
                puddles.transform.SetParent(transform, false);
                for (int i = 0; i < RainPuddle.Count; i++)
                {
                    var spot = RainPuddle.At(i);
                    var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "Puddle";
                    body.transform.SetParent(puddles.transform, false);
                    body.transform.position = new Vector3(spot.X, RainPuddle.Y, spot.Z);
                    body.transform.localScale = new Vector3(spot.W, 0.02f, spot.D);
                    var collider = body.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);
                    var renderer = body.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = new Color(0.1f, 0.12f, 0.14f);
                }
            }
            puddles.SetActive(RainPuddle.Shows(wetness));
        }

        private void HoldMist(WeatherKind weather, float night)
        {
            if (mist == null)
            {
                mist = new GameObject("StreetMist");
                mist.transform.SetParent(transform, false);
                var source = Resources.Load<Material>("OutpostMist");
                if (source == null)
                {
                    var shader = Shader.Find("OutpostZero/Mist");
                    if (shader != null) source = new Material(shader);
                }
                if (source != null) mistMat = new Material(source);
                for (int i = 0; i < MistBank.Count; i++)
                {
                    var spot = MistBank.At(i);
                    var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "Mist";
                    body.transform.SetParent(mist.transform, false);
                    body.transform.position = new Vector3(spot.X, MistBank.Y, spot.Z);
                    body.transform.localScale = new Vector3(spot.W, MistBank.Tall, spot.D);
                    var collider = body.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);
                    var renderer = body.GetComponent<Renderer>();
                    if (renderer == null) continue;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    if (mistMat != null) renderer.sharedMaterial = mistMat;
                }
            }
            if (mistMat != null)
            {
                Color tint = GroundMist.Tint(weather, night);
                tint.a = 0.22f;
                mistMat.color = tint;
            }
            mist.SetActive(MistBank.Shows(weather, night));
        }

        private void EnsureRain()
        {
            if (rain != null) return;
            var go = new GameObject("Rain");
            go.transform.SetParent(transform);
            rain = go.AddComponent<ParticleSystem>();
            var main = rain.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 12f;
            main.startSize = 0.05f;
            main.maxParticles = QualityProfile.For(1).Particles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = rain.emission;
            emission.rateOverTime = 80f;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 30f);
            go.transform.position = new Vector3(0f, 12f, 0f);
        }

        private void EnsureDebris()
        {
            if (debris != null) return;
            var go = new GameObject("WindDebris");
            go.transform.SetParent(transform);
            debris = go.AddComponent<ParticleSystem>();
            var main = debris.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 3.5f;
            main.startSize = 0.08f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.15f;
            var emission = debris.emission;
            emission.rateOverTime = 8f;
            var shape = debris.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 4f, 24f);
            var velocity = debris.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(2.4f);
            go.transform.position = new Vector3(0f, 3f, 0f);
        }

        private void EnsureAsh()
        {
            if (ash != null) return;
            var go = new GameObject("AshFall");
            go.transform.SetParent(transform);
            ash = go.AddComponent<ParticleSystem>();
            var main = ash.main;
            main.startLifetime = AshFall.Life;
            main.startSpeed = AshFall.Drift;
            main.startSize = 0.06f;
            main.startColor = new Color(0.45f, 0.4f, 0.36f, 0.7f);
            main.maxParticles = AshFall.Flakes * 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.08f;
            var emission = ash.emission;
            emission.rateOverTime = AshFall.Flakes;
            var shape = ash.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 6f, 28f);
            go.transform.position = new Vector3(0f, 8f, 0f);
        }
    }
}
