using System;
using System.Collections.Generic;
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

        public const float LotTile = 2f;
        public const float Storey = 3f;

        /// <summary>Kit tiles a block of road-graph lots holds along one side: 1 for a single cell, 3 for two cells.</summary>
        public static int Tiles(int cells) => cells >= 2 ? 3 : 1;

        /// <summary>How a footprint's buildings are dressed: height, the front door, the wall fill and what stands inside.</summary>
        public sealed class Template
        {
            public string Name;
            public int Storeys;
            public int TallStoreys;
            public string Door;
            public bool Garage;
            public string[] Fill;
            public string[] Props;
            public bool Parapet;
        }

        private static readonly Template[] Templates =
        {
            new Template { Name = "storefront", Storeys = 0, TallStoreys = 2, Door = "wall_door", Fill = new[] { "wall_window", "wall_window_broken", "wall_boarded" }, Props = new[] { "shelf", "counter", "shelf" } },
            new Template { Name = "warehouse", Storeys = 1, TallStoreys = 1, Door = "wall_door", Garage = true, Fill = new[] { "wall_plain", "wall_boarded" }, Props = new[] { "pallet", "pallet", "workbench" } },
            new Template { Name = "apartment", Storeys = 2, TallStoreys = 3, Door = "wall_door", Fill = new[] { "wall_window", "wall_window_broken" }, Props = new[] { "chair", "desk", "fridge" } },
            new Template { Name = "clinic", Storeys = 0, TallStoreys = 2, Door = "wall_double_door", Fill = new[] { "wall_window", "wall_plain" }, Props = new[] { "hospital_bed", "filing", "hospital_bed" } },
            new Template { Name = "station", Storeys = 2, TallStoreys = 2, Door = "wall_double_door", Fill = new[] { "wall_window_broken", "wall_boarded" }, Props = new[] { "desk", "filing", "lockers" }, Parapet = true },
            new Template { Name = "hospital", Storeys = 2, TallStoreys = 2, Door = "wall_double_door", Fill = new[] { "wall_window", "wall_boarded" }, Props = new[] { "lockers", "desk", "chair" }, Parapet = true }
        };

        public static Template TemplateFor(string footprint)
        {
            for (int i = 0; i < Templates.Length; i++)
            {
                if (Templates[i].Name == footprint) return Templates[i];
            }
            return Templates[0];
        }

        private struct Slot
        {
            public string Face;
            public int Index;
            public int Count;
        }

        /// <summary>
        /// A kit building on a block of road-graph lots, local to the south-west cell's tile corner (cell centre
        /// less one metre each way). The floor spans <see cref="Tiles"/> of cols by rows, the door opens on
        /// <paramref name="front"/>, and walls stay within 0.2 m of the floor.
        /// </summary>
        public static KitPlacement[] Building(int seed, float x, float z, int cols, int rows, string footprint, string front)
        {
            var template = TemplateFor(footprint);
            uint hash = LotHash(seed, x, z);
            int wide = Tiles(cols);
            int deep = Tiles(rows);
            bool large = wide * deep > 1;
            int storeys = large ? template.TallStoreys : template.Storeys > 0 ? template.Storeys : 1 + (int)(hash & 1u);
            if (string.IsNullOrEmpty(front)) front = "south";
            bool climbs = wide >= 3 && deep >= 3 && storeys > 1;
            int wellA = -1, wellB = -1;
            KitPlacement flight = null;
            if (climbs) flight = Flight(front, out wellA, out wellB);
            else if (storeys > 1) flight = Ladder(front, wide, deep);
            var list = new List<KitPlacement>();
            for (int s = 0; s < storeys; s++)
            {
                float y = s * Storey;
                for (int i = 0; i < wide; i++)
                {
                    for (int j = 0; j < deep; j++)
                    {
                        int tile = i * deep + j;
                        if (s > 0 && (tile == wellA || tile == wellB)) continue;
                        list.Add(At("floor", i * LotTile, y, j * LotTile, 0));
                    }
                }
                if (flight != null && s < storeys - 1) list.Add(At(flight.id, flight.x, y, flight.z, flight.yaw));
                foreach (var face in new[] { "south", "north", "west", "east" })
                {
                    int length = face == "south" || face == "north" ? wide : deep;
                    foreach (var slot in Slots(face, length, s == 0 && face == front && template.Garage))
                    {
                        string id = WallFor(template, hash, s, face == front, slot, length);
                        list.Add(Wall(id, face, slot.Index, y, wide, deep));
                    }
                }
            }
            float top = storeys * Storey;
            for (int i = 0; i < wide; i++)
                for (int j = 0; j < deep; j++) list.Add(At("roof", i * LotTile, top, j * LotTile, 0));
            if (template.Parapet || ((hash >> 8) & 1u) == 1u)
            {
                int length = front == "south" || front == "north" ? wide : deep;
                for (int i = 0; i < length; i++) list.Add(Wall("parapet", front, i, top, wide, deep));
            }
            if (large) Furnish(list, template, hash, front, wide, deep, flight);
            return list.ToArray();
        }

        /// <summary>
        /// The flight up a 3 by 3 tile building: always in an east or west column, since the south and north walls
        /// stand 0.2 m inside the floor and a 2 m flight only clears them running north-south. It climbs away from a
        /// south or north door, and sits in the column across from a west or east door. Two tiles of each slab above it
        /// are left open (tile index = column * 3 + row) for head room, and the top step lands sideways on the tile beside it.
        /// </summary>
        private static KitPlacement Flight(string front, out int wellA, out int wellB)
        {
            const float run = 4.08f;
            float far = 3f * LotTile - 0.2f;
            switch (front)
            {
                case "north":
                    wellA = 2 * 3 + 0; wellB = 2 * 3 + 1;
                    return At("stairs", 3f * LotTile, 0f, 0.2f + run, 180);
                case "east":
                    wellA = 0 * 3 + 1; wellB = 0 * 3 + 2;
                    return At("stairs", 0f, 0f, far - run, 0);
                default:
                    wellA = 2 * 3 + 1; wellB = 2 * 3 + 2;
                    return At("stairs", 2f * LotTile, 0f, far - run, 0);
            }
        }

        /// <summary>
        /// The ladder up a building too small for a flight: against the wall across from the door, at the end that
        /// leaves room beside it (the ladder's local +x) for the spot a climber steps off onto the floor above.
        /// </summary>
        private static KitPlacement Ladder(string front, int wide, int deep)
        {
            float w = wide * LotTile, d = deep * LotTile;
            switch (front)
            {
                case "north": return At("ladder", w - 0.05f, 0f, 0.2f + LadderDepth, 180);
                case "west": return At("ladder", w - LadderDepth, 0f, d - 0.25f, 90);
                case "east": return At("ladder", LadderDepth, 0f, 0.25f, 270);
                default: return At("ladder", 0.05f, 0f, d - 0.2f - LadderDepth, 0);
            }
        }

        public const float LadderWidth = 0.6f;
        public const float LadderDepth = 0.12f;

        /// <summary>Where a climber stands to go up, in the ladder's own frame (the room is on its -z side).</summary>
        public static readonly Vector3 LadderFoot = new Vector3(0.45f, 0f, -0.5f);

        /// <summary>
        /// Where a climber steps off on the floor above, beside the ladder's top, in the ladder's own frame. Both spots
        /// keep a 0.4 m capsule clear of the walls and the ladder even in a one-cell room.
        /// </summary>
        public static readonly Vector3 LadderTop = new Vector3(1.05f, Storey, -0.5f);

        /// <summary>A point in a placed piece's frame, local to the building.</summary>
        public static Vector3 PointOf(KitPlacement placement, Vector3 local)
        {
            float rad = placement.yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector3(placement.x + local.x * cos + local.z * sin, placement.y + local.y, placement.z - local.x * sin + local.z * cos);
        }

        /// <summary>Floor a flight or a ladder keeps clear: the steps, or the ladder with both spots a climber stands on.</summary>
        public static void ClimbArea(KitPlacement climb, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            if (climb.id != "ladder")
            {
                FlightArea(climb, out minX, out maxX, out minZ, out maxZ);
                return;
            }
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;
            foreach (var corner in new[] { Vector3.zero, new Vector3(LadderWidth, 0f, 0f), new Vector3(0f, 0f, LadderDepth), new Vector3(LadderWidth, 0f, LadderDepth), LadderFoot, LadderTop })
            {
                var p = PointOf(climb, corner);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
        }

        /// <summary>Floor area a flight covers, local to the building: x from minX to maxX, z from minZ to maxZ.</summary>
        public static void FlightArea(KitPlacement flight, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            const float run = 4.08f;
            switch (flight.yaw)
            {
                case 180: minX = flight.x - LotTile; maxX = flight.x; minZ = flight.z - run; maxZ = flight.z; return;
                default: minX = flight.x; maxX = flight.x + LotTile; minZ = flight.z; maxZ = flight.z + run; return;
            }
        }

        private static List<Slot> Slots(string face, int length, bool garage)
        {
            var slots = new List<Slot>();
            int i = 0;
            if (garage && length >= 3)
            {
                slots.Add(new Slot { Face = face, Index = 0, Count = 2 });
                i = 2;
            }
            for (; i < length; i++) slots.Add(new Slot { Face = face, Index = i, Count = 1 });
            return slots;
        }

        private static string WallFor(Template template, uint hash, int storey, bool front, Slot slot, int length)
        {
            if (slot.Count == 2) return "wall_garage";
            uint roll = (hash >> ((slot.Index * 3 + storey * 5 + slot.Face.Length) % 24)) & 7u;
            if (front && storey == 0)
            {
                int door = template.Garage && length >= 3 ? length - 1 : length / 2;
                if (slot.Index == door) return template.Door;
                return template.Fill[roll % (uint)template.Fill.Length];
            }
            if (front) return (roll & 1u) == 0u ? "wall_window" : "wall_window_broken";
            if (roll < 4u) return "wall_plain";
            return roll < 6u ? "wall_window" : "wall_boarded";
        }

        /// <summary>A wall piece in slot <paramref name="index"/> of a face, placed so it covers that stretch from outside the floor.</summary>
        private static KitPlacement Wall(string id, string face, int index, float y, int wide, int deep)
        {
            float along = index * LotTile;
            float span = id == "wall_garage" ? 2f * LotTile : LotTile;
            switch (face)
            {
                case "north": return At(id, along, y, deep * LotTile - 0.2f, 0);
                case "west": return At(id, 0f, y, along, 270);
                case "east": return At(id, wide * LotTile, y, along + span, 90);
                default: return At(id, along, y, 0f, 0);
            }
        }

        /// <summary>Props stand against the wall across from the door, clear of the walk in.</summary>
        private static void Furnish(List<KitPlacement> list, Template template, uint hash, string front, int wide, int deep, KitPlacement flight)
        {
            float fx0 = 0f, fx1 = 0f, fz0 = 0f, fz1 = 0f;
            if (flight != null) ClimbArea(flight, out fx0, out fx1, out fz0, out fz1);
            string back = front == "south" ? "north" : front == "north" ? "south" : front == "west" ? "east" : "west";
            bool across = back == "north" || back == "south";
            int length = across ? wide : deep;
            float reach = (across ? deep : wide) * LotTile - 1.5f;
            for (int i = 0; i < length; i++)
            {
                if (((hash >> (12 + i)) & 3u) == 0u) continue;
                string id = template.Props[i % template.Props.Length];
                PropSize(id, out float w, out float d);
                if (d > reach || (!across && w > LotTile - 0.4f)) continue;
                float slot = i * LotTile + (LotTile - w) * 0.5f;
                KitPlacement prop;
                float px0, px1, pz0, pz1;
                switch (back)
                {
                    case "north":
                        prop = At(id, slot, 0f, deep * LotTile - 0.3f - d, 0);
                        px0 = slot; px1 = slot + w; pz0 = prop.z; pz1 = prop.z + d;
                        break;
                    case "south":
                        prop = At(id, slot, 0f, 0.3f, 0);
                        px0 = slot; px1 = slot + w; pz0 = 0.3f; pz1 = 0.3f + d;
                        break;
                    case "east":
                        prop = At(id, wide * LotTile - 0.1f - d, 0f, slot + w, 90);
                        px0 = prop.x; px1 = prop.x + d; pz0 = slot; pz1 = slot + w;
                        break;
                    default:
                        prop = At(id, 0.1f, 0f, slot + w, 90);
                        px0 = 0.1f; px1 = 0.1f + d; pz0 = slot; pz1 = slot + w;
                        break;
                }
                if (flight != null && px0 < fx1 + 0.6f && px1 > fx0 - 0.6f && pz0 < fz1 + 0.6f && pz1 > fz0 - 0.6f) continue;
                list.Add(prop);
            }
        }

        public static void PropSize(string id, out float w, out float d)
        {
            switch (id)
            {
                case "shelf": w = 0.9f; d = 0.4f; return;
                case "counter": w = 1.6f; d = 0.6f; return;
                case "fridge": w = 0.7f; d = 0.7f; return;
                case "pallet": w = 1.2f; d = 1.0f; return;
                case "desk": w = 1.4f; d = 0.7f; return;
                case "chair": w = 0.5f; d = 0.5f; return;
                case "filing": w = 0.5f; d = 0.6f; return;
                case "hospital_bed": w = 2.0f; d = 0.9f; return;
                case "lockers": w = 1.5f; d = 0.5f; return;
                case "workbench": w = 1.6f; d = 0.7f; return;
                default: w = 1f; d = 1f; return;
            }
        }

        private static KitPlacement At(string id, float x, float y, float z, int yaw)
        {
            return new KitPlacement { id = id, x = x, y = y, z = z, yaw = yaw };
        }

        private static uint LotHash(int seed, float x, float z)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= (uint)Mathf.RoundToInt(x * 4f) * 2246822519u;
                h ^= (uint)Mathf.RoundToInt(z * 4f) * 3266489917u;
                h ^= h >> 15;
                h *= 668265263u;
                h ^= h >> 13;
                return h;
            }
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
        private static KitBook catalog;
        private static bool catalogLoaded;

        private static KitBook Book
        {
            get
            {
                if (!catalogLoaded)
                {
                    catalogLoaded = true;
                    var text = Resources.Load<TextAsset>("KitCatalog");
                    catalog = text != null ? JsonUtility.FromJson<KitBook>(text.text) : null;
                }
                return catalog;
            }
        }

        /// <summary>Raises a road-graph lot building with its roof cutaway; false when the kit catalog is missing so the caller can fall back.</summary>
        public static bool RaiseLot(KitPlacement[] placements, Vector3 corner, Transform parent, string variant, string name)
        {
            var kit = Book;
            if (kit == null || kit.pieces == null || parent == null || placements == null) return false;
            var shell = new GameObject(name);
            shell.transform.SetParent(parent, false);
            var floors = Spawn(kit, placements, corner, shell.transform, variant);
            if (shell.transform.childCount > 0)
            {
                shell.AddComponent<KitCutaway>().Configure(floors);
                return true;
            }
            Destroy(shell);
            return false;
        }

        public static void Raise(string districtId, Transform parent)
        {
            if (parent == null) return;
            var book = Book;
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
                        if (!OutpostZero.Graphics.MaterialLibrary.Dress(renderer, KitFamily(piece.id, kind), OutpostZero.Graphics.MaterialLibrary.TintFor(tint))) Paint(renderer, tint);
                        block.AddComponent<SurfaceTag>().Set(kind);
                    }
                }
                if (piece.id == "ladder") host.AddComponent<KitLadder>();
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

        /// <summary>Library family for a kit box: the piece id's words first, then its surface tag.</summary>
        public static OutpostZero.Graphics.SurfaceFamily KitFamily(string pieceId, SurfaceKind kind)
        {
            var family = OutpostZero.Graphics.MaterialLibrary.FamilyFor(pieceId);
            if (family == OutpostZero.Graphics.SurfaceFamily.None) family = OutpostZero.Graphics.MaterialLibrary.FromSurface(kind);
            return family != OutpostZero.Graphics.SurfaceFamily.None ? family : OutpostZero.Graphics.SurfaceFamily.ConcreteCracked;
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
