using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    public enum ModuleKind
    {
        Barricade,
        Cot,
        Water,
        Watchtower
    }

    [Serializable]
    public class PlacedModule
    {
        public string kind;
        public float x;
        public float z;
        public int rotation;
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
            int cost = Cost(kind);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(cost))
            {
                GameplayFeedback.Toast("Need " + cost + " camp scrap");
                return false;
            }

            float x = Mathf.Round(world.x / cell) * cell;
            float z = Mathf.Round(world.z / cell) * cell;
            var record = new PlacedModule { kind = kind.ToString(), x = x, z = z, rotation = 0 };
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
            view.transform.position = new Vector3(module.x, 0.6f, module.z);
            view.transform.rotation = Quaternion.Euler(0f, module.rotation, 0f);
            view.transform.localScale = Scale(module.kind);
            var renderer = view.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = ColorFor(module.kind);
            }
            var obstacle = view.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
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

        public static int Cost(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Barricade: return 6;
                case ModuleKind.Cot: return 8;
                case ModuleKind.Water: return 10;
                case ModuleKind.Watchtower: return 16;
                default: return 6;
            }
        }

        private static Vector3 Scale(string kind)
        {
            switch (kind)
            {
                case "Cot": return new Vector3(1.4f, 0.4f, 0.7f);
                case "Water": return new Vector3(0.8f, 1.1f, 0.8f);
                case "Watchtower": return new Vector3(1.2f, 2.4f, 1.2f);
                default: return new Vector3(1.8f, 1.1f, 0.4f);
            }
        }

        private static Color ColorFor(string kind)
        {
            switch (kind)
            {
                case "Cot": return new Color(0.45f, 0.55f, 0.62f);
                case "Water": return new Color(0.25f, 0.55f, 0.75f);
                case "Watchtower": return new Color(0.42f, 0.36f, 0.28f);
                default: return new Color(0.48f, 0.42f, 0.32f);
            }
        }
    }
}
