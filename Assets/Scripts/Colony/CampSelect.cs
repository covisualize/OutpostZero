using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Outside build mode, a click on the camp ground picks a colonist or a module (<see cref="CampPick"/>) and
    /// rings it on the ground. The camp board reads the pick for its Selected card and marks the colonist's row.
    /// Clicks over the side panel are left to the board.
    /// </summary>
    public class CampSelect : MonoBehaviour
    {
        public const float PanelWidth = 400f;

        public static CampSelect Instance { get; private set; }

        private string survivorId = "";
        private PlacedModule module;
        private Transform ring;
        private int version;

        public string SurvivorId => survivorId;
        public PlacedModule Module => module;
        public int Version => version;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ring != null) Destroy(ring.gameObject);
        }

        public string Card(string language)
        {
            if (survivorId.Length > 0)
                return CampPick.Mate(SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Find(survivorId) : null, language);
            return CampPick.Module(module, language);
        }

        public void Clear()
        {
            if (survivorId.Length == 0 && module == null) return;
            survivorId = "";
            module = null;
            version++;
        }

        private void Update()
        {
            bool camp = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.CampManagement;
            if (!camp)
            {
                Clear();
                Show(false, Vector3.zero, 0f);
                return;
            }
            bool building = GridBuilder.Instance != null && GridBuilder.Instance.BuildMode;
            if (!building && Player.ExpeditionInput.BuildPlacePressed && Player.ExpeditionInput.Pointer.x <= Screen.width - PanelWidth) PickAtPointer();
            Hold();
        }

        private void PickAtPointer()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Player.ExpeditionInput.Pointer);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 point = ray.GetPoint(enter);

            var ids = new List<string>();
            var xs = new List<float>();
            var zs = new List<float>();
            var roster = SurvivorRoster.Instance;
            var yard = CampPopulation.Instance;
            if (roster != null && yard != null)
            {
                foreach (var survivor in roster.Survivors)
                {
                    if (survivor == null || !survivor.alive || !yard.TryStand(survivor.id, out Vector3 at)) continue;
                    ids.Add(survivor.id);
                    xs.Add(at.x);
                    zs.Add(at.z);
                }
            }
            int mate = CampPick.Nearest(point.x, point.z, xs, zs, CampPick.Reach);
            string nextId = mate >= 0 ? ids[mate] : "";
            PlacedModule nextModule = null;
            if (mate < 0 && GridBuilder.Instance != null)
            {
                float snapX = BuildGhost.Snap(point.x, 2f);
                float snapZ = BuildGhost.Snap(point.z, 2f);
                nextModule = GridBuilder.Under(GridBuilder.Instance.Placed, snapX, snapZ, point.x, point.z);
            }
            if (nextId == survivorId && nextModule == module) return;
            survivorId = nextId;
            module = nextModule;
            version++;
        }

        private void Hold()
        {
            if (survivorId.Length > 0)
            {
                var roster = SurvivorRoster.Instance;
                var survivor = roster != null ? roster.Find(survivorId) : null;
                if (survivor == null || !survivor.alive || CampPopulation.Instance == null || !CampPopulation.Instance.TryStand(survivorId, out Vector3 at))
                {
                    Clear();
                    Show(false, Vector3.zero, 0f);
                    return;
                }
                Show(true, at, 1.1f);
                return;
            }
            if (module != null)
            {
                bool standing = false;
                var placed = GridBuilder.Instance != null ? GridBuilder.Instance.Placed : null;
                if (placed != null)
                    for (int i = 0; i < placed.Count; i++)
                        if (placed[i] == module) standing = true;
                if (!standing)
                {
                    Clear();
                    Show(false, Vector3.zero, 0f);
                    return;
                }
                ModuleFootprint.Span(module.kind, module.rotation, out int alongX, out int alongZ);
                float wide = Mathf.Max(alongX, alongZ) * ModuleFootprint.Unit * 1.2f + 0.4f;
                Show(true, new Vector3(module.x, 0f, module.z), wide);
                return;
            }
            Show(false, Vector3.zero, 0f);
        }

        private void Show(bool on, Vector3 at, float size)
        {
            if (!on)
            {
                if (ring != null && ring.gameObject.activeSelf) ring.gameObject.SetActive(false);
                return;
            }
            if (ring == null)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "CampSelectRing";
                var collider = disc.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                var renderer = disc.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.95f, 0.78f, 0.3f, 0.6f);
                ring = disc.transform;
            }
            if (!ring.gameObject.activeSelf) ring.gameObject.SetActive(true);
            ring.position = new Vector3(at.x, 0.03f, at.z);
            ring.localScale = new Vector3(size, 0.01f, size);
        }
    }
}
