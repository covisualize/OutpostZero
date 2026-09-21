using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Builds the district's stalls, barrels, and caches on the street when an expedition opens.
    /// The sanctuary yard is left alone.
    /// </summary>
    public class DistrictDressing : MonoBehaviour
    {
        public static DistrictDressing Instance { get; private set; }

        private Transform root;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Build(string districtId)
        {
            Clear();
            var anchor = new GameObject("DistrictDressing");
            anchor.transform.SetParent(transform, false);
            root = anchor.transform;
            var pieces = DistrictLayout.For(districtId);
            for (int i = 0; i < pieces.Length; i++)
            {
                Spawn(pieces[i]);
            }
            KitStructure.Raise(districtId, root);
            RaiseCaravan();
            StreetDetail.RaiseStreet(districtId, root);
        }

        private void RaiseCaravan()
        {
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            bool post = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("TradingPost");
            if (string.IsNullOrEmpty(CaravanBook.Counterparty(day, post))) return;

            var stall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stall.name = "CaravanStall";
            stall.transform.SetParent(root, false);
            stall.transform.position = new Vector3(8f, 0.7f, 4f);
            stall.transform.localScale = new Vector3(1.8f, 1.2f, 0.8f);
            stall.layer = GameLayers.Interactable;
            var stallRenderer = stall.GetComponent<Renderer>();
            if (stallRenderer != null) stallRenderer.material.color = new Color(0.55f, 0.32f, 0.22f);
            stall.AddComponent<CampStation>().Configure(StationKind.Merchant);
            RaiseGuard(new Vector3(6.6f, 0.95f, 3.2f));
            RaiseGuard(new Vector3(9.4f, 0.95f, 3.2f));
        }

        private void RaiseGuard(Vector3 position)
        {
            var guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guard.name = "CaravanGuard";
            guard.transform.SetParent(root, false);
            guard.transform.position = position;
            guard.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            guard.layer = GameLayers.Environment;
            var renderer = guard.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.28f, 0.32f, 0.28f);
        }

        private void Spawn(DistrictLayout.Piece piece)
        {
            bool barrel = piece.Role.StartsWith("barrel");
            var body = GameObject.CreatePrimitive(barrel ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            body.name = "District_" + piece.Role;
            body.transform.SetParent(root, false);
            body.transform.position = new Vector3(piece.X, barrel ? 0.55f : 0.6f, piece.Z);
            body.transform.rotation = Quaternion.Euler(0f, piece.Yaw, 0f);
            body.transform.localScale = Scale(piece.Role);
            body.layer = GameLayers.Environment;
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = ColorFor(piece.Role);

            if (barrel)
            {
                var hazard = body.AddComponent<DestructibleHazard>();
                if (piece.Role.Contains("toxic")) hazard.Configure(HazardKind.Toxic);
                else if (piece.Role.Contains("oil")) hazard.Configure(HazardKind.Oil);
                else hazard.Configure(HazardKind.Explosive);
            }
            else if (piece.Role.StartsWith("crate"))
            {
                body.layer = GameLayers.Interactable;
                var container = body.AddComponent<LootContainer>();
                container.Configure(piece.Role == "crate_medical" ? "medical" : piece.Role == "crate_military" ? "military" : "crate");
            }
            else if (piece.Role == "lamp")
            {
                var lightObject = new GameObject("DistrictLamp");
                lightObject.transform.SetParent(body.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 9f;
                light.intensity = 1.4f;
                light.color = new Color(1f, 0.86f, 0.62f);
                lightObject.AddComponent<LightSource>().Configure(light.range);
            }
        }

        private static Vector3 Scale(string role)
        {
            if (role.StartsWith("barrel")) return new Vector3(0.55f, 0.55f, 0.55f);
            if (role == "stall") return new Vector3(1.6f, 1.1f, 0.7f);
            if (role.StartsWith("crate")) return new Vector3(0.9f, 0.7f, 0.9f);
            if (role == "lamp") return new Vector3(0.25f, 2.2f, 0.25f);
            return new Vector3(1.8f, 1.1f, 0.45f);
        }

        private static Color ColorFor(string role)
        {
            if (role.Contains("toxic")) return new Color(0.35f, 0.7f, 0.25f);
            if (role.Contains("oil")) return new Color(0.12f, 0.12f, 0.12f);
            if (role.Contains("explosive")) return new Color(0.7f, 0.18f, 0.12f);
            if (role == "crate_medical") return new Color(0.75f, 0.82f, 0.78f);
            if (role == "lamp") return new Color(0.35f, 0.32f, 0.28f);
            if (role == "stall") return new Color(0.42f, 0.28f, 0.18f);
            return new Color(0.4f, 0.36f, 0.3f);
        }

        private void Clear()
        {
            if (root == null) return;
            Destroy(root.gameObject);
            root = null;
        }
    }
}
