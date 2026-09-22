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
                var tint = Tint(piece, variant);
                for (int c = 0; c < piece.colliders.Length; c++)
                {
                    var box = piece.colliders[c];
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = "KitBlock";
                    block.transform.SetParent(host.transform, false);
                    block.transform.localPosition = new Vector3(box.x + box.w * 0.5f, box.y + box.h * 0.5f, box.z + box.d * 0.5f);
                    block.transform.localScale = new Vector3(Mathf.Max(0.02f, box.w), Mathf.Max(0.02f, box.h), Mathf.Max(0.02f, box.d));
                    block.layer = GameLayers.Environment;
                    Paint(block.GetComponent<Renderer>(), tint);
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

        private static Color Tint(KitPiece piece, string variant)
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
