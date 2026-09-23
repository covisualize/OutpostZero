using System;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Expedition
{
    [Serializable]
    public class KitAnchor
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class KitBox
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public float h;
        public float d;
    }

    [Serializable]
    public class KitPiece
    {
        public string id;
        public string category;
        public float w;
        public float h;
        public float d;
        public string surface;
        public float door;
        public float lod;
        public string[] variants;
        public KitBox[] colliders;
    }

    [Serializable]
    public class KitPlacement
    {
        public string id;
        public float x;
        public float y;
        public float z;
        public int yaw;
    }

    [Serializable]
    public class KitBook
    {
        public float grid;
        public float doorClear;
        public KitAnchor anchor;
        public KitPiece[] pieces;
        public KitPlacement[] storefront;
        public KitPlacement[] warehouse;
        public KitPlacement[] hospital;
        public KitPlacement[] apartment;
        public KitPlacement[] edge;
    }

    /// <summary>
    /// Pure kit queries shared by the street builder and the edit-mode tests.
    /// </summary>
    public static class KitPlan
    {
        public static KitPiece Find(KitPiece[] pieces, string id)
        {
            if (pieces == null) return null;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].id == id) return pieces[i];
            }
            return null;
        }

        public static bool UsesOnlyKnownPieces(KitPiece[] pieces, KitPlacement[] placements)
        {
            if (placements == null) return false;
            for (int i = 0; i < placements.Length; i++)
            {
                if (Find(pieces, placements[i].id) == null) return false;
            }
            return placements.Length > 0;
        }

        public static bool ReachesStorey(KitPlacement[] placements, float y)
        {
            if (placements == null) return false;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i].id == "floor" && Mathf.Abs(placements[i].y - y) < 0.01f) return true;
            }
            return false;
        }

        public static bool ClearAt(KitPiece piece, float x, float y, float z)
        {
            if (piece == null || piece.colliders == null) return true;
            for (int i = 0; i < piece.colliders.Length; i++)
            {
                var box = piece.colliders[i];
                if (x >= box.x && x <= box.x + box.w && y >= box.y && y <= box.y + box.h && z >= box.z && z <= box.z + box.d)
                    return false;
            }
            return true;
        }

        public static string RecipeName(string districtId)
        {
            if (districtId == "rail_yard" || districtId == "water_plant") return "warehouse";
            if (districtId == "old_hospital" || districtId == "police_station") return "hospital";
            if (districtId == "north_gate" || districtId == "downtown_core" || districtId == "highway_overpass") return "apartment";
            return "storefront";
        }

        public static KitPlacement[] Recipe(KitBook book, string districtId)
        {
            if (book == null) return Array.Empty<KitPlacement>();
            switch (RecipeName(districtId))
            {
                case "warehouse": return book.warehouse;
                case "hospital": return book.hospital;
                case "apartment": return book.apartment;
                default: return book.storefront;
            }
        }

        public static string Variant(string districtId)
        {
            if (districtId == "rail_yard" || districtId == "water_plant") return "concrete";
            if (districtId == "old_hospital" || districtId == "police_station") return "plaster";
            return "brick";
        }

        /// <summary>
        /// generate_kit.py lays catalog (x, y, z) at Blender (x, z, y); the FBX import flips x and z,
        /// so the mesh lines up with the catalog boxes after a half turn.
        /// </summary>
        public const float MeshYaw = 180f;

        public static string PrefabName(string id) => "Kit_" + id;

        /// <summary>
        /// Pieces with a breakable pane keep their box build so the glass can shatter on its own.
        /// </summary>
        public static bool UsesMesh(KitPiece piece)
        {
            if (piece == null || piece.colliders == null || piece.colliders.Length == 0) return false;
            for (int i = 0; i < piece.colliders.Length; i++)
            {
                var box = piece.colliders[i];
                if (PaneGlass.Opening(piece.id, box.y, box.h, box.w, piece.w)) return false;
            }
            return true;
        }

        public static Color Tint(KitPiece piece, string variant)
        {
            if (piece.id.StartsWith("wall") || piece.id == "corner" || piece.id == "parapet")
            {
                if (variant == "plaster") return new Color(0.62f, 0.58f, 0.5f);
                if (variant == "concrete") return new Color(0.48f, 0.46f, 0.42f);
                return new Color(0.45f, 0.28f, 0.22f);
            }
            if (piece.surface == "metal") return new Color(0.32f, 0.34f, 0.36f);
            if (piece.surface == "wood") return new Color(0.4f, 0.26f, 0.14f);
            if (piece.surface == "glass") return new Color(0.45f, 0.6f, 0.66f);
            return new Color(0.42f, 0.4f, 0.37f);
        }

        /// <summary>
        /// The colour generate_kit.py bakes into each surface's albedo.
        /// </summary>
        public static Color Albedo(string surface)
        {
            if (surface == "metal") return new Color(0.32f, 0.34f, 0.36f);
            if (surface == "wood") return new Color(0.4f, 0.26f, 0.14f);
            if (surface == "glass") return new Color(0.55f, 0.7f, 0.75f);
            return new Color(0.45f, 0.43f, 0.4f);
        }

        /// <summary>
        /// Multiplier over the baked albedo that lands a meshed piece on its district tint.
        /// </summary>
        public static Color Shade(KitPiece piece, string variant)
        {
            var want = Tint(piece, variant);
            var baked = Albedo(piece.surface);
            return new Color(Ratio(want.r, baked.r), Ratio(want.g, baked.g), Ratio(want.b, baked.b), 1f);
        }

        private static float Ratio(float want, float baked)
        {
            if (baked <= 0.001f) return 1f;
            float ratio = want / baked;
            return ratio < 0f ? 0f : ratio > 2f ? 2f : ratio;
        }

        public static SurfaceKind Surface(string surface)
        {
            if (surface == "metal") return SurfaceKind.Metal;
            if (surface == "wood") return SurfaceKind.Wood;
            if (surface == "glass") return SurfaceKind.Glass;
            return SurfaceKind.Concrete;
        }

        public static float Snap(float value, float grid)
        {
            if (grid <= 0f) return value;
            return Mathf.Round(value / grid) * grid;
        }

        public static int SnapYaw(float yaw)
        {
            int quarter = Mathf.RoundToInt(yaw / 90f) % 4;
            if (quarter < 0) quarter += 4;
            return quarter * 90;
        }

        public static KitPlacement Place(string id, Vector3 local, float yaw, float grid, float storey)
        {
            return new KitPlacement
            {
                id = id,
                x = Snap(local.x, grid),
                y = Snap(local.y, storey),
                z = Snap(local.z, grid),
                yaw = SnapYaw(yaw)
            };
        }
    }

    /// <summary>
    /// Raises a district building, its interior, and the map-edge fence from the kit.
    /// </summary>
    public class KitStructure : MonoBehaviour
    {
        public static void Raise(string districtId, Transform parent)
        {
            var text = Resources.Load<TextAsset>("KitCatalog");
            if (text == null || parent == null) return;
            var book = JsonUtility.FromJson<KitBook>(text.text);
            if (book == null || book.anchor == null) return;
            var anchor = new Vector3(book.anchor.x, book.anchor.y, book.anchor.z);
            var shell = new GameObject("KitShell");
            shell.transform.SetParent(parent, false);
            string variant = KitPlan.Variant(districtId);
            var floors = Spawn(book, KitPlan.Recipe(book, districtId), anchor, shell.transform, variant);
            Spawn(book, book.edge, Vector3.zero, shell.transform, variant);
            var cutaway = shell.AddComponent<KitCutaway>();
            cutaway.Configure(floors);
        }

        private static Bounds Spawn(KitBook book, KitPlacement[] placements, Vector3 anchor, Transform parent, string variant)
        {
            var bounds = new Bounds(anchor, Vector3.zero);
            bool started = false;
            if (placements == null) return bounds;
            for (int i = 0; i < placements.Length; i++)
            {
                var placement = placements[i];
                var piece = KitPlan.Find(book.pieces, placement.id);
                if (piece == null || piece.colliders == null) continue;
                var origin = anchor + new Vector3(placement.x, placement.y, placement.z);
                var host = new GameObject("Kit_" + piece.id);
                host.transform.SetParent(parent, false);
                host.transform.position = origin;
                host.transform.rotation = Quaternion.Euler(0f, placement.yaw, 0f);
                bool cap = piece.id == "roof" || piece.id == "ceiling" || (piece.id == "floor" && placement.y > 0.5f);
                if (cap) host.AddComponent<KitCap>().slabY = origin.y;
                var tint = KitPlan.Tint(piece, variant);
                var kind = KitPlan.Surface(piece.surface);
                var mesh = Meshes != null && KitPlan.UsesMesh(piece) ? Meshes.Find(piece.id) : null;
                if (mesh != null) Dress(mesh, host.transform, KitPlan.Shade(piece, variant));
                for (int c = 0; c < piece.colliders.Length; c++)
                {
                    var box = piece.colliders[c];
                    if (mesh != null)
                    {
                        var solid = new GameObject("KitSolid");
                        solid.transform.SetParent(host.transform, false);
                        solid.transform.localPosition = new Vector3(box.x + box.w * 0.5f, box.y + box.h * 0.5f, box.z + box.d * 0.5f);
                        solid.layer = GameLayers.Environment;
                        solid.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(0.02f, box.w), Mathf.Max(0.02f, box.h), Mathf.Max(0.02f, box.d));
                        solid.AddComponent<SurfaceTag>().Set(kind);
                        continue;
                    }
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = "KitBlock";
                    block.transform.SetParent(host.transform, false);
                    block.transform.localPosition = new Vector3(box.x + box.w * 0.5f, box.y + box.h * 0.5f, box.z + box.d * 0.5f);
                    block.transform.localScale = new Vector3(Mathf.Max(0.02f, box.w), Mathf.Max(0.02f, box.h), Mathf.Max(0.02f, box.d));
                    block.layer = GameLayers.Environment;
                    bool pane = PaneGlass.Opening(piece.id, box.y, box.h, box.w, piece.w);
                    var renderer = block.GetComponent<Renderer>();
                    if (pane)
                    {
                        block.name = PaneGlass.Name;
                        block.AddComponent<GlassPane>();
                        if (!PaneGlass.Coat(renderer)) Paint(renderer, PaneGlass.Tint);
                    }
                    else
                    {
                        Paint(renderer, tint);
                        block.AddComponent<SurfaceTag>().Set(kind);
                    }
                }
                if (piece.id == "ceiling_light")
                {
                    var lamp = new GameObject("KitLamp");
                    lamp.transform.SetParent(host.transform, false);
                    lamp.transform.localPosition = new Vector3(piece.w * 0.5f, -0.2f, piece.d * 0.5f);
                    var light = lamp.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 6f;
                    light.intensity = 1.1f;
                    light.color = new Color(1f, 0.9f, 0.7f);
                }
                if (piece.id == "floor" && placement.y < 0.5f)
                {
                    var floorBounds = new Bounds(origin + new Vector3(piece.w * 0.5f, 0f, piece.d * 0.5f), new Vector3(piece.w, 1f, piece.d));
                    if (!started)
                    {
                        bounds = floorBounds;
                        started = true;
                    }
                    else bounds.Encapsulate(floorBounds);
                }
            }
            return bounds;
        }

        private static KitPrefabSet meshes;
        private static bool meshesLoaded;

        private static KitPrefabSet Meshes
        {
            get
            {
                if (!meshesLoaded)
                {
                    meshesLoaded = true;
                    meshes = Resources.Load<KitPrefabSet>(KitPrefabSet.ResourcePath);
                }
                return meshes;
            }
        }

        private static void Dress(GameObject prefab, Transform host, Color shade)
        {
            var visual = Instantiate(prefab, host, false);
            visual.name = "KitMesh";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, KitPlan.MeshYaw, 0f);
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Destroy(collider);
            }
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = GameLayers.Environment;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Tint", shade);
                renderer.SetPropertyBlock(block);
            }
        }

        private static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    public class KitCap : MonoBehaviour
    {
        public float slabY;
    }

    public class KitCutaway : MonoBehaviour
    {
        private Bounds footprint;
        private KitCap[] caps;

        public void Configure(Bounds floors)
        {
            footprint = floors;
            caps = GetComponentsInChildren<KitCap>(true);
        }

        private void LateUpdate()
        {
            var player = PlayerRegistry.Current;
            if (player == null || caps == null) return;
            var position = player.transform.position;
            bool inside = position.x >= footprint.min.x && position.x <= footprint.max.x
                && position.z >= footprint.min.z && position.z <= footprint.max.z;
            for (int i = 0; i < caps.Length; i++)
            {
                bool show = !inside || caps[i].slabY <= position.y + 0.6f;
                var renderers = caps[i].GetComponentsInChildren<Renderer>();
                for (int r = 0; r < renderers.Length; r++) renderers[r].enabled = show;
            }
        }
    }
}
