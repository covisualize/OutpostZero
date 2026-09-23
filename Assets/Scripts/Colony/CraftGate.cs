using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished bench stays at tier 1 until it is ordered and a builder puts in three hours.
    /// Printed recipes stay hidden until that plan is known, and tier-2 plans also wait on the raised bench.
    /// </summary>
    public static class CraftGate
    {
        public const int Hours = 3;
        public const int Done = 4;
        public const int UpgradeScrap = 16;
        public const int UpgradeCloth = 2;
        public const int UpgradeTape = 2;

        public static int TierOf(string recipe)
        {
            return RecipeTable.TryRow(recipe, out var row) ? row.Tier : CodeTier(recipe);
        }

        public static string PrintOf(string recipe)
        {
            return RecipeTable.TryRow(recipe, out var row) ? row.Print ?? "" : CodePrint(recipe);
        }

        public static int CodeTier(string recipe)
        {
            if (recipe == "antibiotics" || recipe == "flare" || recipe == "repair_kit" || recipe == "barricade_kit" || recipe == "radio_spare") return 2;
            return 1;
        }

        public static string CodePrint(string recipe)
        {
            if (recipe == "dressing") return "dressing";
            if (recipe == "flare") return "flare";
            if (recipe == "repair_kit") return "repair";
            if (recipe == "barricade_kit") return "wall";
            if (recipe == "radio_spare") return "radio";
            return "";
        }

        public static string Sheet(string district)
        {
            if (district == "rail_yard") return "repair";
            if (district == "police_station") return "wall";
            if (district == "mall") return "flare";
            if (district == "downtown_core") return "radio";
            return "";
        }

        public static int Shown(int tier)
        {
            return tier >= 2 ? 2 : 1;
        }

        public static string Deny(string recipe, int tier, string prints)
        {
            if (Shown(tier) < TierOf(recipe)) return "tier";
            string print = PrintOf(recipe);
            if (print.Length > 0 && !Holds(prints, print)) return "print";
            return "";
        }

        public static bool Open(string recipe, int tier, string prints)
        {
            return Deny(recipe, tier, prints).Length == 0;
        }

        public static bool Holds(string packed, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id) return true;
            }
            return false;
        }

        public static string Learn(string packed, string id)
        {
            if (string.IsNullOrEmpty(id)) return packed ?? "";
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id) return Join(list);
            }
            list.Add(id);
            list.Sort(StringComparer.Ordinal);
            return Join(list);
        }

        public static string[] Ids(string packed)
        {
            var list = Split(packed);
            return list.ToArray();
        }

        public static bool Ordered(int job)
        {
            return job > 0 && job < Done;
        }

        public static int Worked(int job)
        {
            if (job <= 1) return 0;
            int hours = job - 1;
            if (hours > Hours) return Hours;
            return hours;
        }

        public static void Advance(int job, int pace, out int next, out bool done)
        {
            next = job < 0 ? 0 : job;
            done = false;
            if (next <= 0 || next >= Done) return;
            if (pace <= 0) return;
            next += pace;
            if (next > Done) next = Done;
            done = next >= Done;
        }

        public static int MendGenerator(int integrity)
        {
            if (integrity <= 0) return 0;
            int next = integrity + 50;
            if (next > 100) return 100;
            return next;
        }

        public static int BraceWall(int integrity)
        {
            if (integrity <= 0) return 0;
            int next = integrity + 40;
            if (next > 100) return 100;
            return next;
        }

        public static int PickWorn(int[] integrity)
        {
            if (integrity == null) return -1;
            int best = -1;
            int lowest = 100;
            for (int i = 0; i < integrity.Length; i++)
            {
                if (integrity[i] <= 0 || integrity[i] >= 100) continue;
                if (integrity[i] >= lowest) continue;
                lowest = integrity[i];
                best = i;
            }
            return best;
        }

        private static List<string> Split(string packed)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(packed)) return list;
            var parts = packed.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0) continue;
                list.Add(part);
            }
            return list;
        }

        private static string Join(List<string> list)
        {
            return string.Join(",", list);
        }
    }
}
