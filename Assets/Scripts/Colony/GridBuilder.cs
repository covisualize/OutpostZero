using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    public enum ModuleKind
    {
        Barricade,
        Cot,
        Water,
        Watchtower,
        Generator,
        Workbench,
        TradingPost,
        Farm,
        Purifier,
        Turret,
        Spikes
    }

    [Serializable]
    public class PlacedModule
    {
        public string kind;
        public float x;
        public float z;
        public int rotation;
        public int integrity = 100;
        public int age;
    }

    public class GridBuilder : MonoBehaviour
    {
        public static GridBuilder Instance { get; private set; }

        [SerializeField] private float cell = 2f;
        [SerializeField] private List<PlacedModule> placed = new List<PlacedModule>();
        private readonly List<GameObject> views = new List<GameObject>();
        private ModuleKind selected = ModuleKind.Barricade;
        private bool buildMode;

        public bool BuildMode => buildMode;
        public ModuleKind Selected => selected;
        public IReadOnlyList<PlacedModule> Placed => placed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement) return;
            if (Player.ExpeditionInput.BuildPressed)
            {
                buildMode = !buildMode;
                GameplayFeedback.Toast(buildMode ? "Build mode: click the yard" : "Build mode off");
            }
            if (!buildMode || !PointerPressed()) return;
            if (Player.ExpeditionInput.Pointer.x > Screen.width - 400f) return;
            TryPlaceAtPointer();
        }

        private static bool PointerPressed()
        {
            return UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        }

        public void Select(ModuleKind kind) => selected = kind;

        public void Grow()
        {
            var plots = new CampYield.Plot[placed.Count];
            for (int i = 0; i < placed.Count; i++)
            {
                plots[i].Kind = placed[i].kind;
                plots[i].Age = placed[i].age;
                plots[i].Integrity = placed[i].integrity;
            }
            CampYield.Advance(plots);
            bool rain = WeatherController.Instance != null && WeatherController.Instance.Kind == WeatherKind.Rain;
            CampYield.Produce(plots, rain, out int food, out int water);
            for (int i = 0; i < placed.Count; i++) placed[i].age = plots[i].Age;
            if (ColonyStorage.Instance == null) return;
            if (food > 0) ColonyStorage.Instance.AddFood(food);
            if (water > 0) ColonyStorage.Instance.AddWater(water);
        }

        public void TryPlaceAtPointer()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Player.ExpeditionInput.Pointer);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 point = ray.GetPoint(enter);
            TryPlace(selected, point);
        }

        public bool TryPlace(ModuleKind kind, Vector3 world)
        {
            float x = Mathf.Round(world.x / cell) * cell;
            float z = Mathf.Round(world.z / cell) * cell;
            if (Occupied(placed, x, z))
            {
                GameplayFeedback.Toast("That square is taken");
                return false;
            }

            int cost = Cost(kind);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(cost))
            {
                GameplayFeedback.Toast("Need " + cost + " camp scrap");
                return false;
            }
            var record = new PlacedModule { kind = kind.ToString(), x = x, z = z, rotation = 0, integrity = 100 };
            placed.Add(record);
            SpawnView(record);
            GameplayFeedback.Toast("Placed " + kind);
            return true;
        }

        public void Restore(PlacedModule[] modules)
        {
            ClearViews();
            placed = modules == null ? new List<PlacedModule>() : new List<PlacedModule>(modules);
            foreach (var module in placed) SpawnView(module);
        }

        public void ClearAll()
        {
            placed.Clear();
            ClearViews();
        }

        private void SpawnView(PlacedModule module)
        {
            var view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = "Module_" + module.kind;
            view.transform.position = new Vector3(module.x, module.kind == "Spikes" ? 0.04f : 0.6f, module.z);
            view.transform.rotation = Quaternion.Euler(0f, module.rotation, 0f);
            view.transform.localScale = Scale(module.kind, module.integrity);
            var renderer = view.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = ColorFor(module.kind);
            }
            if (module.kind == "Spikes")
            {
                var pad = view.GetComponent<Collider>();
                if (pad != null) Destroy(pad);
            }
            else
            {
                var obstacle = view.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape = NavMeshObstacleShape.Box;
            }
            if (module.kind == "Barricade")
            {
                view.layer = GameLayers.Environment;
                view.AddComponent<Combat.StreetBoard>().LinkToCamp();
            }
            views.Add(view);
        }

        private void ClearViews()
        {
            foreach (var view in views)
            {
                if (view != null) Destroy(view);
            }
            views.Clear();
        }

        public int CountKind(string kind)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].kind == kind && placed[i].integrity > 0) count++;
            }
            return count;
        }

        public int CoverCount(string approach)
        {
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Barricade" || module.integrity <= 0) continue;
                if (RaidPlan.Covers(ax, az, module.x, module.z)) count++;
            }
            return count;
        }

        public bool StrikeBarricade(int amount)
        {
            return StrikeFrom("gate", amount);
        }

        public bool StrikeAt(float x, float z, float amount)
        {
            PlacedModule target = null;
            float best = 6.25f;
            foreach (var module in placed)
            {
                if (module.kind != "Barricade" || module.integrity <= 0) continue;
                float dx = module.x - x;
                float dz = module.z - z;
                float distance = dx * dx + dz * dz;
                if (distance > best) continue;
                best = distance;
                target = module;
            }
            if (target == null) return false;
            target.integrity = Mathf.Max(0, target.integrity - Mathf.Max(1, Mathf.RoundToInt(amount)));
            if (target.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(target);
            RefreshViews();
            GameplayFeedback.Toast("A barricade gave way");
            return true;
        }

        public bool StrikeFrom(string approach, int amount)
        {
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            PlacedModule target = null;
            float best = float.MaxValue;
            foreach (var module in placed)
            {
                if (module.kind != "Barricade" || module.integrity <= 0) continue;
                float dx = module.x - ax;
                float dz = module.z - az;
                float distance = dx * dx + dz * dz;
                if (distance >= best) continue;
                best = distance;
                target = module;
            }
            if (target == null) return false;
            target.integrity = Mathf.Max(0, target.integrity - Mathf.Max(1, amount));
            if (target.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(target);
            RefreshViews();
            GameplayFeedback.Toast("A barricade gave way");
            return true;
        }

        public bool Chip(PlacedModule module, int amount)
        {
            if (module == null || !placed.Contains(module)) return false;
            module.integrity = TrapHit.WearDown(module.integrity, amount);
            if (module.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(module);
            RefreshViews();
            GameplayFeedback.Toast("The spikes broke");
            return true;
        }

        public bool HasKind(string kind)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].kind == kind && placed[i].integrity > 0) return true;
            }
            return false;
        }

        public static bool Occupied(IReadOnlyList<PlacedModule> modules, float x, float z)
        {
            if (modules == null) return false;
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null) continue;
                if (Mathf.Abs(module.x - x) < 0.01f && Mathf.Abs(module.z - z) < 0.01f) return true;
            }
            return false;
        }

        public int BarricadeCount()
        {
            int count = 0;
            foreach (var module in placed)
            {
                if (module.kind == "Barricade" && module.integrity > 0) count++;
            }
            return count;
        }

        private void RefreshViews()
        {
            var copy = placed.ToArray();
            Restore(copy);
        }

        public static int Cost(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Barricade: return 6;
                case ModuleKind.Cot: return 8;
                case ModuleKind.Water: return 10;
                case ModuleKind.Watchtower: return 16;
                case ModuleKind.Generator: return 14;
                case ModuleKind.Workbench: return 12;
                case ModuleKind.TradingPost: return 20;
                case ModuleKind.Farm: return 18;
                case ModuleKind.Purifier: return 15;
                case ModuleKind.Turret: return 22;
                case ModuleKind.Spikes: return 8;
                default: return 6;
            }
        }

        private static Vector3 Scale(string kind, int integrity)
        {
            float health = Mathf.Clamp01((integrity <= 0 ? 100 : integrity) / 100f);
            switch (kind)
            {
                case "Cot": return new Vector3(1.4f, 0.4f, 0.7f);
                case "Water": return new Vector3(0.8f, 1.1f, 0.8f);
                case "Watchtower": return new Vector3(1.2f, 2.4f, 1.2f);
                case "Generator": return new Vector3(1.1f, 0.8f, 0.7f);
                case "Workbench": return new Vector3(1.6f, 0.9f, 0.8f);
                case "TradingPost": return new Vector3(1.8f, 1.4f, 1.2f);
                case "Farm": return new Vector3(2.2f, 0.25f, 2.2f);
                case "Purifier": return new Vector3(0.7f, 1.3f, 0.7f);
                case "Turret": return new Vector3(0.45f, 1.5f, 0.45f);
                case "Spikes": return new Vector3(1.6f, 0.08f, 1.6f);
                default: return new Vector3(1.8f * health, 1.1f * Mathf.Lerp(0.35f, 1f, health), 0.4f);
            }
        }

        private static Color ColorFor(string kind)
        {
            switch (kind)
            {
                case "Cot": return new Color(0.45f, 0.55f, 0.62f);
                case "Water": return new Color(0.25f, 0.55f, 0.75f);
                case "Watchtower": return new Color(0.42f, 0.36f, 0.28f);
                case "Generator": return new Color(0.55f, 0.48f, 0.18f);
                case "Workbench": return new Color(0.38f, 0.32f, 0.26f);
                case "TradingPost": return new Color(0.55f, 0.32f, 0.22f);
                case "Farm": return new Color(0.28f, 0.48f, 0.24f);
                case "Purifier": return new Color(0.35f, 0.7f, 0.78f);
                case "Turret": return new Color(0.22f, 0.24f, 0.28f);
                case "Spikes": return new Color(0.35f, 0.36f, 0.38f);
                default: return new Color(0.48f, 0.42f, 0.32f);
            }
        }
    }
}
