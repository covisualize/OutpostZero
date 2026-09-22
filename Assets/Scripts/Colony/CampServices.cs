using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Sanctuary machines change the simulation: the generator keeps the lamps lit,
    /// the water collector and cot sustain the leader in camp, and the watchtower
    /// reports infected inside an extended radius.
    /// </summary>
    public class CampServices : MonoBehaviour
    {
        public static CampServices Instance { get; private set; }

        [SerializeField] private float fuelHours = 10f;
        private bool generatorPresent;
        private bool waterPresent;
        private bool cotPresent;
        private bool towerPresent;
        private bool lightsOn;
        private float scanIn;

        public bool GeneratorOnline => FuelTank.Lit(generatorPresent, fuelHours);
        public bool WaterOnline => waterPresent;
        public bool CotOnline => cotPresent;
        public bool WatchtowerOnline => towerPresent;
        public float FuelHours => fuelHours;
        public int Contacts { get; private set; }
        public float DetectionRadius => towerPresent ? 28f : 10f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Refuel(float hours)
        {
            fuelHours = FuelTank.Pour(fuelHours, hours);
        }

        public void SetFuel(float hours)
        {
            fuelHours = FuelTank.Clamp(hours);
        }

        public float BurnTrip(float travel)
        {
            float before = fuelHours;
            fuelHours = FuelTank.Trip(fuelHours, travel);
            float burned = before - fuelHours;
            return burned < 0f ? 0f : burned;
        }

        private void Update()
        {
            if (Time.time >= scanIn)
            {
                scanIn = Time.time + 1.5f;
                Scan();
            }

            float night = Graphics.DayNightCycle.Instance != null ? Graphics.DayNightCycle.Instance.NightFactor : 0f;
            float before = fuelHours;
            fuelHours = FuelTank.Drink(fuelHours, Time.deltaTime, generatorPresent, night);
            var skyKind = OutpostZero.Graphics.WeatherController.Instance != null
                ? OutpostZero.Graphics.WeatherController.Instance.Kind
                : OutpostZero.Graphics.WeatherKind.Clear;
            fuelHours = StormBurn.After(before, fuelHours, skyKind);

            ApplyLights();

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement)
            {
                return;
            }

            var player = PlayerRegistry.Current;
            if (player == null) return;
            if (WaterOnline)
            {
                player.GetComponent<SurvivalNeeds>()?.Drink(4f * Time.deltaTime);
                if (ColonyStorage.Instance != null && Random.value < Time.deltaTime * 0.05f) ColonyStorage.Instance.AddWater(1);
            }
            if (CotOnline)
            {
                player.GetComponent<Combat.HealthSystem>()?.Heal(3f * Time.deltaTime);
            }
        }

        private void Scan()
        {
            generatorPresent = false;
            waterPresent = false;
            cotPresent = false;
            towerPresent = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.name == "DontDestroyOnLoad") continue;
                foreach (var root in scene.GetRootGameObjects()) Note(root);
            }

            if (GridBuilder.Instance != null)
            {
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (module.kind == "Water" && BuildSite.Ready(module.site, module.integrity)) waterPresent = true;
                    if (module.kind == "Cot" && BuildSite.Ready(module.site, module.integrity)) cotPresent = true;
                    if (module.kind == "Watchtower" && BuildSite.Ready(module.site, module.integrity)) towerPresent = true;
                    if (module.kind == "Generator" && BuildSite.Ready(module.site, module.integrity)) generatorPresent = true;
                }
            }
            CountContacts();
        }

        private void Note(GameObject go)
        {
            string name = go.name;
            if (name.Contains("Generator")) generatorPresent = true;
            if (name.Contains("Water")) waterPresent = true;
            if (name.Contains("Cot") || name.Contains("Medical")) cotPresent = true;
            if (name.Contains("Watchtower") || name.Contains("Tower")) towerPresent = true;
            foreach (Transform child in go.transform) Note(child.gameObject);
        }

        private void ApplyLights()
        {
            bool powered = GeneratorOnline;
            if (powered == lightsOn && scanIn > 1f) return;
            lightsOn = powered;
            var player = PlayerRegistry.Current;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional) continue;
                if (player != null && light.transform.IsChildOf(player.transform)) continue;
                if (light.gameObject.name.StartsWith("Module_Lamp")) continue;
                if (light.gameObject.name.StartsWith("Module_Campfire")) continue;
                light.enabled = powered;
            }
        }

        private void CountContacts()
        {
            Vector3 gate = new Vector3(-12f, 0f, -12f);
            int count = 0;
            var zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            foreach (var zombie in zombies)
            {
                if (zombie.CurrentState == ZombieAI.ZombieState.Dead) continue;
                if (Vector3.Distance(zombie.transform.position, gate) <= DetectionRadius) count++;
            }
            Contacts = count;
        }
    }
}
