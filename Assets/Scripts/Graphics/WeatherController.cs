using UnityEngine;

namespace OutpostZero.Graphics
{
    public enum WeatherKind
    {
        Clear,
        Fog,
        Rain
    }

    public static class WeatherSurface
    {
        public static float Wetness(WeatherKind kind)
        {
            if (kind == WeatherKind.Rain) return 0.65f;
            if (kind == WeatherKind.Fog) return 0.2f;
            return 0f;
        }

        public static float Sight(WeatherKind kind)
        {
            if (kind == WeatherKind.Fog) return 0.62f;
            if (kind == WeatherKind.Rain) return 0.8f;
            return 1f;
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
        private WeatherKind applied = (WeatherKind)(-1);
        private float nextShift;

        public WeatherKind Kind => kind;

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
            bool fog = kind != WeatherKind.Clear;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = kind == WeatherKind.Rain ? new Color(0.35f, 0.38f, 0.42f) : new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.fogDensity = kind == WeatherKind.Fog ? 0.045f : kind == WeatherKind.Rain ? 0.02f : 0f;
            multiplier = WeatherSurface.Sight(kind);
            if (kind == WeatherKind.Rain) EnsureRain();
            if (rain != null) rain.gameObject.SetActive(kind == WeatherKind.Rain);
            if (kind != WeatherKind.Clear) EnsureDebris();
            if (debris != null) debris.gameObject.SetActive(kind != WeatherKind.Clear);
            if (applied != kind)
            {
                applied = kind;
                PushWetness(WeatherSurface.Wetness(kind));
            }
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
            main.maxParticles = 400;
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
    }
}
