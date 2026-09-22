using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Ten district nodes, the roads between them, and the radio-tower ending.
    /// The first four ids stay in their old order so a schema-1 save still clears the same streets.
    /// </summary>
    public static class CampaignBoard
    {
        public struct Node
        {
            public string Id;
            public string Name;
            public string Encounter;
            public int Tier;
            public string Part;
            public string Neighbors;
        }

        public static Node[] All()
        {
            return new[]
            {
                NodeAt("ash_market", "Ash Market", "Loot the stalls, watch the alleys", 1, "", "commercial_strip,rail_yard"),
                NodeAt("rail_yard", "Rail Yard", "Barrels and a brute pack", 2, "", "ash_market,police_station,water_plant"),
                NodeAt("old_hospital", "Old Hospital", "Medical caches, infected wards", 2, "hospital", "commercial_strip,police_station"),
                NodeAt("north_gate", "North Gate", "The ring road north of the plant", 3, "", "water_plant,highway_overpass"),
                NodeAt("commercial_strip", "Commercial Strip", "Shops still have cans in the back", 1, "", "ash_market,old_hospital,mall"),
                NodeAt("police_station", "Police Station", "The armoury is on the second floor", 2, "police", "rail_yard,old_hospital,highway_overpass"),
                NodeAt("water_plant", "Water Plant", "Tanks, catwalks, and a long fence", 2, "", "rail_yard,north_gate"),
                NodeAt("mall", "Mall", "The atrium is dark and full of echoes", 3, "", "commercial_strip,downtown_core"),
                NodeAt("highway_overpass", "Highway Overpass", "Cars stacked under the span", 3, "", "police_station,downtown_core,north_gate"),
                NodeAt("downtown_core", "Downtown Core", "The tower site is past the plaza", 4, "downtown", "mall,highway_overpass")
            };
        }

        public static int Tier(string id)
        {
            var node = Find(id);
            return node.Tier <= 0 ? 1 : node.Tier;
        }

        public static float TravelHours(string id)
        {
            return Tier(id) * 2f;
        }

        public static string PartFor(string id)
        {
            return Find(id).Part ?? "";
        }

        public static bool Reachable(string id, string[] cleared)
        {
            if (id == "ash_market") return true;
            var node = Find(id);
            if (string.IsNullOrEmpty(node.Id)) return false;
            var neighbors = Split(node.Neighbors);
            if (cleared == null) return false;
            for (int i = 0; i < neighbors.Count; i++)
            {
                for (int c = 0; c < cleared.Length; c++)
                {
                    if (cleared[c] == neighbors[i]) return true;
                }
            }
            return false;
        }

        public static string AddPart(string packed, string part)
        {
            if (string.IsNullOrEmpty(part)) return packed ?? "";
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == part) return Join(list);
            }
            list.Add(part);
            list.Sort(StringComparer.Ordinal);
            return Join(list);
        }

        public static bool HasPart(string packed, string part)
        {
            if (string.IsNullOrEmpty(part)) return false;
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == part) return true;
            }
            return false;
        }

        public static int PartCount(string packed)
        {
            return Split(packed).Count;
        }

        public static bool PartsComplete(string packed)
        {
            return HasPart(packed, "hospital") && HasPart(packed, "police") && HasPart(packed, "downtown");
        }

        public static bool Ready(string parts, bool generator, bool broadcastWon)
        {
            return generator && !broadcastWon && PartsComplete(parts);
        }

        public static bool Won(string parts, bool generator, bool broadcastWon)
        {
            return generator && broadcastWon && PartsComplete(parts);
        }

        private static Node Find(string id)
        {
            var all = All();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == id) return all[i];
            }
            return new Node();
        }

        private static Node NodeAt(string id, string name, string encounter, int tier, string part, string neighbors)
        {
            return new Node { Id = id, Name = name, Encounter = encounter, Tier = tier, Part = part, Neighbors = neighbors };
        }

        private static List<string> Split(string packed)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(packed)) return list;
            var bits = packed.Split(',');
            for (int i = 0; i < bits.Length; i++)
            {
                if (!string.IsNullOrEmpty(bits[i])) list.Add(bits[i]);
            }
            return list;
        }

        private static string Join(List<string> list)
        {
            if (list.Count == 0) return "";
            var text = list[0];
            for (int i = 1; i < list.Count; i++) text += "," + list[i];
            return text;
        }
    }
}
