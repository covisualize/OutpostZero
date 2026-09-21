using UnityEngine;

namespace OutpostZero.Graphics
{
    public enum WeatherKind
    {
        Clear,
        Fog,
        Rain
    }

    public class WeatherController : MonoBehaviour
    {
        public static WeatherController Instance { get; private set; }
        public static float SightMultiplier => Instance != null ? Instance.multiplier : 1f;

        [SerializeField] private WeatherKind kind = WeatherKind.Clear;
        [SerializeField] private float multiplier = 1f;
        private ParticleSystem rain;
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
            multiplier = kind == WeatherKind.Fog ? 0.62f : kind == WeatherKind.Rain ? 0.8f : 1f;
            if (kind == WeatherKind.Rain) EnsureRain();
            if (rain != null) rain.gameObject.SetActive(kind == WeatherKind.Rain);
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
    }
}
